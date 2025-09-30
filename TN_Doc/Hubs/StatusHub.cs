using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TN_Doc.Hubs
{
    /// <summary>
    /// SignalR Hub для real-time обновлений статуса
    /// </summary>
    public class StatusHub : Hub
    {
        private readonly ILogger<StatusHub> _logger;

        public StatusHub(ILogger<StatusHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task OnConnectedAsync()
        {
            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation(
                "StatusHub: Client connected - ConnectionId: {ConnectionId}, IP: {ClientIP}",
                Context.ConnectionId, clientIp);

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