using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using TN.DocData;
using TN_DocGeneral.Extensions;
using TN_DocGeneral.Services;

namespace TN_Doc.Services;

public class StatusProvider : IStatusProvider
{
    private readonly IAppConfigService _appConfigService;
    private readonly ILogger<StatusProvider> _logger;

    public StatusProvider(
        IAppConfigService appConfigService,
        ILogger<StatusProvider> logger)
    {
        _appConfigService = appConfigService;
        _logger = logger;
    }

    public async Task<StatusResponse> GetStatusAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting status check for all devices");

        try
        {
            var appConfig = _appConfigService.GetAppCfg();

            var devices = new List<DeviceStatus>();

            if (appConfig.Devices?.Any() == true)
            {
                var deviceTasks = appConfig.Devices
                    .Where(d => d.Use)
                    .Select(device => CheckDeviceAsync(device, ct));

                var deviceResults = await Task.WhenAll(deviceTasks);
                devices.AddRange(deviceResults);
            }

            var services = new ServiceStatus
            {
                MessagingService = new ConnectionStatus { IsConnected = true }
            };

            var response = new StatusResponse
            {
                Devices = devices,
                Services = services,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            _logger.LogInformation("Status check completed in {ElapsedMs}ms - Devices: {HealthyDevices}/{TotalDevices}",
                stopwatch.ElapsedMilliseconds,
                devices.Count(d => d.IsConnected),
                devices.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve status information after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static async Task<DeviceStatus> CheckDeviceAsync(Device device, CancellationToken ct)
    {
        var deviceStopwatch = Stopwatch.StartNew();

        var status = new DeviceStatus
        {
            Id = device.IdDevice.ToString(),
            Name = device.Name,
            Type = "database",
            IsConnected = false
        };

        try
        {
            var connString = device.DBConnectionStrings?.FirstOrDefault(x => x.Use);
            if (connString == null)
            {
                status.Error = "Active DB connection not configured";
                return status;
            }

            var cs = connString.GetConnectionString();

            var csb = new MySqlConnectionStringBuilder(cs)
            {
                ConnectionTimeout = 2,
                DefaultCommandTimeout = 2
            };

            await using var connection = new MySqlConnection(csb.ConnectionString);
            await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 2;
            await command.ExecuteScalarAsync(ct);

            status.IsConnected = true;
            status.LatencyMs = (int)deviceStopwatch.ElapsedMilliseconds;
        }
        catch (OperationCanceledException)
        {
            status.Error = "Operation cancelled";
        }
        catch (Exception ex)
        {
            status.Error = ex.Message;
        }
        finally
        {
            status.LastChecked = DateTime.Now;
        }

        return status;
    }
}

public class StatusResponse
{
    public List<DeviceStatus> Devices { get; set; } = new();
    public ServiceStatus Services { get; set; } = new();
    public string Timestamp { get; set; }
}

public class DeviceStatus
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsConnected { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime? LastChecked { get; set; }
    public string Error { get; set; }
}

public class ServiceStatus
{
    public ConnectionStatus MessagingService { get; set; }
    public ConnectionStatus Elis { get; set; }
    public ConnectionStatus OpcDa { get; set; }
    public ConnectionStatus OpcUa { get; set; }
}

public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime? LastChecked { get; set; }
    public string Error { get; set; }
}


