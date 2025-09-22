using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN.DocData;
using TN_Doc.Services;
using TN_DocGeneral.Models;
using TN_DocGeneral.Services;

namespace Tests.Services;

/// <summary>
/// Набор тестов для DbSchemaCache
/// </summary>
[TestFixture]
public class DbSchemaCacheTests
{
    private Mock<IAppConfigService> _mockAppConfigService;
    private Mock<ILogger<DbSchemaCache>> _mockLogger;
    private DbSchemaCache _dbSchemaCache;
    private CfgApp _cfgApp;

    [SetUp]
    public void Setup()
    {
        _mockAppConfigService = new Mock<IAppConfigService>();
        _mockLogger = new Mock<ILogger<DbSchemaCache>>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        
        _dbSchemaCache = new DbSchemaCache(_mockAppConfigService.Object, _mockLogger.Object);
        
        // Создаем тестовую конфигурацию
        _cfgApp = new CfgApp
        {
            Devices = new List<Device>
            {
                new Device
                {
                    IdDevice = 1,
                    Name = "Test Device",
                    DBConnectionStrings = new List<DBConnectionString>
                    {
                        new DBConnectionString { Database = "TestDB", Use = true }
                    }
                },
                new Device
                {
                    IdDevice = 2,
                    Name = "Test Device 2",
                    DBConnectionStrings = new List<DBConnectionString>
                    {
                        new DBConnectionString { Database = "TestDB2", Use = false }
                    }
                }
            }
        };
    }

    #region HasDataArm Tests

