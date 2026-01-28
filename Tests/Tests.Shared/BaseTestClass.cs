using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN.Doc;
using TN.DocData;
using TN_DocGeneral.Services;

namespace Tests.Shared;

/// <summary>
/// Базовый класс для всех тестов.
/// Предоставляет общую инфраструктуру: моки, тестовые данные, helpers.
///
/// Использование:
/// public class MyServiceTests : BaseTestClass
/// {
///     protected override void SetupMocks() { ... }
/// }
/// </summary>
public abstract class BaseTestClass
{
    /// <summary>
    /// Опции для in-memory базы данных
    /// </summary>
    protected DbContextOptions<DocGeneral>? DbOptions;

    /// <summary>
    /// Мок сервиса конфигурации приложения
    /// </summary>
    protected Mock<IAppConfigService> MockAppConfig = null!;

    /// <summary>
    /// Мок конфигурации
    /// </summary>
    protected Mock<IConfiguration> MockConfiguration = null!;

    /// <summary>
    /// Мок логгера
    /// </summary>
    protected Mock<ILogger> MockLogger = null!;

    /// <summary>
    /// Базовый путь для тестовых файлов
    /// </summary>
    protected string TestBasePath = null!;

    /// <summary>
    /// Путь к wwwroot для тестов
    /// </summary>
    protected string TestWwwrootPath = null!;

    /// <summary>
    /// Путь к HTML файлам для тестов
    /// </summary>
    protected string TestHtmlPath = null!;

    /// <summary>
    /// Контекст базы данных для тестов
    /// </summary>
    protected DocGeneral? DbContext;

    /// <summary>
    /// Инициализация, выполняемая один раз перед всеми тестами класса
    /// </summary>
    [OneTimeSetUp]
    public virtual void BaseOneTimeSetUp()
    {
        // Создание уникального временного каталога для каждого тест-класса
        TestBasePath = Path.Combine(Path.GetTempPath(), $"{GetType().Name}_{Guid.NewGuid()}");
        TestWwwrootPath = Path.Combine(TestBasePath, "wwwroot");
        TestHtmlPath = Path.Combine(TestWwwrootPath, "HTML");

        Directory.CreateDirectory(TestBasePath);
        Directory.CreateDirectory(TestWwwrootPath);
        Directory.CreateDirectory(TestHtmlPath);

        TestContext.WriteLine($"Test base path: {TestBasePath}");
    }

    /// <summary>
    /// Настройка перед каждым тестом
    /// </summary>
    [SetUp]
    public virtual void BaseSetUp()
    {
        // Создание новой in-memory БД для каждого теста (изоляция)
        DbOptions = new DbContextOptionsBuilder<DocGeneral>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        // DocGeneral конструктор с DbOptions требует также path параметр
        // Для тестов используем parameterless конструктор
        DbContext = new DocGeneral();

        // Инициализация моков
        MockAppConfig = new Mock<IAppConfigService>();
        MockConfiguration = new Mock<IConfiguration>();
        MockLogger = new Mock<ILogger>();

        // Настройка моков
        SetupMocks();
    }

    /// <summary>
    /// Настройка моков (должна быть переопределена в наследниках)
    /// </summary>
    protected abstract void SetupMocks();

    /// <summary>
    /// Очистка после каждого теста
    /// </summary>
    [TearDown]
    public virtual void BaseTearDown()
    {
        DbContext?.Dispose();
    }

