using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using TN_Doc.Models.Printer;
using TN_Doc.Models.Services;
using TN.Utils;

namespace TN_Doc.Extensions
{
	/// <summary>
	/// Класс с методами расширения для конфигурирования колекции используемых сервисов
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Конфигурирование директории проекта. Директория проекта выставляется в папку c исполняемым файлом
		/// </summary>
		/// <param name="_">Коллекция сервисов</param>
		public static void ConfigAppDirectory(this IServiceCollection _) => Environment.CurrentDirectory = AppContext.BaseDirectory;

		/// <summary>
		/// Добавление принтеров для печати докумнетов
		/// </summary>
		/// <param name="services">Коллекция сервисов</param>
		public static void AddPrinters(this IServiceCollection services)
		{
			if (IsWindows)
				services.AddTransient<AbsPrinter, WindowsPrinter>();
			else
				services.AddTransient<AbsPrinter, LinuxPrinter>();
		}

		/// <summary>
		/// Добавления сервиса взаимодействия с принтерами печати
		/// </summary>
		/// <param name="services">Коллекция сервисов</param>
		public static void AddPrinterService(this IServiceCollection services) => services.AddTransient<PrinterService>();

		/// <summary>
		/// Сбор информации о приложение
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static void AddAppInfoProvider(this IServiceCollection services)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo vi = FileVersionInfo.GetVersionInfo(assembly.Location);
			string version = $"{(vi.FileVersion ?? "???")}";
#if DEBUG
            version += $".test-{Guid.NewGuid().ToString().Substring(0,5)}";
#endif
			AppInfoProvider pr = new(version);
			services.AddSingleton(pr);
			var logger = LogManager.GetCurrentClassLogger();
			logger.Info($"Запуск приложения: {assembly.GetName().Name} версии: {pr.Version}");
		}

		/// <summary>
		/// Флаг использования операционной системы Windows
		/// </summary>
		/// <returns>
		/// Возвращает true - если система развертывания Windows
		/// </returns>
		private static bool IsWindows => Environment.OSVersion.Platform != PlatformID.Unix &&
		                                 Environment.OSVersion.Platform != PlatformID.MacOSX;
	}
}