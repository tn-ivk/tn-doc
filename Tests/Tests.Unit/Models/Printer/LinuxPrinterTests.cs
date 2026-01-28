using System.ComponentModel;
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

    /// <summary>
    /// Проверяет, что можно создать несколько экземпляров LinuxPrinter.
    /// </summary>
    [Test]
    public void Constructor_MultipleInstances_AllAreIndependent()
    {
        // Act
        var printer1 = new LinuxPrinter();
        var printer2 = new LinuxPrinter();

        // Assert
        Assert.That(printer1, Is.Not.SameAs(printer2));
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

    #region GetAvailablePrinters Negative Tests

    /// <summary>
    /// Проверяет, что GetAvailablePrinters выбрасывает Win32Exception на Windows,
    /// так как команда lpstat недоступна.
    /// </summary>
    [Test]
    [Platform("Win")]
    public void GetAvailablePrinters_OnWindows_ThrowsWin32Exception()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act & Assert
        Assert.Throws<Win32Exception>(() =>
        {
            // Перечисление yield return требует материализации
            var result = printer.GetAvailablePrinters().ToList();
        });
    }

    /// <summary>
    /// Проверяет, что GetAvailablePrinters возвращает IEnumerable (ленивая коллекция).
    /// Исключение произойдёт только при перечислении.
    /// </summary>
    [Test]
    public void GetAvailablePrinters_ReturnsLazyEnumerable()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act - вызов метода НЕ должен выбросить исключение (yield return)
        IEnumerable<string>? result = null;
        Assert.DoesNotThrow(() =>
        {
            result = printer.GetAvailablePrinters();
        });

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

    #region PrintDocAsync Negative Tests

    /// <summary>
    /// Проверяет, что PrintDocAsync с null именем принтера выбрасывает исключение на Linux.
    /// Текущая реализация вызывает Contains(null), что выбросит ArgumentNullException при перечислении.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public void PrintDocAsync_WithNullPrinterName_ThrowsException()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act & Assert
        // На Linux lpstat вернёт коллекцию, Contains(null) выбросит ArgumentNullException
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await printer.PrintDocAsync(null!);
        });
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync на Windows выбрасывает Win32Exception,
    /// так как команда lpstat недоступна.
    /// </summary>
    [Test]
    [Platform("Win")]
    public void PrintDocAsync_OnWindows_ThrowsWin32Exception()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act & Assert
        Assert.ThrowsAsync<Win32Exception>(async () =>
        {
            await printer.PrintDocAsync("TestPrinter");
        });
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync возвращает Task даже для несуществующего принтера на Windows.
    /// Task выбросит исключение при await.
    /// </summary>
    [Test]
    [Platform("Win")]
    public void PrintDocAsync_OnWindows_ReturnsTaskThatThrows()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act
        var task = printer.PrintDocAsync("NonExistentPrinter");

        // Assert
        Assert.That(task, Is.Not.Null);
        Assert.That(task, Is.InstanceOf<Task>());
        Assert.ThrowsAsync<Win32Exception>(async () => await task);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync можно вызвать с пустым именем принтера.
    /// Принтер не будет найден в списке, метод завершится без печати.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WithEmptyPrinterName_CompletesWithoutPrinting()
    {
        // Arrange
        var printer = new LinuxPrinter();

        // Act & Assert - пустое имя не будет найдено в списке принтеров
        await printer.PrintDocAsync(string.Empty);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync с очень длинным именем принтера обрабатывается корректно.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WithVeryLongPrinterName_HandlesGracefully()
    {
        // Arrange
        var printer = new LinuxPrinter();
        var longName = new string('A', 1000);

        // Act & Assert - длинное имя не будет найдено в списке принтеров
        await printer.PrintDocAsync(longName);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync с Unicode символами в имени принтера работает корректно.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WithUnicodePrinterName_HandlesGracefully()
    {
        // Arrange
        var printer = new LinuxPrinter();
        const string unicodeName = "Принтер_Офис_中文";

        // Act & Assert - Unicode имя не будет найдено в списке принтеров
        await printer.PrintDocAsync(unicodeName);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync с пробелами в имени принтера работает корректно.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WithSpacesInPrinterName_HandlesGracefully()
    {
        // Arrange
        var printer = new LinuxPrinter();
        const string nameWithSpaces = "Printer With Spaces";

        // Act & Assert
        await printer.PrintDocAsync(nameWithSpaces);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync можно вызвать несколько раз параллельно на Linux.
    /// </summary>
    [Test]
    [Platform("Linux,Unix")]
    public async Task PrintDocAsync_WhenCalledConcurrently_HandlesGracefully()
    {
        // Arrange
        var printer = new LinuxPrinter();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(printer.PrintDocAsync($"NonExistentPrinter_{i}"));
        }

        // Assert - все задачи должны завершиться без исключений
        await Task.WhenAll(tasks);
    }

    #endregion
}
