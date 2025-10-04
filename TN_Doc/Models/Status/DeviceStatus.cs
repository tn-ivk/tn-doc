using System;

namespace TN_Doc.Models.Status;

/// <summary>
/// Статус устройства (БД, OPC сервер и т.д.)
/// </summary>
public class DeviceStatus
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "database";
    public bool IsConnected { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime? LastChecked { get; set; }
    public string Error { get; set; }
}