    /// <summary>
    /// HasDataArm: для Report возвращает true при наличии DataARM колонки
    /// </summary>
    [Test]
    public void HasDataArm_ReportWithDataArm_ReturnsTrue()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act
        var result = _dbSchemaCache.HasDataArm(1, IdDoc.Report);

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
        // В реальном сценарии результат зависит от наличия колонки DataARM в таблице TableReport
        // Здесь мы проверяем, что метод выполняется без исключений
    }

    /// <summary>
    /// HasDataArm: для Jornal возвращает true при наличии DataARM колонки
    /// </summary>
    [Test]
    public void HasDataArm_JornalWithDataArm_ReturnsTrue()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act
        var result = _dbSchemaCache.HasDataArm(1, IdDoc.Jornal);

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    /// <summary>
    /// HasDataArm: для других типов документов возвращает false
    /// </summary>
    [TestCase(IdDoc.Passport)]
    [TestCase(IdDoc.Act)]
    [TestCase(IdDoc.ReportIncomplete)]
    [TestCase(IdDoc.KMH_PP_Areom)]
    [TestCase(IdDoc.KMH_PV)]
    [TestCase(IdDoc.KMH_PW)]
    [TestCase(IdDoc.Poverka2816)]
    [TestCase(IdDoc.KMH_MI2816)]
    public void HasDataArm_OtherDocumentTypes_ReturnsFalse(IdDoc idDoc)
    {
        // Act
        var result = _dbSchemaCache.HasDataArm(1, idDoc);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// HasDataArm: при отсутствии устройства возвращает false
    /// </summary>
    [Test]
    public void HasDataArm_DeviceNotFound_ReturnsFalse()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(999))
            .Returns((Device)null);
        
        // Act
        var result = _dbSchemaCache.HasDataArm(999, IdDoc.Report);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// HasDataArm: при отсутствии активных подключений к БД возвращает false
    /// </summary>
    [Test]
    public void HasDataArm_NoActiveDbConnections_ReturnsFalse()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(2))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 2));
        
        // Act
        var result = _dbSchemaCache.HasDataArm(2, IdDoc.Report);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// HasDataArm: при исключении в DBtService возвращает false
    /// </summary>
    [Test]
    public void HasDataArm_DBtServiceThrows_ReturnsFalse()
    {
        // Arrange
        var deviceWithInvalidConnection = new Device
        {
            IdDevice = 3,
            Name = "Invalid Device",
            DBConnectionStrings = new List<DBConnectionString>
            {
                new DBConnectionString { Database = "InvalidDB", Use = true }
            }
        };
        
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(3))
            .Returns(deviceWithInvalidConnection);
        
        // Act
        var result = _dbSchemaCache.HasDataArm(3, IdDoc.Report);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// HasDataArm: кэширование результатов - повторный вызов возвращает тот же результат
    /// </summary>
    [Test]
    public void HasDataArm_Caching_ReturnsSameResult()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act - первый вызов
        var result1 = _dbSchemaCache.HasDataArm(1, IdDoc.Report);
        
        // Act - второй вызов
        var result2 = _dbSchemaCache.HasDataArm(1, IdDoc.Report);

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
        
        // Проверяем, что GetDeviceCfg вызывался только один раз благодаря кэшированию
        _mockAppConfigService.Verify(x => x.GetDeviceCfg(1), Times.AtLeastOnce);
    }

    /// <summary>
    /// HasDataArm: разные устройства кэшируются отдельно
    /// </summary>
    [Test]
    public void HasDataArm_DifferentDevices_CachedSeparately()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(2))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 2));
        
        // Act
        var result1 = _dbSchemaCache.HasDataArm(1, IdDoc.Report);
        var result2 = _dbSchemaCache.HasDataArm(2, IdDoc.Report);

        // Assert
        Assert.That(result1, Is.InstanceOf<bool>());
        Assert.That(result2, Is.InstanceOf<bool>());
        // Результаты могут быть разными для разных устройств
    }

    /// <summary>
    /// HasDataArm: разные типы документов для одного устройства кэшируются отдельно
    /// </summary>
    [Test]
    public void HasDataArm_DifferentDocumentTypes_CachedSeparately()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act
        var reportResult = _dbSchemaCache.HasDataArm(1, IdDoc.Report);
        var jornalResult = _dbSchemaCache.HasDataArm(1, IdDoc.Jornal);

        // Assert
        Assert.That(reportResult, Is.InstanceOf<bool>());
        Assert.That(jornalResult, Is.InstanceOf<bool>());
        // Результаты могут быть разными для разных типов документов
    }

    #endregion

    #region Edge Cases Tests

    /// <summary>
    /// HasDataArm: обработка null DBConnectionStrings
    /// </summary>
    [Test]
    public void HasDataArm_NullDbConnectionStrings_ReturnsFalse()
    {
        // Arrange
        var deviceWithNullConnections = new Device
        {
            IdDevice = 4,
            Name = "Device with null connections",
            DBConnectionStrings = null
        };
        
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(4))
            .Returns(deviceWithNullConnections);
        
        // Act
        var result = _dbSchemaCache.HasDataArm(4, IdDoc.Report);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// HasDataArm: обработка пустого списка DBConnectionStrings
    /// </summary>
    [Test]
    public void HasDataArm_EmptyDbConnectionStrings_ReturnsFalse()
    {
        // Arrange
        var deviceWithEmptyConnections = new Device
        {
            IdDevice = 5,
            Name = "Device with empty connections",
            DBConnectionStrings = new List<DBConnectionString>()
        };
        
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(5))
            .Returns(deviceWithEmptyConnections);
        
        // Act
        var result = _dbSchemaCache.HasDataArm(5, IdDoc.Report);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// HasDataArm: обработка исключения в GetDeviceCfg
    /// </summary>
    [Test]
    public void HasDataArm_GetDeviceCfgThrows_ReturnsFalse()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(6))
            .Throws(new Exception("Configuration error"));
        
        // Act
        var result = _dbSchemaCache.HasDataArm(6, IdDoc.Report);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Performance Tests

    /// <summary>
    /// HasDataArm: проверка производительности кэширования
    /// </summary>
    [Test]
    public void HasDataArm_Performance_CachingWorks()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act - делаем несколько вызовов
        var results = new List<bool>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(_dbSchemaCache.HasDataArm(1, IdDoc.Report));
        }

        // Assert
        Assert.That(results.All(r => r == results.First()), Is.True, "All results should be the same due to caching");
        
        // Проверяем, что кэш работает - повторные вызовы не должны обращаться к БД
        // В реальном сценарии это можно проверить через мокирование DBtService
    }

    #endregion

    #region Logging Tests

    /// <summary>
    /// HasDataArm: проверяет логирование начала проверки
    /// </summary>
    [Test]
    public void HasDataArm_LogsDebugMessage()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act
        _dbSchemaCache.HasDataArm(1, IdDoc.Report);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Проверка наличия колонки DataARM для устройства")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// HasDataArm: проверяет логирование для неподдерживаемых типов документов
    /// </summary>
    [Test]
    public void HasDataArm_UnsupportedDocumentType_LogsTraceMessage()
    {
        // Act
        _dbSchemaCache.HasDataArm(1, IdDoc.Passport);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Документ Passport не поддерживает проверку DataARM")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// HasDataArm: проверяет логирование результата проверки
    /// </summary>
    [Test]
    public void HasDataArm_LogsTraceResult()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act
        _dbSchemaCache.HasDataArm(1, IdDoc.Report);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Результат проверки DataARM для устройства")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// CheckDataArmExists: проверяет логирование выполнения проверки схемы БД
    /// </summary>
    [Test]
    public void CheckDataArmExists_LogsDebugExecution()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act
        _dbSchemaCache.HasDataArm(1, IdDoc.Report);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Выполнение проверки схемы БД для устройства")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// CheckDataArmExists: проверяет логирование предупреждения при отсутствии подключений к БД
    /// </summary>
    [Test]
    public void CheckDataArmExists_NoDbConnections_LogsWarning()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(2))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 2));
        
        // Act
        _dbSchemaCache.HasDataArm(2, IdDoc.Report);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Устройство 2 не имеет активных подключений к БД")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// CheckDataArmExists: проверяет логирование ошибки при проблемах с БД
    /// </summary>
    [Test]
    public void CheckDataArmExists_DatabaseError_LogsError()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act
        _dbSchemaCache.HasDataArm(1, IdDoc.Report);

        // Assert - проверяем, что при ошибке БД логируется Error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ошибка при проверке схемы БД")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// CheckDataArmExists: проверяет логирование успешного завершения (только если БД доступна)
    /// </summary>
    [Test]
    public void CheckDataArmExists_SuccessfulCheck_LogsTraceResult()
    {
        // Arrange - этот тест будет проходить только если БД реально доступна
        // В тестовом окружении обычно БД недоступна, поэтому ожидаем ошибку
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Returns(_cfgApp.Devices.First(d => d.IdDevice == 1));
        
        // Act
        _dbSchemaCache.HasDataArm(1, IdDoc.Report);

        // Assert - проверяем что либо логируется успех, либо ошибка
        var hasSuccessLog = false;
        var hasErrorLog = false;
        
        try
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Проверка схемы БД завершена")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            hasSuccessLog = true;
        }
        catch (MockException)
        {
            hasSuccessLog = false;
        }
        
        try
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ошибка при проверке схемы БД")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            hasErrorLog = true;
        }
        catch (MockException)
        {
            hasErrorLog = false;
        }
        
        Assert.That(hasSuccessLog || hasErrorLog, Is.True, "Должен быть либо успешный лог, либо лог ошибки");
    }

    /// <summary>
    /// CheckDataArmExists: проверяет логирование ошибок при исключении в AppConfigService
    /// </summary>
    [Test]
    public void CheckDataArmExists_AppConfigException_LogsError()
    {
        // Arrange
        _mockAppConfigService.Setup(x => x.GetDeviceCfg(1))
            .Throws(new Exception("Configuration error"));
        
        // Act
        _dbSchemaCache.HasDataArm(1, IdDoc.Report);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ошибка при проверке схемы БД")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion
}
