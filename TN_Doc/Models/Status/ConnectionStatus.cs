using System;

namespace TN_Doc.Models.Status;

/// <summary>
/// Статус подключения к внешнему сервису
/// </summary>
public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime? LastChecked { get; set; }
    public string? Error { get; set; }
}