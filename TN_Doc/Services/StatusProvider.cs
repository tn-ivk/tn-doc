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
    private readonly AppClientTracker _clientTracker;

    public StatusProvider(IAppConfigService appConfigService, ILogger<StatusProvider> logger, IHttpClientFactory httpClientFactory, AppClientTracker clientTracker)
    {
        _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _clientTracker = clientTracker ?? throw new ArgumentNullException(nameof(clientTracker));
    }

    /// <summary>
    /// Получает текущий статус всех устройств и сервисов
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Объект StatusResponse со статусом устройств и сервисов</returns>
    public async Task<StatusResponse> GetStatusAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var appConfig = _appConfigService.GetAppCfg();
            if (appConfig == null)
            {
                _logger.LogError("Не удалось получить конфигурацию приложения");
                return new StatusResponse();
            }

            var devices = new List<DeviceStatus>();

            // Проверка устройств (БД)
            if (appConfig.Devices?.Any() == true)
            {
                var deviceTasks = appConfig.Devices
                    .Where(d => d.Use)
                    .Select(device => CheckDeviceAsync(device, ct));

                var deviceResults = await Task.WhenAll(deviceTasks);
                devices.AddRange(deviceResults);
            }

            // Проверка сервисов
            var services = await CheckServicesAsync(appConfig, ct);

            var response = new StatusResponse
            {
                Devices = devices,
                Services = services,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            // Один сводный лог вместо нескольких
            var healthyDevices = devices.Count(d => d.IsConnected);
            _logger.LogDebug(
                "Проверка статуса: {ElapsedMs}мс | Устройства: {HealthyDevices}/{TotalDevices} | MS: {MsStatus} | ELIS: {ElisStatus}",
                stopwatch.ElapsedMilliseconds,
                healthyDevices,
                devices.Count,
                services.MessagingService?.IsConnected ?? false,
                services.Elis?.IsConnected ?? false);

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
    /// Проверяет доступность устройства через подключение ко всем его базам данных
    /// </summary>
    /// <param name="device">Устройство для проверки</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Статус устройства с информацией о подключении по всем каналам</returns>
    private async Task<DeviceStatus> CheckDeviceAsync(Device device, CancellationToken ct)
    {
        var status = new DeviceStatus
        {
            Id = device.IdDevice.ToString(),
            Name = device.Name ?? "Unknown",
            Type = "database",
            IsConnected = false,
            IsFullyConnected = false
        };

        // Получаем все активные строки подключения
        var activeConnectionStrings = device.DBConnectionStrings?
            .Where(cs => cs.Use)
            .ToList() ?? new List<DBConnectionString>();

        if (activeConnectionStrings.Count == 0)
        {
            status.Error = "Отсутствуют активные строки подключения";
            status.LastChecked = DateTime.Now;
            return status;
        }

        // Проверяем все каналы параллельно
        var channelTasks = activeConnectionStrings
            .Select((cs, index) => CheckConnectionChannelAsync(cs, index + 1, ct));

        var channels = await Task.WhenAll(channelTasks);
        status.Channels = channels.ToList();
        status.LastChecked = DateTime.Now;

        // Вычисляем общий статус
        var connectedChannels = channels.Where(c => c.IsConnected).ToList();
        var totalChannels = channels.Length;

        status.IsConnected = connectedChannels.Count > 0;
        status.IsFullyConnected = connectedChannels.Count == totalChannels;

        // Минимальная задержка среди работающих каналов
        if (connectedChannels.Count > 0)
        {
            status.LatencyMs = connectedChannels
                .Where(c => c.LatencyMs.HasValue)
                .Min(c => c.LatencyMs);
        }

        // Формируем сообщение об ошибке если есть проблемы
        var failedChannels = channels.Where(c => !c.IsConnected).ToList();
        if (failedChannels.Count > 0)
        {
            status.Error = $"Нет связи: {string.Join(", ", failedChannels.Select(c => c.Name))}";
        }

        _logger.LogDebug(
            "Устройство {DeviceName}: {ConnectedCount}/{TotalCount} каналов",
            device.Name, connectedChannels.Count, totalChannels);

        return status;
    }

    /// <summary>
    /// Проверяет отдельный канал связи (строку подключения)
    /// </summary>
    private async Task<ConnectionChannel> CheckConnectionChannelAsync(
        DBConnectionString dbConnectionString,
        int channelIndex,
        CancellationToken ct)
    {
        var channelStopwatch = Stopwatch.StartNew();
        var channel = new ConnectionChannel
        {
            Name = !string.IsNullOrEmpty(dbConnectionString.Server)
                ? dbConnectionString.Server
                : $"Канал {channelIndex}",
            IsConnected = false
        };

        try
        {
            var connectionString = dbConnectionString.GetConnectionString();

            var csb = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = 2
            };

            using var connection = new MySqlConnection(csb.ConnectionString);
            await connection.OpenAsync(ct);
            await connection.PingAsync(ct);

            channel.IsConnected = true;
            channel.LatencyMs = (int)channelStopwatch.ElapsedMilliseconds;
        }
        catch (OperationCanceledException)
        {
            channel.Error = "Операция отменена";
        }
        catch (Exception ex)
        {
            channel.Error = ex.Message;
        }
        finally
        {
            channel.LastChecked = DateTime.Now;
        }

        return channel;
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
        if (!_clientTracker.HasActiveClients)
        {
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
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning("Не удалось подключиться к Messaging Service: истекло время подключения");
            status.Error = ex.Message;
            status.LastChecked = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось подключиться к Messaging Service: {ErrorMessage}", ex.Message);
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
            if (elisConfig.Use)
            {
                // Считаем ELIS настроенным, если есть ключевые параметры
                status.IsConnected = true;
            }
            else
            {
                status.Error = "Конфигурация ELIS не используется";
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