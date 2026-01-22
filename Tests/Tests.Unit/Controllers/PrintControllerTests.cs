using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_Doc.Models.Printer;
using TN_Doc.Models.Services;

namespace Tests.Controllers;

/// <summary>
/// Набор тестов для контроллера печати документов PrintController
/// </summary>
[TestFixture(TestName = "PrintController - тесты контроллера печати")]
public class PrintControllerTests
{
    private Mock<AbsPrinter> _mockAbsPrinter = null!;
    private Mock<ILogger<PrintController>> _mockLogger = null!;
    private PrinterService _printerService = null!;
    private PrintController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockAbsPrinter = new Mock<AbsPrinter>();
        _mockLogger = new Mock<ILogger<PrintController>>();
        _printerService = new PrinterService(_mockAbsPrinter.Object);
        _controller = new PrintController(_printerService, _mockLogger.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Проверяет, что конструктор выбрасывает ArgumentNullException при передаче null в качестве PrinterService
    /// </summary>
    [Test]
    public void Constructor_WhenNullService_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PrintController(null!, _mockLogger.Object));

        Assert.That(exception!.ParamName, Is.EqualTo("printService"));
    }

    /// <summary>
    /// Проверяет, что конструктор не выбрасывает исключение при передаче null в качестве ILogger
    /// (logger не проверяется на null в конструкторе)
    /// </summary>
    [Test]
    public void Constructor_WhenNullLogger_DoesNotThrow()
    {
        // Arrange & Act & Assert
        Assert.DoesNotThrow(() =>
            new PrintController(_printerService, null!));
    }

    /// <summary>
    /// Проверяет, что конструктор не выбрасывает исключение при передаче валидных параметров
    /// </summary>
    [Test]
    public void Constructor_WhenValidParameters_DoesNotThrow()
    {
        // Arrange & Act & Assert
        Assert.DoesNotThrow(() =>
            new PrintController(_printerService, _mockLogger.Object));
    }

    #endregion

    #region GetListPrinters Tests

    /// <summary>
    /// Проверяет, что метод GetListPrinters возвращает OkObjectResult со списком принтеров,
    /// когда принтеры существуют в системе
    /// </summary>
    [Test]
    public void GetListPrinters_WhenPrintersExist_ReturnsOkWithList()
    {
        // Arrange
        var expectedPrinters = new[] { "Printer1", "Printer2", "Printer3" };
        _mockAbsPrinter
            .Setup(x => x.GetAvailablePrinters())
            .Returns(expectedPrinters);

        // Act
        var result = _controller.GetListPrinters();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.InstanceOf<string[]>());
        var printers = (string[])okResult.Value!;
        Assert.That(printers, Is.EqualTo(expectedPrinters));
    }

    /// <summary>
    /// Проверяет, что метод GetListPrinters возвращает OkObjectResult с пустым массивом,
    /// когда в системе нет установленных принтеров
    /// </summary>
    [Test]
    public void GetListPrinters_WhenNoPrinters_ReturnsOkWithEmptyArray()
    {
        // Arrange
        var expectedPrinters = Array.Empty<string>();
        _mockAbsPrinter
            .Setup(x => x.GetAvailablePrinters())
            .Returns(expectedPrinters);

        // Act
        var result = _controller.GetListPrinters();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.InstanceOf<string[]>());
        var printers = (string[])okResult.Value!;
        Assert.That(printers, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что метод GetListPrinters возвращает StatusCode 500,
    /// когда сервис выбрасывает исключение
    /// </summary>
    [Test]
    public void GetListPrinters_WhenServiceThrows_ReturnsStatusCode500()
    {
        // Arrange
        _mockAbsPrinter
            .Setup(x => x.GetAvailablePrinters())
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        var result = _controller.GetListPrinters();

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        Assert.That(objectResult.Value, Is.EqualTo("Произошла ошибка при получении списка принтеров"));
    }

    #endregion

    #region PrintDoc Tests

    /// <summary>
    /// Проверяет, что метод PrintDoc возвращает OkResult при успешной печати
    /// </summary>
    [Test]
    public async Task PrintDoc_WhenValidPrinter_ReturnsOk()
    {
        // Arrange
        const string printerName = "TestPrinter";
        _mockAbsPrinter
            .Setup(x => x.PrintDocAsync(printerName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PrintDoc(printerName);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    /// <summary>
    /// Проверяет, что метод PrintDoc вызывает метод PrintDocAsync сервиса с правильным именем принтера
    /// </summary>
    [Test]
    public async Task PrintDoc_WhenCalled_CallsServicePrintDocAsync()
    {
        // Arrange
        const string printerName = "TestPrinter";
        _mockAbsPrinter
            .Setup(x => x.PrintDocAsync(printerName))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.PrintDoc(printerName);

        // Assert
        _mockAbsPrinter.Verify(x => x.PrintDocAsync(printerName), Times.Once);
    }

    #endregion
}
