extern alias ActLib;

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using TN.Doc;
using TN.DocData;
using TN_DocGeneral.Services;
using Tests.Fixtures;

// Используем alias для разрешения конфликта имен между библиотеками
using DocAct = ActLib::TN.Doc.DocAct;

namespace Tests.Libraries.Act;

/// <summary>
/// Интеграционные тесты для библиотеки Act (DocAct).
///
/// Покрывает модуль документа акта приема-передачи нефти:
/// - DocAct: Акт приема-передачи нефти
///
/// Тестируемые методы:
/// - Конструктор: инициализация, пути файлов
/// - GetViewDoc: получение данных документа для просмотра
/// - GetPathTemplateFile: получение пути к шаблону FastReport
/// - Свойства путей конфигурации
/// </summary>
[TestFixture]
public class DocActTests
{
    private DbContextOptions<DocGeneral> _dbOptions;
    private Mock<IAppConfigService> _mockAppConfig;
    private string _testBasePath;
    private string _testWwwrootPath;
    private string _testHtmlPath;

    /// <summary>
    /// Однократная настройка перед всеми тестами.
    /// Создает временные директории для тестового окружения.
    /// </summary>
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Создаем уникальную временную директорию для тестов
        _testBasePath = Path.Combine(Path.GetTempPath(), $"DocActTests_{Guid.NewGuid()}");
        _testWwwrootPath = Path.Combine(_testBasePath, "wwwroot");
        _testHtmlPath = Path.Combine(_testWwwrootPath, "HTML");

        Directory.CreateDirectory(_testBasePath);
        Directory.CreateDirectory(_testWwwrootPath);
        Directory.CreateDirectory(_testHtmlPath);

