using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog;

namespace TN_Doc
{
    public class Program
    {
        /// <summary>
        /// Точка входа приложения
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var logger = LogManager.Setup().GetCurrentClassLogger();
            try
            { 
                logger.Info("Запуск приложения");
                CreateHostBuilder(args).Build().Run(); 
            }
            catch (Exception e)
            {
                logger.Error(e, "Приложение останводено в связи с критической ошибкой");
                Console.WriteLine(e);
                var failLogFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, "logs", "startup_fail.log"));
                var directoryPath = failLogFile.DirectoryName;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                using FileStream fs = failLogFile.Create();
                using StreamWriter writer = new StreamWriter(fs);
                writer.Write(e.ToString());
            }
        }

        /// <summary>
        /// Конфигурация билдера хоста приложения
        /// </summary>
        /// <param name="args">Аргументы командной строки</param>
        /// <returns>Билдер хоста приложения</returns>
        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder builder= Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
            return IsHostOsWindows()
                ? builder.UseWindowsService()
                : builder.UseSystemd();
        }
   		
        /// <summary>
        /// Флаг определения операционной системы
        /// </summary>
        /// <returns>
        /// Возвращает true - если ОС является Windows. В других случаях -false.
        /// </returns>
        private static bool IsHostOsWindows() => Environment.OSVersion.Platform != PlatformID.Unix &&
                                             Environment.OSVersion.Platform != PlatformID.MacOSX;
    }
}