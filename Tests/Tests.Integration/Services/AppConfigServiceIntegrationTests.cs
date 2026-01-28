using TN_DocGeneral.Services;

namespace Tests.Integration.Services;

/// <summary>
/// Интеграционные тесты IAppConfigService
/// </summary>
[TestFixture]
public class AppConfigServiceIntegrationTests : IntegrationTestBase
{
    [Test]
    public void AppConfigService_IsRegisteredInDI()
    {
        // Act
        var service = GetServiceOrDefault<IAppConfigService>();

        // Assert
        Assert.That(service, Is.Not.Null, "IAppConfigService should be registered in DI");
    }

    [Test]
    public void GetCfg_ReturnsConfiguration()
    {
        // Arrange
        var service = GetService<IAppConfigService>();

        // Act
        var cfg = service.GetCfg();

        // Assert
        Assert.That(cfg, Is.Not.Null, "Configuration should not be null");
        Assert.That(cfg.Doc, Is.Not.Null, "Doc section should not be null");
    }

    [Test]
    public void GetDeviceCfg_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var service = GetService<IAppConfigService>();

        // Act
        var device = service.GetDeviceCfg(-1);

        // Assert
        Assert.That(device, Is.Null, "Device with invalid ID should return null");
    }

    [Test]
    public void ConfigurationCacheService_IsRegisteredInDI()
    {
        // Act
        var service = GetServiceOrDefault<IConfigurationCacheService>();

        // Assert
        Assert.That(service, Is.Not.Null, "IConfigurationCacheService should be registered in DI");
    }
}
