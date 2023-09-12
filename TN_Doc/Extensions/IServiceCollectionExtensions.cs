using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TN_Doc.Models.Printer;
using TN_Doc.Models.Services;

namespace TN_Doc.Extensions
{
    /// <summary>
    /// Класс с методами расширения для конфигурирования колекции используемых сервисов
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавление принтеров для печати докумнетов
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        public static void AddPrinters(this IServiceCollection services)
        {
            if (IsWindows)
            {
                Console.WriteLine("used windows printer");
                services.AddTransient<AbsPrinter>((item)=>new WindowsPrinter());
            }
            else
            {
                Console.WriteLine("used linux printer");
                services.AddTransient<AbsPrinter>((item)=>new LinuxPrinter());
            }
          
        }

        /// <summary>
        /// Добавления сервиса взаимодействия с принтерами печати
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        public static void AddPrinterService(this IServiceCollection services) => services.AddTransient<PrinterService>();

        /// <summary>
        /// Добавление сервиса взаимодействия со справочниками
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="cfg">Провайдер конфигурации приложения</param>
        public static void AddDirectoryService(this IServiceCollection services,IConfiguration cfg)
        {
            services.AddSingleton((provider) =>
            {
                var cfgPath = cfg.GetValue<string>("CfgAppPath");
                var logger = provider.GetRequiredService<ILogger<DirectoryService>>();
                return new DirectoryService(cfgPath, logger);
            });
        }
        
        /// <summary>
        /// Флаг использования операционной системы Windows
        /// </summary>
        /// <returns>
        /// Возвращает true - если система развертывания Windows
        /// </returns>
        private static bool IsWindows => Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;
    }
    
  
}