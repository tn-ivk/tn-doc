namespace TN_Doc.Models.ConnectionDiagnostic;

/// <summary>
/// Состояние диагностики соединения
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Закрыто - нормальная работа, подключения разрешены
    /// </summary>
    Closed,

    /// <summary>
    /// Открыто - подключения заблокированы (задержка или ручной сброс)
    /// </summary>
    Open,

    /// <summary>
    /// Полуоткрыто - пробное подключение после истечения задержки
    /// </summary>
    HalfOpen
}
