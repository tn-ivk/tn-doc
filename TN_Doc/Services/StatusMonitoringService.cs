using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TN_Doc.Hubs;

namespace TN_Doc.Services;

public class StatusMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<StatusHub> _hubContext;
    private readonly ILogger<StatusMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);
    private StatusResponse _lastStatus;
    private int _consecutiveErrors = 0;
    private const int MAX_CONSECUTIVE_ERRORS = 5;

    public StatusMonitoringService(
        IServiceProvider serviceProvider,
        IHubContext<StatusHub> hubContext,
        ILogger<StatusMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Status monitoring background service started with {CheckInterval}s interval",
            _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var cycleStart = DateTime.UtcNow;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var provider = (IStatusProvider)scope.ServiceProvider.GetService(typeof(IStatusProvider));
                var currentStatus = await provider.GetStatusAsync(stoppingToken);

                var hasChanges = HasStatusChanged(currentStatus);

                if (hasChanges)
                {
                    _lastStatus = currentStatus;
                    await _hubContext.Clients.All.SendAsync(
                        "statusUpdated",
                        currentStatus,
                        stoppingToken);
                }

                _consecutiveErrors = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Status monitoring service is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _consecutiveErrors++;
                var cycleDuration = DateTime.UtcNow - cycleStart;
                if (_consecutiveErrors <= MAX_CONSECUTIVE_ERRORS)
                {
                    _logger.LogWarning(ex, "Error #{ErrorCount} in status monitoring cycle (took {CycleDurationMs}ms): {ErrorMessage}",
                        _consecutiveErrors, cycleDuration.TotalMilliseconds, ex.Message);
                }
                else
                {
                    _logger.LogError(ex, "Critical: {ErrorCount} consecutive errors in status monitoring.",
                        _consecutiveErrors);
                }

                if (_consecutiveErrors > 3)
                {
                    var delayMultiplier = Math.Min(_consecutiveErrors - 2, 5);
                    await Task.Delay(_checkInterval * delayMultiplier, stoppingToken);
                    continue;
                }
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Status monitoring background service stopped");
    }

    private bool HasStatusChanged(StatusResponse current)
    {
        if (_lastStatus == null)
        {
            return true;
        }

        foreach (var device in current.Devices)
        {
            var lastDevice = _lastStatus.Devices.FirstOrDefault(d => d.Id == device.Id);
            if (lastDevice == null) return true;
            if (lastDevice.IsConnected != device.IsConnected) return true;
        }

        if ((current.Services.MessagingService?.IsConnected ?? false) != (_lastStatus.Services.MessagingService?.IsConnected ?? false))
            return true;

        return false;
    }
}


