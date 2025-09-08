using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_Doc.Models.Services;

namespace Tests.Controllers;

/// <summary>
/// Набор тестов для PrintController
/// </summary>
[TestFixture]
public class PrintControllerTests
{
    private Mock<IPrinterService> _mockPrinterService;
    private Mock<ILogger<PrintController>> _mockLogger;
    private PrintController _controller;

    [SetUp]
    public void Setup()
    {
        _mockPrinterService = new Mock<IPrinterService>();
        _mockLogger = new Mock<ILogger<PrintController>>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        
        _controller = new PrintController(_mockPrinterService.Object, _mockLogger.Object);
    }

    /// <summary>
    /// PrintController: конструктор выбрасывает ArgumentNullException при передаче null PrinterService
    /// </summary>
    [Test]
    public void Constructor_NullPrinterService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new PrintController(null, _mockLogger.Object));
        
        Assert.That(exception.ParamName, Is.EqualTo("printService"));
        Assert.That(exception.Message, Does.Contain("Отсутствует сервис взаимодействия с принтером"));
    }

    /// <summary>
    /// GetListPrinters: успешный сценарий — Ok(printers[]), логирование списка при наличии
    /// </summary>
    [Test]
    public void GetListPrinters_HasPrinters_ReturnsOkWithPrintersAndLogsTrace()
    {
        // Arrange
        var expectedPrinters = new[] { "Printer1", "Printer2", "HP LaserJet" };
        _mockPrinterService.Setup(x => x.GetPrinters()).Returns(expectedPrinters);

        // Act
        var result = _controller.GetListPrinters();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.Value, Is.EqualTo(expectedPrinters));

        // Проверяем логирование получения списка принтеров
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Попытка получения списка принтеров")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Проверяем логирование списка принтеров
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Список принтеров:") &&
                                              expectedPrinters.All(p => v.ToString().Contains(p))),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// GetListPrinters: пустой список принтеров — возвращает Ok с пустым массивом и логирует предупреждение
    /// </summary>
    [Test]
    public void GetListPrinters_NoPrinters_ReturnsOkWithEmptyArrayAndLogsWarning()
    {
        // Arrange
        var emptyPrinters = Array.Empty<string>();
        _mockPrinterService.Setup(x => x.GetPrinters()).Returns(emptyPrinters);

        // Act
        var result = _controller.GetListPrinters();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var resultArray = okResult.Value as string[];
        Assert.That(resultArray, Is.Not.Null);
        Assert.That(resultArray.Length, Is.EqualTo(0));

        // Проверяем логирование предупреждения об отсутствии принтеров
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("В системе отсутствуют установленные принтеры")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// GetListPrinters: сценарий исключения из сервиса — StatusCode(500, ...)
    /// </summary>
    [Test]
    public void GetListPrinters_ServiceThrowsException_ReturnsStatusCode500()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Printer service error");
        _mockPrinterService.Setup(x => x.GetPrinters()).Throws(expectedException);

        // Act
        var result = _controller.GetListPrinters();

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        Assert.That(objectResult.Value, Is.EqualTo("Произошла ошибка при получении списка принтеров"));

        // Проверяем логирование ошибки
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ошибка при получении списка принтеров") &&
                                              v.ToString().Contains(expectedException.Message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// GetListPrinters: проверяет вызов GetPrinters() на сервисе
    /// </summary>
    [Test]
    public void GetListPrinters_CallsGetPrintersOnService()
    {
        // Arrange
        _mockPrinterService.Setup(x => x.GetPrinters()).Returns(new[] { "TestPrinter" });

        // Act
        _controller.GetListPrinters();

        // Assert
        _mockPrinterService.Verify(x => x.GetPrinters(), Times.Once);
    }

    /// <summary>
    /// PrintDoc: вызывает _service.PrintDocAsync(printerName) и возвращает Ok()
    /// </summary>
    [Test]
    public async Task PrintDoc_ValidPrinterName_CallsServiceAndReturnsOk()
    {
        // Arrange
        const string printerName = "HP LaserJet Pro";
        _mockPrinterService.Setup(x => x.PrintDocAsync(printerName)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PrintDoc(printerName);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        
        _mockPrinterService.Verify(x => x.PrintDocAsync(printerName), Times.Once);

        // Проверяем логирование начала печати
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Печать документа на принтере: {printerName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// PrintDoc: обрабатывает null и пустое имя принтера
    /// </summary>
    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task PrintDoc_NullOrEmptyPrinterName_CallsServiceAndReturnsOk(string printerName)
    {
        // Arrange
        _mockPrinterService.Setup(x => x.PrintDocAsync(printerName)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PrintDoc(printerName);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        _mockPrinterService.Verify(x => x.PrintDocAsync(printerName), Times.Once);
    }

    /// <summary>
    /// PrintDoc: при исключении в сервисе возвращает StatusCode(500)
    /// </summary>
    [Test]
    public async Task PrintDoc_ServiceThrowsException_ReturnsStatusCode500()
    {
        // Arrange
        const string printerName = "TestPrinter";
        var expectedException = new InvalidOperationException("Print service error");
        _mockPrinterService.Setup(x => x.PrintDocAsync(printerName)).ThrowsAsync(expectedException);

        // Act
        var result = await _controller.PrintDoc(printerName);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        Assert.That(objectResult.Value, Is.EqualTo("Произошла ошибка при печати документа"));
        _mockPrinterService.Verify(x => x.PrintDocAsync(printerName), Times.Once);
    }

    /// <summary>
    /// PrintDoc: проверяет корректную обработку различных имен принтеров
    /// </summary>
    [Test]
    [TestCase("HP LaserJet Pro M404n")]
    [TestCase("Canon PIXMA TS3350")]
    [TestCase("Brother MFC-L3770CDW")]
    [TestCase("Microsoft Print to PDF")]
    public async Task PrintDoc_VariousPrinterNames_CallsServiceCorrectly(string printerName)
    {
        // Arrange
        _mockPrinterService.Setup(x => x.PrintDocAsync(printerName)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PrintDoc(printerName);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        _mockPrinterService.Verify(x => x.PrintDocAsync(printerName), Times.Once);
    }

    /// <summary>
    /// PrintDoc: проверяет асинхронность метода
    /// </summary>
    [Test]
    public async Task PrintDoc_IsAsync_CompletesAsynchronously()
    {
        // Arrange
        const string printerName = "AsyncTestPrinter";
        var tcs = new TaskCompletionSource<bool>();
        _mockPrinterService.Setup(x => x.PrintDocAsync(printerName))
            .Returns(tcs.Task.ContinueWith(_ => { }));

        // Act
        var printTask = _controller.PrintDoc(printerName);
        Assert.That(printTask.IsCompleted, Is.False, "Метод должен быть асинхронным");

        // Complete the task
        tcs.SetResult(true);
        var result = await printTask;

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
    }
}