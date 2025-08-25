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
using Newtonsoft.Json.Linq;
using TN_Doc.Controllers;
using TN_Doc.Models.DTOs;
using TN.DocData;
using TN_DocGeneral.Dictionaries;

namespace Tests.Controllers
{
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
            var currentDir = Directory.GetCurrentDirectory();
            var qpRelPath = "\\TestQp.json"; // will be appended to currentDir inside service
            var qpFullPath = Path.Combine(currentDir + qpRelPath);
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
                                    new TemplateDoc { Use = true, Id = 1, Name = "Test Template", PathToDocEditConfigFile = qpRelPath }
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

        [Test]
        public async Task GetQpConfigsAsync_ReturnsOkWithQpConfigs()
        {
            // Act
            var result = await _controller.GetQpConfigsAsync();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult.Value, Is.InstanceOf<QpEditDto>());
            
            var qpEditDto = okResult.Value as QpEditDto;
            Assert.That(qpEditDto.QpCfgJsonRaw, Is.Not.Null);
            Assert.That(qpEditDto.QpCfgJsonRaw, Is.Not.Empty);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Получения конфигурации используемых паспортов качества")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task SetQpConfigsAsync_ReturnsOkAndUpdatesQpConfigs()
        {
            // Arrange
            // Build JSON in the shape expected by SetQpConfigFromJsonAsync
            var qpRelPath = "\\TestQp.json";
            var qpsInfo = new JObject
            {
                ["QpsInfo"] = new JArray
                {
                    new JObject
                    {
                        ["EditConfigFilePath"] = qpRelPath,
                        ["Methods"] = new JArray
                        {
                            new JObject { ["Name"] = "Updated Test" }
                        },
                        ["Parameters"] = new JArray()
                    }
                }
            };

            var qpEditDto = new QpEditDto { QpCfgJsonRaw = qpsInfo.ToString(Formatting.Indented) };

            // Act
            var result = await _controller.SetQpConfigsAsync(qpEditDto);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Установка новой конфигурации проекта")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            // Verify that subsequent GetQpConfigsAsync reflects the changes
            var getResult = await _controller.GetQpConfigsAsync();
            var getOkResult = getResult as OkObjectResult;
            var getQpEditDto = getOkResult.Value as QpEditDto;
            
            Assert.That(getQpEditDto.QpCfgJsonRaw, Contains.Substring("Updated Test"));
        }

        [Test]
        public async Task SetDirAsync_WithInvalidJson_HandlesGracefully()
        {
            // Arrange
            var invalidJson = "{ invalid json }";
            var dirEditDto = new DirEditDTO { DirJsonRaw = invalidJson };

            // Act & Assert - Should not throw exception
            // The actual behavior depends on the AppConfigService implementation
            // This test verifies that the controller doesn't crash with invalid JSON
            Assert.DoesNotThrowAsync(async () => await _controller.SetDirAsync(dirEditDto));
        }

        [Test]
        public async Task SetQpConfigsAsync_WithInvalidJson_ThrowsJsonReaderException()
        {
            // Arrange
            var invalidJson = "{ invalid json }";
            var qpEditDto = new QpEditDto { QpCfgJsonRaw = invalidJson };

            // Act & Assert - Service does not swallow invalid JSON for QP configs
            Assert.ThrowsAsync<Newtonsoft.Json.JsonReaderException>(async () => await _controller.SetQpConfigsAsync(qpEditDto));
        }
    }
}


