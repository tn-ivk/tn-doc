using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;
using TN_Doc.Controllers;
using TN_Doc.Models.DTOs;
using TN.DocData;
using TN_DocGeneral.Dictionaries;
using TN_DocGeneral.Services;

namespace Tests.Controllers;

[TestFixture]
public class DirEditorControllerTests
{
    private Mock<ILogger<DirEditorController>> _mockLogger;
    private DirEditorController _controller;
    private IConfiguration _configuration;
    private string _testBasePath;
    private string _testCfgPath;
    private string _testCfgAppPath;
    private string _testUserPreferencePath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "DirEditorControllerTests");
        Directory.CreateDirectory(_testBasePath);
        Directory.CreateDirectory(Path.Combine(_testBasePath, "Cfg"));
        Directory.CreateDirectory(Path.Combine(_testBasePath, "UserPreference"));
        
        _testCfgPath = Path.Combine(_testBasePath, "Cfg", "Cfg.json");
        _testCfgAppPath = Path.Combine(_testBasePath, "Cfg", "CfgApp.json");
        _testUserPreferencePath = Path.Combine(_testBasePath, "UserPreference");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }

        // Cleanup TestQp.json created in AppContext.BaseDirectory
        var testQpPathAppBase = Path.Combine(AppContext.BaseDirectory, "TestQp.json");
        if (File.Exists(testQpPathAppBase))
        {
            File.Delete(testQpPathAppBase);
        }

        // Cleanup TestQp.json created in Directory.GetCurrentDirectory() (используется для QP тестов)
        var testQpPathCurrentDir = Path.Combine(Directory.GetCurrentDirectory(), "TestQp.json");
        if (File.Exists(testQpPathCurrentDir))
        {
            File.Delete(testQpPathCurrentDir);
        }
    }

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<DirEditorController>>();

        // Setup test configuration
        var configDictionary = new Dictionary<string, string>
        {
            {"CfgDirPath", Path.Combine(_testBasePath, "Cfg")},
            {"RelCfgName", "Cfg.json"},
            {"RelCfgAppName", "CfgApp.json"},
            {"UserPreferenceDirPath", _testUserPreferencePath},
            {"LastUsedTemplateListFileName", "LastUsedTemplateList.json"}
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(configDictionary);
        _configuration = configurationBuilder.Build();

        // Create test data files
        CreateTestConfigFiles();

        _controller = new DirEditorController(_mockLogger.Object, _configuration);
    }

    private void CreateTestConfigFiles()
    {
        // Create test Cfg.json with minimal valid structure for dictionaries
        var testRoot = new Root
        {
            Doc = new Doc
            {
                Settings = new Settings
                {
                    Dictionarys = new Dictionarys
                    {
                        BIKs = new List<BIK> { new BIK { Id = 1, Name = "БИК" } },
                        Directions = new List<Direction> { new Direction { Id = 1, Name = "СИКН" } }
                    }
                }
            }
        };

        File.WriteAllText(_testCfgPath, JsonConvert.SerializeObject(testRoot, Formatting.Indented));

        // Prepare dummy edit config file for QP with Methods/Parameters
        // Note: CombineSafe в сервисе обрезает начальные слэши, поэтому используем относительный путь без них
        var currentDir = AppContext.BaseDirectory;
        var qpRelPath = "TestQp.json"; // relative path without leading slashes (cross-platform)
        var qpFullPath = Path.Combine(currentDir, qpRelPath);
        File.WriteAllText(qpFullPath, JsonConvert.SerializeObject(new { Methods = new object[] { }, Parameters = new object[] { } }, Formatting.Indented));

        // Create test CfgApp.json with a device that has a "Паспорта" document and a used template pointing to our dummy edit config
        var testCfgApp = new CfgApp
        {
            Devices = new List<Device>
            {
                new Device
                {
                    IdDevice = 1,
                    Name = "Test Device",
                    Use = true,
                    Docs = new List<Document>
                    {
                        new Document
                        {
                            Use = true,
                            IdDoc = IdDoc.Passport,
                            Name = "Паспорта",
                            TemplateDocs = new List<TemplateDoc>
                            {
                                new TemplateDoc { Use = true, Id = 1, Name = "Test Template", PathToDocEditConfigFile = Path.DirectorySeparatorChar + "TestQp.json" }
                            }
                        }
                    }
                }
            }
        };

        File.WriteAllText(_testCfgAppPath, JsonConvert.SerializeObject(testCfgApp, Formatting.Indented));

        // Create LastUsedTemplateList.json
        var lastUsedTemplateList = new LastUsedTemplateListCfg
        {
            Devices = new List<LastUsedTemplateList>
            {
                new LastUsedTemplateList
                {
                    IdDevice = 1,
                    LastTemplateList = new List<LastUsedTemplate>
                    {
                        new LastUsedTemplate { IdDoc = IdDoc.Passport, LastTemplateId = 1 }
                    }
                }
            }
        };
        
        File.WriteAllText(
            Path.Combine(_testUserPreferencePath, "LastUsedTemplateList.json"), 
            JsonConvert.SerializeObject(lastUsedTemplateList, Formatting.Indented));
    }

    /// <summary>
    /// Возвращает 200 и валидный JSON со словарями при успешном чтении.
    /// </summary>
    [Test]
    public async Task GetDirAsync_ReturnsOkWithDirectories()
    {
        // Act
        var result = await _controller.GetDirAsync();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.Value, Is.InstanceOf<DirEditDTO>());
        
        var dirEditDto = okResult.Value as DirEditDTO;
        Assert.That(dirEditDto.DirJsonRaw, Is.Not.Null);
        Assert.That(dirEditDto.DirJsonRaw, Is.Not.Empty);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Получения всех справочников")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Возвращает 200 и сохраняет новые словари; последующий GetDirAsync отражает изменения.
    /// </summary>
    [Test]
    public async Task SetDirAsync_ReturnsOkAndUpdatesDirectories()
    {
        // Arrange
        var newDirectories = new Dictionarys
        {
            Directions = new List<Direction>
            {
                new Direction { Id = 2, Name = "Updated Direction" }
            }
        };
        var newDirectoriesJson = JsonConvert.SerializeObject(newDirectories, Formatting.Indented);

        var dirEditDto = new DirEditDTO { DirJsonRaw = newDirectoriesJson };

        // Act
        var result = await _controller.SetDirAsync(dirEditDto);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Установка нового значения словарей")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify that subsequent GetDirAsync reflects the changes
        var getResult = await _controller.GetDirAsync();
        var getOkResult = getResult as OkObjectResult;
        var getDirEditDto = getOkResult.Value as DirEditDTO;
        
        Assert.That(getDirEditDto.DirJsonRaw, Contains.Substring("Updated Direction"));
    }

    /// <summary>
    /// Не должен падать при некорректном JSON словарей (поведение сервиса — логирует ошибку).
    /// </summary>
    [Test]
    public void SetDirAsync_WithInvalidJson_HandlesGracefully()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var dirEditDto = new DirEditDTO { DirJsonRaw = invalidJson };

        // Act & Assert - Should not throw exception
        // The actual behavior depends on the AppConfigService implementation
        // This test verifies that the controller doesn't crash with invalid JSON
        Assert.DoesNotThrowAsync(async () => await _controller.SetDirAsync(dirEditDto));
    }

    #region GetQpConfigsAsync Tests

    /// <summary>
    /// Возвращает 200 и QpEditDto с непустым JSON при успешном получении конфигурации паспортов качества.
    /// </summary>
    [Test]
    public async Task GetQpConfigsAsync_WhenConfigExists_ReturnsOkWithQpConfig()
    {
        // Arrange
        // Файл TestQp.json уже создан в CreateTestConfigFiles()
        // AppConfigService ищет файлы относительно Directory.GetCurrentDirectory()
        EnsureQpConfigFileExists();

        // Act
        var result = await _controller.GetQpConfigsAsync();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.Value, Is.InstanceOf<QpEditDto>());

        var qpEditDto = okResult.Value as QpEditDto;
        Assert.That(qpEditDto.QpCfgJsonRaw, Is.Not.Null);
        Assert.That(qpEditDto.QpCfgJsonRaw, Is.Not.Empty);

        // Проверяем логирование
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Получения конфигурации используемых паспортов качества")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Возвращаемый JSON содержит структуру QpsInfo с информацией о паспортах.
    /// </summary>
    [Test]
    public async Task GetQpConfigsAsync_WhenConfigExists_ReturnsValidQpsInfoStructure()
    {
        // Arrange
        EnsureQpConfigFileExists();

        // Act
        var result = await _controller.GetQpConfigsAsync();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var qpEditDto = okResult.Value as QpEditDto;

        // Проверяем, что JSON содержит ключ QpsInfo
        Assert.That(qpEditDto.QpCfgJsonRaw, Contains.Substring("QpsInfo"));

        // Парсим JSON и проверяем структуру
        var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(qpEditDto.QpCfgJsonRaw);
        Assert.That(jsonObj["QpsInfo"], Is.Not.Null);
        Assert.That(jsonObj["QpsInfo"].Type, Is.EqualTo(Newtonsoft.Json.Linq.JTokenType.Array));
    }

    /// <summary>
    /// Конфигурация паспортов содержит секции Methods и Parameters из файла редактирования.
    /// </summary>
    [Test]
    public async Task GetQpConfigsAsync_WhenConfigExists_ReturnsMethodsAndParameters()
    {
        // Arrange
        EnsureQpConfigFileExists();

        // Act
        var result = await _controller.GetQpConfigsAsync();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var qpEditDto = okResult.Value as QpEditDto;

        var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(qpEditDto.QpCfgJsonRaw);
        var qpsInfo = jsonObj["QpsInfo"] as Newtonsoft.Json.Linq.JArray;

        // Должен быть хотя бы один элемент (Test Template)
        Assert.That(qpsInfo.Count, Is.GreaterThan(0));

        // Первый элемент должен содержать Methods и Parameters
        var firstQp = qpsInfo[0];
        Assert.That(firstQp["Methods"], Is.Not.Null, "Должен содержать секцию Methods");
        Assert.That(firstQp["Parameters"], Is.Not.Null, "Должен содержать секцию Parameters");
    }

    #endregion

    #region SetQpConfigsAsync Tests

    /// <summary>
    /// Возвращает 200 при успешной установке конфигурации паспортов качества.
    /// </summary>
    [Test]
    public async Task SetQpConfigsAsync_WithValidConfig_ReturnsOk()
    {
        // Arrange
        EnsureQpConfigFileExists();

        // Получаем текущую конфигурацию
        var getResult = await _controller.GetQpConfigsAsync();
        var okGetResult = getResult as OkObjectResult;
        var currentConfig = okGetResult.Value as QpEditDto;

        // Модифицируем конфигурацию - добавляем новый метод
        var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(currentConfig.QpCfgJsonRaw);
        var qpsInfo = jsonObj["QpsInfo"] as Newtonsoft.Json.Linq.JArray;
        if (qpsInfo.Count > 0)
        {
            var methods = qpsInfo[0]["Methods"] as Newtonsoft.Json.Linq.JArray ?? new Newtonsoft.Json.Linq.JArray();
            methods.Add(new Newtonsoft.Json.Linq.JObject(
                new Newtonsoft.Json.Linq.JProperty("Id", 999),
                new Newtonsoft.Json.Linq.JProperty("Name", "Test Method")
            ));
            qpsInfo[0]["Methods"] = methods;
        }

        var modifiedConfig = new QpEditDto { QpCfgJsonRaw = jsonObj.ToString() };

        // Act
        var result = await _controller.SetQpConfigsAsync(modifiedConfig);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());

        // Проверяем логирование
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Установка новой конфигурации проекта")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// После SetQpConfigsAsync последующий GetQpConfigsAsync возвращает обновлённые данные.
    /// </summary>
    [Test]
    public async Task SetQpConfigsAsync_WithValidConfig_UpdatesConfigurationPersistently()
    {
        // Arrange
        EnsureQpConfigFileExists();

        // Получаем текущую конфигурацию
        var getResult = await _controller.GetQpConfigsAsync();
        var okGetResult = getResult as OkObjectResult;
        var currentConfig = okGetResult.Value as QpEditDto;

        // Модифицируем конфигурацию - добавляем уникальный параметр
        var uniqueParamName = $"UniqueTestParam_{Guid.NewGuid():N}";
        var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(currentConfig.QpCfgJsonRaw);
        var qpsInfo = jsonObj["QpsInfo"] as Newtonsoft.Json.Linq.JArray;
        if (qpsInfo.Count > 0)
        {
            var parameters = qpsInfo[0]["Parameters"] as Newtonsoft.Json.Linq.JArray ?? new Newtonsoft.Json.Linq.JArray();
            parameters.Add(new Newtonsoft.Json.Linq.JObject(
                new Newtonsoft.Json.Linq.JProperty("Id", 888),
                new Newtonsoft.Json.Linq.JProperty("Name", uniqueParamName)
            ));
            qpsInfo[0]["Parameters"] = parameters;
        }

        var modifiedConfig = new QpEditDto { QpCfgJsonRaw = jsonObj.ToString() };

        // Act
        await _controller.SetQpConfigsAsync(modifiedConfig);

        // Получаем конфигурацию снова
        var verifyResult = await _controller.GetQpConfigsAsync();
        var okVerifyResult = verifyResult as OkObjectResult;
        var verifyConfig = okVerifyResult.Value as QpEditDto;

        // Assert
        Assert.That(verifyConfig.QpCfgJsonRaw, Contains.Substring(uniqueParamName),
            "Обновлённая конфигурация должна содержать добавленный параметр");
    }

    /// <summary>
    /// SetQpConfigsAsync корректно обрабатывает конфигурацию с пустыми массивами Methods и Parameters.
    /// </summary>
    [Test]
    public async Task SetQpConfigsAsync_WithEmptyMethodsAndParameters_ReturnsOk()
    {
        // Arrange
        EnsureQpConfigFileExists();

        // Получаем текущую конфигурацию
        var getResult = await _controller.GetQpConfigsAsync();
        var okGetResult = getResult as OkObjectResult;
        var currentConfig = okGetResult.Value as QpEditDto;

        // Очищаем Methods и Parameters
        var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(currentConfig.QpCfgJsonRaw);
        var qpsInfo = jsonObj["QpsInfo"] as Newtonsoft.Json.Linq.JArray;
        if (qpsInfo.Count > 0)
        {
            qpsInfo[0]["Methods"] = new Newtonsoft.Json.Linq.JArray();
            qpsInfo[0]["Parameters"] = new Newtonsoft.Json.Linq.JArray();
        }

        var modifiedConfig = new QpEditDto { QpCfgJsonRaw = jsonObj.ToString() };

        // Act
        var result = await _controller.SetQpConfigsAsync(modifiedConfig);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    /// <summary>
    /// SetQpConfigsAsync выбрасывает исключение при отсутствии файла конфигурации редактирования.
    /// </summary>
    [Test]
    public void SetQpConfigsAsync_WhenEditConfigFileNotExists_ThrowsFileNotFoundException()
    {
        // Arrange
        // Создаём конфигурацию с несуществующим путём к файлу редактирования
        var invalidConfig = new QpEditDto
        {
            QpCfgJsonRaw = JsonConvert.SerializeObject(new
            {
                QpsInfo = new[]
                {
                    new
                    {
                        EditConfigFilePath = "/NonExistent/Path/Config.json",
                        Name = "Invalid Config",
                        Methods = new object[] { },
                        Parameters = new object[] { }
                    }
                }
            })
        };

        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _controller.SetQpConfigsAsync(invalidConfig));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Создаёт файл конфигурации паспортов качества в Directory.GetCurrentDirectory(),
    /// так как AppConfigService ищет файлы относительно этой директории.
    /// </summary>
    private void EnsureQpConfigFileExists()
    {
        // AppConfigService использует Directory.GetCurrentDirectory() для поиска файлов QP
        var qpFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestQp.json");

        if (!File.Exists(qpFilePath))
        {
            var qpConfig = new
            {
                Methods = new[]
                {
                    new { Id = 1, Name = "ГОСТ Р 51858-2002", Description = "Метод определения качества" }
                },
                Parameters = new[]
                {
                    new { Id = 1, Name = "Плотность", Unit = "кг/м³" },
                    new { Id = 2, Name = "Вязкость", Unit = "мм²/с" }
                }
            };

            File.WriteAllText(qpFilePath, JsonConvert.SerializeObject(qpConfig, Formatting.Indented));
        }
    }

    #endregion

}