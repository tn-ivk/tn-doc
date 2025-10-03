using System;
using System.Collections.Generic;

namespace TN_Doc.Models.Status;

/// <summary>
/// Ответ со статусом всех систем
/// </summary>
public class StatusResponse
{
    public List<DeviceStatus> Devices { get; set; } = new();
    public ServiceStatus Services { get; set; } = new();
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}