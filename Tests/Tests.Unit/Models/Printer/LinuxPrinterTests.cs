using NUnit.Framework;
using TN_Doc.Models.Printer;

namespace Tests.Models.Printer;

/// <summary>
/// Unit-тесты для <see cref="LinuxPrinter"/>.
/// </summary>
[TestFixture(TestName = "LinuxPrinter: набор тестов принтера Linux")]
public class LinuxPrinterTests
{
    #region Constructor Tests

    /// <summary>
    /// Проверяет, что конструктор не выбрасывает исключение при создании экземпляра.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var printer = new LinuxPrinter();
        });
    }

    /// <summary>
    /// Проверяет, что экземпляр создается и не равен null.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_CreatesInstance()
    {
        // Act
        var printer = new LinuxPrinter();

        // Assert
        Assert.That(printer, Is.Not.Null);
        Assert.That(printer, Is.InstanceOf<AbsPrinter>());
    }

    #endregion

    #region GetAvailablePrinters Tests

    /// <summary>
    /// Проверяет, что GetAvailablePrinters возвращает IEnumerable на Linux.
    /// Требует установленной команды lpstat.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    [Explicit("Requires actual printer system (lpstat command)")]
    public void GetAvailablePrinters_WhenCalled_ReturnsEnumerable()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act
        var result = printer.GetAvailablePrinters();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<IEnumerable<string>>());
    }

    /// <summary>
    /// Проверяет, что GetAvailablePrinters корректно обрабатывает пустой вывод lpstat.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    [Explicit("Requires actual printer system (lpstat command)")]
    public void GetAvailablePrinters_WhenNoPrintersAvailable_ReturnsEnumerable()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act
        var result = printer.GetAvailablePrinters();

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region PrintDocAsync Tests

    /// <summary>
    /// Проверяет, что PrintDocAsync возвращает Task без ошибок, когда принтер не найден.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WhenPrinterNotFound_ReturnsWithoutAction()
    {
        // Arrange
        var printer = new LinuxPrinter();
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
    [Platform("Linux,Unix")]
    public void PrintDocAsync_WhenCalled_ReturnsTask()
    {
        // Arrange
        var printer = new LinuxPrinter();
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
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WhenPrinterNameEmpty_ReturnsWithoutAction()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act & Assert
        await printer.PrintDocAsync(string.Empty);
        // Успешное завершение без исключения означает корректную обработку
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync корректно обрабатывает имя с пробелами.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WhenPrinterNameHasSpaces_ReturnsWithoutAction()
    {
        // Arrange
        var printer = new LinuxPrinter();
        const string printerNameWithSpaces = "Printer Name With Spaces";

        // Act
        await printer.PrintDocAsync(printerNameWithSpaces);

        // Assert - не выбросило исключение
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync корректно обрабатывает специальные символы в имени.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WhenPrinterNameHasSpecialChars_ReturnsWithoutAction()
    {
        // Arrange
        var printer = new LinuxPrinter();
        const string printerNameWithSpecialChars = "Printer (Office) #1";

        // Act
        await printer.PrintDocAsync(printerNameWithSpecialChars);

        // Assert - не выбросило исключение
    }

    /// <summary>
    /// Проверяет реальную печать на существующем принтере Linux.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    [Explicit("Requires actual printer system and configured printer")]
    public async Task PrintDocAsync_WhenPrinterExists_CompletesSuccessfully()
    {
        // Arrange
        var printer = new LinuxPrinter();
        var availablePrinters = printer.GetAvailablePrinters().ToList();

        // Skip if no printers available
        if (!availablePrinters.Any())
        {
            Assert.Ignore("No printers available on the system");
            return;
        }

        var firstPrinter = availablePrinters.First();

        // Act & Assert
        await printer.PrintDocAsync(firstPrinter);
        // Успешное завершение означает корректную работу
    }

    #endregion
}
