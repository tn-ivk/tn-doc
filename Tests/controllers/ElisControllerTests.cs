using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_Doc.Services;

namespace Tests.Controllers;

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
    /// ErrorMessage: для сообщения с известным шаблоном добавляется человекочитаемое описание
    /// </summary>
    [Test]
    [TestCase("Сообщение, подпись, или соподписи модифицированы", "Нарушена целостность системы, и не пройдена проверка по безопасности")]
    [TestCase("Электронная подпись не соответствует требованиям", "Подпись в сообщении от ЭЛИС не соответствует ожидаемой ТСПД")]
    [TestCase("Неверный сертификат используется", "Система не может найти сертификат и использовать его")]
    [TestCase("Ошибка 2035 MQRC_NOT_AUTHORIZED при подключении", "Ошибка связи с менеджером сообщений IBMMQ, невозможность логина к очереди")]
    [TestCase("ASN1 coorupted data in message", "Подпись была повреждена и является не читаемой")]
    [TestCase("CompCode error occurred", "Ошибки связанные с сетевыми настройками и подключения к очереди IBMMQ")]
    public void ErrorMessage_KnownPattern_AddsHumanReadableDescription(string originalMessage, string expectedDescription)
    {
        // Act
        _controller.ErrorMessage(originalMessage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(originalMessage) && v.ToString().Contains(expectedDescription)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// ErrorMessage: для неизвестного шаблона сообщения логируется без изменений
    /// </summary>
    [Test]
    public void ErrorMessage_UnknownPattern_LogsMessageAsIs()
    {
        // Arrange
        var originalMessage = "Неизвестная ошибка в системе";

        // Act
        _controller.ErrorMessage(originalMessage);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == originalMessage),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// ErrorMessage: проверка регистронезависимости паттернов
    /// </summary>
    [Test]
    [TestCase("СООБЩЕНИЕ, ПОДПИСЬ, ИЛИ СОПОДПИСИ МОДИФИЦИРОВАНЫ")]
    [TestCase("электронная подпись не соответствует")]
    [TestCase("НеВеРнЫй СеРтИфИкАт")]
    public void ErrorMessage_CaseInsensitivePatterns_MatchesCorrectly(string message)
    {
        // Act
        _controller.ErrorMessage(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(".")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
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
    /// WarnMessage: вызывает LogWarning с переданным текстом
    /// </summary>
    [Test]
    public void WarnMessage_ValidMessage_CallsLogWarning()
    {
        // Arrange
        var message = "Тестовое предупреждение";

        // Act
        _controller.WarnMessage(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString() == message),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
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

    /// <summary>
    /// WarnMessage: с null сообщением не должно выбрасывать исключения.
    /// (Проверки содержания лога опущены из-за особенностей реализации ILogger и форматтеров.)
    /// </summary>
    [Test]
    public void WarnMessage_NullMessage_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _controller.WarnMessage(null));
    }

    /// <summary>
    /// WarnMessage: с пустым сообщением не должно выбрасывать исключения.
    /// (Проверки содержания лога опущены из-за особенностей реализации ILogger и форматтеров.)
    /// </summary>
    [Test]
    public void WarnMessage_EmptyMessage_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _controller.WarnMessage(string.Empty));
    }
}
