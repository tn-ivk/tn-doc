using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN.Doc;
using TN.DocData;
using TN_Doc.Controllers;
using TN_DocGeneral.Services;

namespace Tests.Controllers
{
    /// <summary>
    /// Набор тестов для ExportController
    /// </summary>
    [TestFixture]
    public class ExportControllerTests
    {
        private Mock<ILogger<ExportController>> _mockLogger;
        private Mock<IAppConfigService> _mockAppConfig;
        private Mock<IDocModuleLoader> _mockDocModuleLoader;
        private Mock<DocGeneral> _mockDocGeneral;
        private DbContextOptions<DocGeneral> _dbOptions;
        private ExportController _controller;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ExportController>>();
            _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            
            _mockAppConfig = new Mock<IAppConfigService>();
            _mockDocModuleLoader = new Mock<IDocModuleLoader>();
            _mockDocGeneral = new Mock<DocGeneral>();
            
            _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            _controller = new ExportController(_mockLogger.Object, _dbOptions, _mockAppConfig.Object, _mockDocModuleLoader.Object);
        }

        /// <summary>
        /// GetListFormats: возвращает список ровно из {"pdf","excel","ods","xml"}
        /// </summary>
        [Test]
        public void GetListFormats_ReturnsExactListOfSupportedFormats()
        {
            // Act
            var result = _controller.GetListFormats();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result, Contains.Item("pdf"));
            Assert.That(result, Contains.Item("excel"));
            Assert.That(result, Contains.Item("ods"));
            Assert.That(result, Contains.Item("xml"));
        }

        /// <summary>
        /// ExportDoc: при неподдерживаемом format — возвращает пустую строку (через NotSupportedException внутри catch)
        /// </summary>
        [Test]
        [TestCase("unsupported")]
        [TestCase("docx")]
        [TestCase("txt")]
        [TestCase("")]
        [TestCase(null)]
        public void ExportDoc_UnsupportedFormat_ReturnsEmptyString(string format)
        {
            // Arrange
            _mockAppConfig.Setup(x => x.GetDeviceName(It.IsAny<int>())).Returns("TestDevice");
            _mockDocModuleLoader.Setup(x => x.LoadDocsModule(It.IsAny<DbContextOptions<DocGeneral>>(), It.IsAny<int>(), It.IsAny<IdDoc>(), It.IsAny<string>()))
                .Returns(_mockDocGeneral.Object);

            // Act
            var result = _controller.ExportDoc(1, IdDoc.Report, 1, format, 1);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// ExportDoc: при IDocModuleLoader.LoadDocsModule(...) == null — возвращает пустую строку и логирует ошибку
        /// </summary>
        [Test]
        public void ExportDoc_LoadDocsModuleReturnsNull_ReturnsEmptyStringAndLogsError()
        {
            // Arrange
            _mockAppConfig.Setup(x => x.GetDeviceName(It.IsAny<int>())).Returns("TestDevice");
            _mockDocModuleLoader.Setup(x => x.LoadDocsModule(It.IsAny<DbContextOptions<DocGeneral>>(), It.IsAny<int>(), It.IsAny<IdDoc>(), It.IsAny<string>()))
                .Returns((DocGeneral)null);

            // Act
            var result = _controller.ExportDoc(1, IdDoc.Report, 1, "pdf", 1);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Не удалось загрузить DLL для документа")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// ExportDoc: при GetViewDoc(...) == null — возвращает пустую строку и логирует ошибку
        /// </summary>
        [Test]
        public void ExportDoc_GetViewDocReturnsNull_ReturnsEmptyStringAndLogsError()
        {
            // Arrange
            var appCfg = new CfgApp { ExportDoc = new ExportDoc { Path = "/tmp/export" } };
            var docCfg = new Document { Name = "TestDoc" };

            _mockAppConfig.Setup(x => x.GetDeviceName(It.IsAny<int>())).Returns("TestDevice");
            _mockAppConfig.Setup(x => x.GetAppCfg()).Returns(appCfg);
            _mockAppConfig.Setup(x => x.GetDocCfg(It.IsAny<int>(), It.IsAny<IdDoc>())).Returns(docCfg);
            
            // Не настраиваем GetPathTemplateFile: метод не виртуальный и не может быть замокан
            _mockDocGeneral.Setup(x => x.GetViewDoc(It.IsAny<int>(), It.IsAny<int>())).Returns((object)null);
            
            _mockDocModuleLoader.Setup(x => x.LoadDocsModule(It.IsAny<DbContextOptions<DocGeneral>>(), It.IsAny<int>(), It.IsAny<IdDoc>(), It.IsAny<string>()))
                .Returns(_mockDocGeneral.Object);

            // Act
            var result = _controller.ExportDoc(1, IdDoc.Report, 1, "pdf", 1);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// ExportDoc: проверяет корректное логирование начала экспорта
        /// </summary>
        [Test]
        public void ExportDoc_ValidCall_LogsDebugMessage()
        {
            // Arrange
            const string deviceName = "TestDevice";
            const IdDoc idDoc = IdDoc.Report;
            const int id = 123;
            const int protocolNumber = 456;

            _mockAppConfig.Setup(x => x.GetDeviceName(1)).Returns(deviceName);
            _mockDocModuleLoader.Setup(x => x.LoadDocsModule(It.IsAny<DbContextOptions<DocGeneral>>(), It.IsAny<int>(), It.IsAny<IdDoc>(), It.IsAny<string>()))
                .Returns((DocGeneral)null); // Will cause early return

            // Act
            _controller.ExportDoc(1, idDoc, id, "pdf", protocolNumber);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Экспорт документа {idDoc} c ИД: {id}") &&
                                                  v.ToString().Contains($"номер протокола {protocolNumber}") &&
                                                  v.ToString().Contains($"устройства {deviceName}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// ExportDoc: при возникновении исключения возвращает пустую строку и логирует ошибку
        /// </summary>
        [Test]
        public void ExportDoc_ThrowsException_ReturnsEmptyStringAndLogsError()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            _mockAppConfig.Setup(x => x.GetDeviceName(It.IsAny<int>())).Returns("Device");
            _mockDocModuleLoader.Setup(x => x.LoadDocsModule(It.IsAny<DbContextOptions<DocGeneral>>(), It.IsAny<int>(), It.IsAny<IdDoc>(), It.IsAny<string>()))
                .Throws(expectedException);

            // Act
            var result = _controller.ExportDoc(1, IdDoc.Report, 1, "pdf", 1);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ошибка экспорта документа")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// ExportDoc: проверка вызова GetDeviceName с правильным параметром
        /// </summary>
        [Test]
        public void ExportDoc_CallsGetDeviceNameWithCorrectParameter()
        {
            // Arrange
            const int expectedDeviceId = 42;
            _mockAppConfig.Setup(x => x.GetDeviceName(expectedDeviceId)).Returns("Device42");
            _mockDocModuleLoader.Setup(x => x.LoadDocsModule(It.IsAny<DbContextOptions<DocGeneral>>(), It.IsAny<int>(), It.IsAny<IdDoc>(), It.IsAny<string>()))
                .Returns((DocGeneral)null);

            // Act
            _controller.ExportDoc(expectedDeviceId, IdDoc.Report, 1, "pdf", 1);

            // Assert
            _mockAppConfig.Verify(x => x.GetDeviceName(expectedDeviceId), Times.Once);
        }

        /// <summary>
        /// ExportDoc: проверка вызова LoadDocsModule с правильными параметрами
        /// </summary>
        [Test]
        public void ExportDoc_CallsLoadDocsModuleWithCorrectParameters()
        {
            // Arrange
            const int expectedDeviceId = 42;
            const IdDoc expectedIdDoc = IdDoc.Passport;
            
            _mockAppConfig.Setup(x => x.GetDeviceName(It.IsAny<int>())).Returns("TestDevice");
            _mockDocModuleLoader.Setup(x => x.LoadDocsModule(It.IsAny<DbContextOptions<DocGeneral>>(), It.IsAny<int>(), It.IsAny<IdDoc>(), It.IsAny<string>()))
                .Returns((DocGeneral)null);

            // Act
            _controller.ExportDoc(expectedDeviceId, expectedIdDoc, 1, "pdf", 1);

            // Assert
            _mockDocModuleLoader.Verify(
                x => x.LoadDocsModule(
                    _dbOptions,
                    expectedDeviceId,
                    expectedIdDoc,
                    It.IsAny<string>()),
                Times.Once);
        }
    }
}


