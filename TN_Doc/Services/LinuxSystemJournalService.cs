using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using NLog;

namespace TN_Doc.Services;

/// <summary>
/// Реализация сервиса записи в системный журнал для Linux (syslog через команду logger)
/// </summary>
[SupportedOSPlatform("linux")]
public class LinuxSystemJournalService : ISystemJournalService
{
    private const string DefaultTag = "TN_Doc";
    private const int TimeoutMs = 5000;

    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly bool _loggerAvailable;

    public LinuxSystemJournalService()
    {
        _loggerAvailable = CheckLoggerAvailability();
        if (!_loggerAvailable)
        {
            _logger.Warn("Команда 'logger' недоступна в системе, запись в syslog отключена");
        }
    }

    /// <inheritdoc />
    public void WriteError(string message, string? source = null)
    {
        if (string.IsNullOrEmpty(message) || !_loggerAvailable)
            return;

        var tag = string.IsNullOrEmpty(source) ? DefaultTag : $"{DefaultTag}:{source}";
        var escapedMessage = EscapeShellArgument(message);

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "logger",
                    Arguments = $"-p user.err -t {tag} {escapedMessage}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var completed = process.WaitForExit(TimeoutMs);

            if (!completed)
            {
                _logger.Warn("Таймаут записи в syslog для сообщения: {Message}", message);
                try { process.Kill(); } catch { /* Игнорируем ошибки при завершении процесса */ }
            }
            else if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                _logger.Warn("Ошибка записи в syslog (код {ExitCode}): {Error}", process.ExitCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Не удалось записать в syslog: {Message}", message);
        }
    }

    private static string EscapeShellArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "''";

        var escaped = new StringBuilder(argument.Length + 10);
        escaped.Append('\'');

        foreach (var c in argument)
        {
            if (c == '\'')
            {
                // Закрываем одинарные кавычки, добавляем экранированную кавычку, открываем снова
                escaped.Append("'\\''");
            }
            else
            {
                escaped.Append(c);
            }
        }

        escaped.Append('\'');
        return escaped.ToString();
    }

    private bool CheckLoggerAvailability()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "logger",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
