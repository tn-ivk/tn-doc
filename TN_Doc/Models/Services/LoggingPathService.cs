using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TN_Doc.Models.Services
{
    /// <summary>
    /// Сервис для определения путей логирования в зависимости от операционной системы
    /// </summary>
    public class LoggingPathService
    {
        private const string LinuxLogDirectory = "/var/log/TN_Doc";
        private const string WindowsLogDirectory = "logs";
        private const string ApplicationName = "TN_Doc";
        
        private readonly ILogger<LoggingPathService> _logger;

        /// <summary>
        /// Получить директорию для логов в зависимости от операционной системы
        /// </summary>
        /// <returns>Полный путь к директории логов</returns>
        public static string GetLogDirectory()
        {
            if (IsLinux())
            {
                return LinuxLogDirectory;
            }
            else
            {
                // Для Windows используем относительный путь от базовой директории приложения
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, WindowsLogDirectory);
            }
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
}