using System;

namespace TN_Doc.Models.CircuitBreaker;

/// <summary>
/// Внутреннее состояние Circuit Breaker для устройства
/// </summary>
public class DeviceCircuitState
{
    /// <summary>
    /// ID устройства
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Название устройства
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Текущее состояние Circuit Breaker
    /// </summary>
    public CircuitState State { get; set; } = CircuitState.Closed;

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
    /// Количество попыток с максимальным backoff
    /// </summary>
    public int MaxBackoffRetryCount { get; set; }

    /// <summary>
    /// Текущий интервал backoff в секундах
    /// </summary>
    public int CurrentBackoffSeconds { get; set; }

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
