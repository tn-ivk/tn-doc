using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using TN_Doc.Extensions;
using TN_Doc.Models.Printer;
using TN_Doc.Models.Services;
using TN.Utils;

namespace Tests.Extensions;

/// <summary>
/// Unit-тесты для <see cref="ServiceCollectionExtensions"/>.
/// </summary>
[TestFixture(TestName = "ServiceCollectionExtensions: набор тестов методов расширения IServiceCollection")]
public class ServiceCollectionExtensionsTests
{
    private IServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
    }

    #region ConfigAppDirectory Tests

    /// <summary>
    /// Проверяет, что ConfigAppDirectory устанавливает текущую директорию в AppContext.BaseDirectory.
    /// </summary>
    [Test]
    public void ConfigAppDirectory_WhenCalled_SetsCurrentDirectory()
    {
        // Arrange
        var expectedDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Act
        _services.ConfigAppDirectory();

        // Assert
        var actualDirectory = Environment.CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        Assert.That(actualDirectory, Is.EqualTo(expectedDirectory));
    }

    #endregion

    #region AddPrinters Tests

    /// <summary>
    /// Проверяет, что на Windows регистрируется WindowsPrinter.
    /// </summary>
    [Test]
    [Platform("Win")]
    public void AddPrinters_OnWindows_RegistersWindowsPrinter()
    {
        // Arrange - добавляем логгер, который требуется для принтеров
        _services.AddLogging();

        // Act
        _services.AddPrinters();
        var provider = _services.BuildServiceProvider();

        // Assert
        var printer = provider.GetService<AbsPrinter>();
        Assert.Multiple(() =>
        {
            Assert.That(printer, Is.Not.Null);
            Assert.That(printer, Is.TypeOf<WindowsPrinter>());
        });
    }

    /// <summary>
    /// Проверяет, что на Linux регистрируется LinuxPrinter.
    /// </summary>
    [Test]
    [Platform("Linux")]
    public void AddPrinters_OnLinux_RegistersLinuxPrinter()
    {
        // Arrange - добавляем логгер, который требуется для принтеров
        _services.AddLogging();

        // Act
        _services.AddPrinters();
        var provider = _services.BuildServiceProvider();

        // Assert
        var printer = provider.GetService<AbsPrinter>();
        Assert.Multiple(() =>
        {
            Assert.That(printer, Is.Not.Null);
            Assert.That(printer, Is.TypeOf<LinuxPrinter>());
        });
    }

    /// <summary>
    /// Проверяет, что AddPrinters регистрирует AbsPrinter как Transient сервис.
    /// </summary>
    [Test]
    public void AddPrinters_WhenCalled_RegistersAbsPrinter()
    {
        // Arrange & Act
        _services.AddPrinters();

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(AbsPrinter));
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
        });

        // Проверяем, что реализация соответствует текущей ОС
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(WindowsPrinter)));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(LinuxPrinter)));
        }
    }

    #endregion

    #region AddPrinterService Tests

    /// <summary>
    /// Проверяет, что AddPrinterService регистрирует PrinterService как Transient сервис.
    /// </summary>
    [Test]
    public void AddPrinterService_WhenCalled_RegistersPrinterService()
    {
        // Arrange & Act
        _services.AddPrinterService();

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(PrinterService));
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
            Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(PrinterService)));
        });
    }

    /// <summary>
    /// Проверяет, что PrinterService можно получить из DI контейнера при наличии зависимостей.
    /// </summary>
    [Test]
    public void AddPrinterService_WhenDependenciesRegistered_CanResolveService()
    {
        // Arrange - добавляем логгер, который требуется для принтеров
        _services.AddLogging();
        _services.AddPrinters();
        _services.AddPrinterService();
        var provider = _services.BuildServiceProvider();

        // Act
        var service = provider.GetService<PrinterService>();

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.TypeOf<PrinterService>());
    }

    #endregion

    #region AddAppInfoProvider Tests

    /// <summary>
    /// Проверяет, что AddAppInfoProvider регистрирует AppInfoProvider как Singleton.
    /// </summary>
    [Test]
    public void AddAppInfoProvider_WhenCalled_RegistersAppInfoProvider()
    {
        // Arrange & Act
        _services.AddAppInfoProvider();

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(AppInfoProvider));
        Assert.Multiple(() =>
        {
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        });
    }

    /// <summary>
    /// Проверяет, что AppInfoProvider содержит непустую версию после регистрации.
    /// </summary>
    [Test]
    public void AddAppInfoProvider_WhenCalled_ProviderHasVersion()
    {
        // Arrange & Act
        _services.AddAppInfoProvider();
        var provider = _services.BuildServiceProvider();

        // Assert
        var appInfoProvider = provider.GetService<AppInfoProvider>();
        Assert.Multiple(() =>
        {
            Assert.That(appInfoProvider, Is.Not.Null);
            Assert.That(appInfoProvider!.Version, Is.Not.Null.And.Not.Empty);
        });
    }

    /// <summary>
    /// Проверяет, что AppInfoProvider возвращает один и тот же экземпляр (Singleton).
    /// </summary>
    [Test]
    public void AddAppInfoProvider_WhenResolvedMultipleTimes_ReturnsSameInstance()
    {
        // Arrange
        _services.AddAppInfoProvider();
        var provider = _services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetService<AppInfoProvider>();
        var instance2 = provider.GetService<AppInfoProvider>();

        // Assert
        Assert.That(instance1, Is.SameAs(instance2));
    }

    #endregion
}
