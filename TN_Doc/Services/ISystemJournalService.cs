namespace TN_Doc.Services;

/// <summary>
/// Интерфейс сервиса для записи в системный журнал ОС
/// </summary>
/// <remarks>
/// Windows: записывает в Windows Event Log
/// Linux: записывает в syslog через команду logger
/// </remarks>
public interface ISystemJournalService
{
    /// <summary>
    /// Записать сообщение об ошибке в системный журнал
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <param name="source">Источник сообщения (подсистема), например: ELIS, OPC</param>
    void WriteError(string message, string? source = null);
}
