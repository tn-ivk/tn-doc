using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using TN_DocGeneral.Services;
using TN.DocData;
using Newtonsoft.Json;

namespace Tests.Unit.Services;

[TestFixture]
public class AppConfigServiceTests
{
    private IConfiguration _configuration;
    private AppConfigService _appConfigService;
    private string _testBasePath;
    private string _testCfgPath;
    private string _testCfgAppPath;
    private string _testLastUsedTemplateListPath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Сброс синглтона, чтобы тесты не зависели от порядка выполнения
        var field = typeof(AppConfigService).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, null);
        _testBasePath = Path.Combine(Path.GetTempPath(), "AppConfigServiceTests");
        Directory.CreateDirectory(_testBasePath);
        Directory.CreateDirectory(Path.Combine(_testBasePath, "Cfg"));
        Directory.CreateDirectory(Path.Combine(_testBasePath, "UserPreference"));
        
        // Создаем пути к тестовым файлам конфигурации
        _testCfgPath = Path.Combine(_testBasePath, "Cfg", "Cfg.json");
        _testCfgAppPath = Path.Combine(_testBasePath, "Cfg", "CfgApp.json");
        _testLastUsedTemplateListPath = Path.Combine(_testBasePath, "UserPreference", "LastUsedTemplateList.json");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }

    [SetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        
        var configurationBuilder = new ConfigurationBuilder();
        var configDictionary = new Dictionary<string, string>
        {
            {"CfgDirPath", Path.Combine(_testBasePath, "Cfg")},
            {"RelCfgName", "Cfg.json"},
            {"RelCfgAppName", "CfgApp.json"},
            {"UserPreferenceDirPath", Path.Combine(_testBasePath, "UserPreference")},
            {"LastUsedTemplateListFileName", "LastUsedTemplateList.json"}
        };
        
        configurationBuilder.AddInMemoryCollection(configDictionary);
        _configuration = configurationBuilder.Build();

        // Создаем тестовые данные
        var root = new Root();
        var cfgApp = new CfgApp
        {
            Devices = new List<Device>
            {
                new Device
                {
                    IdDevice = 1,
                    Name = "Test Device TN01",
                    Docs = new List<Document>
                    {
                        new Document
                        {
                            IdDoc = IdDoc.Passport,
                            LastUsedTemplateId = 1,
                            TemplateDocs = new List<TemplateDoc>
                            {
                                new TemplateDoc { Id = 1, Name = "Test Template" }
                            }
                        }
                    },
                    DBConnectionStrings = new List<DBConnectionString>
                    {
                        new DBConnectionString { Use = true, Server = "localhost", Database = "IVK_TN_01_test", Userid = "user", Password = "pass", ConnectionTimeout = 30 }
                    },
                    Elis = new Elis { Use = true }
                },
                new Device
                {
                    IdDevice = 2,
                    Name = "Test Device TN02",
                    Docs = new List<Document>(),
                    DBConnectionStrings = new List<DBConnectionString>
                    {
                        new DBConnectionString { Use = true, Server = "localhost", Database = "IVK_TN_02_test", Userid = "user", Password = "pass", ConnectionTimeout = 30 }
                    },
                    Elis = new Elis { Use = false }
                },
                new Device
                {
                    IdDevice = 3,
                    Name = "Test Device No DB",
                    Docs = new List<Document>(),
                    DBConnectionStrings = new List<DBConnectionString>(),
                    Elis = new Elis { Use = false }
                }
            },
            Elis = new Elis { Use = true }
        };
        var lastUsedTemplateList = new LastUsedTemplateListCfg
        {
            Devices = new List<LastUsedTemplateList>
            {
                new LastUsedTemplateList
                {
                    IdDevice = 1,
                    LastTemplateList = new List<LastUsedTemplate>
                    {
                        new LastUsedTemplate
                        {
                            IdDoc = IdDoc.Passport,
                            LastTemplateId = 1
                        }
                    }
                }
            }
        };

        // Записываем тестовые данные в файлы
        File.WriteAllText(_testCfgPath, JsonConvert.SerializeObject(root));
        File.WriteAllText(_testCfgAppPath, JsonConvert.SerializeObject(cfgApp));
        File.WriteAllText(_testLastUsedTemplateListPath, JsonConvert.SerializeObject(lastUsedTemplateList));

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        _appConfigService = AppConfigService.GetInstance(_configuration);
    }

    /// <summary>
    /// Проверяет, что GetInstance(null) выбрасывает ArgumentNullException.
    /// </summary>
    [Test]
    public void GetInstance_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AppConfigService.GetInstance(null));
    }

    /// <summary>
    /// Проверяет, что при валидной конфигурации возвращается один и тот же синглтон экземпляр.
    /// </summary>
    [Test]
    public void GetInstance_WithValidConfiguration_ShouldReturnSameInstance()
    {
        // Act
        var instance1 = AppConfigService.GetInstance(_configuration);
        var instance2 = AppConfigService.GetInstance(_configuration);

        // Assert
        Assert.That(instance1, Is.SameAs(instance2));
    }

    /// <summary>
    /// Проверяет, что метод GetCfg возвращает объект Root.
    /// </summary>
    [Test]
    public void GetCfg_ShouldReturnRoot()
    {
        // Act
        var result = _appConfigService.GetCfg();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<Root>());
    }

    /// <summary>
    /// Проверяет, что метод GetAppCfg возвращает объект CfgApp и содержит хотя бы одно устройство.
    /// </summary>
    [Test]
    public void GetAppCfg_ShouldReturnCfgApp()
    {
        // Act
        var result = _appConfigService.GetAppCfg();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CfgApp>());
        Assert.That(result.Devices, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(result.Devices[0].IdDevice, Is.EqualTo(1));
    }

    /// <summary>
    /// Проверяет, что при невалидном deviceId метод GetDeviceCfg возвращает null.
    /// </summary>
    [Test]
    public void GetDeviceCfg_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = _appConfigService.GetDeviceCfg(-1);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Проверяет, что при невалидных идентификаторах метод GetDocCfg возвращает null.
    /// </summary>
    [Test]
    public void GetDocCfg_WithInvalidIds_ShouldReturnNull()
    {
        // Act
        var result = _appConfigService.GetDocCfg(-1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Проверяет, что метод GetLastUsedTemplateList возвращает корректный объект и непустые данные.
    /// Перед проверкой выполняется прогрев списка.
    /// </summary>
    [Test]
    public void GetLastUsedTemplateList_ShouldReturnLastUsedTemplateListCfg()
    {
        // warm-up to ensure LastTemplateList is initialized for device 1
        _appConfigService.GetLastUsedTemplateId(1, IdDoc.Passport);

        // Act
        var result = _appConfigService.GetLastUsedTemplateList();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<LastUsedTemplateListCfg>());
        Assert.That(result.Devices, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(result.Devices[0].LastTemplateList, Has.Count.GreaterThanOrEqualTo(1));
    }

    /// <summary>
    /// Проверяет, что при невалидных идентификаторах метод GetLastUsedTemplateId возвращает null.
    /// </summary>
    [Test]
    public void GetLastUsedTemplateId_WithInvalidIds_ShouldReturnNull()
    {
        // Act
        var result = _appConfigService.GetLastUsedTemplateId(-1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Проверяет, что SetLastUsedTemplateId возвращает false для невалидных идентификаторов.
    /// </summary>
    [Test]
    public void SetLastUsedTemplateId_WithInvalidIds_ShouldReturnFalse()
    {
        // Act
        var result = _appConfigService.SetLastUsedTemplateId(-1, IdDoc.Passport, 1);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Проверяет, что при невалидных идентификаторах метод GetPathToDocDll возвращает null.
    /// </summary>
    [Test]
    public void GetPathToDocDll_WithInvalidIds_ShouldReturnNull()
    {
        // Act
        var result = _appConfigService.GetPathToDocDll(-1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Проверяет, что при невалидных идентификаторах GetPathConfigFile возвращает пустую строку.
    /// </summary>
    [Test]
    public void GetPathConfigFile_WithInvalidIds_ShouldReturnEmptyString()
    {
        // Act
        var result = _appConfigService.GetPathConfigFile(-1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что при невалидных идентификаторах GetPathEditConfigFile возвращает пустую строку.
    /// </summary>
    [Test]
    public void GetPathEditConfigFile_WithInvalidIds_ShouldReturnEmptyString()
    {
        // Act
        var result = _appConfigService.GetPathEditConfigFile(-1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что при невалидных идентификаторах GetPathTemplateFile возвращает пустую строку.
    /// </summary>
    [Test]
    public void GetPathTemplateFile_WithInvalidIds_ShouldReturnEmptyString()
    {
        // Act
        var result = _appConfigService.GetPathTemplateFile(-1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что SetElisClientToken возвращает false для невалидного deviceId.
    /// </summary>
    [Test]
    public void SetElisClientToken_WithInvalidDeviceId_ShouldReturnFalse()
    {
        // Act
        var result = _appConfigService.SetElisClientToken(-1, "test-token");

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Проверяет, что GetDeviceType возвращает TN01 для БД с именем IVK_TN_01.
    /// </summary>
    [Test]
    public void GetDeviceType_WhenDatabaseIsIvkTn01_ThenReturnsTN01()
    {
        // Act
        var result = _appConfigService.GetDeviceType(1);

        // Assert
        Assert.That(result, Is.EqualTo(IvkDeviceType.TN01));
    }

    /// <summary>
    /// Проверяет, что GetDeviceType возвращает TN02 для БД с IVK_TN_02 в имени.
    /// </summary>
    [Test]
    public void GetDeviceType_WhenDatabaseIsIvkTn02_ThenReturnsTN02()
    {
        // Act
        var result = _appConfigService.GetDeviceType(2);

        // Assert
        Assert.That(result, Is.EqualTo(IvkDeviceType.TN02));
    }

    /// <summary>
    /// Проверяет, что GetDeviceType возвращает TN01 при пустых строках подключения (по умолчанию).
    /// </summary>
    [Test]
    public void GetDeviceType_WhenNoConnectionStrings_ThenReturnsTN01()
    {
        // Act
        var result = _appConfigService.GetDeviceType(3);

        // Assert
        Assert.That(result, Is.EqualTo(IvkDeviceType.TN01));
    }

    /// <summary>
    /// Проверяет, что GetDeviceType возвращает TN01 для несуществующего устройства (по умолчанию).
    /// </summary>
    [Test]
    public void GetDeviceType_WhenDeviceNotFound_ThenReturnsTN01()
    {
        // Act
        var result = _appConfigService.GetDeviceType(-1);

        // Assert
        Assert.That(result, Is.EqualTo(IvkDeviceType.TN01));
    }
}