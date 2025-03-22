using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using TN_DocGeneral.Services;

namespace Tests.Services
{
    [TestFixture]
    public class AppConfigServiceTests
    {
        private Mock<IConfiguration> _configurationMock;
        private AppConfigService _appConfigService;

        [SetUp]
        public void Setup()
        {
            _configurationMock = new Mock<IConfiguration>();
            _appConfigService = new AppConfigService(_configurationMock.Object);
        }

        [Test]
        public async Task GetConfig_ShouldReturnConfig()
        {
            // Arrange
            var expectedConfig = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" }
            };

            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x.Value).Returns("Value1");
            _configurationMock.Setup(x => x.GetSection("Key1")).Returns(configSectionMock.Object);

            // Act
            var result = await _appConfigService.GetConfig();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<Dictionary<string, string>>());
            _configurationMock.Verify(x => x.GetSection(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task GetConfig_WithEmptyConfiguration_ShouldReturnEmptyDictionary()
        {
            // Arrange
            _configurationMock.Setup(x => x.GetSection(It.IsAny<string>())).Returns((IConfigurationSection)null);

            // Act
            var result = await _appConfigService.GetConfig();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetConfig_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Arrange
            var nullConfigService = new AppConfigService(null);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await nullConfigService.GetConfig());
        }

        [Test]
        public async Task GetConfig_WithInvalidConfiguration_ShouldHandleException()
        {
            // Arrange
            _configurationMock.Setup(x => x.GetSection(It.IsAny<string>()))
                .Throws(new Exception("Configuration error"));

            // Act
            var result = await _appConfigService.GetConfig();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetConfig_WithMultipleSections_ShouldReturnAllValues()
        {
            // Arrange
            var configSection1Mock = new Mock<IConfigurationSection>();
            configSection1Mock.Setup(x => x.Value).Returns("Value1");
            var configSection2Mock = new Mock<IConfigurationSection>();
            configSection2Mock.Setup(x => x.Value).Returns("Value2");

            _configurationMock.Setup(x => x.GetSection("Key1")).Returns(configSection1Mock.Object);
            _configurationMock.Setup(x => x.GetSection("Key2")).Returns(configSection2Mock.Object);

            // Act
            var result = await _appConfigService.GetConfig();

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result["Key1"], Is.EqualTo("Value1"));
            Assert.That(result["Key2"], Is.EqualTo("Value2"));
        }

        [Test]
        public async Task GetConfig_WithNullSectionValue_ShouldHandleNullValue()
        {
            // Arrange
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x.Value).Returns((string)null);
            _configurationMock.Setup(x => x.GetSection("Key1")).Returns(configSectionMock.Object);

            // Act
            var result = await _appConfigService.GetConfig();

            // Assert
            Assert.That(result["Key1"], Is.Null);
        }
    }
} 