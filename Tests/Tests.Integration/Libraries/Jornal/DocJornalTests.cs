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

namespace Tests.Libraries.Jornal;

/// <summary>
/// Интеграционные тесты для библиотеки DocJornal (Журнал СИ).
///
/// Покрывает документ журнала измерений:
/// - DocJornal: Журнал средств измерений
///
/// Документ использует:
/// - IdDoc.Jornal
/// - Таблица БД: TableMeasurementJornal
/// - Метод GetEditDoc всегда возвращает пустую строку
/// </summary>
[TestFixture]
public class DocJornalTests
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
        // Создаём временную директорию для тестов
        _testBasePath = Path.Combine(Path.GetTempPath(), $"DocJornalTests_{Guid.NewGuid()}");
        _testWwwrootPath = Path.Combine(_testBasePath, "wwwroot");
        _testHtmlPath = Path.Combine(_testWwwrootPath, "HTML");

        Directory.CreateDirectory(_testBasePath);
        Directory.CreateDirectory(_testWwwrootPath);
        Directory.CreateDirectory(_testHtmlPath);

        TestContext.WriteLine($"Базовый путь для тестов: {_testBasePath}");
    }

    [SetUp]
    public void SetUp()
    {
        // Создаём новую in-memory БД для каждого теста (изоляция)
        _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _mockAppConfig = new Mock<IAppConfigService>();
        _mockLogger = new Mock<ILogger>();

        // Настраиваем моки через хелпер
        MockConfigHelper.SetupMockAppConfig(_mockAppConfig, idDevice: 1);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Удаляем временную директорию после всех тестов
        if (Directory.Exists(_testBasePath))
        {
            try
            {
                Directory.Delete(_testBasePath, true);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Не удалось удалить тестовую директорию: {ex.Message}");
            }
        }
    }

    #region Constructor Tests - Тесты конструктора

    [Test]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var instance = CreateDocJornalInstance();

        // Assert
        Assert.That(instance, Is.Not.Null, "Экземпляр не должен быть null");
        Assert.That(instance, Is.InstanceOf<DocJornal>(), "Экземпляр должен быть типа DocJornal");
    }

    [Test]
    public void Constructor_SetsIdDocToJornal()
    {
        // Arrange & Act
        var instance = CreateDocJornalInstance();

        // Assert
        // IdDoc устанавливается в конструкторе как IdDoc.Jornal
        // Проверяем через PathToDocConfigFile который содержит IdDoc в имени
        Assert.That(instance.PathToDocConfigFile, Does.Contain("Jornal"),
            "PathToDocConfigFile должен содержать 'Jornal'");
    }

    [Test]
    public void Constructor_InitializesPathToDocConfigFile()
    {
        // Arrange & Act
        var instance = CreateDocJornalInstance();

        // Assert
        Assert.That(instance.PathToDocConfigFile, Is.Not.Null,
            "PathToDocConfigFile не должен быть null");
        Assert.That(instance.PathToDocConfigFile, Is.Not.Empty,
            "PathToDocConfigFile не должен быть пустым");

        TestContext.WriteLine($"PathToDocConfigFile: {instance.PathToDocConfigFile}");
    }

    [Test]
    public void Constructor_InitializesPathToDocEditConfigFile()
    {
        // Arrange & Act
        var instance = CreateDocJornalInstance();

        // Assert
        Assert.That(instance.PathToDocEditConfigFile, Is.Not.Null,
            "PathToDocEditConfigFile не должен быть null");
        Assert.That(instance.PathToDocEditConfigFile, Is.Not.Empty,
            "PathToDocEditConfigFile не должен быть пустым");

        TestContext.WriteLine($"PathToDocEditConfigFile: {instance.PathToDocEditConfigFile}");
    }

    [Test]
    public void Constructor_InitializesPathToDocTemplateFile()
    {
        // Arrange & Act
        var instance = CreateDocJornalInstance();

        // Assert
        Assert.That(instance.PathToDocTemplateFile, Is.Not.Null,
            "PathToDocTemplateFile не должен быть null");
        Assert.That(instance.PathToDocTemplateFile, Is.Not.Empty,
            "PathToDocTemplateFile не должен быть пустым");

        TestContext.WriteLine($"PathToDocTemplateFile: {instance.PathToDocTemplateFile}");
    }

    #endregion

    #region GetViewDoc Tests - Тесты метода GetViewDoc

    [Test]
    public void GetViewDoc_WithValidId_ReturnsObjectOrNull()
    {
        // Arrange
        var document = CreateDocJornalInstance();
        const int testId = 1;

        // Act & Assert
        try
        {
            var result = document.GetViewDoc(testId);

            // GetViewDoc может вернуть null если запись не найдена в БД
            if (result != null)
            {
                var jsonString = result.ToString();
                Assert.That(jsonString, Is.Not.Null, "JSON не должен быть null");
                Assert.That(jsonString, Is.Not.Empty, "JSON не должен быть пустым");

                // Проверка валидности JSON
                Assert.DoesNotThrow(
                    () => Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString),
                    "JSON должен быть валидным и десериализуемым"
                );

                TestContext.WriteLine($"GetViewDoc вернул валидный JSON ({jsonString.Length} символов)");
            }
            else
            {
                Assert.Pass("GetViewDoc вернул null (допустимо для несуществующих записей)");
            }
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    [Test]
    public void GetViewDoc_WithInvalidId_HandlesGracefully()
    {
        // Arrange
        var document = CreateDocJornalInstance();
        const int invalidId = -1;

        // Act & Assert
        try
        {
            var result = document.GetViewDoc(invalidId);
            TestContext.WriteLine($"GetViewDoc с невалидным ID вернул: {result ?? "null"}");
            Assert.Pass("GetViewDoc корректно обработал невалидный ID");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    [Test]
    public void GetViewDoc_WithZeroId_HandlesGracefully()
    {
        // Arrange
        var document = CreateDocJornalInstance();
        const int zeroId = 0;

        // Act & Assert
        try
        {
            var result = document.GetViewDoc(zeroId);
            TestContext.WriteLine($"GetViewDoc с нулевым ID вернул: {result ?? "null"}");
            Assert.Pass("GetViewDoc корректно обработал нулевой ID");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    [Test]
    public void GetViewDoc_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var document = CreateDocJornalInstance();
        const int nonExistentId = 999999;

        // Act & Assert
        try
        {
            var result = document.GetViewDoc(nonExistentId);
            // Для несуществующей записи метод должен вернуть null
            Assert.That(result, Is.Null,
                "GetViewDoc должен вернуть null для несуществующего ID");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    #endregion

    #region GetEditDoc Tests - Тесты метода GetEditDoc

    [Test]
    public void GetEditDoc_WithAnyId_ReturnsEmptyString()
    {
        // Arrange
        var document = CreateDocJornalInstance();
        const int testId = 1;

        // Act
        var result = document.GetEditDoc(testId);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty),
            "GetEditDoc должен всегда возвращать пустую строку");
    }

    [Test]
    public void GetEditDoc_WithInvalidId_ReturnsEmptyString()
    {
        // Arrange
        var document = CreateDocJornalInstance();
        const int invalidId = -1;

        // Act
        var result = document.GetEditDoc(invalidId);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty),
            "GetEditDoc должен возвращать пустую строку даже для невалидного ID");
    }

    [Test]
    public void GetEditDoc_WithZeroId_ReturnsEmptyString()
    {
        // Arrange
        var document = CreateDocJornalInstance();
        const int zeroId = 0;

        // Act
        var result = document.GetEditDoc(zeroId);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty),
            "GetEditDoc должен возвращать пустую строку для нулевого ID");
    }

    [Test]
    public void GetEditDoc_WithLargeId_ReturnsEmptyString()
    {
        // Arrange
        var document = CreateDocJornalInstance();
        const int largeId = int.MaxValue;

        // Act
        var result = document.GetEditDoc(largeId);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty),
            "GetEditDoc должен возвращать пустую строку для большого ID");
    }

    [Test]
    public void GetEditDoc_IsConsistentAcrossMultipleCalls()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var result1 = document.GetEditDoc(1);
        var result2 = document.GetEditDoc(2);
        var result3 = document.GetEditDoc(100);

        // Assert - Все вызовы должны возвращать одинаковый результат
        Assert.That(result1, Is.EqualTo(result2),
            "GetEditDoc должен быть консистентным для разных ID");
        Assert.That(result2, Is.EqualTo(result3),
            "GetEditDoc должен быть консистентным для разных ID");
        Assert.That(result1, Is.EqualTo(string.Empty),
            "Все вызовы GetEditDoc должны возвращать пустую строку");
    }

    #endregion

    #region GetPathTemplateFile Tests - Тесты метода GetPathTemplateFile

    [Test]
    public void GetPathTemplateFile_ReturnsValidFilePath()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var templatePath = document.GetPathTemplateFile();

        // Assert
        Assert.That(templatePath, Is.Not.Null, "Путь к шаблону не должен быть null");
        Assert.That(templatePath, Is.Not.Empty, "Путь к шаблону не должен быть пустым");

        TestContext.WriteLine($"Путь к шаблону: {templatePath}");
    }

    [Test]
    public void GetPathTemplateFile_ReturnsPathWithFrxExtension()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var templatePath = document.GetPathTemplateFile();

        // Assert
        Assert.That(templatePath, Does.EndWith(".frx"),
            "Путь к шаблону должен заканчиваться на .frx");
    }

    [Test]
    public void GetPathTemplateFile_IsConsistentAcrossMultipleCalls()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var path1 = document.GetPathTemplateFile();
        var path2 = document.GetPathTemplateFile();

        // Assert
        Assert.That(path1, Is.EqualTo(path2),
            "GetPathTemplateFile должен возвращать одинаковый путь при повторных вызовах");
    }

    #endregion

    #region Configuration Path Properties Tests - Тесты свойств путей конфигурации

    [Test]
    public void PathToDocConfigFile_ContainsJornalIdentifier()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var configPath = document.PathToDocConfigFile;

        // Assert
        Assert.That(configPath, Does.Contain("Jornal"),
            "PathToDocConfigFile должен содержать идентификатор 'Jornal'");
    }

    [Test]
    public void PathToDocEditConfigFile_ContainsJornalIdentifier()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var editConfigPath = document.PathToDocEditConfigFile;

        // Assert
        Assert.That(editConfigPath, Does.Contain("Jornal"),
            "PathToDocEditConfigFile должен содержать идентификатор 'Jornal'");
    }

    [Test]
    public void PathToDocTemplateFile_ContainsJornalIdentifier()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var templatePath = document.PathToDocTemplateFile;

        // Assert
        Assert.That(templatePath, Does.Contain("Jornal"),
            "PathToDocTemplateFile должен содержать идентификатор 'Jornal'");
    }

    [Test]
    public void AllConfigPaths_AreDistinct()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var configPath = document.PathToDocConfigFile;
        var editConfigPath = document.PathToDocEditConfigFile;
        var templatePath = document.PathToDocTemplateFile;

        // Assert
        Assert.That(configPath, Is.Not.EqualTo(editConfigPath),
            "PathToDocConfigFile и PathToDocEditConfigFile должны быть разными");
        Assert.That(configPath, Is.Not.EqualTo(templatePath),
            "PathToDocConfigFile и PathToDocTemplateFile должны быть разными");
        Assert.That(editConfigPath, Is.Not.EqualTo(templatePath),
            "PathToDocEditConfigFile и PathToDocTemplateFile должны быть разными");
    }

    [Test]
    public void PathToDocConfigFile_EndsWithJsonExtension()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var configPath = document.PathToDocConfigFile;

        // Assert
        Assert.That(configPath, Does.EndWith(".json"),
            "PathToDocConfigFile должен заканчиваться на .json");
    }

    [Test]
    public void PathToDocEditConfigFile_EndsWithJsonExtension()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var editConfigPath = document.PathToDocEditConfigFile;

        // Assert
        Assert.That(editConfigPath, Does.EndWith(".json"),
            "PathToDocEditConfigFile должен заканчиваться на .json");
    }

    [Test]
    public void PathToDocTemplateFile_EndsWithFrxExtension()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act
        var templatePath = document.PathToDocTemplateFile;

        // Assert
        Assert.That(templatePath, Does.EndWith(".frx"),
            "PathToDocTemplateFile должен заканчиваться на .frx");
    }

    #endregion

    #region GetList Tests - Тесты метода GetList

    [Test]
    public void GetList_WithValidDateRange_ReturnsListOrEmpty()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Используем Unix timestamp для диапазона дат (последний месяц)
        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddMonths(-1);
        var utBegin = startDate.ToUnixTimeSeconds();
        var utEnd = endDate.ToUnixTimeSeconds();

        // Act & Assert
        try
        {
            var result = document.GetList(utBegin, utEnd);
            Assert.That(result, Is.Not.Null, "GetList не должен возвращать null");
            // Список может быть пустым, если в БД нет данных
            TestContext.WriteLine($"GetList вернул {result?.Count ?? 0} записей");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    [Test]
    public void GetList_WithInvertedDateRange_HandlesGracefully()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Начальная дата больше конечной
        var startDate = DateTimeOffset.UtcNow;
        var endDate = startDate.AddMonths(-1);
        var utBegin = startDate.ToUnixTimeSeconds();
        var utEnd = endDate.ToUnixTimeSeconds();

        // Act & Assert - Метод не должен выбрасывать исключение
        try
        {
            var result = document.GetList(utBegin, utEnd);
            TestContext.WriteLine($"GetList с инвертированным диапазоном вернул {result?.Count ?? 0} записей");
            Assert.Pass("GetList корректно обработал инвертированный диапазон дат");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    [Test]
    public void GetList_WithZeroTimestamps_HandlesGracefully()
    {
        // Arrange
        var document = CreateDocJornalInstance();

        // Act & Assert
        try
        {
            var result = document.GetList(0, 0);
            TestContext.WriteLine($"GetList с нулевыми timestamp вернул {result?.Count ?? 0} записей");
            Assert.Pass("GetList корректно обработал нулевые timestamp");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods - Вспомогательные методы

    /// <summary>
    /// Создаёт экземпляр DocJornal для тестирования
    /// </summary>
    private DocJornal CreateDocJornalInstance(DbContextOptions<DocGeneral>? dbOptions = null)
    {
        dbOptions ??= _dbOptions;

        return new DocJornal(
            dbOptions,
            _mockAppConfig.Object,
            idDevice: 1,
            idDoc: IdDoc.Jornal,
            path: _testBasePath
        );
    }

    /// <summary>
    /// Выполняет операцию с БД безопасно, перехватывая исключения связанные с отсутствием реальной БД
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <returns>Результат операции или default(T) при ошибке</returns>
    private T? TryExecuteDbOperation<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DbSet") ||
                                                    ex.Message.Contains("entity type"))
        {
            // In-memory провайдер не поддерживает все операции реальной БД
            TestContext.WriteLine($"Ожидаемое исключение БД: {ex.Message}");
            return default;
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("DbSet") == true ||
                                   ex.InnerException?.Message.Contains("entity type") == true)
        {
            TestContext.WriteLine($"Ожидаемое исключение БД (inner): {ex.InnerException?.Message}");
            return default;
        }
    }

    /// <summary>
    /// Проверяет, является ли исключение ошибкой подключения к базе данных MySQL.
    /// </summary>
    /// <param name="ex">Исключение для проверки</param>
    /// <returns>true если это ошибка подключения к MySQL БД</returns>
    private static bool IsDatabaseConnectionError(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? "";

        return message.Contains("access denied") ||
               message.Contains("unable to connect") ||
               message.Contains("connection refused") ||
               message.Contains("mysql") ||
               message.Contains("authentication") ||
               innerMessage.Contains("access denied") ||
               innerMessage.Contains("unable to connect") ||
               innerMessage.Contains("mysql");
    }

    #endregion
}
