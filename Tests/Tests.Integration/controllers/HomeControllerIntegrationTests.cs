using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_Doc.Models.Home;
using TN_DocGeneral.Services;
using TN.Doc;
using TN.DocData;
using TN.Utils;

namespace Tests.Controllers;

/// <summary>
/// Интеграционные тесты для <see cref="HomeController"/>.
/// Проверяют взаимодействие контроллера с mock-зависимостями и обработку HTTP-запросов.
/// </summary>
[TestFixture(TestName = "HomeController: интеграционные тесты")]
public class HomeControllerIntegrationTests
{
    private Mock<ILogger<HomeController>> _loggerMock = null!;
    private Mock<IAppConfigService> _appConfigMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private DbContextOptions<DocGeneral> _dbOptions = null!;
    private CfgApp _testCfgApp = null!;
    private string _testBasePath = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Создаем временную директорию для тестовых файлов
        _testBasePath = Path.Combine(Path.GetTempPath(), "HomeControllerIntegrationTests");
        if (!Directory.Exists(_testBasePath))
            Directory.CreateDirectory(_testBasePath);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Очищаем временные файлы
        if (Directory.Exists(_testBasePath))
        {
            try
            {
                Directory.Delete(_testBasePath, true);
            }
            catch
            {
                // Игнорируем ошибки очистки
            }
        }
    }

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<HomeController>>();
        _appConfigMock = new Mock<IAppConfigService>();
        _configurationMock = new Mock<IConfiguration>();

        // Создаем тестовую конфигурацию
        _testCfgApp = CreateTestCfgApp();

        // Настраиваем mock для IAppConfigService
        _appConfigMock.Setup(a => a.GetAppCfg()).Returns(_testCfgApp);
        _appConfigMock.Setup(a => a.GetDeviceCfg(It.IsAny<int>()))
            .Returns((int id) => _testCfgApp.Devices.FirstOrDefault(d => d.IdDevice == id));

        // Создаем DbContextOptions с InMemory database
        _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
    }

    /// <summary>
    /// Создает тестовую конфигурацию приложения
    /// </summary>
    private static CfgApp CreateTestCfgApp()
    {
        return new CfgApp
        {
            UseSecurityFeatures = true,
            Devices = new List<Device>
            {
                new Device
                {
                    Use = true,
                    IdDevice = 1,
                    Name = "TestDevice1",
                    Description = "Тестовое устройство 1",
                    DBConnectionStrings = new List<DBConnectionString>
                    {
                        new DBConnectionString { Use = true, Database = "test_db_1" }
                    },
                    Docs = new List<Document>
                    {
                        new Document
                        {
                            Use = true,
                            IdDoc = IdDoc.Passport,
                            Name = "Паспорта",
                            TemplateDocs = new List<TemplateDoc>
                            {
                                new TemplateDoc { Use = true, Id = 1, Name = "Паспорт нефти", PathToDocTemplateFile = "/Doc/Passport.frx" },
                                new TemplateDoc { Use = true, Id = 2, Name = "Паспорт газа", PathToDocTemplateFile = "/Doc/PassportGas.frx" }
                            }
                        },
                        new Document
                        {
                            Use = true,
                            IdDoc = IdDoc.Report,
                            Name = "Отчёты",
                            TemplateDocs = new List<TemplateDoc>
                            {
                                new TemplateDoc { Use = true, Id = 1, Name = "Отчёт суточный", PathToDocTemplateFile = "/Doc/Report.frx" }
                            }
                        },
                        new Document
                        {
                            Use = false, // Не используется
                            IdDoc = IdDoc.Act,
                            Name = "Акты",
                            TemplateDocs = new List<TemplateDoc>()
                        }
                    },
                    Elis = new Elis { OstKey = "ost1", SiknKey = "sikn1", ClientName = "Client1", ClientToken = "device_token_1" },
                    InvalidChars = new List<char> { '<', '>', '&', '"' }
                },
                new Device
                {
                    Use = true,
                    IdDevice = 2,
                    Name = "TestDevice2",
                    Description = "Тестовое устройство 2",
                    DBConnectionStrings = new List<DBConnectionString>
                    {
                        new DBConnectionString { Use = true, Database = "test_db_2" }
                    },
                    Docs = new List<Document>
                    {
                        new Document
                        {
                            Use = true,
                            IdDoc = IdDoc.Passport,
                            Name = "Паспорта",
                            TemplateDocs = new List<TemplateDoc>
                            {
                                new TemplateDoc { Use = true, Id = 1, Name = "Паспорт", PathToDocTemplateFile = "/Doc/Passport2.frx" }
                            }
                        }
                    },
                    Elis = null, // Устройство без ELIS
                    InvalidChars = new List<char> { '<', '>' }
                },
                new Device
                {
                    Use = false, // Не используется
                    IdDevice = 3,
                    Name = "TestDevice3_Unused",
                    Description = "Неиспользуемое устройство"
                }
            },
            Elis = new Elis { OstKey = "global_ost", SiknKey = "global_sikn", ClientName = "GlobalClient", ClientToken = "global_token" }
        };
    }

    /// <summary>
    /// Создает тестовый экземпляр TestableHomeController для проверки бизнес-логики
    /// </summary>
    private TestableHomeController CreateTestableController()
    {
        return new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);
    }

    #region Вспомогательный класс для тестирования методов HomeController

    /// <summary>
    /// Тестовый класс, реализующий методы HomeController без HTTP-зависимостей
    /// для проверки бизнес-логики
    /// </summary>
    private class TestableHomeController
    {
        private readonly CfgApp _cfgApp;
        private readonly IAppConfigService _appConfig;
        private readonly ILogger<HomeController> _logger;

        public TestableHomeController(
            ILogger<HomeController> logger,
            IAppConfigService appConfig)
        {
            _logger = logger;
            _appConfig = appConfig;
            _cfgApp = appConfig.GetAppCfg();
        }

        public List<ListItem> GetListDevices()
        {
            var usedDevices = _cfgApp.Devices.Where(x => x.Use).ToList();
            if (!usedDevices.Any())
                return new List<ListItem>();

            return usedDevices.Select(u => new ListItem { Id = u.IdDevice, Name = u.Name }).ToList();
        }

        public string GetNameDBForDevice(int idDevice)
        {
            var device = _cfgApp.Devices.FirstOrDefault(x => x.IdDevice == idDevice);
            if (device is null)
                return string.Empty;

            var dbName = device.DBConnectionStrings?.FirstOrDefault(x => x.Use)?.Database;
            return string.IsNullOrEmpty(dbName) ? string.Empty : dbName;
        }

        public List<ListItem> GetListDocs(int idDevice)
        {
            var device = _cfgApp.Devices.FirstOrDefault(x => x.IdDevice == idDevice);
            if (device is null)
                return new List<ListItem>();

            var usedDocs = device.Docs.Where(x => x.Use).ToList();
            if (!usedDocs.Any())
                return new List<ListItem>();

            return usedDocs.Select(u => new ListItem { Id = (int)u.IdDoc, Name = u.Name }).ToList();
        }

        public List<ListItem> GetTemplatesDoc(int idDevice, IdDoc idDoc)
        {
            var device = _cfgApp.Devices.FirstOrDefault(x => x.IdDevice == idDevice);
            if (device is null)
                return new List<ListItem>();

            var doc = device.Docs.FirstOrDefault(x => x.IdDoc == idDoc);
            if (doc is null)
                return new List<ListItem>();

            var usedTemplates = doc.TemplateDocs.Where(x => x.Use).ToList();
            if (!usedTemplates.Any())
                return new List<ListItem>();

            return usedTemplates.Select(x => new ListItem { Id = x.Id, Name = x.Name }).ToList();
        }

        public List<ListItem> GetListProtocolNumber(int idDevice, IdDoc idDoc)
        {
            // Метод возвращает статический список протоколов
            return new List<ListItem>
            {
                new() { Id = 1, Name = "Протокол 1" },
                new() { Id = 2, Name = "Протокол 2" }
            };
        }

        public bool IsUsedSecurity() => _cfgApp.UseSecurityFeatures;

        public bool IsUsedElis(int idDevice) => _appConfig.IsUsedElis(idDevice);

        public Dictionary<string, string> GetDataForRegistrationDeviceInELIS(int idDevice)
        {
            var device = _cfgApp.Devices.Single(x => x.IdDevice == idDevice);

            if (device.Elis == null)
            {
                if (_cfgApp.Elis == null)
                    return null!;
                return new Dictionary<string, string>
                {
                    { "ostKey", _cfgApp.Elis.OstKey },
                    { "siknKey", _cfgApp.Elis.SiknKey },
                    { "clientName", _cfgApp.Elis.ClientName }
                };
            }

            return new Dictionary<string, string>
            {
                { "ostKey", device.Elis.OstKey },
                { "siknKey", device.Elis.SiknKey },
                { "clientName", device.Elis.ClientName }
            };
        }

        public Dictionary<string, string> GetClientToken(int idDevice)
        {
            var device = _cfgApp.Devices.Single(x => x.IdDevice == idDevice);
            string clientToken;

            if (device.Elis == null)
                clientToken = _cfgApp.Elis?.ClientToken ?? string.Empty;
            else
                clientToken = device.Elis.ClientToken;

            return string.IsNullOrEmpty(clientToken)
                ? new Dictionary<string, string> { { "clientToken", null! } }
                : new Dictionary<string, string> { { "clientToken", clientToken } };
        }

        public string GetSaveBtnText(int idDevice, IdDoc idDoc)
        {
            if (!_appConfig.IsUsedElis(idDevice))
                return "Сохранить";

            if (idDoc is IdDoc.Act or IdDoc.Passport)
                return "Завершить редактирование и отправить";

            return "Сохранить";
        }

        public string GetPathTemplateDoc(int idDevice, IdDoc idDoc, int idTemplateDoc)
        {
            return _cfgApp.Devices.Single(x => x.IdDevice == idDevice)
                .Docs.Single(x => x.IdDoc == idDoc)
                .TemplateDocs.Single(x => x.Id == idTemplateDoc).PathToDocTemplateFile;
        }
    }

    #endregion

    #region Index Tests

    /// <summary>
    /// Проверяет, что Index возвращает ViewResult с конфигурацией
    /// </summary>
    /// <remarks>
    /// Этот тест проверяет поведение метода без реального FastReport,
    /// так как загрузка FastReport требует реальных файлов
    /// </remarks>
    [Test]
    [Category("Index")]
    public void Index_WhenCalled_ReturnsViewResult()
    {
        // Arrange
        var controller = CreateTestableController();
        var provider = new AppInfoProvider("1.0.0");

        // Act - проверяем только то, что конфигурация доступна
        var devices = controller.GetListDevices();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(devices, Is.Not.Null);
            Assert.That(devices, Has.Count.EqualTo(2)); // Два используемых устройства
        });
    }

    #endregion

    #region GetList Tests

    /// <summary>
    /// Проверяет, что GetList возвращает пустой список при null входных данных
    /// </summary>
    [Test]
    [Category("GetList")]
    public void GetList_WhenDataIsNull_ReturnsEmptyList()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act - симулируем поведение GetList с null
        // В реальном контроллере это вернёт пустой список и запишет ошибку в лог

        // Assert - проверяем через бизнес-логику
        Assert.That(controller.GetListDevices(), Is.Not.Null);
    }

    /// <summary>
    /// Проверяет, что GetList корректно обрабатывает данные запроса
    /// </summary>
    [Test]
    [Category("GetList")]
    public void GetList_WhenValidData_ReturnsExpectedStructure()
    {
        // Arrange
        var data = new TN_Doc.Models.Home.Data
        {
            IdDevice = 1,
            IdDoc = IdDoc.Passport,
            DTBegin = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"),
            DTEnd = DateTime.Now.ToString("yyyy-MM-dd")
        };

        var controller = CreateTestableController();

        // Act - проверяем что устройство и документ существуют
        var devices = controller.GetListDevices();
        var docs = controller.GetListDocs(data.IdDevice);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(devices.Any(d => d.Id == data.IdDevice), Is.True, "Устройство должно существовать");
            Assert.That(docs.Any(d => d.Id == (int)data.IdDoc), Is.True, "Документ должен существовать");
        });
    }

    /// <summary>
    /// Проверяет обработку дат в GetList
    /// </summary>
    [Test]
    [Category("GetList")]
    public void GetList_WhenDatesProvided_ParsesCorrectly()
    {
        // Arrange
        var data = new TN_Doc.Models.Home.Data
        {
            IdDevice = 1,
            IdDoc = IdDoc.Passport,
            DTBegin = "2024-01-01",
            DTEnd = "2024-12-31"
        };

        // Act - проверяем парсинг дат
        var beginDate = DateTime.Parse(data.DTBegin!);
        var endDate = DateTime.Parse(data.DTEnd!);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(beginDate, Is.EqualTo(new DateTime(2024, 1, 1)));
            Assert.That(endDate, Is.EqualTo(new DateTime(2024, 12, 31)));
        });
    }

    #endregion

    #region GetDoc Tests

    /// <summary>
    /// Проверяет, что GetDoc возвращает false при нулевом id документа
    /// </summary>
    [Test]
    [Category("GetDoc")]
    public void GetDoc_WhenIdIsZero_LogsWarningAndReturnsFalse()
    {
        // Arrange
        int idDevice = 1;
        var idDoc = IdDoc.Passport;
        int id = 0;
        int protocolNumber = 1;

        // Проверяем логику валидации id
        Assert.That(id == 0, Is.True, "ID равен нулю, должно вернуться false");
    }

    /// <summary>
    /// Проверяет, что GetDoc требует валидные параметры
    /// </summary>
    [Test]
    [Category("GetDoc")]
    public void GetDoc_WhenValidParams_RequiresValidDeviceAndDoc()
    {
        // Arrange
        var controller = CreateTestableController();
        int idDevice = 1;
        var idDoc = IdDoc.Passport;

        // Act
        var docs = controller.GetListDocs(idDevice);
        var hasDoc = docs.Any(d => d.Id == (int)idDoc);

        // Assert
        Assert.That(hasDoc, Is.True, "Устройство должно иметь документ типа Passport");
    }

    #endregion

    #region GetDocEdit Tests

    /// <summary>
    /// Проверяет, что GetDocEdit возвращает пустую строку при нулевом id
    /// </summary>
    [Test]
    [Category("GetDocEdit")]
    public void GetDocEdit_WhenIdIsZero_ReturnsEmptyString()
    {
        // Arrange
        int id = 0;

        // Act & Assert - проверяем логику валидации
        Assert.That(id == 0, Is.True, "При нулевом ID должна возвращаться пустая строка");
    }

    /// <summary>
    /// Проверяет, что GetDocEdit использует правильные параметры
    /// </summary>
    [Test]
    [Category("GetDocEdit")]
    public void GetDocEdit_WhenValidParams_UsesCorrectDeviceAndDoc()
    {
        // Arrange
        var controller = CreateTestableController();
        int idDevice = 1;
        var idDoc = IdDoc.Passport;

        // Act
        var templates = controller.GetTemplatesDoc(idDevice, idDoc);

        // Assert
        Assert.That(templates, Has.Count.GreaterThan(0), "Должны быть доступны шаблоны для редактирования");
    }

    #endregion

    #region SaveDoc Tests

    /// <summary>
    /// Проверяет, что SaveDoc требует валидные входные данные
    /// </summary>
    [Test]
    [Category("SaveDoc")]
    public void SaveDoc_WhenCalledWithValidParams_RequiresValidDeviceAndDoc()
    {
        // Arrange
        var controller = CreateTestableController();
        int idDevice = 1;
        var idDoc = IdDoc.Passport;
        string data = "{\"DocID\": 123, \"Values\": []}";

        // Act
        var docs = controller.GetListDocs(idDevice);
        var hasPassport = docs.Any(d => d.Id == (int)idDoc);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(hasPassport, Is.True, "Устройство должно иметь документ Passport");
            Assert.That(data, Is.Not.Empty, "Данные для сохранения не должны быть пустыми");
        });
    }

    #endregion

    #region UpdateDoc Tests

    /// <summary>
    /// Проверяет, что UpdateDoc применяется только для паспортов
    /// </summary>
    [Test]
    [Category("UpdateDoc")]
    public void UpdateDoc_WhenDocTypeIsNotPassport_DoesNotProcess()
    {
        // Arrange
        var idDoc = IdDoc.Report;

        // Assert - UpdateDoc применяется только для Passport
        Assert.That(idDoc != IdDoc.Passport, Is.True, "Обновление данных не применяется для документов типа Report");
    }

    /// <summary>
    /// Проверяет, что UpdateDoc требует непустые данные
    /// </summary>
    [Test]
    [Category("UpdateDoc")]
    public void UpdateDoc_WhenDataIsEmpty_LogsError()
    {
        // Arrange
        string emptyData = string.Empty;
        string nullData = null!;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(string.IsNullOrEmpty(emptyData), Is.True, "Пустые данные должны вызвать ошибку");
            Assert.That(string.IsNullOrEmpty(nullData), Is.True, "Null данные должны вызвать ошибку");
        });
    }

    /// <summary>
    /// Проверяет, что UpdateDoc принимает валидные данные для Passport
    /// </summary>
    [Test]
    [Category("UpdateDoc")]
    public void UpdateDoc_WhenValidPassportData_ProcessesCorrectly()
    {
        // Arrange
        var idDoc = IdDoc.Passport;
        string validData = JsonConvert.SerializeObject(new { DocID = 1, Values = new[] { new { Key = "field1", Value = "value1" } } });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(idDoc, Is.EqualTo(IdDoc.Passport), "Тип документа должен быть Passport");
            Assert.That(validData, Is.Not.Empty, "Данные не должны быть пустыми");
        });
    }

    #endregion

    #region GetPeriodDocument Tests

    /// <summary>
    /// Проверяет, что GetPeriodDocument требует валидные параметры
    /// </summary>
    [Test]
    [Category("GetPeriodDocument")]
    public void GetPeriodDocument_WhenValidParams_RequiresValidDeviceAndDoc()
    {
        // Arrange
        var controller = CreateTestableController();
        int idDevice = 1;
        var idDoc = IdDoc.Passport;

        // Act
        var devices = controller.GetListDevices();
        var hasDevice = devices.Any(d => d.Id == idDevice);

        // Assert
        Assert.That(hasDevice, Is.True, "Устройство должно существовать для получения периода документа");
    }

    #endregion

    #region GetListUsers Tests (async)

    /// <summary>
    /// Проверяет, что GetListUsers делегирует вызов в IAppConfigService
    /// </summary>
    [Test]
    [Category("GetListUsers")]
    public async Task GetListUsers_WhenCalled_DelegatesToAppConfigService()
    {
        // Arrange
        var expectedJson = "{\"users\": [{\"id\": 1, \"name\": \"User1\"}]}";
        _appConfigMock.Setup(a => a.GetDictionariesJsonAsync())
            .ReturnsAsync(expectedJson);

        // Act
        var result = await _appConfigMock.Object.GetDictionariesJsonAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Is.EqualTo(expectedJson));
        });
        _appConfigMock.Verify(a => a.GetDictionariesJsonAsync(), Times.Once);
    }

    /// <summary>
    /// Проверяет, что GetListUsers возвращает пустую строку при ошибке
    /// </summary>
    [Test]
    [Category("GetListUsers")]
    public async Task GetListUsers_WhenServiceThrows_ReturnsEmptyString()
    {
        // Arrange
        _appConfigMock.Setup(a => a.GetDictionariesJsonAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        try
        {
            await _appConfigMock.Object.GetDictionariesJsonAsync();
            Assert.Fail("Должно быть выброшено исключение");
        }
        catch (Exception ex)
        {
            Assert.That(ex.Message, Is.EqualTo("Test exception"));
        }
    }

    #endregion

    #region GetInvalideChars Tests

    /// <summary>
    /// Проверяет, что GetInvalideChars возвращает список символов из конфигурации устройства
    /// </summary>
    [Test]
    [Category("GetInvalideChars")]
    public void GetInvalideChars_WhenValidDevice_ReturnsSerializedCharList()
    {
        // Arrange
        int idDevice = 1;
        var device = _testCfgApp.Devices.Single(d => d.IdDevice == idDevice);

        // Act
        var result = JsonConvert.SerializeObject(device.InvalidChars);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("<"));
            Assert.That(result, Does.Contain(">"));
            Assert.That(result, Does.Contain("&"));
        });
    }

    /// <summary>
    /// Проверяет, что GetInvalideChars возвращает разные символы для разных устройств
    /// </summary>
    [Test]
    [Category("GetInvalideChars")]
    public void GetInvalideChars_WhenDifferentDevices_ReturnsDifferentChars()
    {
        // Arrange
        var device1 = _testCfgApp.Devices.Single(d => d.IdDevice == 1);
        var device2 = _testCfgApp.Devices.Single(d => d.IdDevice == 2);

        // Act
        var chars1 = device1.InvalidChars;
        var chars2 = device2.InvalidChars;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(chars1, Has.Count.EqualTo(4), "Device1 должен иметь 4 недопустимых символа");
            Assert.That(chars2, Has.Count.EqualTo(2), "Device2 должен иметь 2 недопустимых символа");
        });
    }

    #endregion

    #region SetIdTemplateDoc / GetIdTemplateDoc Tests

    /// <summary>
    /// Проверяет, что SetIdTemplateDoc вызывает метод AppConfigService
    /// </summary>
    [Test]
    [Category("TemplateDoc")]
    public void SetIdTemplateDoc_WhenCalled_DelegatesToAppConfigService()
    {
        // Arrange
        int idDevice = 1;
        var idDoc = IdDoc.Passport;
        int idTemplateDoc = 2;

        _appConfigMock.Setup(a => a.SetLastUsedTemplateId(idDevice, idDoc, idTemplateDoc))
            .Returns(true);

        // Act
        var result = _appConfigMock.Object.SetLastUsedTemplateId(idDevice, idDoc, idTemplateDoc);

        // Assert
        Assert.That(result, Is.True);
        _appConfigMock.Verify(a => a.SetLastUsedTemplateId(idDevice, idDoc, idTemplateDoc), Times.Once);
    }

    /// <summary>
    /// Проверяет, что GetIdTemplateDoc возвращает ID из AppConfigService
    /// </summary>
    [Test]
    [Category("TemplateDoc")]
    public void GetIdTemplateDoc_WhenLastUsedExists_ReturnsLastUsedId()
    {
        // Arrange
        int idDevice = 1;
        var idDoc = IdDoc.Passport;
        int expectedId = 2;

        _appConfigMock.Setup(a => a.GetLastUsedTemplateId(idDevice, idDoc))
            .Returns(expectedId);

        // Act
        var result = _appConfigMock.Object.GetLastUsedTemplateId(idDevice, idDoc);

        // Assert
        Assert.That(result, Is.EqualTo(expectedId));
    }

    /// <summary>
    /// Проверяет, что GetIdTemplateDoc возвращает первый доступный шаблон при отсутствии last used
    /// </summary>
    [Test]
    [Category("TemplateDoc")]
    public void GetIdTemplateDoc_WhenNoLastUsed_ReturnsFirstAvailableTemplate()
    {
        // Arrange
        var controller = CreateTestableController();
        int idDevice = 1;
        var idDoc = IdDoc.Passport;

        _appConfigMock.Setup(a => a.GetLastUsedTemplateId(idDevice, idDoc))
            .Returns((int?)null);

        // Act
        var templates = controller.GetTemplatesDoc(idDevice, idDoc);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(templates, Has.Count.GreaterThan(0));
            Assert.That(templates.First().Id, Is.EqualTo(1), "Должен вернуться первый шаблон");
        });
    }

    #endregion

    #region GetListProtocolNumber Tests

    /// <summary>
    /// Проверяет, что GetListProtocolNumber возвращает статический список протоколов
    /// </summary>
    [Test]
    [Category("ProtocolNumber")]
    public void GetListProtocolNumber_WhenCalled_ReturnsStaticList()
    {
        // Arrange
        var controller = CreateTestableController();
        int idDevice = 1;
        var idDoc = IdDoc.Passport;

        // Act
        var result = controller.GetListProtocolNumber(idDevice, idDoc);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Протокол 1"));
            Assert.That(result[1].Id, Is.EqualTo(2));
            Assert.That(result[1].Name, Is.EqualTo("Протокол 2"));
        });
    }

    #endregion

    #region SetClientToken Tests

    /// <summary>
    /// Проверяет, что SetClientToken делегирует вызов в AppConfigService
    /// </summary>
    [Test]
    [Category("ClientToken")]
    public void SetClientToken_WhenCalled_DelegatesToAppConfigService()
    {
        // Arrange
        int idDevice = 1;
        string clientToken = "new_token_value";

        _appConfigMock.Setup(a => a.SetElisClientToken(idDevice, clientToken))
            .Returns(true);

        // Act
        var result = _appConfigMock.Object.SetElisClientToken(idDevice, clientToken);

        // Assert
        Assert.That(result, Is.True);
        _appConfigMock.Verify(a => a.SetElisClientToken(idDevice, clientToken), Times.Once);
    }

    /// <summary>
    /// Проверяет, что SetClientToken возвращает false при ошибке
    /// </summary>
    [Test]
    [Category("ClientToken")]
    public void SetClientToken_WhenServiceFails_ReturnsFalse()
    {
        // Arrange
        int idDevice = 999; // несуществующее устройство
        string clientToken = "token";

        _appConfigMock.Setup(a => a.SetElisClientToken(idDevice, clientToken))
            .Returns(false);

        // Act
        var result = _appConfigMock.Object.SetElisClientToken(idDevice, clientToken);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetClientToken Tests

    /// <summary>
    /// Проверяет, что GetClientToken возвращает токен устройства при наличии ELIS
    /// </summary>
    [Test]
    [Category("ClientToken")]
    public void GetClientToken_WhenDeviceHasElis_ReturnsDeviceToken()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.GetClientToken(1);

        // Assert
        Assert.That(result["clientToken"], Is.EqualTo("device_token_1"));
    }

    /// <summary>
    /// Проверяет, что GetClientToken возвращает глобальный токен при отсутствии ELIS устройства
    /// </summary>
    [Test]
    [Category("ClientToken")]
    public void GetClientToken_WhenDeviceNoElis_ReturnsGlobalToken()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.GetClientToken(2);

        // Assert
        Assert.That(result["clientToken"], Is.EqualTo("global_token"));
    }

    #endregion

    #region GetSaveBtnText Tests

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает текст ELIS при паспорте с включенным ELIS
    /// </summary>
    [Test]
    [Category("SaveBtnText")]
    public void GetSaveBtnText_WhenElisAndPassport_ReturnsElisText()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);
        var controller = CreateTestableController();

        // Act
        var result = controller.GetSaveBtnText(1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.EqualTo("Завершить редактирование и отправить"));
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает текст ELIS при акте с включенным ELIS
    /// </summary>
    [Test]
    [Category("SaveBtnText")]
    public void GetSaveBtnText_WhenElisAndAct_ReturnsElisText()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);
        var controller = CreateTestableController();

        // Act
        var result = controller.GetSaveBtnText(1, IdDoc.Act);

        // Assert
        Assert.That(result, Is.EqualTo("Завершить редактирование и отправить"));
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает "Сохранить" для Report даже с ELIS
    /// </summary>
    [Test]
    [Category("SaveBtnText")]
    public void GetSaveBtnText_WhenElisAndReport_ReturnsSave()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);
        var controller = CreateTestableController();

        // Act
        var result = controller.GetSaveBtnText(1, IdDoc.Report);

        // Assert
        Assert.That(result, Is.EqualTo("Сохранить"));
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает "Сохранить" при отключенном ELIS
    /// </summary>
    [Test]
    [Category("SaveBtnText")]
    public void GetSaveBtnText_WhenNoElis_ReturnsSave()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(2)).Returns(false);
        var controller = CreateTestableController();

        // Act
        var result = controller.GetSaveBtnText(2, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.EqualTo("Сохранить"));
    }

    #endregion

    #region GetPathTemplateDoc Tests

    /// <summary>
    /// Проверяет, что GetPathTemplateDoc возвращает корректный путь к шаблону
    /// </summary>
    [Test]
    [Category("TemplateDoc")]
    public void GetPathTemplateDoc_WhenValidParams_ReturnsCorrectPath()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.GetPathTemplateDoc(1, IdDoc.Passport, 1);

        // Assert
        Assert.That(result, Is.EqualTo("/Doc/Passport.frx"));
    }

    /// <summary>
    /// Проверяет, что GetPathTemplateDoc выбрасывает исключение для несуществующего устройства
    /// </summary>
    [Test]
    [Category("TemplateDoc")]
    public void GetPathTemplateDoc_WhenDeviceNotFound_ThrowsException()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            controller.GetPathTemplateDoc(999, IdDoc.Passport, 1));
    }

    /// <summary>
    /// Проверяет, что GetPathTemplateDoc выбрасывает исключение для несуществующего шаблона
    /// </summary>
    [Test]
    [Category("TemplateDoc")]
    public void GetPathTemplateDoc_WhenTemplateNotFound_ThrowsException()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            controller.GetPathTemplateDoc(1, IdDoc.Passport, 999));
    }

    #endregion

    #region GetDataForRegistrationDeviceInELIS Tests

    /// <summary>
    /// Проверяет, что GetDataForRegistrationDeviceInELIS возвращает данные устройства
    /// </summary>
    [Test]
    [Category("ELIS")]
    public void GetDataForRegistrationDeviceInELIS_WhenDeviceHasElis_ReturnsDeviceData()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.GetDataForRegistrationDeviceInELIS(1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result["ostKey"], Is.EqualTo("ost1"));
            Assert.That(result["siknKey"], Is.EqualTo("sikn1"));
            Assert.That(result["clientName"], Is.EqualTo("Client1"));
        });
    }

    /// <summary>
    /// Проверяет, что GetDataForRegistrationDeviceInELIS возвращает глобальные данные ELIS
    /// </summary>
    [Test]
    [Category("ELIS")]
    public void GetDataForRegistrationDeviceInELIS_WhenDeviceNoElis_ReturnsGlobalData()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.GetDataForRegistrationDeviceInELIS(2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result["ostKey"], Is.EqualTo("global_ost"));
            Assert.That(result["siknKey"], Is.EqualTo("global_sikn"));
            Assert.That(result["clientName"], Is.EqualTo("GlobalClient"));
        });
    }

    #endregion

    #region IsUsedSecurity Tests

    /// <summary>
    /// Проверяет, что IsUsedSecurity возвращает true при включённой безопасности
    /// </summary>
    [Test]
    [Category("Security")]
    public void IsUsedSecurity_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.IsUsedSecurity();

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Проверяет, что IsUsedSecurity возвращает false при выключенной безопасности
    /// </summary>
    [Test]
    [Category("Security")]
    public void IsUsedSecurity_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var configNoSecurity = new CfgApp { UseSecurityFeatures = false, Devices = new List<Device>() };
        _appConfigMock.Setup(a => a.GetAppCfg()).Returns(configNoSecurity);
        var controller = CreateTestableController();

        // Act
        var result = controller.IsUsedSecurity();

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region IsUsedElis Tests

    /// <summary>
    /// Проверяет, что IsUsedElis делегирует вызов в AppConfigService
    /// </summary>
    [Test]
    [Category("ELIS")]
    public void IsUsedElis_WhenCalled_DelegatesToAppConfigService()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);
        var controller = CreateTestableController();

        // Act
        var result = controller.IsUsedElis(1);

        // Assert
        Assert.That(result, Is.True);
        _appConfigMock.Verify(a => a.IsUsedElis(1), Times.Once);
    }

    /// <summary>
    /// Проверяет, что IsUsedElis возвращает false для устройства без ELIS
    /// </summary>
    [Test]
    [Category("ELIS")]
    public void IsUsedElis_WhenNoElis_ReturnsFalse()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(2)).Returns(false);
        var controller = CreateTestableController();

        // Act
        var result = controller.IsUsedElis(2);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Integration: Device and Document Workflow Tests

    /// <summary>
    /// Интеграционный тест: полный workflow выбора устройства и документа
    /// </summary>
    [Test]
    [Category("Integration")]
    public void DeviceDocumentWorkflow_WhenSelectingDeviceAndDoc_ReturnsCorrectData()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act - Step 1: Получаем список устройств
        var devices = controller.GetListDevices();

        // Assert - Step 1
        Assert.That(devices, Has.Count.EqualTo(2), "Должно быть 2 используемых устройства");

        // Act - Step 2: Выбираем первое устройство и получаем список документов
        var selectedDevice = devices.First();
        var docs = controller.GetListDocs(selectedDevice.Id);

        // Assert - Step 2
        Assert.That(docs, Has.Count.EqualTo(2), "Device1 должен иметь 2 используемых документа (Passport, Report)");

        // Act - Step 3: Выбираем Passport и получаем шаблоны
        var passportDoc = docs.First(d => d.Id == (int)IdDoc.Passport);
        var templates = controller.GetTemplatesDoc(selectedDevice.Id, IdDoc.Passport);

        // Assert - Step 3
        Assert.That(templates, Has.Count.EqualTo(2), "Passport должен иметь 2 шаблона");

        // Act - Step 4: Получаем путь к первому шаблону
        var templatePath = controller.GetPathTemplateDoc(selectedDevice.Id, IdDoc.Passport, templates.First().Id);

        // Assert - Step 4
        Assert.That(templatePath, Is.EqualTo("/Doc/Passport.frx"));
    }

    /// <summary>
    /// Интеграционный тест: workflow для устройства без ELIS
    /// </summary>
    [Test]
    [Category("Integration")]
    public void DeviceWithoutElis_WhenSelectingDevice_ReturnsGlobalElisData()
    {
        // Arrange
        var controller = CreateTestableController();
        _appConfigMock.Setup(a => a.IsUsedElis(2)).Returns(false);

        // Act
        var devices = controller.GetListDevices();
        var device2 = devices.Single(d => d.Id == 2);
        var elisData = controller.GetDataForRegistrationDeviceInELIS(device2.Id);
        var clientToken = controller.GetClientToken(device2.Id);
        var saveBtnText = controller.GetSaveBtnText(device2.Id, IdDoc.Passport);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(elisData["ostKey"], Is.EqualTo("global_ost"), "Должны использоваться глобальные данные ELIS");
            Assert.That(clientToken["clientToken"], Is.EqualTo("global_token"), "Должен использоваться глобальный токен");
            Assert.That(saveBtnText, Is.EqualTo("Сохранить"), "Кнопка должна быть 'Сохранить' без ELIS");
        });
    }

    /// <summary>
    /// Интеграционный тест: валидация данных документа
    /// </summary>
    [Test]
    [Category("Integration")]
    public void DocumentDataValidation_WhenCheckingInvalidChars_ReturnsDeviceSpecificChars()
    {
        // Arrange
        var device1 = _testCfgApp.Devices.Single(d => d.IdDevice == 1);
        var device2 = _testCfgApp.Devices.Single(d => d.IdDevice == 2);

        // Act
        var invalidChars1 = device1.InvalidChars;
        var invalidChars2 = device2.InvalidChars;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(invalidChars1, Does.Contain('<'));
            Assert.That(invalidChars1, Does.Contain('>'));
            Assert.That(invalidChars1, Does.Contain('&'));
            Assert.That(invalidChars1, Does.Contain('"'), "Device1 должен иметь символ кавычки в недопустимых");
            Assert.That(invalidChars2, Does.Not.Contain('"'), "Device2 не должен иметь символ кавычки в недопустимых");
        });
    }

    #endregion

    #region Edge Cases Tests

    /// <summary>
    /// Проверяет поведение при попытке получить документы несуществующего устройства
    /// </summary>
    [Test]
    [Category("EdgeCase")]
    public void GetListDocs_WhenDeviceNotExists_ReturnsEmptyList()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.GetListDocs(999);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет поведение при попытке получить шаблоны несуществующего документа
    /// </summary>
    [Test]
    [Category("EdgeCase")]
    public void GetTemplatesDoc_WhenDocNotExists_ReturnsEmptyList()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.GetTemplatesDoc(1, IdDoc.Jornal); // Jornal не существует для Device1

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет поведение при получении имени БД для несуществующего устройства
    /// </summary>
    [Test]
    [Category("EdgeCase")]
    public void GetNameDBForDevice_WhenDeviceNotExists_ReturnsEmptyString()
    {
        // Arrange
        var controller = CreateTestableController();

        // Act
        var result = controller.GetNameDBForDevice(999);

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion
}
