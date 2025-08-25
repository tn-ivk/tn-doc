using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_Doc.Models;


namespace Tests.Controllers
{
    [TestFixture]
    public class ClientLogControllerTests
    {
        private Mock<ILogger<ClientLogController>> _mockLogger;
        private ClientLogController _controller;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ClientLogController>>();
            _controller = new ClientLogController(_mockLogger.Object);
        }

        /// <summary>
        /// Должен вернуть 400 и залогировать ошибку, когда входной объект лога равен null.
        /// </summary>
        [Test]
        public void LogClientMessage_NullLog_Returns400AndLogsError()
        {
            // Act
            var result = _controller.LogClientMessage(null);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo("Log object is null"));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("объект log равен null")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Должен вернуть 400 и залогировать ошибку, если уровень логирования пустой/пробельный.
        /// </summary>
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void LogClientMessage_EmptyLevel_Returns400AndLogsError(string level)
        {
            // Arrange
            var log = new ClientLogMessage { Level = level, Message = "Test message" };

            // Act
            var result = _controller.LogClientMessage(log);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo("Log level is required"));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("уровень логирования")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Должен вернуть 400 и залогировать ошибку, если сообщение пустое/пробельное.
        /// </summary>
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void LogClientMessage_EmptyMessage_Returns400AndLogsError(string message)
        {
            // Arrange
            var log = new ClientLogMessage { Level = "info", Message = message };

            // Act
            var result = _controller.LogClientMessage(log);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo("Log message is required"));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("пустое сообщение")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Должен вернуть 200 и залогировать Trace при уровне trace (любые регистры/пробелы).
        /// </summary>
        [Test]
        [TestCase("trace", LogLevel.Trace)]
        [TestCase("TRACE", LogLevel.Trace)]
        [TestCase("  trace  ", LogLevel.Trace)]
        public void LogClientMessage_TraceLevel_Returns200AndLogsTrace(string level, LogLevel expectedLogLevel)
        {
            // Arrange
            var log = new ClientLogMessage { Level = level, Message = "Test trace message" };

            // Act
            var result = _controller.LogClientMessage(log);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());

            _mockLogger.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == "Test trace message"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Должен вернуть 200 и залогировать Debug при уровне debug (любые регистры/пробелы).
        /// </summary>
        [Test]
        [TestCase("debug", LogLevel.Debug)]
        [TestCase("DEBUG", LogLevel.Debug)]
        [TestCase("  debug  ", LogLevel.Debug)]
        public void LogClientMessage_DebugLevel_Returns200AndLogsDebug(string level, LogLevel expectedLogLevel)
        {
            // Arrange
            var log = new ClientLogMessage { Level = level, Message = "Test debug message" };

            // Act
            var result = _controller.LogClientMessage(log);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());

            _mockLogger.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == "Test debug message"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Должен вернуть 200 и залогировать Information при уровне info (любые регистры/пробелы).
        /// </summary>
        [Test]
        [TestCase("info", LogLevel.Information)]
        [TestCase("INFO", LogLevel.Information)]
        [TestCase("  info  ", LogLevel.Information)]
        public void LogClientMessage_InfoLevel_Returns200AndLogsInformation(string level, LogLevel expectedLogLevel)
        {
            // Arrange
            var log = new ClientLogMessage { Level = level, Message = "Test info message" };

            // Act
            var result = _controller.LogClientMessage(log);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());

            _mockLogger.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == "Test info message"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Должен вернуть 200 и залогировать Warning при уровне warn/warning (любые регистры/пробелы).
        /// </summary>
        [Test]
        [TestCase("warn", LogLevel.Warning)]
        [TestCase("warning", LogLevel.Warning)]
        [TestCase("WARN", LogLevel.Warning)]
        [TestCase("WARNING", LogLevel.Warning)]
        [TestCase("  warn  ", LogLevel.Warning)]
        [TestCase("  warning  ", LogLevel.Warning)]
        public void LogClientMessage_WarningLevel_Returns200AndLogsWarning(string level, LogLevel expectedLogLevel)
        {
            // Arrange
            var log = new ClientLogMessage { Level = level, Message = "Test warning message" };

            // Act
            var result = _controller.LogClientMessage(log);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());

            _mockLogger.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == "Test warning message"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Должен вернуть 200 и залогировать Error при уровне error/fatal/critical (любые регистры/пробелы).
        /// </summary>
        [Test]
        [TestCase("error", LogLevel.Error)]
        [TestCase("fatal", LogLevel.Error)]
        [TestCase("critical", LogLevel.Error)]
        [TestCase("ERROR", LogLevel.Error)]
        [TestCase("FATAL", LogLevel.Error)]
        [TestCase("CRITICAL", LogLevel.Error)]
        [TestCase("  error  ", LogLevel.Error)]
        [TestCase("  fatal  ", LogLevel.Error)]
        [TestCase("  critical  ", LogLevel.Error)]
        public void LogClientMessage_ErrorLevel_Returns200AndLogsError(string level, LogLevel expectedLogLevel)
        {
            // Arrange
            var log = new ClientLogMessage { Level = level, Message = "Test error message" };

            // Act
            var result = _controller.LogClientMessage(log);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());

            _mockLogger.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == "Test error message"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Должен вернуть 400 и залогировать Error при неизвестном уровне логирования.
        /// </summary>
        [Test]
        [TestCase("unknown")]
        [TestCase("invalid")]
        [TestCase("badlevel")]
        public void LogClientMessage_UnknownLevel_Returns400AndLogsError(string level)
        {
            // Arrange
            var log = new ClientLogMessage { Level = level, Message = "Test message" };

            // Act
            var result = _controller.LogClientMessage(log);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo($"Unknown log level: {level}"));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}


