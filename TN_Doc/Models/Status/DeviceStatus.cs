using System;
using System.Collections.Generic;

namespace TN_Doc.Models.Status;

/// <summary>
/// Статус устройства (БД, OPC сервер и т.д.)
/// </summary>
public class DeviceStatus
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "database";

    /// <summary>
    /// Хотя бы один канал связи работает
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Все каналы связи работают
    /// </summary>
    public bool IsFullyConnected { get; set; }

    /// <summary>
    /// Минимальная задержка среди работающих каналов
    /// </summary>
    public int? LatencyMs { get; set; }

    public DateTime? LastChecked { get; set; }
    public string? Error { get; set; }

    /// <summary>
    /// Информация по каждому каналу связи (строке подключения)
    /// </summary>
    public List<ConnectionChannel> Channels { get; set; } = new();
}
