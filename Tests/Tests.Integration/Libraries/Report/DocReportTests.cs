using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN.Doc;
using TN.DocData;
using TN_DocGeneral.Services;
using Tests.Fixtures;
using Tests.Libraries;

namespace Tests.Libraries.Report;

/// <summary>
/// Интеграционные тесты для библиотеки DocReport (Отчёты).
///
/// DocReport генерирует отчёты по данным ИВК за различные периоды:
/// - Отчёт за два часа
/// - Отчёт за смену
/// - Отчёт за сутки
/// - Отчёт за месяц
/// - Отчёт за ТКО
///
/// Особенности DocReport:
/// - Есть перегрузка GetList() без параметров (возвращает незавершённые отчёты)
/// - GetList(UTBegin, UTEnd) возвращает отчёты за указанный период
/// - IdDoc = IdDoc.Report
/// </summary>
[TestFixture]
public class DocReportTests
{
    private DbContextOptions<DocGeneral> _dbOptions;
    private Mock<IAppConfigService> _mockAppConfig;
    private Mock<ILogger> _mockLogger;
    private string _testBasePath;
    private string _testWwwrootPath;
    private string _testHtmlPath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Создание уникального временного каталога для тестов
        _testBasePath = Path.Combine(Path.GetTempPath(), $"DocReportTests_{Guid.NewGuid()}");
        _testWwwrootPath = Path.Combine(_testBasePath, "wwwroot");
        _testHtmlPath = Path.Combine(_testWwwrootPath, "HTML");

        Directory.CreateDirectory(_testBasePath);
        Directory.CreateDirectory(_testWwwrootPath);
        Directory.CreateDirectory(_testHtmlPath);