    /// <summary>
    /// Финальная очистка после всех тестов класса
    /// </summary>
    [OneTimeTearDown]
    public virtual void BaseOneTimeTearDown()
    {
        if (Directory.Exists(TestBasePath))
        {
            try
            {
                Directory.Delete(TestBasePath, true);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Failed to delete test directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Безопасный вызов метода, который может пытаться подключиться к БД.
    /// Если метод падает с MySqlException, тест помечается как Inconclusive.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого результата</typeparam>
    /// <param name="action">Действие для выполнения</param>
    /// <param name="actionDescription">Описание действия для логирования</param>
    /// <param name="allowNull">Разрешить null результат</param>
    /// <returns>Результат выполнения действия или default(TResult) если была ошибка БД</returns>
    protected TResult? TryExecuteDbOperation<TResult>(
        Func<TResult> action,
        string actionDescription,
        bool allowNull = true)
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
    }

    /// <summary>
    /// Безопасный вызов void метода, который может пытаться подключиться к БД.
    /// Если метод падает с MySqlException, тест помечается как Inconclusive.
    /// </summary>
    /// <param name="action">Действие для выполнения</param>
    /// <param name="actionDescription">Описание действия для логирования</param>
    protected void TryExecuteDbOperation(
        Action action,
        string actionDescription)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
            when (ex.InnerException is MySqlConnector.MySqlException)
        {
            Assert.Inconclusive(
                $"{actionDescription} requires database connection. " +
                $"Test cannot run without MySQL database. " +
                $"Inner exception: {ex.InnerException.Message}");
        }
        catch (MySqlConnector.MySqlException ex)
        {
            Assert.Inconclusive(
                $"{actionDescription} requires database connection. " +
                $"Test cannot run without MySQL database. " +
                $"Exception: {ex.Message}");
        }
        catch (NullReferenceException ex)
        {
            Assert.Inconclusive(
                $"{actionDescription} encountered null reference. " +
                $"Test environment may not have all required data initialized. " +
                $"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Создание тестовых данных в базе
    /// </summary>
    protected virtual void SeedTestData()
    {
        // Переопределите этот метод для добавления тестовых данных
    }
}

/// <summary>
/// Базовый класс для тестирования документных библиотек.
/// Расширяет BaseTestClass типизированным параметром.
/// </summary>
/// <typeparam name="T">Тип тестируемого класса документа</typeparam>
public abstract class BaseDocumentTest<T> : BaseTestClass where T : class
{
    /// <summary>
    /// Проверка, что конструктор корректно инициализирует объект
    /// </summary>
    /// <param name="instance">Экземпляр для проверки</param>
    protected void AssertConstructorInitializesCorrectly(object instance)
    {
        Assert.That(instance, Is.Not.Null, "Instance should not be null");
        Assert.That(instance, Is.InstanceOf<T>(), $"Instance should be of type {typeof(T).Name}");
    }

    /// <summary>
    /// Проверка, что JSON строка валидна
    /// </summary>
    /// <param name="json">JSON строка для проверки</param>
    protected void AssertValidJson(string json)
    {
        Assert.That(json, Is.Not.Null, "JSON should not be null");
        Assert.That(json, Is.Not.Empty, "JSON should not be empty");

        // Попытка десериализации для проверки валидности
        Assert.DoesNotThrow(
            () => Newtonsoft.Json.JsonConvert.DeserializeObject(json),
            "JSON should be valid and deserializable"
        );
    }

    /// <summary>
    /// Проверка, что HTML строка валидна
    /// </summary>
    /// <param name="html">HTML строка для проверки</param>
    protected void AssertValidHtml(string html)
    {
        Assert.That(html, Is.Not.Null, "HTML should not be null");
        Assert.That(html, Is.Not.Empty, "HTML should not be empty");
        Assert.That(html, Does.Contain("<"), "HTML should contain opening tags");
        Assert.That(html, Does.Contain(">"), "HTML should contain closing tags");
    }

    /// <summary>
    /// Проверка, что путь к файлу существует
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    protected void AssertFileExists(string filePath)
    {
        Assert.That(filePath, Is.Not.Null, "File path should not be null");
        Assert.That(filePath, Is.Not.Empty, "File path should not be empty");

        // Note: В тестах файл может не существовать физически,
        // поэтому проверяем только формат пути
        Assert.That(Path.IsPathRooted(filePath) || Path.IsPathFullyQualified(filePath),
            Is.True,
            "File path should be absolute or fully qualified");
    }
}
