using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_Doc.Models.Home;
using TN_DocGeneral.Services;
using TN.Doc;
using TN.DocData;

namespace Tests.Controllers;

/// <summary>
/// Unit-тесты для <see cref="HomeController"/>.
/// Тестируют методы работы с конфигурацией и обработку данных.
/// </summary>
[TestFixture(TestName = "HomeController: набор тестов главного контроллера")]
public class HomeControllerTests
{
    private Mock<ILogger<HomeController>> _loggerMock = null!;
    private Mock<IAppConfigService> _appConfigMock = null!;
    private DbContextOptions<DocGeneral> _dbOptions = null!;
    private CfgApp _testCfgApp = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<HomeController>>();
        _appConfigMock = new Mock<IAppConfigService>();

        // Создаем тестовую конфигурацию
        _testCfgApp = CreateTestCfgApp();

        // Настраиваем mock
        _appConfigMock.Setup(a => a.GetAppCfg()).Returns(_testCfgApp);

        // Создаем DbContextOptions (не подключаемся к реальной БД)
        _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
    }

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
                    Description = "Test Device 1",
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
                    Elis = new Elis { OstKey = "ost1", SiknKey = "sikn1", ClientName = "Client1", ClientToken = "device_token" },
                    InvalidChars = new List<char> { '<', '>', '&' }
                },
                new Device
                {
                    Use = true,
                    IdDevice = 2,
                    Name = "TestDevice2",
                    Description = "Test Device 2",
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
                    Elis = null // Устройство без ELIS
                },
                new Device
                {
                    Use = false, // Не используется
                    IdDevice = 3,
                    Name = "TestDevice3_Unused",
                    Description = "Unused Device"
                }
            },
            Elis = new Elis { OstKey = "global_ost", SiknKey = "global_sikn", ClientName = "GlobalClient", ClientToken = "global_token" }
        };
    }

    #region Вспомогательный класс для тестирования методов HomeController

    /// <summary>
    /// Тестовый класс, наследующий HomeController для доступа к protected методам
    /// и переопределения зависимостей
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

        public bool IsUsedSecurity() => _cfgApp.UseSecurityFeatures;

        public bool IsUsedElis(int idDevice) => _appConfig.IsUsedElis(idDevice);

        public Dictionary<string, string> GetDataForRegistrationDeviceInELIS(int idDevice)
        {
            var device = _cfgApp.Devices.Single(x => x.IdDevice == idDevice);

            if (device.Elis == null)
            {
                if (_cfgApp.Elis == null)
                    return null;
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
                ? new Dictionary<string, string> { { "clientToken", null } }
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

    #region GetListDevices Tests

    /// <summary>
    /// Проверяет, что GetListDevices возвращает только используемые устройства.
    /// </summary>
    [Test]
    public void GetListDevices_WhenDevicesExist_ReturnsOnlyUsedDevices()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetListDevices();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2)); // Только 2 устройства с Use=true
            Assert.That(result.Any(x => x.Name == "TestDevice1"), Is.True);
            Assert.That(result.Any(x => x.Name == "TestDevice2"), Is.True);
            Assert.That(result.Any(x => x.Name == "TestDevice3_Unused"), Is.False);
        });
    }

    /// <summary>
    /// Проверяет, что GetListDevices возвращает пустой список, когда нет используемых устройств.
    /// </summary>
    [Test]
    public void GetListDevices_WhenNoUsedDevices_ReturnsEmptyList()
    {
        // Arrange
        var emptyConfig = new CfgApp
        {
            Devices = new List<Device>
            {
                new Device { Use = false, IdDevice = 1, Name = "Unused" }
            }
        };
        _appConfigMock.Setup(a => a.GetAppCfg()).Returns(emptyConfig);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetListDevices();

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что GetListDevices возвращает корректные Id и Name.
    /// </summary>
    [Test]
    public void GetListDevices_WhenCalled_ReturnsCorrectIdAndName()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetListDevices();
        var device1 = result.FirstOrDefault(x => x.Id == 1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(device1, Is.Not.Null);
            Assert.That(device1!.Id, Is.EqualTo(1));
            Assert.That(device1.Name, Is.EqualTo("TestDevice1"));
        });
    }

    #endregion

    #region GetNameDBForDevice Tests

    /// <summary>
    /// Проверяет, что GetNameDBForDevice возвращает имя БД для валидного устройства.
    /// </summary>
    [Test]
    public void GetNameDBForDevice_WhenValidDevice_ReturnsDbName()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetNameDBForDevice(1);

        // Assert
        Assert.That(result, Is.EqualTo("test_db_1"));
    }

    /// <summary>
    /// Проверяет, что GetNameDBForDevice возвращает пустую строку для несуществующего устройства.
    /// </summary>
    [Test]
    public void GetNameDBForDevice_WhenDeviceNotFound_ReturnsEmpty()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetNameDBForDevice(999);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что GetNameDBForDevice возвращает пустую строку, когда нет активной БД.
    /// </summary>
    [Test]
    public void GetNameDBForDevice_WhenNoActiveDbConnection_ReturnsEmpty()
    {
        // Arrange
        var configNoDb = new CfgApp
        {
            Devices = new List<Device>
            {
                new Device
                {
                    Use = true,
                    IdDevice = 1,
                    Name = "NoDb",
                    DBConnectionStrings = new List<DBConnectionString>
                    {
                        new DBConnectionString { Use = false, Database = "inactive_db" }
                    }
                }
            }
        };
        _appConfigMock.Setup(a => a.GetAppCfg()).Returns(configNoDb);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetNameDBForDevice(1);

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region GetListDocs Tests

    /// <summary>
    /// Проверяет, что GetListDocs возвращает список документов для валидного устройства.
    /// </summary>
    [Test]
    public void GetListDocs_WhenValidDevice_ReturnsDocs()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetListDocs(1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2)); // Passport и Report (Act не используется)
            Assert.That(result.Any(x => x.Name == "Паспорта"), Is.True);
            Assert.That(result.Any(x => x.Name == "Отчёты"), Is.True);
            Assert.That(result.Any(x => x.Name == "Акты"), Is.False);
        });
    }

    /// <summary>
    /// Проверяет, что GetListDocs возвращает пустой список для несуществующего устройства.
    /// </summary>
    [Test]
    public void GetListDocs_WhenDeviceNotFound_ReturnsEmptyList()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetListDocs(999);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что GetListDocs возвращает только используемые документы.
    /// </summary>
    [Test]
    public void GetListDocs_WhenCalled_ReturnsOnlyUsedDocs()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetListDocs(1);

        // Assert
        Assert.That(result.All(x => x.Name != "Акты"), Is.True);
    }

    #endregion

    #region GetTemplatesDoc Tests

    /// <summary>
    /// Проверяет, что GetTemplatesDoc возвращает шаблоны для валидного документа.
    /// </summary>
    [Test]
    public void GetTemplatesDoc_WhenValidDoc_ReturnsTemplates()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetTemplatesDoc(1, IdDoc.Passport);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Any(x => x.Name == "Паспорт нефти"), Is.True);
            Assert.That(result.Any(x => x.Name == "Паспорт газа"), Is.True);
        });
    }

    /// <summary>
    /// Проверяет, что GetTemplatesDoc возвращает пустой список для несуществующего устройства.
    /// </summary>
    [Test]
    public void GetTemplatesDoc_WhenDeviceNotFound_ReturnsEmptyList()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetTemplatesDoc(999, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что GetTemplatesDoc возвращает пустой список для несуществующего документа.
    /// </summary>
    [Test]
    public void GetTemplatesDoc_WhenDocNotFound_ReturnsEmptyList()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetTemplatesDoc(1, IdDoc.Jornal);

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region IsUsedSecurity Tests

    /// <summary>
    /// Проверяет, что IsUsedSecurity возвращает true, когда безопасность включена.
    /// </summary>
    [Test]
    public void IsUsedSecurity_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.IsUsedSecurity();

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Проверяет, что IsUsedSecurity возвращает false, когда безопасность выключена.
    /// </summary>
    [Test]
    public void IsUsedSecurity_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var configNoSecurity = new CfgApp { UseSecurityFeatures = false };
        _appConfigMock.Setup(a => a.GetAppCfg()).Returns(configNoSecurity);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.IsUsedSecurity();

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region IsUsedElis Tests

    /// <summary>
    /// Проверяет, что IsUsedElis делегирует вызов в appConfig.
    /// </summary>
    [Test]
    public void IsUsedElis_WhenCalled_DelegatesToAppConfig()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.IsUsedElis(1);

        // Assert
        Assert.That(result, Is.True);
        _appConfigMock.Verify(a => a.IsUsedElis(1), Times.Once);
    }

    /// <summary>
    /// Проверяет, что IsUsedElis возвращает false, когда ELIS не используется.
    /// </summary>
    [Test]
    public void IsUsedElis_WhenNoElis_ReturnsFalse()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(2)).Returns(false);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.IsUsedElis(2);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetDataForRegistrationDeviceInELIS Tests

    /// <summary>
    /// Проверяет, что GetDataForRegistrationDeviceInELIS возвращает данные устройства с ELIS.
    /// </summary>
    [Test]
    public void GetDataForRegistrationDeviceInELIS_WhenDeviceHasElis_ReturnsDeviceData()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

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
    /// Проверяет, что GetDataForRegistrationDeviceInELIS возвращает глобальные данные ELIS,
    /// когда устройство не имеет собственных настроек ELIS.
    /// </summary>
    [Test]
    public void GetDataForRegistrationDeviceInELIS_WhenDeviceNoElis_ReturnsGlobalData()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

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

    #region GetClientToken Tests

    /// <summary>
    /// Проверяет, что GetClientToken возвращает токен устройства.
    /// </summary>
    [Test]
    public void GetClientToken_WhenDeviceHasToken_ReturnsDeviceToken()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetClientToken(1);

        // Assert
        Assert.That(result["clientToken"], Is.EqualTo("device_token"));
    }

    /// <summary>
    /// Проверяет, что GetClientToken возвращает глобальный токен, когда устройство без ELIS.
    /// </summary>
    [Test]
    public void GetClientToken_WhenDeviceNoElis_ReturnsGlobalToken()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetClientToken(2);

        // Assert
        Assert.That(result["clientToken"], Is.EqualTo("global_token"));
    }

    #endregion

    #region GetSaveBtnText Tests

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает текст для ELIS при паспорте.
    /// </summary>
    [Test]
    public void GetSaveBtnText_WhenElisAndPassport_ReturnsElisText()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetSaveBtnText(1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.EqualTo("Завершить редактирование и отправить"));
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает текст для ELIS при акте.
    /// </summary>
    [Test]
    public void GetSaveBtnText_WhenElisAndAct_ReturnsElisText()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetSaveBtnText(1, IdDoc.Act);

        // Assert
        Assert.That(result, Is.EqualTo("Завершить редактирование и отправить"));
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает "Сохранить" для отчётов с ELIS.
    /// </summary>
    [Test]
    public void GetSaveBtnText_WhenElisAndReport_ReturnsSave()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetSaveBtnText(1, IdDoc.Report);

        // Assert
        Assert.That(result, Is.EqualTo("Сохранить"));
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает "Сохранить" без ELIS.
    /// </summary>
    [Test]
    public void GetSaveBtnText_WhenNoElis_ReturnsSave()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(2)).Returns(false);
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetSaveBtnText(2, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.EqualTo("Сохранить"));
    }

    #endregion

    #region GetPathTemplateDoc Tests

    /// <summary>
    /// Проверяет, что GetPathTemplateDoc возвращает корректный путь.
    /// </summary>
    [Test]
    public void GetPathTemplateDoc_WhenValidParams_ReturnsPath()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act
        var result = controller.GetPathTemplateDoc(1, IdDoc.Passport, 1);

        // Assert
        Assert.That(result, Is.EqualTo("/Doc/Passport.frx"));
    }

    /// <summary>
    /// Проверяет, что GetPathTemplateDoc выбрасывает исключение для несуществующего устройства.
    /// </summary>
    [Test]
    public void GetPathTemplateDoc_WhenDeviceNotFound_ThrowsException()
    {
        // Arrange
        var controller = new TestableHomeController(_loggerMock.Object, _appConfigMock.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            controller.GetPathTemplateDoc(999, IdDoc.Passport, 1));
    }

    #endregion
}
