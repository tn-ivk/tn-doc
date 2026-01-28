using System.Net;
using System.Net.Http.Json;

namespace Tests.Integration.Controllers;

/// <summary>
/// Интеграционные тесты HomeController
/// </summary>
[TestFixture]
public class HomeControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Index_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.Redirect));
    }

    [Test]
    public async Task GetStatus_ReturnsJsonResponse()
    {
        // Act
        var response = await Client.GetAsync("/Home/GetStatus");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True, $"Expected success but got {response.StatusCode}");
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    [Test]
    public async Task GetDevices_ReturnsDeviceList()
    {
        // Act
        var response = await Client.GetAsync("/Home/GetDevices");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);

        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [TestCase("/Home/Index")]
    [TestCase("/Home/GetStatus")]
    [TestCase("/Home/GetDevices")]
    public async Task Endpoints_AreAccessible(string endpoint)
    {
        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        Assert.That((int)response.StatusCode, Is.LessThan(500),
            $"Endpoint {endpoint} returned server error: {response.StatusCode}");
    }
}
