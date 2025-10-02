using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using TN_Doc.Models.Status;
using TN_DocGeneral.Services;

namespace TN_Doc.Services;

/// <summary>
/// Сервис для проверки статуса устройств и внешних сервисов
/// Интегрирован с существующим IAppConfigService TN_Doc
/// </summary>
public class StatusProvider : IStatusProvider
{
    private readonly IAppConfigService _appConfigService;
    private readonly ILogger<StatusProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public StatusProvider(
        IAppConfigService appConfigService,
        ILogger<StatusProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<StatusResponse> GetStatusAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting status check for all devices and services");

        try
        {
            var appConfig = _appConfigService.GetAppCfg();

            if (appConfig == null)
            {
                _logger.LogError("Failed to retrieve application configuration");
                return new StatusResponse();
            }

            _logger.LogDebug("Retrieved app configuration with {DeviceCount} devices",
                appConfig.Devices?.Count ?? 0);

            var devices = new List<DeviceStatus>();
            var healthyDevices = 0;

            // Проверка устройств (БД)
            if (appConfig.Devices?.Any() == true)
            {
                var deviceTasks = appConfig.Devices
                    .Where(d => d.Use)
                    .Select(device => CheckDeviceAsync(device, ct));

                var deviceResults = await Task.WhenAll(deviceTasks);
                devices.AddRange(deviceResults);

                healthyDevices = devices.Count(d => d.IsConnected);

                _logger.LogInformation(
                    "Device status check completed: {HealthyCount}/{TotalCount} devices healthy",
                    healthyDevices, devices.Count);
            }

            // Проверка сервисов
            var services = await CheckServicesAsync(appConfig, ct);

            var response = new StatusResponse
            {
                Devices = devices,
                Services = services,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            _logger.LogInformation(
                "Status check completed in {ElapsedMs}ms - Devices: {HealthyDevices}/{TotalDevices}, MS: {MsStatus}",
                stopwatch.ElapsedMilliseconds,
                healthyDevices,
                devices.Count,
                services.MessagingService?.IsConnected ?? false);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve status information after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<DeviceStatus> CheckDeviceAsync(TN.DocData.Device device, CancellationToken ct)
    {
        var deviceStopwatch = Stopwatch.StartNew();
        var status = new DeviceStatus
        {
            Id = device.IdDevice.ToString(),
            Name = device.Name ?? "Unknown",
            Type = "database",
            IsConnected = false
        };

        _logger.LogDebug("Checking device {DeviceName} (ID: {DeviceId})", device.Name, device.IdDevice);

        try
        {
            // Получаем строку подключения из первого активного подключения
            var connectionString = device.DBConnectionStrings?
                .FirstOrDefault(cs => cs.Use);

            if (connectionString == null)
            {
                throw new InvalidOperationException($"No active connection string for device {device.Name}");
            }

            // Формируем строку подключения MySQL
            var csb = new MySqlConnectionStringBuilder
            {
                Server = connectionString.Server,
                UserID = connectionString.Userid,
                Password = connectionString.Password,
                Database = connectionString.Database,
                ConnectionTimeout = 2,
                DefaultCommandTimeout = 2
            };

            using var connection = new MySqlConnection(csb.ConnectionString);
            await connection.OpenAsync(ct);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 2;
            await command.ExecuteScalarAsync(ct);

            status.IsConnected = true;
            status.LatencyMs = (int)deviceStopwatch.ElapsedMilliseconds;

            _logger.LogDebug("Device {DeviceName} connection successful in {LatencyMs}ms",
                device.Name, status.LatencyMs);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Device {DeviceName} connection check was cancelled", device.Name);
            status.Error = "Operation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Device {DeviceName} connection check failed in {ElapsedMs}ms: {ErrorMessage}",
                device.Name, deviceStopwatch.ElapsedMilliseconds, ex.Message);
            status.Error = ex.Message;
        }
        finally
        {
            status.LastChecked = DateTime.Now;
        }

        return status;
    }

    private async Task<ServiceStatus> CheckServicesAsync(TN.DocData.CfgApp appConfig, CancellationToken ct)
    {
        var services = new ServiceStatus();

        // Проверка Messaging Service
        services.MessagingService = await CheckMessagingServiceAsync(ct);

        // Проверка ELIS если настроен
        if (appConfig.Elis?.Use == true)
        {
            services.Elis = await CheckElisServiceAsync(appConfig.Elis, ct);
        }

        // TODO: Добавить проверку OPC DA/UA через TN_MessagingService
        // services.OpcDa = await CheckOpcDaAsync(ct);
        // services.OpcUa = await CheckOpcUaAsync(ct);

        return services;
    }

    private async Task<ConnectionStatus> CheckMessagingServiceAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new ConnectionStatus { IsConnected = false };

        try
        {
            using var client = _httpClientFactory.CreateClient("MessagingService");
            var response = await client.GetAsync("/health", ct);

            status.IsConnected = response.IsSuccessStatusCode;
            status.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
            status.LastChecked = DateTime.Now;

            _logger.LogDebug("Messaging Service check: {Status} in {LatencyMs}ms",
                status.IsConnected ? "Connected" : "Disconnected", status.LatencyMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Messaging Service check failed: {ErrorMessage}", ex.Message);
            status.Error = ex.Message;
            status.LastChecked = DateTime.Now;
        }

        return status;
    }

    private Task<ConnectionStatus> CheckElisServiceAsync(TN.DocData.Elis elisConfig, CancellationToken ct)
    {
        var status = new ConnectionStatus { IsConnected = false, LastChecked = DateTime.Now };

        try
        {
            // ELIS проверка через TN.ElisConnector (отдельный сервис)
            // Пока просто проверяем, что конфигурация заполнена
            if (!string.IsNullOrEmpty(elisConfig.ClientName) &&
                !string.IsNullOrEmpty(elisConfig.OstKey))
            {
                // Считаем ELIS настроенным, если есть ключевые параметры
                status.IsConnected = true;
                _logger.LogDebug("ELIS configuration is present");
            }
            else
            {
                status.Error = "ELIS configuration incomplete";
                _logger.LogDebug("ELIS configuration is incomplete");
            }

            // TODO: Реализовать реальную проверку через TN.ElisConnector
            // когда будет известен endpoint для health check
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ELIS configuration check failed: {ErrorMessage}", ex.Message);
            status.Error = ex.Message;
        }

        return Task.FromResult(status);
    }
}