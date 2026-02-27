using System;

namespace TN_Doc.Models.DeviceConnectionDiagnostic;

/// <summary>
/// Внутреннее состояние диагностики соединения для устройства.
/// Все операции чтения/записи мутабельных полей должны выполняться внутри lock (SyncRoot).
/// </summary>
public class DeviceConnectionState
{
    /// <summary>
    /// Объект синхронизации для потокобезопасного доступа к мутабельным полям.
    /// ConcurrentDictionary защищает только коллекцию, но не содержимое объектов —
    /// все чтения/записи полей этого объекта должны быть обёрнуты в lock (SyncRoot).
    /// </summary>
    public object SyncRoot { get; } = new();

    /// <summary>
    /// ID устройства
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Название устройства
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Текущее состояние соединения
    /// </summary>
    public ConnectionState State { get; set; } = ConnectionState.Active;

    /// <summary>
    /// Категория последней ошибки
    /// </summary>
    public ErrorCategory ErrorCategory { get; set; } = ErrorCategory.None;

    /// <summary>
    /// Сообщение последней ошибки
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Количество последовательных неудачных попыток
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Количество попыток с максимальным интервалом опроса
    /// </summary>
    public int MaxPollRetryCount { get; set; }

    /// <summary>
    /// Текущий интервал опроса в секундах
    /// </summary>
    public int CurrentPollSeconds { get; set; }

    /// <summary>
    /// Время следующей разрешённой попытки подключения
    /// </summary>
    public DateTime? NextAllowedAttempt { get; set; }

    /// <summary>
    /// Время последней ошибки
    /// </summary>
    public DateTime? LastFailureTime { get; set; }

    /// <summary>
    /// Требуется ручной сброс (для Auth ошибок или после MaxRetryCount)
    /// </summary>
    public bool RequiresManualReset { get; set; }
}
