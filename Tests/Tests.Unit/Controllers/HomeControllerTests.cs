using System.Reflection;
using System.Runtime.CompilerServices;
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
/// Используется Reflection для тестирования реального HomeController без дублирования логики.
/// </summary>
[TestFixture(TestName = "HomeController: набор тестов главного контроллера")]
public class HomeControllerTests
{
    private Mock<ILogger<HomeController>> _loggerMock = null!;
    private Mock<IAppConfigService> _appConfigMock = null!;
    private CfgApp _testCfgApp = null!;
    private HomeController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<HomeController>>();
        _appConfigMock = new Mock<IAppConfigService>();

        // Создаем тестовую конфигурацию
        _testCfgApp = CreateTestCfgApp();

        // Настраиваем mock
        _appConfigMock.Setup(a => a.GetAppCfg()).Returns(_testCfgApp);

        // Создаем экземпляр HomeController через Reflection без вызова конструктора
        // (обходим статический вызов AppConfigService.GetInstance в конструкторе)
        _controller = CreateHomeControllerWithMocks();
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    /// <summary>
    /// Создаёт экземпляр HomeController без вызова конструктора и устанавливает
    /// приватные поля через Reflection для возможности мокирования.
    /// </summary>
    private HomeController CreateHomeControllerWithMocks()
    {
        // Создаём экземпляр без вызова конструктора (обходим статический вызов AppConfigService.GetInstance)
        var controller = (HomeController)RuntimeHelpers.GetUninitializedObject(typeof(HomeController));

        // Устанавливаем приватные поля через Reflection
        SetPrivateField(controller, "_cfgApp", _testCfgApp);
        SetPrivateField(controller, "_appConfig", _appConfigMock.Object);
        SetPrivateField(controller, "_logger", _loggerMock.Object);

        return controller;
    }

    /// <summary>
    /// Устанавливает значение приватного поля объекта через Reflection.
    /// </summary>
    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
            throw new InvalidOperationException($"Поле '{fieldName}' не найдено в типе {target.GetType().Name}");
        field.SetValue(target, value);
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

    #region GetListDevices Tests

