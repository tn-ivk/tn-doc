using System.Net;

namespace Tests.Integration.Controllers;

/// <summary>
/// Интеграционные тесты StatusController (API статуса устройств)
/// </summary>
[TestFixture]
public class StatusControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetStatus_ReturnsJsonResponse()
    {
        // Act
        var response = await Client.GetAsync("/api/status");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True, $"Expected success but got {response.StatusCode}");
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    [Test]
    public async Task GetStatus_ReturnsValidStatusResponse()
    {
        // Act
        var response = await Client.GetAsync("/api/status");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);

        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [TestCase("/api/status")]
    public async Task StatusEndpoints_AreAccessible(string endpoint)
    {
        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        Assert.That((int)response.StatusCode, Is.LessThan(500),
            $"Endpoint {endpoint} returned server error: {response.StatusCode}");
    }
}
