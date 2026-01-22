using NUnit.Framework;
using TN_Doc.Models.DTOs;

namespace Tests.Models.DTOs;

/// <summary>
/// Набор тестов для модели QpEditDto.
/// </summary>
[TestFixture]
public class QpEditDtoTests
{
    /// <summary>
    /// Проверяет, что конструктор по умолчанию инициализирует свойство QpCfgJsonRaw значением null.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_InitializesDefaults()
    {
        // Arrange & Act
        var dto = new QpEditDto();

        // Assert
        Assert.That(dto.QpCfgJsonRaw, Is.Null);
    }

    /// <summary>
    /// Проверяет, что свойство QpCfgJsonRaw корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void QpCfgJsonRaw_WhenSet_ReturnsValue()
    {
        // Arrange
        var dto = new QpEditDto();
        const string expectedJson = "{\"config\": \"test\"}";

        // Act
        dto.QpCfgJsonRaw = expectedJson;

        // Assert
        Assert.That(dto.QpCfgJsonRaw, Is.EqualTo(expectedJson));
    }

    /// <summary>
    /// Проверяет, что свойство QpCfgJsonRaw можно установить в пустую строку.
    /// </summary>
    [Test]
    public void QpCfgJsonRaw_WhenSetToEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var dto = new QpEditDto();

        // Act
        dto.QpCfgJsonRaw = string.Empty;

        // Assert
        Assert.That(dto.QpCfgJsonRaw, Is.EqualTo(string.Empty));
    }

    /// <summary>
    /// Проверяет инициализацию через object initializer.
    /// </summary>
    [Test]
    public void QpCfgJsonRaw_WhenInitializedViaObjectInitializer_ReturnsValue()
    {
        // Arrange & Act
        var dto = new QpEditDto
        {
            QpCfgJsonRaw = "{\"qualityPassport\": {}}"
        };

        // Assert
        Assert.That(dto.QpCfgJsonRaw, Is.EqualTo("{\"qualityPassport\": {}}"));
    }

    /// <summary>
    /// Проверяет, что свойство QpCfgJsonRaw корректно обрабатывает сложный JSON конфигурации.
    /// </summary>
    [Test]
    public void QpCfgJsonRaw_WhenSetToComplexConfigJson_ReturnsValue()
    {
        // Arrange
        var dto = new QpEditDto();
        const string complexJson = """
            {
                "qualityPassport": {
                    "fields": ["field1", "field2"],
                    "validation": {
                        "required": true,
                        "maxLength": 100
                    }
                },
                "settings": {
                    "autoSave": true
                }
            }
            """;

        // Act
        dto.QpCfgJsonRaw = complexJson;

        // Assert
        Assert.That(dto.QpCfgJsonRaw, Is.EqualTo(complexJson));
    }
}
