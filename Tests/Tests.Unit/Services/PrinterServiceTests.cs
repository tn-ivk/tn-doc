using Moq;
using NUnit.Framework;
using TN_Doc.Models.Printer;
using TN_Doc.Models.Services;

namespace Tests.Services;

/// <summary>
/// Unit-тесты для <see cref="PrinterService"/>.
/// </summary>
[TestFixture(TestName = "PrinterService: набор тестов сервиса печати")]
public class PrinterServiceTests
{
    private Mock<AbsPrinter> _mockPrinter = null!;
    private PrinterService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPrinter = new Mock<AbsPrinter>();
        _sut = new PrinterService(_mockPrinter.Object);
    }

    #region GetPrinters Tests

    /// <summary>
    /// Проверяет, что GetPrinters возвращает список принтеров от AbsPrinter.
    /// </summary>
    [Test]
    public void GetPrinters_WhenCalled_ReturnsListFromAbsPrinter()
    {
        // Arrange
        var expectedPrinters = new List<string> { "Printer1" };
        _mockPrinter
            .Setup(p => p.GetAvailablePrinters())
            .Returns(expectedPrinters);

        // Act
        var result = _sut.GetPrinters();

        // Assert
        Assert.That(result, Is.EqualTo(expectedPrinters));
        _mockPrinter.Verify(p => p.GetAvailablePrinters(), Times.Once);
    }

    /// <summary>
    /// Проверяет, что GetPrinters возвращает все принтеры, когда их несколько.
    /// </summary>
    [Test]
    public void GetPrinters_WhenMultiplePrinters_ReturnsAll()
    {
        // Arrange
        var expectedPrinters = new List<string>
        {
            "HP LaserJet Pro",
            "Canon PIXMA",
            "Epson WorkForce",
            "Brother HL-L2350DW"
        };
        _mockPrinter
            .Setup(p => p.GetAvailablePrinters())
            .Returns(expectedPrinters);

        // Act
        var result = _sut.GetPrinters().ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(4));
            Assert.That(result, Is.EquivalentTo(expectedPrinters));
        });
    }

    /// <summary>
    /// Проверяет, что GetPrinters возвращает пустой список, когда принтеры недоступны.
    /// </summary>
    [Test]
    public void GetPrinters_WhenNoPrinters_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = Enumerable.Empty<string>();
        _mockPrinter
            .Setup(p => p.GetAvailablePrinters())
            .Returns(emptyList);

        // Act
        var result = _sut.GetPrinters();

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region PrintDocAsync Tests

    /// <summary>
    /// Проверяет, что PrintDocAsync вызывает метод AbsPrinter при валидном имени принтера.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WhenValidPrinter_CallsAbsPrinter()
    {
        // Arrange
        const string printerName = "HP LaserJet Pro";
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.PrintDocAsync(printerName);

        // Assert
        _mockPrinter.Verify(p => p.PrintDocAsync(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync передаёт корректное имя принтера в AbsPrinter.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WhenCalled_PassesPrinterNameToAbsPrinter()
    {
        // Arrange
        const string expectedPrinterName = "Canon PIXMA MG3620";
        string? actualPrinterName = null;

        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .Callback<string>(name => actualPrinterName = name)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.PrintDocAsync(expectedPrinterName);

        // Assert
        Assert.That(actualPrinterName, Is.EqualTo(expectedPrinterName));
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Проверяет, что конструктор не выбрасывает исключение при передаче валидного принтера.
    /// </summary>
    [Test]
    public void Constructor_WithValidPrinter_DoesNotThrow()
    {
        // Arrange
        var mockPrinter = new Mock<AbsPrinter>();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var service = new PrinterService(mockPrinter.Object);
        });
    }

    #endregion
}
