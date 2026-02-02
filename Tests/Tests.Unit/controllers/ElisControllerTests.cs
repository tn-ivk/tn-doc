using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_Doc.Services;

namespace Tests.Unit.Controllers;

[TestFixture]
public class ElisControllerTests
{
    private Mock<ILogger<ElisController>> _mockLogger;
    private Mock<ISystemJournalService> _mockSystemJournal;
    private ElisController _controller;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ElisController>>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _mockSystemJournal = new Mock<ISystemJournalService>();
        _controller = new ElisController(_mockLogger.Object, _mockSystemJournal.Object);
    }

    /// <summary>
    /// ErrorMessage: при null/empty сообщении - лог не вызывается
    /// </summary>
    [Test]
    [TestCase(null)]
    [TestCase("")]
    public void ErrorMessage_NullOrEmptyMessage_DoesNotCallLogger(string msg)
    {
        // Act
        _controller.ErrorMessage(msg);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    /// <summary>
    /// ErrorMessage: при null/empty сообщении - WriteError не вызывается
    /// </summary>
    [Test]
    [TestCase(null)]
    [TestCase("")]
    public void ErrorMessage_NullOrEmptyMessage_DoesNotCallSystemJournal(string msg)
    {
        // Act
        _controller.ErrorMessage(msg);

        // Assert
        _mockSystemJournal.Verify(
            x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// ErrorMessage: вызывает WriteError с источником ELIS
    /// </summary>
    [Test]
    public void ErrorMessage_ValidMessage_CallsSystemJournalWriteError()
    {
        // Arrange
        var message = "Тестовая ошибка ELIS";

        // Act
        _controller.ErrorMessage(message);

        // Assert
        _mockSystemJournal.Verify(
            x => x.WriteError(message, "ELIS"),
            Times.Once);
    }

    /// <summary>
    /// ErrorMessage: WriteError вызывается с дополненным сообщением при известном паттерне
    /// </summary>
    [Test]
    public void ErrorMessage_KnownPattern_CallsSystemJournalWithEnhancedMessage()
    {
        // Arrange
        var originalMessage = "Неверный сертификат";
        var expectedDescription = "Система не может найти сертификат и использовать его";

        // Act
        _controller.ErrorMessage(originalMessage);

        // Assert
        _mockSystemJournal.Verify(
            x => x.WriteError(
                It.Is<string>(msg => msg.Contains(originalMessage) && msg.Contains(expectedDescription)),
                "ELIS"),
            Times.Once);
    }

    /// <summary>
    /// WarnMessage: не вызывает WriteError (только ошибки пишутся в системный журнал)
    /// </summary>
    [Test]
    public void WarnMessage_ValidMessage_DoesNotCallSystemJournal()
    {
        // Arrange
        var message = "Тестовое предупреждение";

        // Act
        _controller.WarnMessage(message);

        // Assert
        _mockSystemJournal.Verify(
            x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
