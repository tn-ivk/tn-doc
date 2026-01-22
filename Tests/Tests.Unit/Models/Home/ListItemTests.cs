using NUnit.Framework;
using TN_Doc.Models.Home;

namespace Tests.Models.Home;

/// <summary>
/// Набор тестов для модели ListItem.
/// </summary>
[TestFixture]
public class ListItemTests
{
    /// <summary>
    /// Проверяет, что конструктор по умолчанию инициализирует свойства значениями по умолчанию.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_InitializesDefaults()
    {
        // Arrange & Act
        var item = new ListItem();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(item.Id, Is.EqualTo(default(int)));
            Assert.That(item.Name, Is.Null);
        });
    }

    /// <summary>
    /// Проверяет, что свойство Id корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void Id_WhenSet_ReturnsValue()
    {
        // Arrange
        var item = new ListItem();
        const int expectedId = 42;

        // Act
        item.Id = expectedId;

        // Assert
        Assert.That(item.Id, Is.EqualTo(expectedId));
    }

    /// <summary>
    /// Проверяет, что свойство Name корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void Name_WhenSet_ReturnsValue()
    {
        // Arrange
        var item = new ListItem();
        const string expectedName = "Test Item";

        // Act
        item.Name = expectedName;

        // Assert
        Assert.That(item.Name, Is.EqualTo(expectedName));
    }

    /// <summary>
    /// Проверяет, что свойство Id корректно обрабатывает отрицательные значения.
    /// </summary>
    [Test]
    public void Id_WhenSetToNegativeValue_ReturnsValue()
    {
        // Arrange
        var item = new ListItem();
        const int negativeId = -1;

        // Act
        item.Id = negativeId;

        // Assert
        Assert.That(item.Id, Is.EqualTo(negativeId));
    }

    /// <summary>
    /// Проверяет, что свойство Id корректно обрабатывает максимальное значение int.
    /// </summary>
    [Test]
    public void Id_WhenSetToMaxValue_ReturnsValue()
    {
        // Arrange
        var item = new ListItem();

        // Act
        item.Id = int.MaxValue;

        // Assert
        Assert.That(item.Id, Is.EqualTo(int.MaxValue));
    }

    /// <summary>
    /// Проверяет, что свойство Name можно установить в пустую строку.
    /// </summary>
    [Test]
    public void Name_WhenSetToEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var item = new ListItem();

        // Act
        item.Name = string.Empty;

        // Assert
        Assert.That(item.Name, Is.EqualTo(string.Empty));
    }

    /// <summary>
    /// Проверяет инициализацию всех свойств через object initializer.
    /// </summary>
    [Test]
    public void AllProperties_WhenInitializedViaObjectInitializer_ReturnCorrectValues()
    {
        // Arrange & Act
        var item = new ListItem
        {
            Id = 100,
            Name = "Test List Item"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(item.Id, Is.EqualTo(100));
            Assert.That(item.Name, Is.EqualTo("Test List Item"));
        });
    }

    /// <summary>
    /// Проверяет, что свойство Id по умолчанию равно 0.
    /// </summary>
    [Test]
    public void Id_WhenNotSet_ReturnsZero()
    {
        // Arrange & Act
        var item = new ListItem();

        // Assert
        Assert.That(item.Id, Is.Default);
    }
}
