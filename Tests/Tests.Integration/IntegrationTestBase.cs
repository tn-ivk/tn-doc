using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TN.Doc;
using TN_Doc;

namespace Tests.Integration;

/// <summary>
/// Базовый класс для интеграционных тестов с использованием WebApplicationFactory.
/// Предоставляет настроенное тестовое окружение с InMemory БД.
/// </summary>
public abstract class IntegrationTestBase
{
    protected WebApplicationFactory<Program> Factory = null!;
    protected HttpClient Client = null!;
    protected IServiceScope Scope = null!;

    /// <summary>
    /// Настройка перед каждым тестом
    /// </summary>
    [SetUp]
    public virtual void SetUp()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(ConfigureWebHost);

        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Scope = Factory.Services.CreateScope();
    }

    /// <summary>
    /// Настройка веб-хоста для тестов
    /// </summary>
    protected virtual void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Удаляем реальный DbContext и заменяем на InMemory
            services.RemoveAll<DbContextOptions<DocGeneral>>();
            services.RemoveAll<DocGeneral>();

            services.AddDbContext<DocGeneral>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            });

            // Дополнительная настройка сервисов для тестов
            ConfigureTestServices(services);
        });
    }

    /// <summary>
    /// Дополнительная настройка сервисов для тестов.
    /// Переопределите в наследниках для добавления моков.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Переопределите для добавления моков или замены сервисов
    }

    /// <summary>
    /// Получение сервиса из DI контейнера
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Попытка получения сервиса из DI контейнера
    /// </summary>
    protected T GetServiceOrDefault<T>() where T : class
    {
        return Scope.ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// Очистка после каждого теста
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        Scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }
}
