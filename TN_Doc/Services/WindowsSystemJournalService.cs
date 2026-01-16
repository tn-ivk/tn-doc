using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using NLog;

namespace TN_Doc.Services;

/// <summary>
/// Реализация сервиса записи в системный журнал для Windows (Event Log)
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsSystemJournalService : ISystemJournalService
{
    private const string DefaultSource = "TN_Doc";
    private const string FallbackSource = ".NET Runtime";
    private const int EventId = 1000;
    private const short Category = 1;

    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly string _effectiveSource;

    public WindowsSystemJournalService()
    {
        _effectiveSource = DetermineEffectiveSource();
    }

    /// <inheritdoc />
    public void WriteError(string message, string? source = null)
    {
        if (string.IsNullOrEmpty(message))
            return;

        var formattedMessage = FormatMessage(message, source);

        try
        {
            EventLog.WriteEntry(_effectiveSource, formattedMessage, EventLogEntryType.Error, EventId, Category);
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Не удалось записать в Windows Event Log: {Message}", formattedMessage);
        }
    }

    private static string FormatMessage(string message, string? source)
    {
        return string.IsNullOrEmpty(source)
            ? message
            : $"[{source}] {message}";
    }

    private string DetermineEffectiveSource()
    {
        try
        {
            if (EventLog.SourceExists(DefaultSource))
            {
                return DefaultSource;
            }

            // Попытка создать источник (требует права администратора)
            EventLog.CreateEventSource(DefaultSource, "Application");
            return DefaultSource;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Не удалось создать источник '{Source}' в Event Log, используется fallback '{Fallback}'",
                DefaultSource, FallbackSource);
            return FallbackSource;
        }
    }
}
