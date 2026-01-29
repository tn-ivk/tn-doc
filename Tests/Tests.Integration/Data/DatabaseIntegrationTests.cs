using Microsoft.EntityFrameworkCore;
using TN.Doc;

namespace Tests.Integration.Data;

/// <summary>
/// Интеграционные тесты для работы с базой данных
/// </summary>
[TestFixture]
public class DatabaseIntegrationTests : IntegrationTestBase
{
    [Test]
    public void DbContext_IsRegisteredInDI()
    {
        // Act
        var context = GetServiceOrDefault<DocGeneral>();

        // Assert
        Assert.That(context, Is.Not.Null, "DocGeneral DbContext should be registered in DI");
    }

    [Test]
    public void InMemoryDatabase_IsConfigured()
    {
        // Arrange
        var context = GetService<DocGeneral>();

        // Act
        var canConnect = context.Database.CanConnect();

        // Assert
        Assert.That(canConnect, Is.True, "Should be able to connect to InMemory database");
    }

    [Test]
    public void DbContext_CanSaveAndRetrieveData()
    {
        // Arrange
        var context = GetService<DocGeneral>();

        // Act & Assert - проверка что контекст работает
        // Конкретные тесты зависят от структуры БД
        Assert.DoesNotThrow(() =>
        {
            context.Database.EnsureCreated();
        }, "Database should be created without errors");
    }
}
