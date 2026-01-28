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

    #region Constructor Negative Tests

    /// <summary>
    /// Проверяет, что конструктор с null принтером создаёт экземпляр,
    /// но при последующем использовании выбрасывается NullReferenceException.
    /// Примечание: текущая реализация не валидирует параметр конструктора.
    /// </summary>
    [Test]
    public void Constructor_WithNullPrinter_CreatesInstanceButFailsOnUse()
    {
        // Arrange
        AbsPrinter? nullPrinter = null;

        // Act
        var service = new PrinterService(nullPrinter!);

        // Assert - конструктор не выбрасывает исключение
        Assert.That(service, Is.Not.Null);

        // При попытке использования - NullReferenceException
        Assert.Throws<NullReferenceException>(() => service.GetPrinters());
    }

    #endregion

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

    /// <summary>
    /// Проверяет, что GetPrinters пробрасывает исключение от AbsPrinter.
    /// </summary>
    [Test]
    public void GetPrinters_WhenAbsPrinterThrowsException_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Ошибка получения принтеров");
        _mockPrinter
            .Setup(p => p.GetAvailablePrinters())
            .Throws(expectedException);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _sut.GetPrinters());
        Assert.That(ex.Message, Is.EqualTo("Ошибка получения принтеров"));
    }

    /// <summary>
    /// Проверяет, что GetPrinters возвращает null, если AbsPrinter вернул null.
    /// Примечание: текущая реализация не обрабатывает этот случай.
    /// </summary>
    [Test]
    public void GetPrinters_WhenAbsPrinterReturnsNull_ReturnsNull()
    {
        // Arrange
        _mockPrinter
            .Setup(p => p.GetAvailablePrinters())
            .Returns((IEnumerable<string>)null!);

        // Act
        var result = _sut.GetPrinters();

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Проверяет, что GetPrinters пробрасывает ArgumentException от AbsPrinter.
    /// </summary>
    [Test]
    public void GetPrinters_WhenAbsPrinterThrowsArgumentException_PropagatesException()
    {
        // Arrange
        _mockPrinter
            .Setup(p => p.GetAvailablePrinters())
            .Throws<ArgumentException>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.GetPrinters());
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

    #region PrintDocAsync Negative Tests

    /// <summary>
    /// Проверяет, что PrintDocAsync передаёт null имя принтера в AbsPrinter без валидации.
    /// Примечание: текущая реализация не валидирует параметры.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WithNullPrinterName_PassesToAbsPrinter()
    {
        // Arrange
        string? capturedPrinterName = "not-null";
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .Callback<string>(name => capturedPrinterName = name)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.PrintDocAsync(null!);

        // Assert - null передаётся в AbsPrinter
        Assert.That(capturedPrinterName, Is.Null);
        _mockPrinter.Verify(p => p.PrintDocAsync(null!), Times.Once);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync передаёт пустое имя принтера в AbsPrinter без валидации.
    /// Примечание: текущая реализация не валидирует параметры.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WithEmptyPrinterName_PassesToAbsPrinter()
    {
        // Arrange
        string? capturedPrinterName = null;
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .Callback<string>(name => capturedPrinterName = name)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.PrintDocAsync(string.Empty);

        // Assert
        Assert.That(capturedPrinterName, Is.Empty);
        _mockPrinter.Verify(p => p.PrintDocAsync(string.Empty), Times.Once);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync передаёт имя с пробелами в AbsPrinter без валидации.
    /// Примечание: текущая реализация не валидирует параметры.
    /// </summary>
    [Test]
    public async Task PrintDocAsync_WithWhitespacePrinterName_PassesToAbsPrinter()
    {
        // Arrange
        const string whitespaceName = "   ";
        string? capturedPrinterName = null;
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .Callback<string>(name => capturedPrinterName = name)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.PrintDocAsync(whitespaceName);

        // Assert
        Assert.That(capturedPrinterName, Is.EqualTo(whitespaceName));
        _mockPrinter.Verify(p => p.PrintDocAsync(whitespaceName), Times.Once);
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync пробрасывает исключение от AbsPrinter.
    /// </summary>
    [Test]
    public void PrintDocAsync_WhenAbsPrinterThrowsException_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Ошибка печати");
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.PrintDocAsync("TestPrinter"));
        Assert.That(ex!.Message, Is.EqualTo("Ошибка печати"));
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync пробрасывает IOException от AbsPrinter.
    /// </summary>
    [Test]
    public void PrintDocAsync_WhenAbsPrinterThrowsIOException_PropagatesException()
    {
        // Arrange
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .ThrowsAsync(new IOException("Файл не найден"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<IOException>(
            async () => await _sut.PrintDocAsync("TestPrinter"));
        Assert.That(ex!.Message, Is.EqualTo("Файл не найден"));
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync пробрасывает OperationCanceledException при отмене.
    /// </summary>
    [Test]
    public void PrintDocAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .ThrowsAsync(new OperationCanceledException("Операция отменена"));

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _sut.PrintDocAsync("TestPrinter"));
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync пробрасывает TaskCanceledException при отмене задачи.
    /// </summary>
    [Test]
    public void PrintDocAsync_WhenTaskCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .ThrowsAsync(new TaskCanceledException("Задача отменена"));

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(
            async () => await _sut.PrintDocAsync("TestPrinter"));
    }

    /// <summary>
    /// Проверяет, что PrintDocAsync пробрасывает UnauthorizedAccessException от AbsPrinter.
    /// </summary>
    [Test]
    public void PrintDocAsync_WhenUnauthorizedAccess_PropagatesException()
    {
        // Arrange
        _mockPrinter
            .Setup(p => p.PrintDocAsync(It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Доступ запрещён"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.PrintDocAsync("TestPrinter"));
        Assert.That(ex!.Message, Is.EqualTo("Доступ запрещён"));
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
