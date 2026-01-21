using NUnit.Framework;
using TN_Doc.Models.DTOs;

namespace Tests.Models.DTOs;

/// <summary>
/// Набор тестов для модели DirEditDTO.
/// </summary>
[TestFixture]
public class DirEditDTOTests
{
    /// <summary>
    /// Проверяет, что конструктор по умолчанию инициализирует свойство DirJsonRaw значением null.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_InitializesDefaults()
    {
        // Arrange & Act
        var dto = new DirEditDTO();

        // Assert
        Assert.That(dto.DirJsonRaw, Is.Null);
    }

    /// <summary>
    /// Проверяет, что свойство DirJsonRaw корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void DirJsonRaw_WhenSet_ReturnsValue()
    {
        // Arrange
        var dto = new DirEditDTO();
        const string expectedJson = "{\"key\": \"value\"}";

        // Act
        dto.DirJsonRaw = expectedJson;

        // Assert
        Assert.That(dto.DirJsonRaw, Is.EqualTo(expectedJson));
    }

    /// <summary>
    /// Проверяет, что свойство DirJsonRaw можно установить в пустую строку.
    /// </summary>
    [Test]
    public void DirJsonRaw_WhenSetToEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var dto = new DirEditDTO();

        // Act
        dto.DirJsonRaw = string.Empty;

        // Assert
        Assert.That(dto.DirJsonRaw, Is.EqualTo(string.Empty));
    }

    /// <summary>
    /// Проверяет инициализацию через object initializer.
    /// </summary>
    [Test]
    public void DirJsonRaw_WhenInitializedViaObjectInitializer_ReturnsValue()
    {
        // Arrange & Act
        var dto = new DirEditDTO
        {
            DirJsonRaw = "{\"directories\": []}"
        };

        // Assert
        Assert.That(dto.DirJsonRaw, Is.EqualTo("{\"directories\": []}"));
    }

    /// <summary>
    /// Проверяет, что свойство DirJsonRaw корректно обрабатывает сложный JSON.
    /// </summary>
    [Test]
    public void DirJsonRaw_WhenSetToComplexJson_ReturnsValue()
    {
        // Arrange
        var dto = new DirEditDTO();
        const string complexJson = """
            {
                "directories": [
                    {"id": 1, "name": "dir1"},
                    {"id": 2, "name": "dir2"}
                ],
                "metadata": {
                    "version": "1.0",
                    "created": "2024-01-01"
                }
            }
            """;

        // Act
        dto.DirJsonRaw = complexJson;

        // Assert
        Assert.That(dto.DirJsonRaw, Is.EqualTo(complexJson));
    }
}
