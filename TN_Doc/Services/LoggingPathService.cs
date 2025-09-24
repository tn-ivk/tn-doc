using System;
using System.IO;

namespace TN_Doc.Services;

/// <summary>
/// Сервис для определения путей логирования в зависимости от операционной системы
/// </summary>
public class LoggingPathService
{
    private const string LinuxLogDirectory = "/var/log/TN_Doc";
    private const string WindowsLogDirectory = "logs";

    /// <summary>
    /// Получить директорию для логов в зависимости от операционной системы
    /// </summary>
    /// <returns>Полный путь к директории логов</returns>
    public static string GetLogDirectory()
    {
        return IsLinux() ? LinuxLogDirectory :
            // Для Windows используем относительный путь от базовой директории приложения
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, WindowsLogDirectory);
    }

    /// <summary>
    /// Получить путь для файла внутренних логов NLog
    /// </summary>
    /// <returns>Полный путь к файлу внутренних логов</returns>
    public static string GetInternalLogPath()
    {
        var logDirectory = GetLogDirectory();
        return Path.Combine(logDirectory, "internal-nlog-log.txt");
    }

    /// <summary>
    /// Создать директорию для логов если она не существует
    /// </summary>
    /// <returns>True если директория была создана или уже существует</returns>
    public static bool EnsureLogDirectoryExists()
    {
        try
        {
            var logDirectory = GetLogDirectory();
            if (string.IsNullOrEmpty(logDirectory))
                return false;

            if (Directory.Exists(logDirectory)) 
                return true;
            
            Directory.CreateDirectory(logDirectory);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Определить является ли текущая ОС Linux/Unix
    /// </summary>
    /// <returns>True если Linux/Unix, false если Windows</returns>
    private static bool IsLinux()
    {
        return Environment.OSVersion.Platform == PlatformID.Unix ||
               Environment.OSVersion.Platform == PlatformID.MacOSX;
    }
}