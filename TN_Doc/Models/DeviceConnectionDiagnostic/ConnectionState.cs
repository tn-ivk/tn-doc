namespace TN_Doc.Models.DeviceConnectionDiagnostic;

/// <summary>
/// Состояние диагностики соединения (Circuit Breaker pattern)
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Активно - нормальная работа, подключения разрешены
    /// </summary>
    Active,

    /// <summary>
    /// Заблокировано - подключения запрещены (задержка или ручной сброс)
    /// </summary>
    Blocked,

    /// <summary>
    /// Восстановление - пробное подключение после истечения задержки
    /// </summary>
    Recovering
}