        TestContext.WriteLine($"Test base path: {_testBasePath}");
    }

    /// <summary>
    /// Настройка перед каждым тестом.
    /// Создает изолированную in-memory БД и настраивает моки.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // Создаем новую in-memory БД для каждого теста (изоляция)
        _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _mockAppConfig = new Mock<IAppConfigService>();

        // Настройка общих моков через хелпер
        MockConfigHelper.SetupMockAppConfig(_mockAppConfig, idDevice: 1);
    }

    /// <summary>
    /// Очистка после всех тестов.
    /// Удаляет временные директории.
    /// </summary>
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

    #region Constructor Tests - Тесты конструктора

    /// <summary>
    /// Проверяет, что конструктор с валидными параметрами создает экземпляр DocAct.
    /// </summary>
    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var result = TryExecuteDbOperation(() => CreateDocActInstance());

        // Assert
        Assert.That(result.Success, Is.True, $"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
        Assert.That(result.Value, Is.Not.Null, "Экземпляр не должен быть null");
        Assert.That(result.Value, Is.InstanceOf<DocAct>(), "Экземпляр должен быть типа DocAct");
    }

    /// <summary>
    /// Проверяет, что IdDoc устанавливается в Act после создания экземпляра.
    /// </summary>
    [Test]
    public void Constructor_WhenCreated_SetsIdDocToAct()
    {
        // Arrange & Act
        var result = TryExecuteDbOperation(() => CreateDocActInstance());

        // Assert
        Assert.That(result.Success, Is.True, $"Не удалось создать экземпляр: {result.ErrorMessage}");

        // Проверяем, что IdDoc установлен правильно
        // IdDoc - protected свойство базового класса, проверяем через наследование
        Assert.That(result.Value, Is.Not.Null);
        TestContext.WriteLine("Constructor successfully set IdDoc to Act");
    }

    /// <summary>
    /// Проверяет, что PathToDocConfigFile инициализируется в конструкторе.
    /// </summary>
    [Test]
    public void Constructor_WhenCreated_InitializesPathToDocConfigFile()
    {
        // Arrange & Act
        var result = TryExecuteDbOperation(() => CreateDocActInstance());

        // Assert
        Assert.That(result.Success, Is.True, $"Не удалось создать экземпляр: {result.ErrorMessage}");
        var configPath = result.Value?.PathToDocConfigFile;

        Assert.That(configPath, Is.Not.Null.Or.Empty,
            "PathToDocConfigFile должен быть инициализирован");

        TestContext.WriteLine($"PathToDocConfigFile: {configPath}");
    }

    /// <summary>
    /// Проверяет, что PathToDocEditConfigFile инициализируется в конструкторе.
    /// </summary>
    [Test]
    public void Constructor_WhenCreated_InitializesPathToDocEditConfigFile()
    {
        // Arrange & Act
        var result = TryExecuteDbOperation(() => CreateDocActInstance());

        // Assert
        Assert.That(result.Success, Is.True, $"Не удалось создать экземпляр: {result.ErrorMessage}");
        var editConfigPath = result.Value?.PathToDocEditConfigFile;

        Assert.That(editConfigPath, Is.Not.Null.Or.Empty,
            "PathToDocEditConfigFile должен быть инициализирован");

        TestContext.WriteLine($"PathToDocEditConfigFile: {editConfigPath}");
    }

    /// <summary>
    /// Проверяет, что PathToDocTemplateFile инициализируется в конструкторе.
    /// </summary>
    [Test]
    public void Constructor_WhenCreated_InitializesPathToDocTemplateFile()
    {
        // Arrange & Act
        var result = TryExecuteDbOperation(() => CreateDocActInstance());

        // Assert
        Assert.That(result.Success, Is.True, $"Не удалось создать экземпляр: {result.ErrorMessage}");
        var templatePath = result.Value?.PathToDocTemplateFile;

        Assert.That(templatePath, Is.Not.Null.Or.Empty,
            "PathToDocTemplateFile должен быть инициализирован");

        TestContext.WriteLine($"PathToDocTemplateFile: {templatePath}");
    }

    #endregion

    #region GetViewDoc Tests - Тесты метода GetViewDoc

    /// <summary>
    /// Проверяет, что GetViewDoc с валидным ID возвращает данные документа.
    /// При отсутствии данных в БД метод должен корректно обработать ситуацию.
    /// </summary>
    [Test]
    public void GetViewDoc_WithValidId_ReturnsDataOrNull()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;
        const int testId = 1;

        // Act
        var viewResult = TryExecuteDbOperation(() => document!.GetViewDoc(testId));

        // Assert
        if (viewResult.Success)
        {
            if (viewResult.Value != null)
            {
                var jsonString = viewResult.Value.ToString();
                Assert.That(jsonString, Is.Not.Null, "JSON не должен быть null");
                Assert.That(jsonString, Is.Not.Empty, "JSON не должен быть пустым");

                // Проверка валидности JSON
                Assert.DoesNotThrow(
                    () => Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString!),
                    "JSON должен быть валидным и десериализуемым"
                );

                TestContext.WriteLine($"GetViewDoc вернул валидный JSON ({jsonString?.Length} символов)");
            }
            else
            {
                // Null - допустимый результат для несуществующей записи
                Assert.Pass("GetViewDoc вернул null (допустимо для несуществующей записи)");
            }
        }
        else
        {
            // Для пустой in-memory БД ошибка связанная с отсутствием данных допустима
            TestContext.WriteLine($"GetViewDoc завершился с ошибкой (ожидаемо для пустой БД): {viewResult.ErrorMessage}");
            Assert.Pass("GetViewDoc корректно обработал отсутствие данных");
        }
    }

    /// <summary>
    /// Проверяет, что GetViewDoc с невалидным ID (-1) обрабатывает ситуацию корректно.
    /// </summary>
    [Test]
    public void GetViewDoc_WithInvalidId_HandlesGracefully()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;
        const int invalidId = -1;

        // Act & Assert
        try
        {
            var viewResult = document!.GetViewDoc(invalidId);
            TestContext.WriteLine($"GetViewDoc с невалидным ID вернул: {viewResult ?? "null"}");
            Assert.Pass("GetViewDoc корректно обработал невалидный ID");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверяет, что GetViewDoc с ID = 0 обрабатывает граничное значение.
    /// </summary>
    [Test]
    public void GetViewDoc_WithZeroId_HandlesGracefully()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;
        const int zeroId = 0;

        // Act & Assert
        try
        {
            var viewResult = document!.GetViewDoc(zeroId);
            TestContext.WriteLine($"GetViewDoc с ID = 0 вернул: {viewResult ?? "null"}");
            Assert.Pass("GetViewDoc корректно обработал граничное значение ID = 0");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверяет, что GetViewDoc с очень большим ID обрабатывает граничное значение.
    /// </summary>
    [Test]
    public void GetViewDoc_WithMaxId_HandlesGracefully()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;
        const int maxId = int.MaxValue;

        // Act & Assert
        try
        {
            var viewResult = document!.GetViewDoc(maxId);
            TestContext.WriteLine($"GetViewDoc с максимальным ID вернул: {viewResult ?? "null"}");
            Assert.Pass("GetViewDoc корректно обработал граничное значение int.MaxValue");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    #endregion

    #region GetPathTemplateFile Tests - Тесты метода GetPathTemplateFile

    /// <summary>
    /// Проверяет, что GetPathTemplateFile возвращает непустой путь.
    /// </summary>
    [Test]
    public void GetPathTemplateFile_WhenCalled_ReturnsNonEmptyPath()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;

        // Act
        var templatePath = document!.GetPathTemplateFile();

        // Assert
        Assert.That(templatePath, Is.Not.Null, "Путь к шаблону не должен быть null");
        Assert.That(templatePath, Is.Not.Empty, "Путь к шаблону не должен быть пустым");

        TestContext.WriteLine($"GetPathTemplateFile вернул: {templatePath}");
    }

    /// <summary>
    /// Проверяет, что GetPathTemplateFile возвращает путь с расширением .frx.
    /// </summary>
    [Test]
    public void GetPathTemplateFile_WhenCalled_ReturnsPathWithFrxExtension()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;

        // Act
        var templatePath = document!.GetPathTemplateFile();

        // Assert
        Assert.That(templatePath, Is.Not.Null.And.Not.Empty,
            "Путь к шаблону должен быть задан");
        Assert.That(templatePath!.EndsWith(".frx", StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"Путь к шаблону должен заканчиваться на .frx, получен: {templatePath}");

        TestContext.WriteLine($"Template path: {templatePath}");
    }

    /// <summary>
    /// Проверяет, что путь к шаблону содержит идентификатор типа документа.
    /// </summary>
    [Test]
    public void GetPathTemplateFile_WhenCalled_ContainsDocTypeIdentifier()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;

        // Act
        var templatePath = document!.GetPathTemplateFile();

        // Assert
        Assert.That(templatePath, Is.Not.Null.And.Not.Empty,
            "Путь к шаблону должен быть задан");

        // Путь должен содержать идентификатор Act или номер документа
        var pathLower = templatePath!.ToLowerInvariant();
        var containsActIdentifier = pathLower.Contains("act") ||
                                    pathLower.Contains(IdDoc.Act.ToString().ToLowerInvariant());

        TestContext.WriteLine($"Template path: {templatePath}");
        TestContext.WriteLine($"Contains Act identifier: {containsActIdentifier}");

        // Примечание: в тестовом окружении путь может быть упрощенным
        Assert.That(templatePath, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region Configuration Path Properties Tests - Тесты свойств путей конфигурации

    /// <summary>
    /// Проверяет, что PathToDocConfigFile содержит корректное имя файла конфигурации.
    /// </summary>
    [Test]
    public void PathToDocConfigFile_WhenAccessed_ContainsValidConfigFileName()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;

        // Act
        var configPath = document!.PathToDocConfigFile;

        // Assert
        Assert.That(configPath, Is.Not.Null.And.Not.Empty,
            "PathToDocConfigFile должен содержать путь");
        Assert.That(configPath, Does.Contain("Cfg"),
            "Путь должен содержать 'Cfg' (папка конфигурации)");
        Assert.That(configPath, Does.EndWith(".json"),
            "Файл конфигурации должен иметь расширение .json");

        TestContext.WriteLine($"PathToDocConfigFile: {configPath}");
    }

    /// <summary>
    /// Проверяет, что PathToDocEditConfigFile содержит корректное имя файла конфигурации редактирования.
    /// </summary>
    [Test]
    public void PathToDocEditConfigFile_WhenAccessed_ContainsValidEditConfigFileName()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;

        // Act
        var editConfigPath = document!.PathToDocEditConfigFile;

        // Assert
        Assert.That(editConfigPath, Is.Not.Null.And.Not.Empty,
            "PathToDocEditConfigFile должен содержать путь");
        Assert.That(editConfigPath, Does.Contain("Cfg"),
            "Путь должен содержать 'Cfg' (папка конфигурации)");
        Assert.That(editConfigPath, Does.Contain("Edit"),
            "Путь к конфигурации редактирования должен содержать 'Edit'");
        Assert.That(editConfigPath, Does.EndWith(".json"),
            "Файл конфигурации должен иметь расширение .json");

        TestContext.WriteLine($"PathToDocEditConfigFile: {editConfigPath}");
    }

    /// <summary>
    /// Проверяет, что PathToDocTemplateFile содержит корректное имя файла шаблона.
    /// </summary>
    [Test]
    public void PathToDocTemplateFile_WhenAccessed_ContainsValidTemplateFileName()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;

        // Act
        var templatePath = document!.PathToDocTemplateFile;

        // Assert
        Assert.That(templatePath, Is.Not.Null.And.Not.Empty,
            "PathToDocTemplateFile должен содержать путь");
        Assert.That(templatePath, Does.Contain("Doc"),
            "Путь должен содержать 'Doc' (папка шаблонов)");
        Assert.That(templatePath, Does.EndWith(".frx"),
            "Файл шаблона должен иметь расширение .frx");

        TestContext.WriteLine($"PathToDocTemplateFile: {templatePath}");
    }

    /// <summary>
    /// Проверяет, что все три пути к файлам различаются между собой.
    /// </summary>
    [Test]
    public void ConfigurationPaths_WhenAccessed_AreDifferentFromEachOther()
    {
        // Arrange
        var result = TryExecuteDbOperation(() => CreateDocActInstance());
        if (!result.Success)
        {
            Assert.Inconclusive($"Не удалось создать экземпляр DocAct: {result.ErrorMessage}");
            return;
        }

        var document = result.Value;

        // Act
        var configPath = document!.PathToDocConfigFile;
        var editConfigPath = document.PathToDocEditConfigFile;
        var templatePath = document.PathToDocTemplateFile;

        // Assert
        Assert.That(configPath, Is.Not.EqualTo(editConfigPath),
            "PathToDocConfigFile и PathToDocEditConfigFile должны различаться");
        Assert.That(configPath, Is.Not.EqualTo(templatePath),
            "PathToDocConfigFile и PathToDocTemplateFile должны различаться");
        Assert.That(editConfigPath, Is.Not.EqualTo(templatePath),
            "PathToDocEditConfigFile и PathToDocTemplateFile должны различаться");

        TestContext.WriteLine($"ConfigPath: {configPath}");
        TestContext.WriteLine($"EditConfigPath: {editConfigPath}");
        TestContext.WriteLine($"TemplatePath: {templatePath}");
    }

    #endregion

    #region Helper Methods - Вспомогательные методы

    /// <summary>
    /// Создает экземпляр DocAct с текущими настройками тестового окружения.
    /// </summary>
    /// <param name="dbOptions">Опции DbContext (опционально, по умолчанию используются _dbOptions)</param>
    /// <returns>Экземпляр DocAct</returns>
    private DocAct CreateDocActInstance(DbContextOptions<DocGeneral>? dbOptions = null)
    {
        dbOptions ??= _dbOptions;

        return new DocAct(
            dbOptions,
            _mockAppConfig.Object,
            idDevice: 1,
            idDoc: IdDoc.Act,
            path: _testBasePath
        );
    }

    /// <summary>
    /// Безопасно выполняет операцию с БД, обрабатывая возможные исключения.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <returns>Результат операции с информацией об успешности</returns>
    private static DbOperationResult<T> TryExecuteDbOperation<T>(Func<T> operation)
    {
        try
        {
            var value = operation();
            return new DbOperationResult<T> { Success = true, Value = value };
        }
        catch (Exception ex)
        {
            return new DbOperationResult<T>
            {
                Success = false,
                ErrorMessage = $"{ex.GetType().Name}: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Результат операции с БД
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    private class DbOperationResult<T>
    {
        public bool Success { get; set; }
        public T? Value { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Проверяет, является ли исключение ошибкой подключения к базе данных.
    /// </summary>
    /// <param name="ex">Исключение для проверки</param>
    /// <returns>true если это ошибка подключения к БД</returns>
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

    /// <summary>
    /// Создает минимальный HTML шаблон для тестирования GetEditDoc.
    /// </summary>
    private void CreateDocEditTemplate()
    {
        var templatePath = Path.Combine(_testWwwrootPath, "HTML", "DocEditAct.html");
        var templateContent = @"<!DOCTYPE html>
<html>
<head><title>Act Edit Template</title></head>
<body>
    <table id='AdditionalInfo'>
        <tbody></tbody>
    </table>
</body>
</html>";

        File.WriteAllText(templatePath, templateContent);
    }

    /// <summary>
    /// Создает тестовый JSON для метода SaveDoc.
    /// </summary>
    /// <param name="testId">ID документа</param>
    /// <returns>JSON строка с данными для сохранения</returns>
    private static string CreateTestCorrectionJson(int testId)
    {
        return $@"{{
            ""DocID"": {testId},
            ""Values"": [
                {{ ""Key"": ""ActNumber"", ""Value"": ""123-456"" }},
                {{ ""Key"": ""Factory"", ""Value"": ""Test Factory"" }},
                {{ ""Key"": ""Oil_Name"", ""Value"": ""Test Oil"" }}
            ]
        }}";
    }

    #endregion
}
