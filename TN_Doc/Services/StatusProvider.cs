using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using TN_Doc.Models.CircuitBreaker;
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
    private readonly ICircuitBreakerService _circuitBreaker;

    public StatusProvider(
        IAppConfigService appConfigService,
        ILogger<StatusProvider> logger,
        IHttpClientFactory httpClientFactory,
        AppClientTracker clientTracker,
        ICircuitBreakerService circuitBreaker)
    {
        _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _clientTracker = clientTracker ?? throw new ArgumentNullException(nameof(clientTracker));
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
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
                    .Select(device => CheckDeviceInternalAsync(device, ct));

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
    /// Проверяет статус конкретного устройства по ID (с принудительной проверкой)
    /// </summary>
    /// <param name="deviceId">ID устройства</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Статус устройства или null если устройство не найдено</returns>
    public async Task<DeviceStatus?> CheckDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        var appConfig = _appConfigService.GetAppCfg();
        if (appConfig?.Devices == null)
            return null;

        var device = appConfig.Devices.FirstOrDefault(d => d.IdDevice.ToString() == deviceId && d.Use);
        if (device == null)
            return null;

        _logger.LogInformation("Принудительная проверка устройства {DeviceId} ({DeviceName})",
            deviceId, device.Name);

        // Сбрасываем Circuit Breaker перед принудительной проверкой
        _circuitBreaker.ResetDevice(deviceId);

        return await CheckDeviceInternalAsync(device, ct, forceCheck: true);
    }

    /// <summary>
    /// Проверяет доступность устройства через подключение ко всем его базам данных
    /// </summary>
    /// <param name="device">Конфигурация устройства</param>
    /// <param name="ct">Токен отмены</param>
    /// <param name="forceCheck">Принудительная проверка (игнорировать Circuit Breaker)</param>
    /// <returns>Статус устройства с информацией о подключении по всем каналам</returns>
    private async Task<DeviceStatus> CheckDeviceInternalAsync(Device device, CancellationToken ct, bool forceCheck = false)
    {
        var deviceId = device.IdDevice.ToString();
        var deviceName = device.Name ?? $"Device {device.IdDevice}";

        var status = new DeviceStatus
        {
            Id = deviceId,
            Name = deviceName,
            Type = "database",
            IsConnected = false,
            IsFullyConnected = false
        };

        // Проверяем Circuit Breaker (если не принудительная проверка)
        if (!forceCheck && !_circuitBreaker.ShouldAllowConnection(deviceId))
        {
            // Устройство заблокировано - возвращаем кэшированное состояние ошибки
            var cbInfo = _circuitBreaker.GetCircuitBreakerInfo(deviceId);
            status.CircuitBreaker = cbInfo;
            status.Error = cbInfo?.LastError ?? "Устройство заблокировано Circuit Breaker";
            status.LastChecked = DateTime.Now;

            _logger.LogDebug(
                "Устройство {DeviceName} заблокировано Circuit Breaker (категория: {Category})",
                deviceName, cbInfo?.ErrorCategory);

            return status;
        }

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
            .Select((cs, index) => CheckConnectionChannelAsync(deviceId, deviceName, cs, index + 1, ct));

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

            // Успешное подключение - сбрасываем Circuit Breaker
            _circuitBreaker.RecordSuccess(deviceId);
        }

        // Формируем сообщение об ошибке если есть проблемы
        var failedChannels = channels.Where(c => !c.IsConnected).ToList();
        if (failedChannels.Count > 0)
        {
            status.Error = $"Нет связи: {string.Join(", ", failedChannels.Select(c => c.Name))}";
        }

        // Добавляем информацию о Circuit Breaker (если есть)
        status.CircuitBreaker = _circuitBreaker.GetCircuitBreakerInfo(deviceId);

        _logger.LogDebug(
            "Устройство {DeviceName}: {ConnectedCount}/{TotalCount} каналов",
            deviceName, connectedChannels.Count, totalChannels);

        return status;
    }

    /// <summary>
    /// Проверяет отдельный канал связи (строку подключения)
    /// </summary>
    private async Task<ConnectionChannel> CheckConnectionChannelAsync(
        string deviceId,
        string deviceName,
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
            // Получаем строку подключения с расшифрованным паролем
            var connectionString = dbConnectionString.GetConnectionString();
            var builder = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = 2
            };

            await using var connection = new MySqlConnection(builder.ConnectionString);
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
            // Регистрируем ошибку в Circuit Breaker
            _circuitBreaker.RecordFailure(deviceId, deviceName, ex);
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
            if (elisConfig.Use)
            {
                // Считаем ELIS настроенным, если есть ключевые параметры
                status.IsConnected = true;
                _logger.LogDebug("Конфигурация ELIS используется");
            }
            else
            {
                status.Error = "Конфигурация ELIS не используется";
                _logger.LogDebug("Конфигурация ELIS не используется");
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