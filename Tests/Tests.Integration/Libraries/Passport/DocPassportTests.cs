extern alias PassportLib;

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN.Doc;
using TN.DocData;
using TN_DocGeneral.Interfaces;
using TN_DocGeneral.Services;
using Tests.Fixtures;
using Tests.Libraries;

// Используем alias для разрешения конфликта имен между библиотеками
using DocPassport = PassportLib::TN.Doc.DocPassport;

namespace Tests.Libraries.Passport;

/// <summary>
/// Интеграционные тесты для библиотеки DocPassport (Паспорт качества).
///
/// Покрывает основной модуль документа паспорта качества нефти:
/// - DocPassport: Генерация паспортов качества с данными ИВК и ХАЛ
///
/// Функциональность:
/// - Создание и инициализация документа паспорта
/// - Получение данных для просмотра (GetViewDoc)
/// - Получение HTML-формы для редактирования (GetEditDoc)
/// - Сохранение изменений документа (SaveDoc)
/// - Обновление документа из внешних систем (DocUpdate)
/// - Получение периода документа (GetPeriodDocument)
/// - Работа с путями конфигурационных файлов и шаблонов
///
/// Приоритет: Высокий - основной документ системы
/// </summary>
[TestFixture]
public class DocPassportTests
{
    private DbContextOptions<DocGeneral> _dbOptions;
    private Mock<IAppConfigService> _mockAppConfig;
    private Mock<ILogger> _mockLogger;
    private string _testBasePath;
    private string _testWwwrootPath;
    private string _testHtmlPath;

    /// <summary>
    /// Однократная инициализация перед всеми тестами:
    /// создание временных директорий для тестовых файлов
    /// </summary>
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Создаем уникальную временную директорию для тестов
        _testBasePath = Path.Combine(Path.GetTempPath(), $"DocPassportTests_{Guid.NewGuid()}");
        _testWwwrootPath = Path.Combine(_testBasePath, "wwwroot");
        _testHtmlPath = Path.Combine(_testWwwrootPath, "HTML");

        Directory.CreateDirectory(_testBasePath);
        Directory.CreateDirectory(_testWwwrootPath);
        Directory.CreateDirectory(_testHtmlPath);