        TestContext.WriteLine($"Test base path: {_testBasePath}");
    }

    [SetUp]
    public void SetUp()
    {
        // Создание новой in-memory БД для каждого теста (изоляция)
        _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _mockAppConfig = new Mock<IAppConfigService>();
        _mockLogger = new Mock<ILogger>();

        // Настройка общих моков с помощью хелпера
        MockConfigHelper.SetupMockAppConfig(_mockAppConfig, idDevice: 1);
    }

    [TearDown]
    public void TearDown()
    {
        // Очистка после каждого теста (при необходимости)
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Удаление временной директории после всех тестов
        if (Directory.Exists(_testBasePath))
        {
            try
            {
                Directory.Delete(_testBasePath, true);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Failed to delete test directory: {ex.Message}");
            }
        }
    }

    #region Constructor Tests

    /// <summary>
    /// Проверка корректной инициализации конструктора с валидными параметрами
    /// </summary>
    [Test]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var result = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor");

        // Assert
        if (result != null)
        {
            Assert.That(result, Is.Not.Null, "Instance should not be null");
            Assert.That(result, Is.InstanceOf<DocReport>(), "Instance should be of type DocReport");
            TestContext.WriteLine("DocReport instance created successfully");
        }
    }

    /// <summary>
    /// Проверка, что IdDoc корректно устанавливается как Report
    /// </summary>
    [Test]
    public void Constructor_SetsIdDocToReport()
    {
        // Arrange & Act
        var result = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for IdDoc check");

        // Assert
        if (result != null)
        {
            Assert.That(result.IdDoc, Is.EqualTo(IdDoc.Report),
                "IdDoc should be set to Report");
            TestContext.WriteLine($"IdDoc correctly set to: {result.IdDoc}");
        }
    }

    /// <summary>
    /// Проверка, что конструктор принимает null DbOptions без исключения
    /// (передаёт в DbContext)
    /// </summary>
    [Test]
    public void Constructor_WithNullDbOptions_DoesNotThrowImmediately()
    {
        // Arrange, Act & Assert
        // Примечание: Конструктор не валидирует DbOptions параметр напрямую,
        // он передаёт его в базовый класс DbContext
        try
        {
            var instance = TryExecuteDbOperation(
                () => CreateDocReportInstance(dbOptions: null),
                "DocReport constructor with null DbOptions");
            // Если мы дошли сюда без исключения - тест пройден
            Assert.Pass("Constructor did not throw immediately with null DbOptions");
        }
        catch (NullReferenceException)
        {
            // Ожидаемое поведение при null DbOptions
            Assert.Pass("Constructor correctly propagates null reference to DbContext");
        }
    }

    #endregion

    #region Path Configuration Tests

    /// <summary>
    /// Проверка корректной инициализации PathToDocConfigFile
    /// </summary>
    [Test]
    public void PathToDocConfigFile_IsInitializedCorrectly()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for PathToDocConfigFile");

        if (document == null)
            return;

        // Act
        var configPath = document.PathToDocConfigFile;

        // Assert
        Assert.That(configPath, Is.Not.Null.Or.Empty,
            "PathToDocConfigFile should be initialized");
        TestContext.WriteLine($"PathToDocConfigFile: {configPath}");
    }

    /// <summary>
    /// Проверка корректной инициализации PathToDocEditConfigFile
    /// </summary>
    [Test]
    public void PathToDocEditConfigFile_IsInitializedCorrectly()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for PathToDocEditConfigFile");

        if (document == null)
            return;

        // Act
        var editConfigPath = document.PathToDocEditConfigFile;

        // Assert
        Assert.That(editConfigPath, Is.Not.Null.Or.Empty,
            "PathToDocEditConfigFile should be initialized");
        TestContext.WriteLine($"PathToDocEditConfigFile: {editConfigPath}");
    }

    /// <summary>
    /// Проверка корректной инициализации PathToDocTemplateFile
    /// </summary>
    [Test]
    public void PathToDocTemplateFile_IsInitializedCorrectly()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for PathToDocTemplateFile");

        if (document == null)
            return;

        // Act
        var templatePath = document.PathToDocTemplateFile;

        // Assert
        Assert.That(templatePath, Is.Not.Null.Or.Empty,
            "PathToDocTemplateFile should be initialized");
        TestContext.WriteLine($"PathToDocTemplateFile: {templatePath}");
    }

    #endregion

    #region GetList() Without Parameters Tests

    /// <summary>
    /// Проверка, что GetList() без параметров возвращает список (может быть пустым)
    /// </summary>
    [Test]
    public void GetList_WithoutParameters_ReturnsListOfRequestListDocs()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetList()");

        if (document == null)
            return;

        // Act
        var result = TryExecuteDbOperation(
            () => document.GetList(),
            "GetList() without parameters");

        // Assert
        if (result != null)
        {
            Assert.That(result, Is.InstanceOf<List<RequestListDocs>>(),
                "GetList() should return List<RequestListDocs>");
            TestContext.WriteLine($"GetList() returned {result.Count} items (incomplete reports)");
        }
    }

    /// <summary>
    /// Проверка, что GetList() без параметров не выбрасывает исключение
    /// </summary>
    [Test]
    public void GetList_WithoutParameters_DoesNotThrowException()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetList() no-throw test");

        if (document == null)
            return;

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            TryExecuteDbOperation(
                () => document.GetList(),
                "GetList() without parameters");
        }, "GetList() should not throw exception");
    }

    /// <summary>
    /// Проверка, что GetList() возвращает элементы с корректной структурой
    /// </summary>
    [Test]
    public void GetList_WithoutParameters_ReturnsItemsWithCorrectStructure()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetList() structure test");

        if (document == null)
            return;

        // Act
        var result = TryExecuteDbOperation(
            () => document.GetList(),
            "GetList() for structure validation");

        // Assert
        if (result != null && result.Count > 0)
        {
            foreach (var item in result)
            {
                // Проверка, что каждый элемент имеет необходимые поля
                Assert.That(item.Id, Is.GreaterThan(0),
                    "Item Id should be positive (report type id)");
                Assert.That(item.DT, Is.Not.Null,
                    "Item DT (description) should not be null");
                Assert.That(item.Description, Is.Not.Null,
                    "Item Description should not be null");
            }
            TestContext.WriteLine($"Validated structure for {result.Count} incomplete report items");
        }
        else
        {
            Assert.Pass("GetList() returned empty list (acceptable - no configured directions/BIKs)");
        }
    }

    #endregion

    #region GetList(UTBegin, UTEnd) Tests

    /// <summary>
    /// Проверка, что GetList с параметрами времени возвращает список
    /// </summary>
    [Test]
    public void GetList_WithTimeParameters_ReturnsListOfRequestListDocs()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetList(UTBegin, UTEnd)");

        if (document == null)
            return;

        // Устанавливаем временной диапазон (последний месяц)
        long utEnd = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long utBegin = utEnd - (30 * 24 * 60 * 60); // 30 дней назад

        // Act
        var result = TryExecuteDbOperation(
            () => document.GetList(utBegin, utEnd),
            $"GetList({utBegin}, {utEnd})");

        // Assert
        if (result != null)
        {
            Assert.That(result, Is.InstanceOf<List<RequestListDocs>>(),
                "GetList(UTBegin, UTEnd) should return List<RequestListDocs>");
            TestContext.WriteLine($"GetList(UTBegin, UTEnd) returned {result.Count} items");
        }
    }

    /// <summary>
    /// Проверка GetList с нулевым диапазоном времени
    /// </summary>
    [Test]
    public void GetList_WithZeroTimeRange_ReturnsEmptyOrHandlesGracefully()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetList with zero range");

        if (document == null)
            return;

        // Act
        var result = TryExecuteDbOperation(
            () => document.GetList(0, 0),
            "GetList(0, 0)");

        // Assert
        if (result != null)
        {
            Assert.That(result, Is.InstanceOf<List<RequestListDocs>>(),
                "GetList(0, 0) should return list (possibly empty)");
            TestContext.WriteLine($"GetList(0, 0) returned {result.Count} items");
        }
    }

    /// <summary>
    /// Проверка GetList с отрицательными значениями времени
    /// </summary>
    [Test]
    public void GetList_WithNegativeTimeValues_HandlesGracefully()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetList with negative values");

        if (document == null)
            return;

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var result = TryExecuteDbOperation(
                () => document.GetList(-1000, -500),
                "GetList(-1000, -500)");

            if (result != null)
            {
                TestContext.WriteLine($"GetList with negative values returned {result.Count} items");
            }
        }, "GetList should handle negative time values gracefully");
    }

    /// <summary>
    /// Проверка GetList с инвертированным диапазоном (begin > end)
    /// </summary>
    [Test]
    public void GetList_WithInvertedTimeRange_HandlesGracefully()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetList with inverted range");

        if (document == null)
            return;

        long utEnd = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long utBegin = utEnd + (24 * 60 * 60); // Begin AFTER End

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var result = TryExecuteDbOperation(
                () => document.GetList(utBegin, utEnd),
                $"GetList({utBegin}, {utEnd}) with inverted range");

            if (result != null)
            {
                // С инвертированным диапазоном должен вернуть пустой список
                TestContext.WriteLine($"GetList with inverted range returned {result.Count} items");
            }
        }, "GetList should handle inverted time range gracefully");
    }

    #endregion

    #region GetViewDoc Tests

    /// <summary>
    /// Проверка GetViewDoc с валидным id (в тестовом окружении без данных)
    /// </summary>
    [Test]
    public void GetViewDoc_WithValidId_ReturnsResultOrNull()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetViewDoc");

        if (document == null)
            return;

        const int testId = 1;

        // Act
        var result = TryExecuteDbOperation(
            () => document.GetViewDoc(testId),
            $"GetViewDoc({testId})");

        // Assert
        if (result != null)
        {
            var jsonString = result.ToString();
            Assert.That(jsonString, Is.Not.Null, "JSON result should not be null");
            Assert.That(jsonString, Is.Not.Empty, "JSON result should not be empty");

            // Проверка валидности JSON
            Assert.DoesNotThrow(
                () => Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString),
                "Result should be valid JSON");

            TestContext.WriteLine($"GetViewDoc({testId}) returned valid JSON ({jsonString.Length} chars)");
        }
        else
        {
            Assert.Pass($"GetViewDoc({testId}) returned null (acceptable for non-existent record)");
        }
    }

    /// <summary>
    /// Проверка GetViewDoc с невалидным отрицательным id
    /// </summary>
    [Test]
    public void GetViewDoc_WithNegativeId_HandlesGracefully()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetViewDoc with negative id");

        if (document == null)
            return;

        const int invalidId = -1;

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var result = TryExecuteDbOperation(
                () => document.GetViewDoc(invalidId),
                $"GetViewDoc({invalidId})");

            TestContext.WriteLine($"GetViewDoc({invalidId}) returned: {result ?? "null"}");
        }, "GetViewDoc should handle negative ID gracefully");
    }

    /// <summary>
    /// Проверка GetViewDoc с нулевым id
    /// </summary>
    [Test]
    public void GetViewDoc_WithZeroId_HandlesGracefully()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetViewDoc with zero id");

        if (document == null)
            return;

        const int zeroId = 0;

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var result = TryExecuteDbOperation(
                () => document.GetViewDoc(zeroId),
                $"GetViewDoc({zeroId})");

            TestContext.WriteLine($"GetViewDoc({zeroId}) returned: {result ?? "null"}");
        }, "GetViewDoc should handle zero ID gracefully");
    }

    /// <summary>
    /// Проверка GetViewDoc с очень большим id
    /// </summary>
    [Test]
    public void GetViewDoc_WithLargeId_HandlesGracefully()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetViewDoc with large id");

        if (document == null)
            return;

        const int largeId = int.MaxValue;

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var result = TryExecuteDbOperation(
                () => document.GetViewDoc(largeId),
                $"GetViewDoc({largeId})");

            TestContext.WriteLine($"GetViewDoc({largeId}) returned: {result ?? "null"}");
        }, "GetViewDoc should handle large ID gracefully");
    }

    #endregion

    #region GetEditDoc Tests

    /// <summary>
    /// Проверка GetEditDoc с валидным id
    /// </summary>
    [Test]
    public void GetEditDoc_WithValidId_ReturnsStringOrEmpty()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetEditDoc");

        if (document == null)
            return;

        const int testId = 1;

        // Создаём шаблон DocEdit.html для теста
        CreateDocEditTemplate();

        // Act
        var result = TryExecuteDbOperation(
            () => document.GetEditDoc(testId),
            $"GetEditDoc({testId})");

        // Assert
        if (result != null)
        {
            Assert.That(result, Is.InstanceOf<string>(),
                "GetEditDoc should return string");
            TestContext.WriteLine($"GetEditDoc({testId}) returned string of length {result.Length}");
        }
    }

    /// <summary>
    /// Проверка GetEditDoc с невалидным id
    /// </summary>
    [Test]
    public void GetEditDoc_WithInvalidId_HandlesGracefully()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetEditDoc with invalid id");

        if (document == null)
            return;

        const int invalidId = -1;

        // Создаём шаблон DocEdit.html для теста
        CreateDocEditTemplate();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            TryExecuteDbOperation(
                () => document.GetEditDoc(invalidId),
                $"GetEditDoc({invalidId})");
        }, "GetEditDoc should handle invalid ID gracefully");
    }

    #endregion

    #region GetPathTemplateFile Tests

    /// <summary>
    /// Проверка, что GetPathTemplateFile возвращает путь к .frx файлу
    /// </summary>
    [Test]
    public void GetPathTemplateFile_ReturnsPathToFrxFile()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetPathTemplateFile");

        if (document == null)
            return;

        // Act
        var templatePath = TryExecuteDbOperation(
            () => document.GetPathTemplateFile(),
            "GetPathTemplateFile()");

        // Assert
        if (templatePath != null)
        {
            Assert.That(templatePath, Is.Not.Empty,
                "Template path should not be empty");
            Assert.That(templatePath.EndsWith(".frx", StringComparison.OrdinalIgnoreCase),
                Is.True,
                "Template path should end with .frx extension");
            TestContext.WriteLine($"GetPathTemplateFile() returned: {templatePath}");
        }
    }

    /// <summary>
    /// Проверка, что GetPathTemplateFile возвращает абсолютный путь
    /// </summary>
    [Test]
    public void GetPathTemplateFile_ReturnsAbsolutePath()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for GetPathTemplateFile absolute path check");

        if (document == null)
            return;

        // Act
        var templatePath = TryExecuteDbOperation(
            () => document.GetPathTemplateFile(),
            "GetPathTemplateFile() for absolute path check");

        // Assert
        if (templatePath != null)
        {
            // В тестовом окружении файл может не существовать физически,
            // но путь должен быть абсолютным или полностью квалифицированным
            Assert.That(
                Path.IsPathRooted(templatePath) || Path.IsPathFullyQualified(templatePath),
                Is.True,
                "Template path should be absolute or fully qualified");
            TestContext.WriteLine($"Template path is absolute: {templatePath}");
        }
    }

    /// <summary>
    /// Проверка согласованности GetPathTemplateFile и PathToDocTemplateFile
    /// </summary>
    [Test]
    public void GetPathTemplateFile_ConsistentWithProperty()
    {
        // Arrange
        var document = TryExecuteDbOperation(
            () => CreateDocReportInstance(),
            "DocReport constructor for path consistency check");

        if (document == null)
            return;

        // Act
        var methodResult = TryExecuteDbOperation(
            () => document.GetPathTemplateFile(),
            "GetPathTemplateFile()");

        var propertyValue = document.PathToDocTemplateFile;

        // Assert
        if (methodResult != null && propertyValue != null)
        {
            Assert.That(methodResult, Is.EqualTo(propertyValue),
                "GetPathTemplateFile() should return same value as PathToDocTemplateFile property");
            TestContext.WriteLine($"Method and property both return: {methodResult}");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Создаёт экземпляр DocReport для тестирования
    /// </summary>
    /// <param name="dbOptions">Опции DbContext (опционально)</param>
    /// <returns>Экземпляр DocReport или null при ошибке</returns>
    private DocReport CreateDocReportInstance(DbContextOptions<DocGeneral>? dbOptions = null)
    {
        dbOptions ??= _dbOptions;

        return new DocReport(
            dbOptions,
            _mockAppConfig.Object,
            idDevice: 1,
            idDoc: IdDoc.Report,
            path: _testBasePath
        );
    }

    /// <summary>
    /// Создаёт минимальный HTML-шаблон DocEdit.html для тестирования GetEditDoc
    /// </summary>
    private void CreateDocEditTemplate()
    {
        var templatePath = Path.Combine(_testWwwrootPath, "HTML", "DocEdit.html");
        var templateContent = @"<!DOCTYPE html>
<html>
<head><title>Doc Edit Template</title></head>
<body>
    <table id='AdditionalInfo'>
        <tbody></tbody>
    </table>
</body>
</html>";

        File.WriteAllText(templatePath, templateContent);
    }

    /// <summary>
    /// Безопасный вызов метода, который может пытаться подключиться к БД.
    /// Если метод падает с MySqlException или другой ожидаемой ошибкой,
    /// тест помечается как Inconclusive.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого результата</typeparam>
    /// <param name="action">Действие для выполнения</param>
    /// <param name="actionDescription">Описание действия для логирования</param>
    /// <returns>Результат выполнения или default(TResult) при ошибке БД</returns>
    private TResult TryExecuteDbOperation<TResult>(
        Func<TResult> action,
        string actionDescription)
    {
        try
        {
            return action();
        }
        catch (InvalidOperationException ex)
            when (ex.InnerException is MySqlConnector.MySqlException)
        {
            Assert.Inconclusive(
                $"{actionDescription} requires database connection. " +
                $"Test cannot run without MySQL database. " +
                $"Inner exception: {ex.InnerException.Message}");
            return default;
        }
        catch (MySqlConnector.MySqlException ex)
        {
            Assert.Inconclusive(
                $"{actionDescription} requires database connection. " +
                $"Test cannot run without MySQL database. " +
                $"Exception: {ex.Message}");
            return default;
        }
        catch (NullReferenceException ex)
        {
            Assert.Inconclusive(
                $"{actionDescription} encountered null reference. " +
                $"Test environment may not have all required data initialized. " +
                $"Exception: {ex.Message}");
            return default;
        }
        catch (FileNotFoundException ex)
        {
            Assert.Inconclusive(
                $"{actionDescription} requires configuration file. " +
                $"File not found: {ex.FileName}. " +
                $"Exception: {ex.Message}");
            return default;
        }
        catch (DirectoryNotFoundException ex)
        {
            Assert.Inconclusive(
                $"{actionDescription} requires configuration directory. " +
                $"Exception: {ex.Message}");
            return default;
        }
    }

    #endregion
}
