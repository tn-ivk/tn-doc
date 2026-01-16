using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using NLog;

namespace TN_Doc.Services;

/// <summary>
/// Реализация сервиса записи в системный журнал для Linux (syslog через команду logger)
/// </summary>
[SupportedOSPlatform("linux")]
public class LinuxSystemJournalService : ISystemJournalService
{
    private const string DefaultTag = "TN_Doc";
    private const int TimeoutMs = 500;

    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    /// <inheritdoc />
    public void WriteError(string message, string? source = null)
    {
        if (string.IsNullOrEmpty(message))
            return;

        var tag = string.IsNullOrEmpty(source) ? DefaultTag : $"{DefaultTag}:{source}";

        // Fire-and-forget: не блокируем HTTP-запрос
        Task.Run(() => WriteToSyslog(message, tag));
    }

    private void WriteToSyslog(string message, string tag)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "logger",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            // ArgumentList безопасно передаёт аргументы без shell-интерпретации
            process.StartInfo.ArgumentList.Add("-p");
            process.StartInfo.ArgumentList.Add("user.err");
            process.StartInfo.ArgumentList.Add("-t");
            process.StartInfo.ArgumentList.Add(tag);
            process.StartInfo.ArgumentList.Add("--");
            process.StartInfo.ArgumentList.Add(message);

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
}
