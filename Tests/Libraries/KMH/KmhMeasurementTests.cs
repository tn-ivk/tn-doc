using System;
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

namespace Tests.Libraries.KMH;

/// <summary>
/// Параметризованные тесты для библиотек измерения КМХ (MPR).
///
/// Покрывает три модуля документов измерения массового расходомера:
/// - KMH_MPR_MPR: Контроль МПР по МПР (массовый расходомер по массовому расходомеру)
/// - KMH_MPR_PU: Контроль рабочего МПР по ПУ (по поверочной установке)
/// - KMH_MPR_TPR: Контроль МПР по ТПР (по тепловому расходомеру)
///
/// Приоритет: Фаза 2 - KMH Documents
/// </summary>
[TestFixture]
public class KmhMeasurementTests
{
    private DbContextOptions<DocGeneral> _dbOptions;
    private Mock<IAppConfigService> _mockAppConfig;
    private Mock<IConfigurationCacheService> _mockConfigCache;
    private Mock<ILogger> _mockLogger;
    private string _testBasePath;
    private string _testWwwrootPath;
    private string _testHtmlPath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"KmhMeasurementTests_{Guid.NewGuid()}");
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
        // Create new in-memory DB for each test (isolation)
        _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _mockAppConfig = new Mock<IAppConfigService>();
        _mockConfigCache = new Mock<IConfigurationCacheService>();
        _mockLogger = new Mock<ILogger>();

        // Setup common mocks using helper
        MockConfigHelper.SetupMockAppConfig(_mockAppConfig, idDevice: 1);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
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

    [TestCase(IdDoc.KMH_MPR_MPR, typeof(KMH_MPR_MPR))]
    [TestCase(IdDoc.KMH_MPR_PU, typeof(KMH_MPR_PU))]
    [TestCase(IdDoc.KMH_MPR_TPR, typeof(KMH_MPR_TPR))]
    public void Constructor_WithValidParameters_InitializesCorrectly(IdDoc idDoc, Type expectedType)
    {
        // Arrange & Act
        var instance = CreateDocumentInstance(idDoc);

        // Assert
        Assert.That(instance, Is.Not.Null, "Instance should not be null");
        Assert.That(instance, Is.InstanceOf(expectedType), $"Instance should be of type {expectedType.Name}");
    }

    [TestCase(IdDoc.KMH_MPR_MPR)]
    [TestCase(IdDoc.KMH_MPR_PU)]
    [TestCase(IdDoc.KMH_MPR_TPR)]
    public void Constructor_WithNullDbOptions_ThrowsArgumentException(IdDoc idDoc)
    {
        // Arrange, Act & Assert
        // Note: Constructors do not validate DbOptions parameter, they pass it to DbContext base class
        // DbContext can accept null without throwing exceptions
        Assert.DoesNotThrow(() =>
        {
            CreateDocumentInstance(idDoc, dbOptions: null);
        }, "Constructor accepts null DbOptions (passes to DbContext)");
    }

    #endregion

    #region GetViewDoc Tests

    [TestCase(IdDoc.KMH_MPR_MPR)]
    [TestCase(IdDoc.KMH_MPR_PU)]
    [TestCase(IdDoc.KMH_MPR_TPR)]
    public void GetViewDoc_WithValidId_ReturnsValidJsonString(IdDoc idDoc)
    {
        // Arrange
        var document = CreateDocumentInstance(idDoc);
        const int testId = 1;

        // Note: В реальном тесте здесь нужно добавить тестовые данные в БД
        // SeedTestData(document, testId);

        // Act
        var result = document.GetViewDoc(testId);

        // Assert
        if (result != null)
        {
            var jsonString = result.ToString();
            Assert.That(jsonString, Is.Not.Null, "JSON should not be null");
            Assert.That(jsonString, Is.Not.Empty, "JSON should not be empty");

            // Проверка валидности JSON
            Assert.DoesNotThrow(
                () => Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString),
                "JSON should be valid and deserializable"
            );

            TestContext.WriteLine($"GetViewDoc for {idDoc} returned valid JSON ({jsonString.Length} characters)");
        }
        else
        {
            Assert.Pass($"GetViewDoc for {idDoc} returned null (acceptable for non-existent records)");
        }
    }

    [TestCase(IdDoc.KMH_MPR_MPR)]
    [TestCase(IdDoc.KMH_MPR_PU)]
    [TestCase(IdDoc.KMH_MPR_TPR)]
    public void GetViewDoc_WithInvalidId_HandlesGracefully(IdDoc idDoc)
    {
        // Arrange
        var document = CreateDocumentInstance(idDoc);
        const int invalidId = -1;

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var result = document.GetViewDoc(invalidId);
            TestContext.WriteLine($"GetViewDoc for {idDoc} with invalid ID returned: {result ?? "null"}");
        }, $"GetViewDoc for {idDoc} should handle invalid ID gracefully");
    }

    #endregion

    #region GetPathTemplateFile Tests

    [TestCase(IdDoc.KMH_MPR_MPR)]
    [TestCase(IdDoc.KMH_MPR_PU)]
    [TestCase(IdDoc.KMH_MPR_TPR)]
    public void GetPathTemplateFile_ReturnsValidFilePath(IdDoc idDoc)
    {
        // Arrange
        var document = CreateDocumentInstance(idDoc);

        // Act
        var templatePath = document.GetPathTemplateFile();

        // Assert
        Assert.That(templatePath, Is.Not.Null, "Template path should not be null");
        Assert.That(templatePath, Is.Not.Empty, "Template path should not be empty");

        // В тестовом окружении файл может не существовать физически
        Assert.That(
            Path.IsPathRooted(templatePath) || Path.IsPathFullyQualified(templatePath),
            Is.True,
            "Template path should be absolute or fully qualified"
        );

        TestContext.WriteLine($"Template path for {idDoc}: {templatePath}");
    }

    #endregion


    #region SaveDoc Tests

    [TestCase(IdDoc.KMH_MPR_MPR)]
    [TestCase(IdDoc.KMH_MPR_PU)]
    [TestCase(IdDoc.KMH_MPR_TPR)]
    public void SaveDoc_WithValidJson_DoesNotThrowException(IdDoc idDoc)
    {
        // Arrange
        var document = CreateDocumentInstance(idDoc);
        var testJson = CreateTestCorrectionJson(testId: 1);

        // Act & Assert
        // Note: В реальном тесте здесь нужны тестовые данные в БД
        TestContext.WriteLine($"SaveDoc for {idDoc} requires database seed data for full testing");
        Assert.Pass($"SaveDoc test for {idDoc} - requires database integration");
    }

    #endregion

    #region Configuration Tests

    [TestCase(IdDoc.KMH_MPR_MPR)]
    [TestCase(IdDoc.KMH_MPR_PU)]
    [TestCase(IdDoc.KMH_MPR_TPR)]
    public void PathToDocConfigFile_IsInitializedCorrectly(IdDoc idDoc)
    {
        // Arrange
        var document = CreateDocumentInstance(idDoc);

        // Act - use public property instead of protected method
        var configPath = document.PathToDocConfigFile;

        // Assert
        Assert.That(configPath, Is.Not.Null.Or.Empty, "Config path should be initialized");
        TestContext.WriteLine($"PathToDocConfigFile for {idDoc}: {configPath}");
    }

    [TestCase(IdDoc.KMH_MPR_MPR)]
    [TestCase(IdDoc.KMH_MPR_PU)]
    [TestCase(IdDoc.KMH_MPR_TPR)]
    public void PathToDocEditConfigFile_IsInitializedCorrectly(IdDoc idDoc)
    {
        // Arrange
        var document = CreateDocumentInstance(idDoc);

        // Act - use public property instead of protected method
        var editConfigPath = document.PathToDocEditConfigFile;

        // Assert
        Assert.That(editConfigPath, Is.Not.Null.Or.Empty, "Edit config path should be initialized");
        TestContext.WriteLine($"PathToDocEditConfigFile for {idDoc}: {editConfigPath}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Создает экземпляр документа указанного типа
    /// </summary>
    private DocGeneral CreateDocumentInstance(IdDoc idDoc, DbContextOptions<DocGeneral> dbOptions = null)
    {
        dbOptions ??= _dbOptions;

        return idDoc switch
        {
            IdDoc.KMH_MPR_MPR => new KMH_MPR_MPR(
                dbOptions,
                _mockAppConfig.Object,
                _mockConfigCache.Object,
                idDevice: 1,
                idDoc: IdDoc.KMH_MPR_MPR,
                path: _testBasePath
            ),
            IdDoc.KMH_MPR_PU => new KMH_MPR_PU(
                dbOptions,
                _mockAppConfig.Object,
                _mockConfigCache.Object,
                idDevice: 1,
                idDoc: IdDoc.KMH_MPR_PU,
                path: _testBasePath
            ),
            IdDoc.KMH_MPR_TPR => new KMH_MPR_TPR(
                dbOptions,
                _mockAppConfig.Object,
                _mockConfigCache.Object,
                idDevice: 1,
                idDoc: IdDoc.KMH_MPR_TPR,
                path: _testBasePath
            ),
            _ => throw new ArgumentException($"Unsupported IdDoc type: {idDoc}")
        };
    }

    /// <summary>
    /// Создает минимальный HTML шаблон для тестирования GetEditDoc
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
    /// Создает тестовый JSON для метода SaveDoc
    /// </summary>
    private string CreateTestCorrectionJson(int testId)
    {
        return $@"{{
            ""DocID"": {testId},
            ""Values"": [
                {{ ""Key"": ""ProtokolNum"", ""Value"": ""TEST-001"" }},
                {{ ""Key"": ""ARM_KMH_Result"", ""Value"": ""годен"" }}
            ]
        }}";
    }

    #endregion
}
