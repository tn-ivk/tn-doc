using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TN_Doc.Services;

namespace TN_Doc.Hubs
{
    /// <summary>
    /// SignalR Hub для real-time обновлений статуса
    /// </summary>
    public class StatusHub : Hub
    {
        private readonly ILogger<StatusHub> _logger;
        private readonly IServiceProvider _serviceProvider;

        public StatusHub(ILogger<StatusHub> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override async Task OnConnectedAsync()
        {
            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation(
                "StatusHub: Client connected - ConnectionId: {ConnectionId}, IP: {ClientIP}",
                Context.ConnectionId, clientIp);

            // Внеочередной опрос всех устройств при подключении клиента
            // Это позволяет восстановить заблокированные устройства при переоткрытии браузера
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var statusProvider = scope.ServiceProvider.GetRequiredService<IStatusProvider>();
                var status = await statusProvider.GetStatusAsync();
                await Clients.Caller.SendAsync("statusUpdated", status);

                _logger.LogDebug(
                    "StatusHub: Sent initial status to {ConnectionId} - {DeviceCount} devices",
                    Context.ConnectionId, status.Devices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "StatusHub: Failed to send initial status to {ConnectionId}",
                    Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (exception != null)
            {
                _logger.LogWarning(exception,
                    "StatusHub: Client disconnected with error - ConnectionId: {ConnectionId}, IP: {ClientIP}",
                    Context.ConnectionId, clientIp);
            }
            else
            {
                _logger.LogInformation(
                    "StatusHub: Client disconnected - ConnectionId: {ConnectionId}, IP: {ClientIP}",
                    Context.ConnectionId, clientIp);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}