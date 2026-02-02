using System;

namespace TN_Doc.Models.Status;

/// <summary>
/// Информация о канале связи (строке подключения) устройства
/// </summary>
public class ConnectionChannel
{
    /// <summary>
    /// Имя канала (из конфигурации или "Канал N")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Подключение установлено успешно
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Задержка подключения в миллисекундах
    /// </summary>
    public int? LatencyMs { get; set; }

    /// <summary>
    /// Сообщение об ошибке (если подключение не удалось)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Время последней проверки
    /// </summary>
    public DateTime? LastChecked { get; set; }
}
