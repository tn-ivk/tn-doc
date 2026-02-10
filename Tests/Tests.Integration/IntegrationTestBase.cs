using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TN.Doc;
using TN.DocData;
using TN_Doc;
using TN_DocGeneral.Services;

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
    protected const string TestDbConnectionStringEnvVar = "TEST_DB_CONNECTION_STRING";

    protected virtual bool UseLiveDb => false;

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

            if (UseLiveDb)
            {
                var connectionString = GetTestDbConnectionStringOrThrow();
                ConfigureAppConfigForLiveDb(services, connectionString);
                services.AddDbContext<DocGeneral>(options =>
                {
                    options.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                });
            }
            else
            {
                services.AddDbContext<DocGeneral>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            }

            // Дополнительная настройка сервисов для тестов
            ConfigureTestServices(services);
        });
    }

    protected virtual string GetTestDbConnectionStringOrThrow()
    {
        var connectionString = Environment.GetEnvironmentVariable(TestDbConnectionStringEnvVar);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{TestDbConnectionStringEnvVar} is required when {nameof(UseLiveDb)} is enabled.");
        }

        return connectionString;
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

    private static void ConfigureAppConfigForLiveDb(IServiceCollection services, string connectionString)
    {
        var connection = ParseConnectionString(connectionString);
        var server = GetValue(connection, "Server", "Host", "Data Source") ?? "127.0.0.1";
        var port = GetValue(connection, "Port");
        if (!string.IsNullOrWhiteSpace(port))
        {
            server = $"{server};Port={port}";
        }

        var user = GetValue(connection, "Uid", "User Id", "UserID", "Userid", "Username", "User") ?? "root";
        var password = GetValue(connection, "Pwd", "Password") ?? string.Empty;
        var database = GetValue(connection, "Database", "Initial Catalog") ?? "IVK_TN_01";
        var timeout = 5;
        if (int.TryParse(GetValue(connection, "Connection Timeout", "Connect Timeout"), out var parsedTimeout))
        {
            timeout = parsedTimeout;
        }

        services.RemoveAll<IAppConfigService>();
        services.AddSingleton<IAppConfigService>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var appConfig = AppConfigService.GetInstance(configuration);
            ApplyLiveDbToAllDevices(appConfig.GetAppCfg(), server, user, password, database, timeout);
            return appConfig;
        });
    }

    private static void ApplyLiveDbToAllDevices(
        CfgApp cfgApp,
        string server,
        string user,
        string password,
        string database,
        int timeout)
    {
        if (cfgApp?.Devices == null)
        {
            return;
        }

        foreach (var device in cfgApp.Devices)
        {
            if (device.DBConnectionStrings == null || device.DBConnectionStrings.Count == 0)
            {
                device.DBConnectionStrings = new List<DBConnectionString>
                {
                    new()
                    {
                        Use = true,
                        GuidDevice = device.IdDevice,
                        Server = server,
                        Userid = user,
                        Password = password,
                        Database = database,
                        ConnectionTimeout = timeout
                    }
                };
                continue;
            }

            for (var i = 0; i < device.DBConnectionStrings.Count; i++)
            {
                var connection = device.DBConnectionStrings[i];
                connection.Use = i == 0;
                connection.GuidDevice = device.IdDevice;
                connection.Server = server;
                connection.Userid = user;
                connection.Password = password;
                connection.Database = database;
                connection.ConnectionTimeout = timeout;
            }
        }
    }

    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return builder.Keys
            .Cast<string>()
            .ToDictionary(k => k, k => builder[k]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
