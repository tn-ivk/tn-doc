using NUnit.Framework;
using TN.DocData;
using HomeData = TN_Doc.Models.Home.Data;

namespace Tests.Models.Home;

/// <summary>
/// Набор тестов для модели Data.
/// </summary>
[TestFixture]
public class DataTests
{
    /// <summary>
    /// Проверяет, что конструктор по умолчанию инициализирует свойства значениями по умолчанию.
    /// </summary>
    [Test]
    public void Constructor_WhenCalled_InitializesDefaults()
    {
        // Arrange & Act
        var data = new HomeData();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(data.IdDevice, Is.EqualTo(default(int)));
            Assert.That(data.IdDoc, Is.EqualTo(default(IdDoc)));
            Assert.That(data.DTBegin, Is.Null);
            Assert.That(data.DTEnd, Is.Null);
        });
    }

    /// <summary>
    /// Проверяет, что свойство IdDevice корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void IdDevice_WhenSet_ReturnsValue()
    {
        // Arrange
        var data = new HomeData();
        const int expectedIdDevice = 123;

        // Act
        data.IdDevice = expectedIdDevice;

        // Assert
        Assert.That(data.IdDevice, Is.EqualTo(expectedIdDevice));
    }

    /// <summary>
    /// Проверяет, что свойство IdDoc корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void IdDoc_WhenSet_ReturnsValue()
    {
        // Arrange
        var data = new HomeData();
        const IdDoc expectedIdDoc = IdDoc.Passport;

        // Act
        data.IdDoc = expectedIdDoc;

        // Assert
        Assert.That(data.IdDoc, Is.EqualTo(expectedIdDoc));
    }

    /// <summary>
    /// Проверяет, что свойство DTBegin корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void DTBegin_WhenSet_ReturnsValue()
    {
        // Arrange
        var data = new HomeData();
        const string expectedDTBegin = "2024-01-01 00:00:00";

        // Act
        data.DTBegin = expectedDTBegin;

        // Assert
        Assert.That(data.DTBegin, Is.EqualTo(expectedDTBegin));
    }

    /// <summary>
    /// Проверяет, что свойство DTEnd корректно устанавливается и возвращает значение.
    /// </summary>
    [Test]
    public void DTEnd_WhenSet_ReturnsValue()
    {
        // Arrange
        var data = new HomeData();
        const string expectedDTEnd = "2024-12-31 23:59:59";

        // Act
        data.DTEnd = expectedDTEnd;

        // Assert
        Assert.That(data.DTEnd, Is.EqualTo(expectedDTEnd));
    }

    /// <summary>
    /// Проверяет инициализацию всех свойств через object initializer.
    /// </summary>
    [Test]
    public void AllProperties_WhenInitializedViaObjectInitializer_ReturnCorrectValues()
    {
        // Arrange & Act
        var data = new HomeData
        {
            IdDevice = 456,
            IdDoc = IdDoc.Report,
            DTBegin = "2024-06-01",
            DTEnd = "2024-06-30"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(data.IdDevice, Is.EqualTo(456));
            Assert.That(data.IdDoc, Is.EqualTo(IdDoc.Report));
            Assert.That(data.DTBegin, Is.EqualTo("2024-06-01"));
            Assert.That(data.DTEnd, Is.EqualTo("2024-06-30"));
        });
    }

    /// <summary>
    /// Проверяет, что свойство IdDoc может принимать различные значения перечисления.
    /// </summary>
    [TestCase(IdDoc.Report)]
    [TestCase(IdDoc.Passport)]
    [TestCase(IdDoc.Act)]
    [TestCase(IdDoc.Jornal)]
    [TestCase(IdDoc.Poverka3287)]
    public void IdDoc_WhenSetToDifferentValues_ReturnsCorrectValue(IdDoc expectedIdDoc)
    {
        // Arrange
        var data = new HomeData();

        // Act
        data.IdDoc = expectedIdDoc;

        // Assert
        Assert.That(data.IdDoc, Is.EqualTo(expectedIdDoc));
    }

    /// <summary>
    /// Проверяет, что свойство IdDevice по умолчанию равно 0.
    /// </summary>
    [Test]
    public void IdDevice_WhenNotSet_ReturnsZero()
    {
        // Arrange & Act
        var data = new HomeData();

        // Assert
        Assert.That(data.IdDevice, Is.Default);
    }

    /// <summary>
    /// Проверяет, что свойство IdDoc по умолчанию равно первому значению перечисления (Report).
    /// </summary>
    [Test]
    public void IdDoc_WhenNotSet_ReturnsDefaultEnumValue()
    {
        // Arrange & Act
        var data = new HomeData();

        // Assert
        Assert.That(data.IdDoc, Is.EqualTo(IdDoc.Report));
    }

    /// <summary>
    /// Проверяет, что свойства DTBegin и DTEnd можно установить в пустую строку.
    /// </summary>
    [Test]
    public void DateTimeProperties_WhenSetToEmptyString_ReturnEmptyString()
    {
        // Arrange
        var data = new HomeData
        {
            DTBegin = string.Empty,
            DTEnd = string.Empty
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(data.DTBegin, Is.EqualTo(string.Empty));
            Assert.That(data.DTEnd, Is.EqualTo(string.Empty));
        });
    }
}
