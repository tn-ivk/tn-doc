using NUnit.Framework;
using TN_Doc.Models;

namespace Tests.Models;

/// <summary>
/// Набор тестов для модели ClientLogMessage.
/// </summary>
[TestFixture]
public class ClientLogMessageTests
{
    /// <summary>
    /// Проверяет, что конструктор по умолчанию инициализирует свойства значениями null.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_InitializesDefaults()
    {
        // Arrange & Act
        var message = new ClientLogMessage();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(message.Level, Is.Null);
            Assert.That(message.Message, Is.Null);
        });
    }

    /// <summary>
    /// Проверяет, что свойство Level корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void Level_WhenSet_ReturnsValue()
    {
        // Arrange
        var message = new ClientLogMessage();
        const string expectedLevel = "Error";

        // Act
        message.Level = expectedLevel;

        // Assert
        Assert.That(message.Level, Is.EqualTo(expectedLevel));
    }

    /// <summary>
    /// Проверяет, что свойство Message корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void Message_WhenSet_ReturnsValue()
    {
        // Arrange
        var logMessage = new ClientLogMessage();
        const string expectedMessage = "Test error message";

        // Act
        logMessage.Message = expectedMessage;

        // Assert
        Assert.That(logMessage.Message, Is.EqualTo(expectedMessage));
    }

    /// <summary>
    /// Проверяет, что все свойства можно установить одновременно.
    /// </summary>
    [Test]
    public void AllProperties_WhenSet_ReturnCorrectValues()
    {
        // Arrange
        var message = new ClientLogMessage
        {
            Level = "Warning",
            Message = "This is a warning"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(message.Level, Is.EqualTo("Warning"));
            Assert.That(message.Message, Is.EqualTo("This is a warning"));
        });
    }

    /// <summary>
    /// Проверяет, что свойства можно установить в пустую строку.
    /// </summary>
    [Test]
    public void Properties_WhenSetToEmptyString_ReturnEmptyString()
    {
        // Arrange
        var message = new ClientLogMessage
        {
            Level = string.Empty,
            Message = string.Empty
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(message.Level, Is.EqualTo(string.Empty));
            Assert.That(message.Message, Is.EqualTo(string.Empty));
        });
    }
}
