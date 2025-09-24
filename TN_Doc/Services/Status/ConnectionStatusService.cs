using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using TN_Doc.Models.Status;
using TN_DocGeneral.Services;

namespace TN_Doc.Services.Status
{
    /// <summary>
    /// Сервис проверки статуса подключений к устройствам
    /// </summary>
    public class ConnectionStatusService : IConnectionStatusService
    {
        private readonly IAppConfigService _configService;
        private readonly ILogger<ConnectionStatusService> _logger;
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<int, DeviceStatus> _deviceStatuses;
        private readonly ConcurrentDictionary<int, ConnectionStatistics> _deviceStatistics;
        private readonly string _cacheKeyPrefix = "DeviceStatus_";
        private readonly string _signalRCacheKey = "SignalRStatus";
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Событие изменения статуса устройства
        /// </summary>
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        public ConnectionStatusService(
            IAppConfigService configService,
            ILogger<ConnectionStatusService> logger,
            IMemoryCache cache)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            _deviceStatuses = new ConcurrentDictionary<int, DeviceStatus>();
            _deviceStatistics = new ConcurrentDictionary<int, ConnectionStatistics>();
        }

        /// <summary>
        /// Проверка подключения к конкретному устройству
        /// </summary>
        public async Task<ConnectionStatus> CheckDeviceConnectionAsync(int deviceId)
        {
            var cacheKey = $"{_cacheKeyPrefix}{deviceId}";

            // Проверяем кэш
            if (_cache.TryGetValue(cacheKey, out ConnectionStatus? cachedStatus))
            {
                return cachedStatus!;
            }

            var status = new ConnectionStatus
            {
                ConnectionType = ConnectionType.Database,
                LastCheck = DateTime.Now
            };

            try
            {
                var connectionString = GetConnectionStringForDevice(deviceId);
                if (string.IsNullOrEmpty(connectionString))
                {
                    status.IsConnected = false;
                    status.ErrorMessage = "Строка подключения не найдена для устройства";
                    status.ConnectionInfo = $"Устройство {deviceId}: строка подключения отсутствует";
                    _logger.LogWarning("Строка подключения не найдена для устройства {DeviceId}", deviceId);
                    return status;
                }

                var stopwatch = Stopwatch.StartNew();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Простой тест подключения
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandTimeout = 5; // 5 секунд таймаут
                await command.ExecuteScalarAsync();

                stopwatch.Stop();

                status.IsConnected = connection.State == ConnectionState.Open;
                status.ResponseTime = stopwatch.ElapsedMilliseconds;
                status.ConnectionInfo = $"Подключено за {stopwatch.ElapsedMilliseconds}мс";

                _logger.LogDebug("Устройство {DeviceId} подключено за {ResponseTime}мс", deviceId, stopwatch.ElapsedMilliseconds);

                // Обновляем статистику
                UpdateDeviceStatistics(deviceId, true, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                status.IsConnected = false;
                status.ErrorMessage = ex.Message;
                status.ConnectionInfo = $"Ошибка: {ex.Message}";

                _logger.LogWarning("Ошибка подключения к устройству {DeviceId}: {Error}", deviceId, ex.Message);

                // Обновляем статистику с ошибкой
                UpdateDeviceStatistics(deviceId, false, 0);

                // Увеличиваем счетчик попыток
                status.RetryCount = GetRetryCount(deviceId) + 1;
            }

            // Кэшируем результат
            _cache.Set(cacheKey, status, _cacheTimeout);

            // Проверяем изменение статуса и вызываем событие
            CheckAndFireStatusChangeEvent(deviceId, status.IsConnected);

            return status;
        }

        /// <summary>
        /// Проверка подключения к SignalR Hub
        /// </summary>
        public async Task<ServiceStatus> CheckSignalRConnectionAsync()
        {
            // Проверяем кэш
            if (_cache.TryGetValue(_signalRCacheKey, out ServiceStatus? cachedStatus))
            {
                return cachedStatus!;
            }

            var status = new ServiceStatus
            {
                ServiceName = "SignalR Hub",
                Url = "http://localhost:5010/SignalRApp",
                LastCheck = DateTime.Now
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(3); // 3 секунды таймаут

                // Проверяем доступность SignalR Hub через HTTP запрос
                var response = await httpClient.GetAsync("http://localhost:5010/SignalRApp/negotiate");

                stopwatch.Stop();

                status.IsAvailable = response.IsSuccessStatusCode;
                status.ResponseTime = stopwatch.ElapsedMilliseconds;
                status.Status = response.IsSuccessStatusCode ? "Доступен" : $"Ошибка HTTP {response.StatusCode}";

                _logger.LogDebug("SignalR Hub проверен за {ResponseTime}мс, статус: {Status}",
                    stopwatch.ElapsedMilliseconds, status.Status);
            }
            catch (Exception ex)
            {
                status.IsAvailable = false;
                status.Status = $"Ошибка: {ex.Message}";
                status.ErrorMessage = ex.Message;

                _logger.LogWarning("Ошибка проверки SignalR Hub: {Error}", ex.Message);
            }

            // Кэшируем результат
            _cache.Set(_signalRCacheKey, status, _cacheTimeout);

            return status;
        }

        /// <summary>
        /// Получение статусов всех устройств
        /// </summary>
        public async Task<List<DeviceStatus>> GetAllDeviceStatusesAsync()
        {
            var config = _configService.GetAppCfg();
            var deviceStatuses = new List<DeviceStatus>();

            var tasks = config.Devices.Select(async device =>
            {
                var connectionStatus = await CheckDeviceConnectionAsync(device.IdDevice);

                return new DeviceStatus
                {
                    DeviceId = device.IdDevice,
                    DeviceName = device.Name ?? $"ИВК-{device.IdDevice}",
                    IsConnected = connectionStatus.IsConnected,
                    LastCheck = connectionStatus.LastCheck,
                    ResponseTime = connectionStatus.ResponseTime,
                    ConnectionInfo = connectionStatus.ConnectionInfo,
                    ErrorMessage = connectionStatus.ErrorMessage,
                    RetryCount = connectionStatus.RetryCount,
                    ConnectionString = MaskConnectionString(GetConnectionStringForDevice(device.IdDevice))
                };
            });

            deviceStatuses.AddRange(await Task.WhenAll(tasks));

            return deviceStatuses;
        }

        /// <summary>
        /// Получение статуса конкретного устройства
        /// </summary>
        public async Task<DeviceStatus?> GetDeviceStatusAsync(int deviceId)
        {
            var config = _configService.GetAppCfg();
            var device = config.Devices.FirstOrDefault(d => d.IdDevice == deviceId);

            if (device == null)
            {
                _logger.LogWarning("Устройство с ID {DeviceId} не найдено в конфигурации", deviceId);
                return null;
            }

            var connectionStatus = await CheckDeviceConnectionAsync(deviceId);

            return new DeviceStatus
            {
                DeviceId = device.IdDevice,
                DeviceName = device.Name ?? $"ИВК-{device.IdDevice}",
                IsConnected = connectionStatus.IsConnected,
                LastCheck = connectionStatus.LastCheck,
                ResponseTime = connectionStatus.ResponseTime,
                ConnectionInfo = connectionStatus.ConnectionInfo,
                ErrorMessage = connectionStatus.ErrorMessage,
                RetryCount = connectionStatus.RetryCount,
                ConnectionString = MaskConnectionString(GetConnectionStringForDevice(device.IdDevice))
            };
        }

        /// <summary>
        /// Получение конфигурации для клиента
        /// </summary>
        public async Task<StatusConfiguration> GetStatusConfigurationAsync()
        {
            var config = _configService.GetAppCfg();

            var statusConfig = new StatusConfiguration
            {
                Devices = config.Devices.Select(d => new DeviceConfiguration
                {
                    IdDevice = d.IdDevice,
                    Name = d.Name ?? $"ИВК-{d.IdDevice}",
                    Description = d.Description,
                    IsActive = true
                }).ToList(),

                Elis = new ElisConfiguration
                {
                    Use = config.Elis?.Use ?? false,
                    ApiUrl = config.Elis?.ClientName, // Используем ClientName как базовый URL
                    IsGlobalSettings = !string.IsNullOrEmpty(config.Elis?.ClientName)
                },

                UpdateSettings = new StatusUpdateSettings
                {
                    DeviceUpdateInterval = 60,
                    OpcUpdateInterval = 30,
                    ServiceUpdateInterval = 10,
                    CacheTtl = 10
                }
            };

            return await Task.FromResult(statusConfig);
        }

        /// <summary>
        /// Принудительное обновление статуса устройства
        /// </summary>
        public async Task RefreshDeviceStatusAsync(int deviceId)
        {
            var cacheKey = $"{_cacheKeyPrefix}{deviceId}";
            _cache.Remove(cacheKey);

            await CheckDeviceConnectionAsync(deviceId);
        }

        /// <summary>
        /// Получение статистики подключений
        /// </summary>
        public async Task<Dictionary<string, ConnectionStatistics>> GetConnectionStatisticsAsync()
        {
            var result = new Dictionary<string, ConnectionStatistics>();

            foreach (var kvp in _deviceStatistics)
            {
                result[$"Device_{kvp.Key}"] = kvp.Value;
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Очистка кэша статусов
        /// </summary>
        public void ClearStatusCache()
        {
            var config = _configService.GetAppCfg();

            foreach (var device in config.Devices)
            {
                _cache.Remove($"{_cacheKeyPrefix}{device.IdDevice}");
            }

            _cache.Remove(_signalRCacheKey);

            _logger.LogInformation("Кэш статусов очищен");
        }

        /// <summary>
        /// Обновление статистики устройства
        /// </summary>
        private void UpdateDeviceStatistics(int deviceId, bool success, long responseTime)
        {
            _deviceStatistics.AddOrUpdate(deviceId,
                new ConnectionStatistics
                {
                    TotalChecks = 1,
                    SuccessfulConnections = success ? 1 : 0,
                    FailedConnections = success ? 0 : 1,
                    AverageResponseTime = responseTime,
                    FirstCheckTime = DateTime.Now,
                    Uptime = success ? TimeSpan.Zero : null
                },
                (key, existing) =>
                {
                    existing.TotalChecks++;
                    if (success)
                    {
                        existing.SuccessfulConnections++;
                        existing.AverageResponseTime = (existing.AverageResponseTime * (existing.TotalChecks - 1) + responseTime) / existing.TotalChecks;

                        if (existing.Uptime == null)
                            existing.Uptime = TimeSpan.Zero;
                    }
                    else
                    {
                        existing.FailedConnections++;
                        existing.Uptime = null;
                    }

                    return existing;
                });
        }

        /// <summary>
        /// Проверка изменения статуса и вызов события
        /// </summary>
        private void CheckAndFireStatusChangeEvent(int deviceId, bool currentStatus)
        {
            var wasConnected = _deviceStatuses.ContainsKey(deviceId) && _deviceStatuses[deviceId].IsConnected;

            _deviceStatuses.AddOrUpdate(deviceId,
                new DeviceStatus
                {
                    DeviceId = deviceId,
                    IsConnected = currentStatus,
                    LastCheck = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.IsConnected = currentStatus;
                    existing.LastCheck = DateTime.Now;
                    return existing;
                });

            // Если статус изменился, вызываем событие
            if (wasConnected != currentStatus)
            {
                DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                {
                    DeviceId = deviceId,
                    PreviousStatus = wasConnected,
                    CurrentStatus = currentStatus,
                    Timestamp = DateTime.Now
                });

                _logger.LogInformation("Изменен статус устройства {DeviceId}: {PreviousStatus} -> {CurrentStatus}",
                    deviceId, wasConnected, currentStatus);
            }
        }

        /// <summary>
        /// Получение количества попыток переподключения
        /// </summary>
        private int GetRetryCount(int deviceId)
        {
            return _deviceStatuses.ContainsKey(deviceId) ? _deviceStatuses[deviceId].RetryCount : 0;
        }

        /// <summary>
        /// Получение строки подключения для устройства
        /// </summary>
        private string GetConnectionStringForDevice(int deviceId)
        {
            var config = _configService.GetAppCfg();
            var device = config.Devices.FirstOrDefault(d => d.IdDevice == deviceId);

            if (device?.DBConnectionStrings == null || !device.DBConnectionStrings.Any())
            {
                return string.Empty;
            }

            var dbConnection = device.DBConnectionStrings.FirstOrDefault(c => c.Use);
            if (dbConnection == null)
            {
                return string.Empty;
            }

            return $"Server={dbConnection.Server};Database={dbConnection.Database};Uid={dbConnection.Userid};Pwd={dbConnection.Password};Connection Timeout={dbConnection.ConnectionTimeout};";
        }

        /// <summary>
        /// Маскировка строки подключения для безопасности
        /// </summary>
        private string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            // Простая маскировка пароля
            var parts = connectionString.Split(';');
            var maskedParts = parts.Select(part =>
            {
                if (part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) ||
                    part.Trim().StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Split('=')[0] + "=***";
                }
                return part;
            });

            return string.Join(";", maskedParts);
        }
    }
}