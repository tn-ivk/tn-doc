namespace TN_Doc.Models.ConnectionDiagnostic;

/// <summary>
/// DTO с информацией о диагностике соединения для API ответа
/// </summary>
public class ConnectionDiagnosticInfo
{
    /// <summary>
    /// Устройство заблокировано (Open state с RequiresManualReset)
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Текущее состояние диагностики соединения
    /// </summary>
    public string State { get; set; } = "Closed";

    /// <summary>
    /// Категория ошибки (если есть)
    /// </summary>
    public string? ErrorCategory { get; set; }

    /// <summary>
    /// Сообщение последней ошибки
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Количество последовательных неудачных попыток
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Достигнут лимит попыток (MaxRetryCount)
    /// </summary>
    public bool MaxRetryReached { get; set; }

    /// <summary>
    /// Текущий интервал опроса в секундах (0 если нет задержки)
    /// </summary>
    public int CurrentPollSeconds { get; set; }

    /// <summary>
    /// Секунд до следующей автоматической попытки (null если блокировка)
    /// </summary>
    public int? SecondsUntilNextAttempt { get; set; }
}
