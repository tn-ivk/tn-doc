using Microsoft.AspNetCore.SignalR;
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
        private readonly ConnectionTracker _connectionTracker;

        public StatusHub(ILogger<StatusHub> logger, ConnectionTracker connectionTracker)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionTracker = connectionTracker ?? throw new ArgumentNullException(nameof(connectionTracker));
        }

        public override async Task OnConnectedAsync()
        {
            _connectionTracker.IncrementConnections();

            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation(
                "StatusHub: Client connected - ConnectionId: {ConnectionId}, IP: {ClientIP}, Active connections: {ActiveConnections}",
                Context.ConnectionId, clientIp, _connectionTracker.ActiveConnectionCount);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _connectionTracker.DecrementConnections();

            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (exception != null)
            {
                _logger.LogWarning(exception,
                    "StatusHub: Client disconnected with error - ConnectionId: {ConnectionId}, IP: {ClientIP}, Active connections: {ActiveConnections}",
                    Context.ConnectionId, clientIp, _connectionTracker.ActiveConnectionCount);
            }
            else
            {
                _logger.LogInformation(
                    "StatusHub: Client disconnected - ConnectionId: {ConnectionId}, IP: {ClientIP}, Active connections: {ActiveConnections}",
                    Context.ConnectionId, clientIp, _connectionTracker.ActiveConnectionCount);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}