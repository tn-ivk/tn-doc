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
using TN_DocGeneral.Extensions;
using TN.DocData;

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
    private readonly ConnectionTracker _connectionTracker;

    public StatusProvider(IAppConfigService appConfigService, ILogger<StatusProvider> logger, IHttpClientFactory httpClientFactory, ConnectionTracker connectionTracker)
    {
        _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _connectionTracker = connectionTracker ?? throw new ArgumentNullException(nameof(connectionTracker));
    }

    /// <summary>
    /// Получает текущий статус всех устройств и сервисов
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Объект StatusResponse со статусом устройств и сервисов</returns>
    public async Task<StatusResponse> GetStatusAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Начинается проверка статуса всех устройств и сервисов");

        try
        {
            var appConfig = _appConfigService.GetAppCfg();
            if (appConfig == null)
            {
                _logger.LogError("Не удалось получить конфигурацию приложения");
                return new StatusResponse();
            }

            _logger.LogDebug("Получена конфигурация с {DeviceCount} устройствами", appConfig.Devices?.Count ?? 0);

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
                    "Проверка устройств завершена: {HealthyCount}/{TotalCount} устройств доступны",
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

            _logger.LogDebug(
                "Проверка статуса завершена за {ElapsedMs}мс - Устройства: {HealthyDevices}/{TotalDevices}, MS: {MsStatus}",
                stopwatch.ElapsedMilliseconds,
                healthyDevices,
                devices.Count,
                services.MessagingService?.IsConnected ?? false);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось получить информацию о статусе за {ElapsedMs}мс",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Проверяет доступность устройства через подключение к его базе данных
    /// </summary>
    /// <param name="device">Устройство для проверки</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Статус устройства с информацией о подключении и задержке</returns>
    private async Task<DeviceStatus> CheckDeviceAsync(Device device, CancellationToken ct)
    {
        var deviceStopwatch = Stopwatch.StartNew();
        var status = new DeviceStatus
        {
            Id = device.IdDevice.ToString(),
            Name = device.Name ?? "Unknown",
            Type = "database",
            IsConnected = false
        };

        _logger.LogDebug("Проверка устройства {DeviceName} (ID: {DeviceId})", device.Name, device.IdDevice);

        try
        {
            // Получаем строку подключения из первого активного подключения
            var dbConnectionString = device.DBConnectionStrings?
                .FirstOrDefault(cs => cs.Use);

            if (dbConnectionString == null)
            {
                throw new InvalidOperationException($"Отсутствует активная строка подключения для устройства {device.Name}");
            }

            // Получаем строку подключения с расшифрованным паролем через extension метод
            var connectionString = dbConnectionString.GetConnectionString();

            // Устанавливаем таймауты для проверки статуса
            var csb = new MySqlConnectionStringBuilder(connectionString)
            {
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

            _logger.LogDebug("Устройство {DeviceName} успешно подключено за {LatencyMs}мс",
                device.Name, status.LatencyMs);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Проверка устройства {DeviceName} была отменена", device.Name);
            status.Error = "Операция отменена";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Не удалось подключиться к устройству {DeviceName} за {ElapsedMs}мс: {ErrorMessage}",
                device.Name, deviceStopwatch.ElapsedMilliseconds, ex.Message);
            status.Error = ex.Message;
        }
        finally
        {
            status.LastChecked = DateTime.Now;
        }

        return status;
    }

    /// <summary>
    /// Проверяет статус всех внешних сервисов (Messaging Service, ELIS, OPC)
    /// Проверка выполняется только если есть активные клиенты
    /// </summary>
    /// <param name="appConfig">Конфигурация приложения</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Объект ServiceStatus со статусом всех сервисов</returns>
    private async Task<ServiceStatus> CheckServicesAsync(CfgApp appConfig, CancellationToken ct)
    {
        var services = new ServiceStatus();

        // Проверяем сервисы только если есть активные клиенты
        if (!_connectionTracker.HasActiveConnections)
        {
            _logger.LogTrace("Пропуск проверки сервисов: нет активных клиентов");

            // Возвращаем статусы по умолчанию (отключено)
            services.MessagingService = new ConnectionStatus { IsConnected = false, LastChecked = DateTime.Now };

            if (appConfig.Elis?.Use == true)
            {
                services.Elis = new ConnectionStatus { IsConnected = false, LastChecked = DateTime.Now };
            }

            return services;
        }

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

    /// <summary>
    /// Проверяет доступность Messaging Service через health endpoint
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Статус подключения к Messaging Service</returns>
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

            _logger.LogDebug("Проверка Messaging Service: {Status} за {LatencyMs}мс",
                status.IsConnected ? "Подключен" : "Отключен", status.LatencyMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось подключиться к Messaging Service: {ErrorMessage}", ex.Message);
            status.Error = ex.Message;
            status.LastChecked = DateTime.Now;
        }

        return status;
    }

    /// <summary>
    /// Проверяет конфигурацию ELIS (Единая Лабораторная Информационная Система)
    /// Пока проверяет только наличие конфигурации, реальная проверка подключения будет добавлена позже
    /// </summary>
    /// <param name="elisConfig">Конфигурация ELIS</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Статус подключения к ELIS</returns>
    private Task<ConnectionStatus> CheckElisServiceAsync(Elis elisConfig, CancellationToken ct)
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
                _logger.LogDebug("Конфигурация ELIS присутствует");
            }
            else
            {
                status.Error = "Конфигурация ELIS неполная";
                _logger.LogDebug("Конфигурация ELIS неполная");
            }

            // TODO: Реализовать реальную проверку через TN.ElisConnector
            // когда будет известен endpoint для health check
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось проверить конфигурацию ELIS: {ErrorMessage}", ex.Message);
            status.Error = ex.Message;
        }

        return Task.FromResult(status);
    }
}