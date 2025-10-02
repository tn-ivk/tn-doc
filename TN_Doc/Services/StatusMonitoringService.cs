using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TN_Doc.Hubs;
using TN_Doc.Models.Status;

namespace TN_Doc.Services;

/// <summary>
/// Background service для мониторинга статуса и отправки обновлений через SignalR
/// </summary>
public class StatusMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<StatusHub> _hubContext;
    private readonly ILogger<StatusMonitoringService> _logger;
    private readonly ConnectionTracker _connectionTracker;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);
    private StatusResponse _lastStatus;
    private int _consecutiveErrors = 0;
    private const int MAX_CONSECUTIVE_ERRORS = 5;

    public StatusMonitoringService(
        IServiceProvider serviceProvider,
        IHubContext<StatusHub> hubContext,
        ILogger<StatusMonitoringService> logger,
        ConnectionTracker connectionTracker)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionTracker = connectionTracker ?? throw new ArgumentNullException(nameof(connectionTracker));
    }

    /// <summary>
    /// Основной метод фонового сервиса мониторинга
    /// Периодически проверяет статус устройств и сервисов, отправляя обновления через SignalR при изменениях
    /// </summary>
    /// <param name="stoppingToken">Токен остановки сервиса</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Фоновый сервис мониторинга статуса запущен с интервалом {CheckInterval}с",
            _checkInterval.TotalSeconds);

        // Даем системе время на инициализацию
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var cycleStart = DateTime.UtcNow;

            // Проверяем наличие активных подключений перед выполнением проверки
            if (!_connectionTracker.HasActiveConnections)
            {
                _logger.LogTrace("Пропуск проверки статуса: нет активных клиентов");
                await Task.Delay(_checkInterval, stoppingToken);
                continue;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<IStatusProvider>();
                var currentStatus = await provider.GetStatusAsync(stoppingToken);

                var hasChanges = HasStatusChanged(currentStatus);

                if (hasChanges)
                {
                    _lastStatus = currentStatus;

                    _logger.LogInformation(
                        "Обнаружены изменения статуса, отправка обновления. " +
                        "Устройства: {HealthyDevices}/{TotalDevices}, MS: {MsStatus}",
                        currentStatus.Devices.Count(d => d.IsConnected),
                        currentStatus.Devices.Count,
                        currentStatus.Services.MessagingService?.IsConnected ?? false);

                    await _hubContext.Clients.All.SendAsync(
                        "statusUpdated",
                        currentStatus,
                        stoppingToken);
                }
                else
                {
                    _logger.LogTrace("Изменений статуса не обнаружено в цикле мониторинга");
                }

                // Сброс счетчика ошибок при успешном выполнении
                _consecutiveErrors = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Сервис мониторинга статуса останавливается");
                break;
            }
            catch (Exception ex)
            {
                _consecutiveErrors++;
                var cycleDuration = DateTime.UtcNow - cycleStart;

                if (_consecutiveErrors <= MAX_CONSECUTIVE_ERRORS)
                {
                    _logger.LogWarning(ex,
                        "Ошибка #{ErrorCount} в цикле мониторинга статуса (выполнено за {CycleDurationMs}мс): {ErrorMessage}",
                        _consecutiveErrors, cycleDuration.TotalMilliseconds, ex.Message);
                }
                else
                {
                    _logger.LogError(ex,
                        "Критическая ситуация: {ErrorCount} последовательных ошибок в мониторинге статуса. " +
                        "Последний цикл выполнен за {CycleDurationMs}мс. Стабильность сервиса под угрозой.",
                        _consecutiveErrors, cycleDuration.TotalMilliseconds);
                }

                // Увеличиваем интервал при множественных ошибках
                if (_consecutiveErrors > 3)
                {
                    var delayMultiplier = Math.Min(_consecutiveErrors - 2, 5);
                    await Task.Delay(TimeSpan.FromSeconds(_checkInterval.TotalSeconds * delayMultiplier), stoppingToken);
                    continue;
                }
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Фоновый сервис мониторинга статуса остановлен");
    }

    /// <summary>
    /// Сравнивает текущий статус с предыдущим и определяет наличие изменений
    /// </summary>
    /// <param name="current">Текущий статус для сравнения</param>
    /// <returns>true если обнаружены изменения, иначе false</returns>
    private bool HasStatusChanged(StatusResponse current)
    {
        if (_lastStatus == null)
        {
            _logger.LogDebug("Первая проверка статуса, помечено как изменение");
            return true;
        }

        var changes = new List<string>();

        // Сравниваем устройства
        foreach (var device in current.Devices)
        {
            var lastDevice = _lastStatus.Devices.FirstOrDefault(d => d.Id == device.Id);
            if (lastDevice == null)
            {
                changes.Add($"Новое устройство: {device.Name}");
            }
            else if (lastDevice.IsConnected != device.IsConnected)
            {
                changes.Add($"Устройство {device.Name}: {(device.IsConnected ? "подключено" : "отключено")}");
            }
        }

        // Сравниваем сервисы
        if (current.Services.MessagingService?.IsConnected != _lastStatus.Services.MessagingService?.IsConnected)
        {
            changes.Add($"MessagingService: {(current.Services.MessagingService?.IsConnected == true ? "подключен" : "отключен")}");
        }

        if (current.Services.Elis?.IsConnected != _lastStatus.Services.Elis?.IsConnected)
        {
            changes.Add($"ELIS: {(current.Services.Elis?.IsConnected == true ? "подключен" : "отключен")}");
        }

        if (changes.Any())
        {
            _logger.LogInformation("Обнаружены изменения статуса: {Changes}", string.Join(", ", changes));
            return true;
        }

        return false;
    }
}