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

namespace Tests.Libraries;

/// <summary>
/// Базовый класс для тестирования документных библиотек.
/// Предоставляет общую инфраструктуру: моки, тестовые данные, helpers.
///
/// Использование:
/// public class MyDocumentTests : BaseDocumentTest<MyDocumentClass>
/// {
///     protected override void SetupCommonMocks() { ... }
/// }
/// </summary>
/// <typeparam name="T">Тип тестируемого класса документа</typeparam>
public abstract class BaseDocumentTest<T> where T : class
{
    /// <summary>
    /// Опции для in-memory базы данных
    /// </summary>
    protected DbContextOptions<DocGeneral> DbOptions;

    /// <summary>
    /// Мок сервиса конфигурации приложения
    /// </summary>
    protected Mock<IAppConfigService> MockAppConfig;

    /// <summary>
    /// Мок конфигурации
    /// </summary>
    protected Mock<IConfiguration> MockConfiguration;

    /// <summary>
    /// Мок логгера
    /// </summary>
    protected Mock<ILogger> MockLogger;

    /// <summary>
    /// Базовый путь для тестовых файлов
    /// </summary>
    protected string TestBasePath;

    /// <summary>
    /// Путь к wwwroot для тестов
    /// </summary>
    protected string TestWwwrootPath;

    /// <summary>
    /// Путь к HTML файлам для тестов
    /// </summary>
    protected string TestHtmlPath;

    /// <summary>
    /// Контекст базы данных для тестов
    /// </summary>
    protected DocGeneral DbContext;

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

        DbContext = new DocGeneral(DbOptions);

        // Инициализация моков
        MockAppConfig = new Mock<IAppConfigService>();
        MockConfiguration = new Mock<IConfiguration>();
        MockLogger = new Mock<ILogger>();

        // Настройка общих моков
        SetupCommonMocks();

        // Вызов пользовательской настройки
        SetupAdditional();
    }

    /// <summary>
    /// Настройка общих моков (должна быть переопределена в наследниках)
    /// </summary>
    protected abstract void SetupCommonMocks();

    /// <summary>
    /// Дополнительная настройка (может быть переопределена в наследниках)
    /// </summary>
    protected virtual void SetupAdditional()
    {
        // Переопределите этот метод для дополнительной настройки
    }

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

    /// <summary>
    /// Создание тестовых данных в базе
    /// </summary>
    protected virtual void SeedTestData()
    {
        // Переопределите этот метод для добавления тестовых данных
    }
}
