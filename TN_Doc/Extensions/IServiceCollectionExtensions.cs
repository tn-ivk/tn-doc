using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using TN_Doc.Models.Printer;
using TN_Doc.Services;
using TN.Utils;
using TN_DocGeneral.Services;

namespace TN_Doc.Extensions;

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
	/// Добавление сервиса записи в системный журнал ОС
	/// </summary>
	/// <param name="services">Коллекция сервисов</param>
	/// <remarks>
	/// Windows: использует Windows Event Log
	/// Linux: использует syslog через команду logger
	/// </remarks>
	public static void AddSystemJournal(this IServiceCollection services)
	{
		if (IsWindows)
			services.AddSingleton<ISystemJournalService, WindowsSystemJournalService>();
		else
			services.AddSingleton<ISystemJournalService, LinuxSystemJournalService>();
	}

	/// <summary>
	/// Добавления сервиса взаимодействия с принтерами печати
	/// </summary>
	/// <param name="services">Коллекция сервисов</param>
	public static void AddPrinterService(this IServiceCollection services) => services.AddTransient<IPrinterService, PrinterService>();

	/// <summary>
	/// Добавление сервиса кэширования схемы базы данных
	/// </summary>
	/// <param name="services">Коллекция сервисов</param>
	public static void AddDbSchemaCache(this IServiceCollection services) => services.AddScoped<IDbSchemaCache, DbSchemaCache>();

	/// <summary>
	/// Добавление сервиса кэширования конфигурационных файлов документов
	/// </summary>
	/// <param name="services">Коллекция сервисов</param>
	public static void AddConfigurationCache(this IServiceCollection services) => services.AddSingleton<IConfigurationCacheService, ConfigurationCacheService>();

	/// <summary>
	/// Сбор информации о приложение
	/// </summary>
	/// <param name="services">Коллекция сервисов</param>
	public static void AddAppInfoProvider(this IServiceCollection services)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
		var version = $"{(fileVersionInfo.FileVersion ?? "???")}";
#if DEBUG
        version += $".test-{Guid.NewGuid().ToString().Substring(0,5)}";
#endif
		var appInfoProvider = new AppInfoProvider(version);
		services.AddSingleton(appInfoProvider);
		var logger = LogManager.GetCurrentClassLogger();
		logger.Info($"Запуск приложения: {assembly.GetName().Name} версии: {appInfoProvider.Version}");
	}

	private static bool IsWindows => Environment.OSVersion.Platform != PlatformID.Unix &&
	                                 Environment.OSVersion.Platform != PlatformID.MacOSX;
}