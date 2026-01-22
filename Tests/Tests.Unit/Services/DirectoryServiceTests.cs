using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TN_Doc.Models.Services;

namespace Tests.Services;

/// <summary>
/// Тесты для DirectoryService.
/// Проверяют корректность работы сервиса справочников.
/// </summary>
[TestFixture]
public class DirectoryServiceTests
{
    private string _tempDir = null!;
    private string _mainCfgFileName = null!;
    private string _mainAppCfgFileName = null!;
    private string _originalCurrentDirectory = null!;
    private Mock<ILogger<DirectoryService>> _loggerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _originalCurrentDirectory = Directory.GetCurrentDirectory();
        _tempDir = Path.Combine(Path.GetTempPath(), $"DirectoryServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _mainCfgFileName = "CfgMain.json";
        _mainAppCfgFileName = "CfgApp.json";

        var mainCfgContent = @"{
  ""Doc"": {
    ""Settings"": {
      ""Dictionarys"": { ""Users"": [] }
    }
  }
}";
        var mainAppCfgContent = @"{
  ""Devices"": []
}";

        File.WriteAllText(Path.Combine(_tempDir, _mainCfgFileName), mainCfgContent, Encoding.Default);
        File.WriteAllText(Path.Combine(_tempDir, _mainAppCfgFileName), mainAppCfgContent, Encoding.Default);

        Directory.SetCurrentDirectory(_tempDir);
        _loggerMock = new Mock<ILogger<DirectoryService>>();
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(_originalCurrentDirectory);

        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Игнорируем ошибки при удалении временной директории
            }
        }
    }

    #region Constructor Tests

    /// <summary>
    /// Проверяет, что конструктор выбрасывает ArgumentNullException при null mainCfgFile.
    /// </summary>
    [Test]
    public void Constructor_WhenNullMainCfgFile_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DirectoryService(null!, _mainAppCfgFileName, string.Empty, _loggerMock.Object));

        Assert.That(exception!.ParamName, Is.EqualTo("mainCfgFile"));
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает ArgumentNullException при пустом mainCfgFile.
    /// </summary>
    [Test]
    public void Constructor_WhenEmptyMainCfgFile_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DirectoryService(string.Empty, _mainAppCfgFileName, string.Empty, _loggerMock.Object));

        Assert.That(exception!.ParamName, Is.EqualTo("mainCfgFile"));
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает ArgumentNullException при null mainAppCfgFile.
    /// </summary>
    [Test]
    public void Constructor_WhenNullMainAppCfgFile_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DirectoryService(_mainCfgFileName, null!, string.Empty, _loggerMock.Object));

        Assert.That(exception!.ParamName, Is.EqualTo("mainAppCfgFile"));
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает ArgumentNullException при пустом mainAppCfgFile.
    /// </summary>
    [Test]
    public void Constructor_WhenEmptyMainAppCfgFile_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DirectoryService(_mainCfgFileName, string.Empty, string.Empty, _loggerMock.Object));

        Assert.That(exception!.ParamName, Is.EqualTo("mainAppCfgFile"));
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает FileNotFoundException при отсутствии файла.
    /// </summary>
    [Test]
    public void Constructor_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() =>
            new DirectoryService("NonExistentFile.json", _mainAppCfgFileName, string.Empty, _loggerMock.Object));

        Assert.That(exception!.Message, Does.Contain("NonExistentFile.json"));
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает FileNotFoundException при отсутствии файла mainAppCfgFile.
    /// </summary>
    [Test]
    public void Constructor_WhenAppCfgFileNotFound_ThrowsFileNotFoundException()
    {
        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() =>
            new DirectoryService(_mainCfgFileName, "NonExistentAppCfg.json", string.Empty, _loggerMock.Object));

        Assert.That(exception!.Message, Does.Contain("NonExistentAppCfg.json"));
    }

    /// <summary>
    /// Проверяет, что конструктор создаёт экземпляр при валидных путях.
    /// </summary>
    [Test]
    public void Constructor_WhenValidPaths_CreatesInstance()
    {
        // Act
        using var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    /// <summary>
    /// Проверяет, что конструктор создаёт экземпляр без логгера.
    /// </summary>
    [Test]
    public void Constructor_WhenLoggerIsNull_CreatesInstance()
    {
        // Act
        using var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, null);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    #endregion

    #region GetDirectoriesJsonAsync Tests

    /// <summary>
    /// Проверяет, что GetDirectoriesJsonAsync возвращает JSON при валидной конфигурации.
    /// </summary>
    [Test]
    public async Task GetDirectoriesJsonAsync_WhenValidConfig_ReturnsJson()
    {
        // Arrange
        using var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);

        // Act
        var result = await service.GetDirectoriesJsonAsync();

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        var jObject = JObject.Parse(result);
        Assert.That(jObject["Users"], Is.Not.Null);
    }

    /// <summary>
    /// Проверяет, что GetDirectoriesJsonAsync возвращает кэшированное значение при повторном вызове.
    /// </summary>
    [Test]
    public async Task GetDirectoriesJsonAsync_WhenCached_ReturnsCachedValue()
    {
        // Arrange
        using var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);

        // Act
        var result1 = await service.GetDirectoriesJsonAsync();
        var result2 = await service.GetDirectoriesJsonAsync();

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
    }

    /// <summary>
    /// Проверяет, что GetDirectoriesJsonAsync выбрасывает InvalidDataException при отсутствии данных справочников.
    /// </summary>
    [Test]
    public async Task GetDirectoriesJsonAsync_WhenNoDictionarysSection_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidCfgContent = @"{ ""Doc"": { ""Settings"": {} } }";
        File.WriteAllText(Path.Combine(_tempDir, _mainCfgFileName), invalidCfgContent, Encoding.Default);
        using var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidDataException>(async () =>
            await service.GetDirectoriesJsonAsync());

        Assert.That(exception!.Message, Does.Contain("Отсутствует данные по справочникам"));
    }

    #endregion

    #region SetDirectoriesFromJsonAsync Tests

    /// <summary>
    /// Проверяет, что SetDirectoriesFromJsonAsync обновляет файл при валидном JSON.
    /// </summary>
    [Test]
    public async Task SetDirectoriesFromJsonAsync_WhenValidJson_UpdatesFile()
    {
        // Arrange
        using var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);
        var newJson = @"{ ""Users"": [{ ""Name"": ""TestUser"" }] }";

        // Act
        await service.SetDirectoriesFromJsonAsync(newJson);

        // Assert
        var fileContent = File.ReadAllText(Path.Combine(_tempDir, _mainCfgFileName));
        var jObject = JObject.Parse(fileContent);
        var dictionarys = jObject["Doc"]?["Settings"]?["Dictionarys"];
        Assert.That(dictionarys, Is.Not.Null);
        Assert.That(dictionarys!["Users"]!.Count(), Is.EqualTo(1));
        Assert.That(dictionarys["Users"]![0]!["Name"]!.ToString(), Is.EqualTo("TestUser"));
    }

    /// <summary>
    /// Проверяет, что SetDirectoriesFromJsonAsync инвалидирует кэш.
    /// </summary>
    [Test]
    public async Task SetDirectoriesFromJsonAsync_WhenCalled_InvalidatesCache()
    {
        // Arrange
        using var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);
        var originalJson = await service.GetDirectoriesJsonAsync();
        var newJson = @"{ ""Users"": [{ ""Name"": ""UpdatedUser"" }] }";

        // Act
        await service.SetDirectoriesFromJsonAsync(newJson);
        var updatedJson = await service.GetDirectoriesJsonAsync();

        // Assert
        Assert.That(updatedJson, Is.Not.EqualTo(originalJson));
        var jObject = JObject.Parse(updatedJson);
        Assert.That(jObject["Users"]![0]!["Name"]!.ToString(), Is.EqualTo("UpdatedUser"));
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Проверяет, что Dispose корректно освобождает ресурсы FileWatcher.
    /// </summary>
    [Test]
    public void Dispose_WhenCalled_DisposesFileWatcher()
    {
        // Arrange
        var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);

        // Act & Assert - Dispose не должен выбрасывать исключения
        Assert.DoesNotThrow(() => service.Dispose());
    }

    /// <summary>
    /// Проверяет, что повторный вызов Dispose не выбрасывает исключение.
    /// </summary>
    [Test]
    public void Dispose_WhenCalledTwice_DoesNotThrow()
    {
        // Arrange
        var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            service.Dispose();
            service.Dispose();
        });
    }

    #endregion

    #region GetQualityPassportConfigs Tests

    /// <summary>
    /// Проверяет, что GetQualityPassportConfigs возвращает JSON при пустом списке устройств.
    /// </summary>
    [Test]
    public async Task GetQualityPassportConfigs_WhenNoDevices_ReturnsEmptyQpsInfo()
    {
        // Arrange
        using var service = new DirectoryService(_mainCfgFileName, _mainAppCfgFileName, string.Empty, _loggerMock.Object);

        // Act
        var result = await service.GetQualityPassportConfigs();

        // Assert
        Assert.That(result, Is.Not.Null);
        var jObject = JObject.Parse(result);
        Assert.That(jObject["QpsInfo"], Is.Not.Null);
        Assert.That(jObject["QpsInfo"]!.Count(), Is.EqualTo(0));
    }

    #endregion
}
