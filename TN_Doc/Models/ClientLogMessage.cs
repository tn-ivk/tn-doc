namespace TN_Doc.Models;

/// <summary>
/// Модель для приёма логов от клиентской части
/// </summary>
public class ClientLogMessage
{
    /// <summary>
    /// Уровень логирования (Trace, Debug, Info, Warn, Error)
    /// </summary>
    public string Level { get; set; }

    /// <summary>
    /// Текст сообщения
    /// </summary>
    public string Message { get; set; }
}
