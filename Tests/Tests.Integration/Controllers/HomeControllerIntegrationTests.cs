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
    [TestCase("/")]
    [TestCase("/Home/Index")]
    public async Task Endpoints_AreAccessible(string endpoint)
    {
        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        Assert.That((int)response.StatusCode, Is.LessThan(500),
            $"Endpoint {endpoint} returned server error: {response.StatusCode}");
    }
}
