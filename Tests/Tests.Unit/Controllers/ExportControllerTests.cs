using NUnit.Framework;
using TN_Doc.Controllers;

namespace Tests.Controllers;

/// <summary>
/// Набор тестов для контроллера экспорта документов ExportController
/// </summary>
[TestFixture(TestName = "ExportController - тесты контроллера экспорта")]
public class ExportControllerTests
{
    private ExportController? _controller;

    [SetUp]
    public void SetUp()
    {
        _controller = new ExportController();
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
        _controller = null;
    }

    #region GetListFormats Tests

    /// <summary>
    /// Проверяет, что метод GetListFormats возвращает список из 4 форматов
    /// </summary>
    [Test]
    public void GetListFormats_WhenCalled_ReturnsFourFormats()
    {
        // Act
        var result = _controller.GetListFormats();

        // Assert
        Assert.That(result.Count, Is.EqualTo(4));
    }

    /// <summary>
    /// Проверяет, что метод GetListFormats возвращает список, содержащий формат pdf
    /// </summary>
    [Test]
    public void GetListFormats_WhenCalled_ContainsPdf()
    {
        // Act
        var result = _controller.GetListFormats();

        // Assert
        Assert.That(result, Contains.Item("pdf"));
    }

    /// <summary>
    /// Проверяет, что метод GetListFormats возвращает список, содержащий формат excel
    /// </summary>
    [Test]
    public void GetListFormats_WhenCalled_ContainsExcel()
    {
        // Act
        var result = _controller.GetListFormats();

        // Assert
        Assert.That(result, Contains.Item("excel"));
    }

    /// <summary>
    /// Проверяет, что метод GetListFormats возвращает список, содержащий формат ods
    /// </summary>
    [Test]
    public void GetListFormats_WhenCalled_ContainsOds()
    {
        // Act
        var result = _controller.GetListFormats();

        // Assert
        Assert.That(result, Contains.Item("ods"));
    }

    /// <summary>
    /// Проверяет, что метод GetListFormats возвращает список, содержащий формат xml
    /// </summary>
    [Test]
    public void GetListFormats_WhenCalled_ContainsXml()
    {
        // Act
        var result = _controller.GetListFormats();

        // Assert
        Assert.That(result, Contains.Item("xml"));
    }

    /// <summary>
    /// Проверяет, что метод GetListFormats возвращает список форматов в правильном порядке
    /// </summary>
    [Test]
    public void GetListFormats_WhenCalled_ReturnsListInCorrectOrder()
    {
        // Arrange
        var expectedFormats = new List<string> { "pdf", "excel", "ods", "xml" };

        // Act
        var result = _controller.GetListFormats();

        // Assert
        Assert.That(result, Is.EqualTo(expectedFormats));
    }

    #endregion

    #region ExportDoc Tests

    /// <summary>
    /// Проверяет, что метод ExportDoc не выбрасывает исключение при наличии исходного файла
    /// </summary>
    [Test]
    public void ExportDoc_WhenCalled_DoesNotThrow()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var pdfDir = Path.Combine(currentDir, "wwwroot", "PDF");
        var sourceFile = Path.Combine(pdfDir, "PDF.pdf");
        var targetFile = Path.Combine(pdfDir, "PDF2.pdf");

        // Создаём временную директорию и файл для теста
        Directory.CreateDirectory(pdfDir);
        File.WriteAllText(sourceFile, "Test PDF content");

        // Удаляем целевой файл, если он существует (копирование с overwrite=false)
        if (File.Exists(targetFile))
        {
            File.Delete(targetFile);
        }

        try
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
                _controller.ExportDoc(1, TN.DocData.IdDoc.Act, 1, "pdf"));

            // Проверяем, что файл был создан
            Assert.That(File.Exists(targetFile), Is.True);
        }
        finally
        {
            // Cleanup
            if (File.Exists(sourceFile))
            {
                File.Delete(sourceFile);
            }
            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }
            // Удаляем директории, если они пустые
            if (Directory.Exists(pdfDir) && !Directory.EnumerateFileSystemEntries(pdfDir).Any())
            {
                Directory.Delete(pdfDir);
            }
            var wwwrootDir = Path.Combine(currentDir, "wwwroot");
            if (Directory.Exists(wwwrootDir) && !Directory.EnumerateFileSystemEntries(wwwrootDir).Any())
            {
                Directory.Delete(wwwrootDir);
            }
        }
    }

    #endregion
}