    /// <summary>
    /// Проверяет, что GetListDevices возвращает только используемые устройства.
    /// </summary>
    [Test]
    public void GetListDevices_WhenDevicesExist_ReturnsOnlyUsedDevices()
    {
        // Act
        var result = _controller.GetListDevices();

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
        SetPrivateField(_controller, "_cfgApp", emptyConfig);

        // Act
        var result = _controller.GetListDevices();

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что GetListDevices возвращает корректные Id и Name.
    /// </summary>
    [Test]
    public void GetListDevices_WhenCalled_ReturnsCorrectIdAndName()
    {
        // Act
        var result = _controller.GetListDevices();
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
        // Act
        var result = _controller.GetNameDBForDevice(1);

        // Assert
        Assert.That(result, Is.EqualTo("test_db_1"));
    }

    /// <summary>
    /// Проверяет, что GetNameDBForDevice возвращает пустую строку для несуществующего устройства.
    /// </summary>
    [Test]
    public void GetNameDBForDevice_WhenDeviceNotFound_ReturnsEmpty()
    {
        // Act
        var result = _controller.GetNameDBForDevice(999);

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
        SetPrivateField(_controller, "_cfgApp", configNoDb);

        // Act
        var result = _controller.GetNameDBForDevice(1);

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
        // Act
        var result = _controller.GetListDocs(1);

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
        // Act
        var result = _controller.GetListDocs(999);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что GetListDocs возвращает только используемые документы.
    /// </summary>
    [Test]
    public void GetListDocs_WhenCalled_ReturnsOnlyUsedDocs()
    {
        // Act
        var result = _controller.GetListDocs(1);

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
        // Act
        var result = _controller.GetTemplatesDoc(1, IdDoc.Passport);

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
        // Act
        var result = _controller.GetTemplatesDoc(999, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Проверяет, что GetTemplatesDoc возвращает пустой список для несуществующего документа.
    /// </summary>
    [Test]
    public void GetTemplatesDoc_WhenDocNotFound_ReturnsEmptyList()
    {
        // Act
        var result = _controller.GetTemplatesDoc(1, IdDoc.Jornal);

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
        // Act
        var result = _controller.IsUsedSecurity();

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
        SetPrivateField(_controller, "_cfgApp", configNoSecurity);

        // Act
        var result = _controller.IsUsedSecurity();

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region IsUsedElis Tests

    /// <summary>
    /// Проверяет, что IsUsedElis делегирует вызов в appConfig и возвращает true.
    /// </summary>
    [Test]
    public void IsUsedElis_WhenElisEnabled_ReturnsTrue()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);

        // Act
        var result = _controller.IsUsedElis(1);

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

        // Act
        var result = _controller.IsUsedElis(2);

        // Assert
        Assert.That(result, Is.False);
        _appConfigMock.Verify(a => a.IsUsedElis(2), Times.Once);
    }

    #endregion

    #region GetDataForRegistrationDeviceInELIS Tests

    /// <summary>
    /// Проверяет, что GetDataForRegistrationDeviceInELIS возвращает данные устройства с ELIS.
    /// </summary>
    [Test]
    public void GetDataForRegistrationDeviceInELIS_WhenDeviceHasElis_ReturnsDeviceData()
    {
        // Act
        var result = _controller.GetDataForRegistrationDeviceInELIS(1);

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
        // Act
        var result = _controller.GetDataForRegistrationDeviceInELIS(2);

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
        // Act
        var result = _controller.GetClientToken(1);

        // Assert
        Assert.That(result["clientToken"], Is.EqualTo("device_token"));
    }

    /// <summary>
    /// Проверяет, что GetClientToken возвращает глобальный токен, когда устройство без ELIS.
    /// </summary>
    [Test]
    public void GetClientToken_WhenDeviceNoElis_ReturnsGlobalToken()
    {
        // Act
        var result = _controller.GetClientToken(2);

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

        // Act
        var result = _controller.GetSaveBtnText(1, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.EqualTo("Завершить редактирование и отправить"));
        _appConfigMock.Verify(a => a.IsUsedElis(1), Times.Once);
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает текст для ELIS при акте.
    /// </summary>
    [Test]
    public void GetSaveBtnText_WhenElisAndAct_ReturnsElisText()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);

        // Act
        var result = _controller.GetSaveBtnText(1, IdDoc.Act);

        // Assert
        Assert.That(result, Is.EqualTo("Завершить редактирование и отправить"));
        _appConfigMock.Verify(a => a.IsUsedElis(1), Times.Once);
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает "Сохранить" для отчётов с ELIS.
    /// </summary>
    [Test]
    public void GetSaveBtnText_WhenElisAndReport_ReturnsSave()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(1)).Returns(true);

        // Act
        var result = _controller.GetSaveBtnText(1, IdDoc.Report);

        // Assert
        Assert.That(result, Is.EqualTo("Сохранить"));
        _appConfigMock.Verify(a => a.IsUsedElis(1), Times.Once);
    }

    /// <summary>
    /// Проверяет, что GetSaveBtnText возвращает "Сохранить" без ELIS.
    /// </summary>
    [Test]
    public void GetSaveBtnText_WhenNoElis_ReturnsSave()
    {
        // Arrange
        _appConfigMock.Setup(a => a.IsUsedElis(2)).Returns(false);

        // Act
        var result = _controller.GetSaveBtnText(2, IdDoc.Passport);

        // Assert
        Assert.That(result, Is.EqualTo("Сохранить"));
        _appConfigMock.Verify(a => a.IsUsedElis(2), Times.Once);
    }

    #endregion

    #region GetPathTemplateDoc Tests

    /// <summary>
    /// Проверяет, что GetPathTemplateDoc возвращает корректный путь.
    /// </summary>
    [Test]
    public void GetPathTemplateDoc_WhenValidParams_ReturnsPath()
    {
        // Act
        var result = _controller.GetPathTemplateDoc(1, IdDoc.Passport, 1);

        // Assert
        Assert.That(result, Is.EqualTo("/Doc/Passport.frx"));
    }

    /// <summary>
    /// Проверяет, что GetPathTemplateDoc выбрасывает исключение для несуществующего устройства.
    /// </summary>
    [Test]
    public void GetPathTemplateDoc_WhenDeviceNotFound_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _controller.GetPathTemplateDoc(999, IdDoc.Passport, 1));
    }

    #endregion

    #region IsProtocolNumberUsed Tests

    /// <summary>
    /// Проверяет, что IsProtocolNumberUsed возвращает true для документов, использующих номер протокола.
    /// </summary>
    [Test]
    [TestCase(IdDoc.KMH_PP_Areom, true)]
    [TestCase(IdDoc.KMH_PV, true)]
    [TestCase(IdDoc.KMH_PW, true)]
    [TestCase(IdDoc.Poverka2816, true)]
    [TestCase(IdDoc.KMH_MI2816, true)]
    [TestCase(IdDoc.Passport, false)]
    [TestCase(IdDoc.Report, false)]
    [TestCase(IdDoc.Act, false)]
    public void IsProtocolNumberUsed_WhenCalled_ReturnsExpectedResult(IdDoc idDoc, bool expected)
    {
        // Act
        var result = _controller.IsProtocolNumberUsed(idDoc);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region GetListProtocolNumber Tests

    /// <summary>
    /// Проверяет, что GetListProtocolNumber возвращает список протоколов.
    /// </summary>
    [Test]
    public void GetListProtocolNumber_WhenCalled_ReturnsProtocolList()
    {
        // Act
        var result = _controller.GetListProtocolNumber(1, IdDoc.KMH_PV);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Any(x => x.Name == "Протокол 1"), Is.True);
            Assert.That(result.Any(x => x.Name == "Протокол 2"), Is.True);
        });
    }

    #endregion
}
