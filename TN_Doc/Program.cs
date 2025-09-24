using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog;
using TN_Doc.Services;

namespace TN_Doc;

public class Program
{
    /// <summary>
    /// Точка входа приложения
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        LogManager.Configuration.Variables["logDirectory"] = LoggingPathService.GetLogDirectory();
        var logger = LogManager.GetCurrentClassLogger();
        try
        {
            logger.Info("Инициализация приложения");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            logger.Error($"Приложение завершено в связи с критической ошибкой: {ex.Message}");
        }
        finally
        {
            logger.Info("Стоп приложения");
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