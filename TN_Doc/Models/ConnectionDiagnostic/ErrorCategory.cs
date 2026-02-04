namespace TN_Doc.Models.ConnectionDiagnostic;

/// <summary>
/// Категория ошибки подключения к БД
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Нет ошибки
    /// </summary>
    None,

    /// <summary>
    /// Ошибка аутентификации (неверный пароль, нет доступа)
    /// MySQL коды: 1045, 1044, 1698
    /// </summary>
    Authentication,

    /// <summary>
    /// Сетевая ошибка (timeout, connection refused)
    /// MySQL коды: 2003, 2002, 2006, 2013
    /// </summary>
    Network,

    /// <summary>
    /// Прочие ошибки
    /// </summary>
    Other
}
