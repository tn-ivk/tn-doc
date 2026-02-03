namespace TN_Doc.Models.CircuitBreaker;

/// <summary>
/// Состояние Circuit Breaker
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Закрыт - нормальная работа, подключения разрешены
    /// </summary>
    Closed,

    /// <summary>
    /// Открыт - подключения заблокированы (backoff или ручной сброс)
    /// </summary>
    Open,

    /// <summary>
    /// Полуоткрыт - пробное подключение после истечения backoff
    /// </summary>
    HalfOpen
}