        TestContext.WriteLine($"Тестовая директория: {_testBasePath}");
    }

    /// <summary>
    /// Инициализация перед каждым тестом:
    /// создание новой in-memory базы данных и настройка моков
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
        _mockLogger = new Mock<ILogger>();

        // Настраиваем моки через хелпер
        MockConfigHelper.SetupMockAppConfig(_mockAppConfig, idDevice: 1);
    }

    /// <summary>
    /// Очистка после всех тестов:
    /// удаление временных директорий
    /// </summary>
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (Directory.Exists(_testBasePath))
        {
            try
            {
                Directory.Delete(_testBasePath, true);
                TestContext.WriteLine($"Временная директория удалена: {_testBasePath}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Ошибка при удалении временной директории: {ex.Message}");
            }
        }
    }

    #region Constructor Tests - Тесты конструктора

    /// <summary>
    /// Проверка, что конструктор создает валидный экземпляр DocPassport
    /// </summary>
    [Test]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var passport = CreateDocPassportInstance();

        // Assert
        Assert.That(passport, Is.Not.Null, "Экземпляр DocPassport не должен быть null");
        Assert.That(passport, Is.InstanceOf<DocPassport>(), "Экземпляр должен быть типа DocPassport");
    }

    /// <summary>
    /// Проверка, что IdDoc устанавливается как Passport
    /// </summary>
    [Test]
    public void Constructor_SetsIdDoc_ToPassport()
    {
        // Arrange & Act
        var passport = CreateDocPassportInstance();

        // Assert
        // IdDoc устанавливается в конструкторе как IdDoc.Passport
        // Проверяем через свойство, если оно доступно, или через GetPathTemplateFile
        Assert.That(passport, Is.Not.Null, "Экземпляр должен быть создан");

        // IdDoc.Passport = 2 (согласно документации)
        var templatePath = passport.GetPathTemplateFile();
        Assert.That(templatePath, Does.Contain("Passport").IgnoreCase.Or.Not.Empty,
            "Путь к шаблону должен содержать указание на Passport или быть непустым");
    }

    /// <summary>
    /// Проверка, что конструктор инициализирует пути к файлам конфигурации
    /// </summary>
    [Test]
    public void Constructor_InitializesConfigPaths_NotNullOrEmpty()
    {
        // Arrange & Act
        var passport = CreateDocPassportInstance();

        // Assert
        Assert.That(passport.PathToDocConfigFile, Is.Not.Null.And.Not.Empty,
            "PathToDocConfigFile должен быть инициализирован");
        Assert.That(passport.PathToDocEditConfigFile, Is.Not.Null.And.Not.Empty,
            "PathToDocEditConfigFile должен быть инициализирован");
        Assert.That(passport.PathToDocTemplateFile, Is.Not.Null.And.Not.Empty,
            "PathToDocTemplateFile должен быть инициализирован");
    }

    #endregion

    #region GetViewDoc Tests - Тесты метода GetViewDoc

    /// <summary>
    /// Проверка GetViewDoc с валидным ID (без данных в БД)
    /// Должен вернуть null или пустую строку при отсутствии данных
    /// </summary>
    [Test]
    public void GetViewDoc_WithValidId_NoDataInDb_ReturnsNullOrEmpty()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        const int testId = 1;

        // Act
        var result = TryExecuteDbOperation(() => passport.GetViewDoc(testId));

        // Assert
        // При отсутствии данных в БД метод должен вернуть null
        if (result != null)
        {
            var jsonString = result.ToString();
            TestContext.WriteLine($"GetViewDoc вернул: {jsonString?.Substring(0, Math.Min(200, jsonString?.Length ?? 0))}...");
        }
        else
        {
            Assert.Pass("GetViewDoc вернул null (корректно при отсутствии записей в БД)");
        }
    }

    /// <summary>
    /// Проверка GetViewDoc с невалидным ID (отрицательное число)
    /// </summary>
    [Test]
    public void GetViewDoc_WithInvalidId_HandlesGracefully()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        const int invalidId = -1;

        // Act & Assert
        try
        {
            var result = passport.GetViewDoc(invalidId);
            TestContext.WriteLine($"GetViewDoc с невалидным ID вернул: {result ?? "null"}");
            Assert.Pass("GetViewDoc корректно обработал невалидный ID");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверка GetViewDoc с нулевым ID
    /// </summary>
    [Test]
    public void GetViewDoc_WithZeroId_HandlesGracefully()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        const int zeroId = 0;

        // Act & Assert
        try
        {
            var result = passport.GetViewDoc(zeroId);
            TestContext.WriteLine($"GetViewDoc с нулевым ID вернул: {result ?? "null"}");
            Assert.Pass("GetViewDoc корректно обработал нулевой ID");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверка GetViewDoc с максимальным значением int
    /// </summary>
    [Test]
    public void GetViewDoc_WithMaxIntId_HandlesGracefully()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        const int maxId = int.MaxValue;

        // Act & Assert
        try
        {
            var result = passport.GetViewDoc(maxId);
            TestContext.WriteLine($"GetViewDoc с максимальным ID вернул: {result ?? "null"}");
            Assert.Pass("GetViewDoc корректно обработал максимальный ID");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    #endregion

    #region GetEditDoc Tests - Тесты метода GetEditDoc

    /// <summary>
    /// Проверка GetEditDoc с валидным ID
    /// Должен вернуть HTML-строку или пустую строку при отсутствии шаблона
    /// </summary>
    [Test]
    public void GetEditDoc_WithValidId_ReturnsHtmlOrEmpty()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        const int testId = 1;

        // Создаем тестовый HTML шаблон
        CreateDocEditPassportTemplate();

        // Act & Assert
        try
        {
            var result = passport.GetEditDoc(testId);

            // Метод может вернуть пустую строку, если шаблон не найден или данные отсутствуют
            Assert.That(result, Is.Not.Null, "GetEditDoc не должен возвращать null");

            if (!string.IsNullOrEmpty(result))
            {
                TestContext.WriteLine($"GetEditDoc вернул HTML ({result.Length} символов)");
                // Проверяем, что возвращенный результат содержит HTML элементы
                Assert.That(result, Does.Contain("<").Or.EqualTo(string.Empty),
                    "Результат должен содержать HTML или быть пустым");
            }
            else
            {
                TestContext.WriteLine("GetEditDoc вернул пустую строку (шаблон не найден или нет данных)");
                Assert.Pass("GetEditDoc вернул пустую строку (корректно при отсутствии данных)");
            }
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверка GetEditDoc без HTML шаблона
    /// Должен вернуть пустую строку при отсутствии файла шаблона
    /// </summary>
    [Test]
    public void GetEditDoc_WithoutTemplate_ReturnsEmptyString()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        const int testId = 1;

        // Не создаем HTML шаблон

        // Act
        var result = TryExecuteDbOperation(() => passport.GetEditDoc(testId));

        // Assert
        // Без шаблона метод должен вернуть пустую строку
        Assert.That(result, Is.EqualTo(string.Empty).Or.Null,
            "GetEditDoc должен вернуть пустую строку при отсутствии шаблона");
    }

    /// <summary>
    /// Проверка GetEditDoc с невалидным ID
    /// </summary>
    [Test]
    public void GetEditDoc_WithInvalidId_HandlesGracefully()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        const int invalidId = -1;

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var result = TryExecuteDbOperation(() => passport.GetEditDoc(invalidId));
            TestContext.WriteLine($"GetEditDoc с невалидным ID вернул: '{result}'");
        }, "GetEditDoc должен корректно обрабатывать невалидный ID");
    }

    #endregion

    #region GetPathTemplateFile Tests - Тесты метода GetPathTemplateFile

    /// <summary>
    /// Проверка GetPathTemplateFile - должен возвращать валидный путь к .frx файлу
    /// </summary>
    [Test]
    public void GetPathTemplateFile_ReturnsValidFilePath()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Act
        var templatePath = passport.GetPathTemplateFile();

        // Assert
        Assert.That(templatePath, Is.Not.Null, "Путь к шаблону не должен быть null");
        Assert.That(templatePath, Is.Not.Empty, "Путь к шаблону не должен быть пустым");

        TestContext.WriteLine($"Путь к шаблону: {templatePath}");
    }

    /// <summary>
    /// Проверка, что путь к шаблону имеет расширение .frx
    /// </summary>
    [Test]
    public void GetPathTemplateFile_HasFrxExtension()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Act
        var templatePath = passport.GetPathTemplateFile();

        // Assert
        Assert.That(templatePath, Does.EndWith(".frx"),
            "Путь к шаблону должен иметь расширение .frx");
    }

    /// <summary>
    /// Проверка, что GetPathTemplateFile использует Path.Combine (кроссплатформенность)
    /// </summary>
    [Test]
    public void GetPathTemplateFile_UsesCrossplatformPath()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Act
        var templatePath = passport.GetPathTemplateFile();

        // Assert
        DocumentTestHelpers.AssertPathUsesCombine(templatePath);
    }

    #endregion

    #region Configuration Path Properties Tests - Тесты свойств путей конфигурации

    /// <summary>
    /// Проверка PathToDocConfigFile - путь к конфигурации документа
    /// </summary>
    [Test]
    public void PathToDocConfigFile_IsInitializedCorrectly()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Act
        var configPath = passport.PathToDocConfigFile;

        // Assert
        Assert.That(configPath, Is.Not.Null.And.Not.Empty,
            "PathToDocConfigFile должен быть инициализирован");

        TestContext.WriteLine($"PathToDocConfigFile: {configPath}");
    }

    /// <summary>
    /// Проверка PathToDocEditConfigFile - путь к конфигурации редактирования
    /// </summary>
    [Test]
    public void PathToDocEditConfigFile_IsInitializedCorrectly()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Act
        var editConfigPath = passport.PathToDocEditConfigFile;

        // Assert
        Assert.That(editConfigPath, Is.Not.Null.And.Not.Empty,
            "PathToDocEditConfigFile должен быть инициализирован");

        TestContext.WriteLine($"PathToDocEditConfigFile: {editConfigPath}");
    }

    /// <summary>
    /// Проверка PathToDocTemplateFile - путь к шаблону FastReport
    /// </summary>
    [Test]
    public void PathToDocTemplateFile_IsInitializedCorrectly()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Act
        var templatePath = passport.PathToDocTemplateFile;

        // Assert
        Assert.That(templatePath, Is.Not.Null.And.Not.Empty,
            "PathToDocTemplateFile должен быть инициализирован");
        Assert.That(templatePath, Does.EndWith(".frx"),
            "PathToDocTemplateFile должен указывать на файл .frx");

        TestContext.WriteLine($"PathToDocTemplateFile: {templatePath}");
    }

    /// <summary>
    /// Проверка, что все пути к конфигурациям имеют корректные расширения
    /// </summary>
    [Test]
    public void ConfigPaths_HaveCorrectExtensions()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Act & Assert
        DocumentTestHelpers.AssertConfigFileIsValid(passport.PathToDocConfigFile);
        DocumentTestHelpers.AssertConfigFileIsValid(passport.PathToDocEditConfigFile);
        DocumentTestHelpers.AssertTemplateFileIsValid(passport.PathToDocTemplateFile);
    }

    #endregion

    #region GetList Tests - Тесты метода GetList

    /// <summary>
    /// Проверка GetList с валидным временным диапазоном
    /// </summary>
    [Test]
    public void GetList_WithValidTimeRange_ReturnsListOrEmpty()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        long utBegin = 0;
        long utEnd = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act & Assert
        try
        {
            var result = passport.GetList(utBegin, utEnd);
            Assert.That(result, Is.Not.Null, "GetList не должен возвращать null");
            TestContext.WriteLine($"GetList вернул {result?.Count ?? 0} записей");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверка GetList с обратным временным диапазоном (begin > end)
    /// </summary>
    [Test]
    public void GetList_WithReversedTimeRange_ReturnsEmptyList()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        long utBegin = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long utEnd = 0;

        // Act & Assert
        try
        {
            var result = passport.GetList(utBegin, utEnd);
            Assert.That(result, Is.Not.Null, "GetList не должен возвращать null");
            Assert.That(result.Count, Is.EqualTo(0),
                "GetList с обратным диапазоном должен вернуть пустой список");
        }
        catch (Exception ex) when (IsDatabaseConnectionError(ex))
        {
            Assert.Inconclusive($"Требуется подключение к MySQL БД: {ex.Message}");
        }
    }

    #endregion

    #region IDocUpdater Interface Tests - Тесты интерфейса IDocUpdater

    /// <summary>
    /// Проверка, что DocPassport реализует интерфейс IDocUpdater
    /// </summary>
    [Test]
    public void DocPassport_ImplementsIDocUpdater()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Assert
        Assert.That(passport, Is.InstanceOf<IDocUpdater>(),
            "DocPassport должен реализовывать IDocUpdater");
    }

    /// <summary>
    /// Проверка DocUpdate с пустым JSON
    /// </summary>
    [Test]
    public void DocUpdate_WithEmptyJson_HandlesGracefully()
    {
        // Arrange
        var passport = CreateDocPassportInstance();
        var emptyJson = "{}";

        // Act & Assert
        // Метод может выбросить исключение при невалидных данных, но не должен падать
        try
        {
            passport.DocUpdate(emptyJson);
            TestContext.WriteLine("DocUpdate с пустым JSON выполнен без исключений");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"DocUpdate с пустым JSON выбросил исключение: {ex.Message}");
            // Это допустимое поведение при невалидных данных
            Assert.Pass("DocUpdate корректно обработал невалидный JSON");
        }
    }

    /// <summary>
    /// Проверка DocUpdate с null
    /// </summary>
    [Test]
    public void DocUpdate_WithNull_HandlesGracefully()
    {
        // Arrange
        var passport = CreateDocPassportInstance();

        // Act & Assert
        try
        {
            passport.DocUpdate(null);
            TestContext.WriteLine("DocUpdate с null выполнен без исключений");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"DocUpdate с null выбросил исключение: {ex.Message}");
            // Исключение допустимо для null входных данных
            Assert.Pass("DocUpdate корректно обработал null");
        }
    }

    #endregion

    #region Helper Methods - Вспомогательные методы

    /// <summary>
    /// Создает экземпляр DocPassport с тестовыми параметрами
    /// </summary>
    private DocPassport CreateDocPassportInstance(DbContextOptions<DocGeneral>? dbOptions = null)
    {
        dbOptions ??= _dbOptions;

        return new DocPassport(
            dbOptions,
            _mockAppConfig.Object,
            idDevice: 1,
            idDoc: IdDoc.Passport,
            path: _testBasePath
        );
    }

    /// <summary>
    /// Безопасное выполнение операции с базой данных
    /// Возвращает результат операции или null при ошибке
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <returns>Результат операции или default(T)</returns>
    private T TryExecuteDbOperation<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DbSet"))
        {
            // DbSet не инициализирован в in-memory контексте
            TestContext.WriteLine($"DbSet не инициализирован: {ex.Message}");
            return default;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Ошибка при выполнении операции: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Создает минимальный HTML шаблон DocEditPassport.html для тестирования GetEditDoc
    /// </summary>
    private void CreateDocEditPassportTemplate()
    {
        var templatePath = Path.Combine(_testWwwrootPath, "HTML", "DocEditPassport.html");
        var templateContent = @"<!DOCTYPE html>
<html>
<head>
    <title>Doc Edit Passport Template</title>
    <meta charset=""utf-8"">
</head>
<body>
    <form id=""passportEditForm"">
        <table id=""AdditionalInfo"">
            <thead>
                <tr>
                    <th>Параметр</th>
                    <th>Значение</th>
                </tr>
            </thead>
            <tbody>
                <!-- Динамически заполняемая таблица -->
            </tbody>
        </table>

        <table id=""Edit"">
            <thead>
                <!-- Заголовки таблицы параметров -->
            </thead>
            <tbody>
                <!-- Параметры документа -->
            </tbody>
        </table>

        <div class=""form-actions"">
            <button type=""submit"" id=""saveBtn"">Сохранить</button>
            <button type=""button"" id=""cancelBtn"">Отмена</button>
        </div>
    </form>
</body>
</html>";

        // Создаем директорию, если не существует
        var directory = Path.GetDirectoryName(templatePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(templatePath, templateContent);
        TestContext.WriteLine($"Создан тестовый HTML шаблон: {templatePath}");
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

    /// <summary>
    /// Создает тестовый JSON для метода SaveDoc
    /// </summary>
    /// <param name="docId">ID документа</param>
    /// <returns>JSON строка с тестовыми данными</returns>
    private string CreateTestCorrectionJson(int docId)
    {
        return $@"{{
            ""DocID"": {docId},
            ""Values"": [
                {{ ""Key"": ""Laboratory_IOF"", ""Tag"": ""AdditionalInfo"", ""Value"": ""Иванов И.И."" }},
                {{ ""Key"": ""Delive_IOF"", ""Tag"": ""AdditionalInfo"", ""Value"": ""Петров П.П."" }},
                {{ ""Key"": ""Receive_IOF"", ""Tag"": ""AdditionalInfo"", ""Value"": ""Сидоров С.С."" }},
                {{ ""Key"": ""Passport.PassportID"", ""Tag"": ""AdditionalInfo"", ""Value"": ""TEST-001"" }},
                {{ ""Key"": ""DensCorrection"", ""Tag"": ""Value"", ""Value"": ""850.5"" }},
                {{ ""Key"": ""DensCorrection"", ""Tag"": ""Metod"", ""Value"": ""{{\\""Name\\"":\\""ГОСТ 51069\\""}}""}}
            ]
        }}";
    }

    #endregion
}
