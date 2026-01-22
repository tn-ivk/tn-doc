using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_Doc.Models;

namespace Tests.Controllers;

/// <summary>
/// Модульные тесты для <see cref="ClientLogController"/>.
/// Проверяет корректность обработки клиентских логов и валидации входных данных.
/// </summary>
[TestFixture]
public class ClientLogControllerTests
{
    private Mock<ILogger<ClientLogController>> _loggerMock = null!;
    private ClientLogController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ClientLogController>>();
        _controller = new ClientLogController(_loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Контроллер не реализует IDisposable, очистка не требуется
    }

    #region Constructor Tests

    /// <summary>
    /// Проверяет, что конструктор выбрасывает ArgumentNullException при передаче null в качестве логгера.
    /// </summary>
    [Test]
    public void Constructor_WhenNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ClientLogController(null!));
        Assert.That(exception!.ParamName, Is.EqualTo("logger"));
    }

    #endregion

    #region Valid Log Level Tests

    /// <summary>
    /// Проверяет, что уровень логирования "trace" корректно обрабатывается и возвращает OkResult.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenValidTrace_ReturnsOk()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "trace", Message = "Test trace message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        VerifyLogWasCalled(LogLevel.Trace, "Test trace message");
    }

    /// <summary>
    /// Проверяет, что уровень логирования "debug" корректно обрабатывается и возвращает OkResult.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenValidDebug_ReturnsOk()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "debug", Message = "Test debug message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        VerifyLogWasCalled(LogLevel.Debug, "Test debug message");
    }

    /// <summary>
    /// Проверяет, что уровень логирования "info" корректно обрабатывается и возвращает OkResult.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenValidInfo_ReturnsOk()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "info", Message = "Test info message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        VerifyLogWasCalled(LogLevel.Information, "Test info message");
    }

    /// <summary>
    /// Проверяет, что уровень логирования "warn" корректно обрабатывается и возвращает OkResult.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenValidWarn_ReturnsOk()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "warn", Message = "Test warn message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        VerifyLogWasCalled(LogLevel.Warning, "Test warn message");
    }

    /// <summary>
    /// Проверяет, что уровень логирования "warning" корректно обрабатывается и возвращает OkResult.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenValidWarning_ReturnsOk()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "warning", Message = "Test warning message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        VerifyLogWasCalled(LogLevel.Warning, "Test warning message");
    }

    /// <summary>
    /// Проверяет, что уровень логирования "error" корректно обрабатывается и возвращает OkResult.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenValidError_ReturnsOk()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "error", Message = "Test error message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        VerifyLogWasCalled(LogLevel.Error, "Test error message");
    }

    /// <summary>
    /// Проверяет, что уровень логирования "fatal" корректно обрабатывается как Error и возвращает OkResult.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenValidFatal_ReturnsOk()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "fatal", Message = "Test fatal message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        VerifyLogWasCalled(LogLevel.Error, "Test fatal message");
    }

    /// <summary>
    /// Проверяет, что уровень логирования "critical" корректно обрабатывается как Error и возвращает OkResult.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenValidCritical_ReturnsOk()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "critical", Message = "Test critical message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        VerifyLogWasCalled(LogLevel.Error, "Test critical message");
    }

    #endregion

    #region Validation Error Tests

    /// <summary>
    /// Проверяет, что при передаче null в качестве объекта лога возвращается BadRequest.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenNullLog_ReturnsBadRequest()
    {
        // Act
        var result = _controller.LogClientMessage(null!);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult.Value, Is.EqualTo("Log object is null"));
        VerifyLogWasCalled(LogLevel.Error, "Ошибка лога клиентской части: объект log равен null");
    }

    /// <summary>
    /// Проверяет, что при передаче null в качестве уровня логирования возвращается BadRequest.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenNullLevel_ReturnsBadRequest()
    {
        // Arrange
        var log = new ClientLogMessage { Level = null!, Message = "Test message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult.Value, Is.EqualTo("Log level is required"));
        VerifyLogWasCalled(LogLevel.Error, "Ошибка лога клиентской части: не указан уровень логирования");
    }

    /// <summary>
    /// Проверяет, что при передаче пустого сообщения возвращается BadRequest.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "info", Message = "" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult.Value, Is.EqualTo("Log message is required"));
        VerifyLogWasCalled(LogLevel.Error, "Ошибка лога клиентской части: пустое сообщение");
    }

    /// <summary>
    /// Проверяет, что при передаче неизвестного уровня логирования возвращается BadRequest.
    /// </summary>
    [Test]
    public void LogClientMessage_WhenUnknownLevel_ReturnsBadRequest()
    {
        // Arrange
        var log = new ClientLogMessage { Level = "unknown", Message = "Test message" };

        // Act
        var result = _controller.LogClientMessage(log);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult.Value, Is.EqualTo("Unknown log level: unknown"));
        VerifyLogWasCalled(LogLevel.Error, "Неизвестный уровень логирования от клиента: 'unknown'. Сообщение: Test message");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Проверяет, что логгер был вызван с указанным уровнем и сообщением.
    /// </summary>
    /// <param name="expectedLevel">Ожидаемый уровень логирования.</param>
    /// <param name="expectedMessage">Ожидаемое сообщение.</param>
    private void VerifyLogWasCalled(LogLevel expectedLevel, string expectedMessage)
    {
        _loggerMock.Verify(
            x => x.Log(
                expectedLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
