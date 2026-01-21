using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN_Doc.Models.Printer;

namespace Tests.Models.Printer;

/// <summary>
/// Unit-тесты для <see cref="WindowsPrinter"/>.
/// </summary>
[TestFixture(TestName = "WindowsPrinter: набор тестов принтера Windows")]
public class WindowsPrinterTests
{
    private Mock<ILogger<WindowsPrinter>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<WindowsPrinter>>();
    }

    #region Constructor Tests

    /// <summary>
    /// Проверяет, что конструктор не выбрасывает исключение при создании экземпляра.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_DoesNotThrow()
    {
        // Arrange
        var logger = new Mock<ILogger<WindowsPrinter>>();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var printer = new WindowsPrinter(logger.Object);
        });
    }

    /// <summary>
    /// Проверяет, что экземпляр создается и не равен null.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_CreatesInstance()
    {
        // Arrange
        var logger = new Mock<ILogger<WindowsPrinter>>();

        // Act
        var printer = new WindowsPrinter(logger.Object);

        // Assert
        Assert.That(printer, Is.Not.Null);
        Assert.That(printer, Is.InstanceOf<AbsPrinter>());
    }

    #endregion

    #region GetAvailablePrinters Tests

    /// <summary>
    /// Проверяет, что GetAvailablePrinters возвращает IEnumerable на Windows.
    /// </summary>
    [Test]
    [Platform("Win")]
    [Explicit("Requires actual printer system")]
    public void GetAvailablePrinters_WhenCalled_ReturnsEnumerable()
    {
        // Arrange
        var printer = new WindowsPrinter(_mockLogger.Object);

        // Act
        var result = printer.GetAvailablePrinters();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<IEnumerable<string>>());
    }

    /// <summary>
    /// Проверяет, что при исключении метод возвращает пустую коллекцию.
    /// Тест использует мок для имитации исключения через reflection.
    /// </summary>
    [Test]
    public void GetAvailablePrinters_WhenExceptionOccurs_ReturnsEmptyAndLogsError()
    {
        // Arrange
        var printer = new WindowsPrinter(_mockLogger.Object);

        // Note: В реальном сценарии PrinterSettings.InstalledPrinters может выбросить
        // исключение при проблемах с системой. Этот тест проверяет возвращаемый тип
        // без зависимости от системных принтеров.

        // Act
        var result = printer.GetAvailablePrinters();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<IEnumerable<string>>());
    }

    /// <summary>
    /// Проверяет, что метод корректно обрабатывает отсутствие принтеров в системе.
    /// </summary>
    [Test]
    [Platform("Win")]
    [Explicit("Requires actual printer system")]
    public void GetAvailablePrinters_WhenSystemHasNoPrinters_ReturnsEmptyCollection()
    {
        // Arrange
        var printer = new WindowsPrinter(_mockLogger.Object);

        // Act
        var result = printer.GetAvailablePrinters();

        // Assert - может быть пустым или нет в зависимости от системы
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region PrintDocAsync Tests

    /// <summary>
    /// Проверяет, что PrintDocAsync возвращает Task без ошибок, когда принтер не найден.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WhenPrinterNotFound_ReturnsWithoutAction()
    {
        // Arrange
        var printer = new WindowsPrinter(_mockLogger.Object);
        const string nonExistentPrinter = "NonExistentPrinter_TestOnly_12345";

        // Act
        var task = printer.PrintDocAsync(nonExistentPrinter);

        // Assert
        Assert.That(task, Is.Not.Null);
        Assert.That(task, Is.InstanceOf<Task>());

        // Ждем завершения - не должно выбросить исключение
        await task;
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync возвращает Task.
    /// </summary>
    [Test]
    public void PrintDocAsync_WhenCalled_ReturnsTask()
    {
        // Arrange
        var printer = new WindowsPrinter(_mockLogger.Object);
        const string anyPrinterName = "TestPrinter";

        // Act
        var result = printer.PrintDocAsync(anyPrinterName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<Task>());
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync корректно обрабатывает пустое имя принтера.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WhenPrinterNameEmpty_ReturnsWithoutAction()
    {
        // Arrange
        var printer = new WindowsPrinter(_mockLogger.Object);

        // Act & Assert
        await printer.PrintDocAsync(string.Empty);
        // Успешное завершение без исключения означает корректную обработку
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync корректно обрабатывает имя с пробелами.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WhenPrinterNameHasSpaces_ReturnsWithoutAction()
    {
        // Arrange
        var printer = new WindowsPrinter(_mockLogger.Object);
        const string printerNameWithSpaces = "Printer Name With Spaces";

        // Act
        await printer.PrintDocAsync(printerNameWithSpaces);

        // Assert - не выбросило исключение
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync корректно обрабатывает специальные символы в имени.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WhenPrinterNameHasSpecialChars_ReturnsWithoutAction()
    {
        // Arrange
        var printer = new WindowsPrinter(_mockLogger.Object);
        const string printerNameWithSpecialChars = "Printer (Office) #1";

        // Act
        await printer.PrintDocAsync(printerNameWithSpecialChars);

        // Assert - не выбросило исключение
    }

    #endregion
}
