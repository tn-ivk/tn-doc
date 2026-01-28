using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using TN_DocGeneral.Services;

namespace Tests.Unit.Services;

/// <summary>
/// Набор тестов для ConfigurationCacheService с проверкой LRU eviction
/// </summary>
[TestFixture]
public class ConfigurationCacheServiceTests
{
    private ConfigurationCacheService _cacheService;
    private string _testConfigDirectory;

    [SetUp]
    public void Setup()
    {
        _cacheService = new ConfigurationCacheService();
        _testConfigDirectory = Path.Combine(Path.GetTempPath(), "ConfigCacheTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testConfigDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testConfigDirectory))
        {
            Directory.Delete(_testConfigDirectory, true);
        }
    }

    #region Helper Methods

    private string CreateTestConfigFile(string fileName, string content = null)
    {
        var filePath = Path.Combine(_testConfigDirectory, fileName);
        var jsonContent = content ?? $"{{ \"Name\": \"{fileName}\", \"Value\": 123 }}";
        File.WriteAllText(filePath, jsonContent);
        return filePath;
    }

    private class TestConfig
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    #endregion

    #region Basic Caching Tests

    /// <summary>
    /// GetOrLoadConfig: первая загрузка читает с диска
    /// </summary>
    [Test]
    public void GetOrLoadConfig_FirstLoad_ReadsFromDisk()
    {
        // Arrange
        var filePath = CreateTestConfigFile("config1.json");

        // Act
        var config = _cacheService.GetOrLoadConfig<TestConfig>(filePath);

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.Name, Is.EqualTo("config1.json"));
        Assert.That(config.Value, Is.EqualTo(123));
    }

    /// <summary>
    /// GetOrLoadConfig: вторая загрузка возвращает свежий объект из кэша
    /// </summary>
    [Test]
    public void GetOrLoadConfig_SecondLoad_ReturnsFreshObjectFromCache()
    {
        // Arrange
        var filePath = CreateTestConfigFile("config2.json");

        // Act
        var config1 = _cacheService.GetOrLoadConfig<TestConfig>(filePath);
        config1.Value = 999; // Изменяем первый объект

        var config2 = _cacheService.GetOrLoadConfig<TestConfig>(filePath);

        // Assert
        Assert.That(config2.Value, Is.EqualTo(123), "Второй объект должен быть свежим, без изменений первого");
        Assert.That(config1, Is.Not.SameAs(config2), "Объекты должны быть разными экземплярами");
    }

    /// <summary>
    /// GetOrLoadConfig: статистика показывает cache hit
    /// </summary>
    [Test]
    public void GetOrLoadConfig_Statistics_ShowsCacheHits()
    {
        // Arrange
        var filePath = CreateTestConfigFile("config3.json");

        // Act
        _cacheService.GetOrLoadConfig<TestConfig>(filePath); // Cache miss
        _cacheService.GetOrLoadConfig<TestConfig>(filePath); // Cache hit
        _cacheService.GetOrLoadConfig<TestConfig>(filePath); // Cache hit

        var stats = _cacheService.GetStatistics();

        // Assert
        Assert.That(stats.CacheMisses, Is.EqualTo(1));
        Assert.That(stats.CacheHits, Is.EqualTo(2));
        Assert.That(stats.TotalRequests, Is.EqualTo(3));
        Assert.That(stats.HitRate, Is.EqualTo(66.66).Within(0.01));
    }

    #endregion

    #region LRU Eviction Tests

    /// <summary>
    /// LRU: при добавлении 51-й записи удаляется самая старая
    /// </summary>
    [Test]
    public void LRU_Adding51stEntry_EvictsOldest()
    {
        // Arrange - создаем 50 конфигураций
        for (int i = 1; i <= 50; i++)
        {
            var filePath = CreateTestConfigFile($"config{i}.json", $"{{ \"Name\": \"config{i}\", \"Value\": {i} }}");
            _cacheService.GetOrLoadConfig<TestConfig>(filePath);
            Thread.Sleep(10); // Небольшая задержка для разных LastAccessTime
        }

        var stats1 = _cacheService.GetStatistics();
        Assert.That(stats1.CachedConfigsCount, Is.EqualTo(50));

        // Act - добавляем 51-ю конфигурацию
        var filePath51 = CreateTestConfigFile("config51.json", "{ \"Name\": \"config51\", \"Value\": 51 }");
        _cacheService.GetOrLoadConfig<TestConfig>(filePath51);

        var stats2 = _cacheService.GetStatistics();

        // Assert
        Assert.That(stats2.CachedConfigsCount, Is.EqualTo(50), "Размер кэша должен остаться 50");
        Assert.That(stats2.CacheEvictions, Is.EqualTo(1), "Должно быть одно вытеснение");
    }

    /// <summary>
    /// LRU: добавление 100 конфигураций приводит к 50 evictions
    /// </summary>
    [Test]
    public void LRU_Adding100Entries_Evicts50()
    {
        // Arrange & Act - создаем 100 конфигураций
        for (int i = 1; i <= 100; i++)
        {
            var filePath = CreateTestConfigFile($"config_lru_{i}.json", $"{{ \"Name\": \"config{i}\", \"Value\": {i} }}");
            _cacheService.GetOrLoadConfig<TestConfig>(filePath);
        }

        var stats = _cacheService.GetStatistics();

        // Assert
        Assert.That(stats.CachedConfigsCount, Is.EqualTo(50), "Размер кэша не должен превышать 50");
        Assert.That(stats.CacheEvictions, Is.EqualTo(50), "Должно быть 50 вытеснений");
        Assert.That(stats.MaxCacheSize, Is.EqualTo(50));
    }

    /// <summary>
    /// LRU: обращение к записи обновляет LastAccessTime и защищает от eviction
    /// </summary>
    [Test]
    public void LRU_AccessingEntry_UpdatesLastAccessTime()
    {
        // Arrange - создаем 50 конфигураций
        var firstFilePath = CreateTestConfigFile("first.json", "{ \"Name\": \"first\", \"Value\": 1 }");
        _cacheService.GetOrLoadConfig<TestConfig>(firstFilePath);

        Thread.Sleep(50); // Задержка

        for (int i = 2; i <= 50; i++)
        {
            var filePath = CreateTestConfigFile($"config{i}.json", $"{{ \"Name\": \"config{i}\", \"Value\": {i} }}");
            _cacheService.GetOrLoadConfig<TestConfig>(filePath);
            Thread.Sleep(5);
        }

        // Act - обращаемся к первой конфигурации, обновляя её LastAccessTime
        _cacheService.GetOrLoadConfig<TestConfig>(firstFilePath);

        // Добавляем 51-ю конфигурацию
        var filePath51 = CreateTestConfigFile("config51.json", "{ \"Name\": \"config51\", \"Value\": 51 }");
        _cacheService.GetOrLoadConfig<TestConfig>(filePath51);

        // Assert - проверяем что "first" всё ещё в кэше
        var firstConfig = _cacheService.GetOrLoadConfig<TestConfig>(firstFilePath);
        Assert.That(firstConfig, Is.Not.Null);
        Assert.That(firstConfig.Name, Is.EqualTo("first"));

        var stats = _cacheService.GetStatistics();
        Assert.That(stats.CacheHits, Is.GreaterThan(0), "Должны быть cache hits для first.json");
    }

    /// <summary>
    /// LRU: статистика содержит MaxCacheSize
    /// </summary>
    [Test]
    public void Statistics_ContainsMaxCacheSize()
    {
        // Act
        var stats = _cacheService.GetStatistics();

        // Assert
        Assert.That(stats.MaxCacheSize, Is.EqualTo(50));
    }

    #endregion

    #region Update and Clear Tests

    /// <summary>
    /// Update: обновляет JSON в кэше
    /// </summary>
    [Test]
    public void Update_UpdatesJsonInCache()
    {
        // Arrange
        var filePath = CreateTestConfigFile("config_update.json", "{ \"Name\": \"old\", \"Value\": 100 }");
        _cacheService.GetOrLoadConfig<TestConfig>(filePath);

        // Act - обновляем кэш напрямую
        var newJson = "{ \"Name\": \"new\", \"Value\": 200 }";
        _cacheService.Update<TestConfig>(filePath, newJson);

        var config = _cacheService.GetOrLoadConfig<TestConfig>(filePath);

        // Assert
        Assert.That(config.Name, Is.EqualTo("new"));
        Assert.That(config.Value, Is.EqualTo(200));
    }

    /// <summary>
    /// ClearCache(filePath): очищает конкретный файл из кэша
    /// </summary>
    [Test]
    public void ClearCache_WithFilePath_RemovesSpecificFile()
    {
        // Arrange
        var filePath1 = CreateTestConfigFile("config_clear1.json");
        var filePath2 = CreateTestConfigFile("config_clear2.json");

        _cacheService.GetOrLoadConfig<TestConfig>(filePath1);
        _cacheService.GetOrLoadConfig<TestConfig>(filePath2);

        var stats1 = _cacheService.GetStatistics();
        Assert.That(stats1.CachedConfigsCount, Is.EqualTo(2));

        // Act - очищаем только первый файл
        _cacheService.ClearCache(filePath1);

        var stats2 = _cacheService.GetStatistics();

        // Assert
        Assert.That(stats2.CachedConfigsCount, Is.EqualTo(1));
    }

    /// <summary>
    /// ClearCache(): полностью очищает кэш и сбрасывает статистику
    /// </summary>
    [Test]
    public void ClearCache_Full_ClearsAllAndResetsStatistics()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            var filePath = CreateTestConfigFile($"config_full_{i}.json");
            _cacheService.GetOrLoadConfig<TestConfig>(filePath);
        }

        var stats1 = _cacheService.GetStatistics();
        Assert.That(stats1.CachedConfigsCount, Is.EqualTo(10));
        Assert.That(stats1.CacheMisses, Is.EqualTo(10));

        // Act
        _cacheService.ClearCache();

        var stats2 = _cacheService.GetStatistics();

        // Assert
        Assert.That(stats2.CachedConfigsCount, Is.EqualTo(0));
        Assert.That(stats2.CacheHits, Is.EqualTo(0));
        Assert.That(stats2.CacheMisses, Is.EqualTo(0));
        Assert.That(stats2.CacheEvictions, Is.EqualTo(0));
    }

    #endregion

    #region AccessCount Tests

    /// <summary>
    /// AccessCount: увеличивается при каждом обращении
    /// </summary>
    [Test]
    public void AccessCount_IncreasesWithEachAccess()
    {
        // Arrange
        var filePath = CreateTestConfigFile("config_access.json");

        // Act
        _cacheService.GetOrLoadConfig<TestConfig>(filePath); // AccessCount = 1
        _cacheService.GetOrLoadConfig<TestConfig>(filePath); // AccessCount = 2
        _cacheService.GetOrLoadConfig<TestConfig>(filePath); // AccessCount = 3

        var stats = _cacheService.GetStatistics();
        var configInfo = stats.CachedConfigs.Find(c => c.FileName == "config_access.json");

        // Assert
        Assert.That(configInfo, Is.Not.Null);
        Assert.That(configInfo.AccessCount, Is.EqualTo(3));
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// GetOrLoadConfig: возвращает null для несуществующего файла
    /// </summary>
    [Test]
    public void GetOrLoadConfig_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testConfigDirectory, "nonexistent.json");

        // Act
        var config = _cacheService.GetOrLoadConfig<TestConfig>(nonExistentPath);

        // Assert
        Assert.That(config, Is.Null);
    }

    /// <summary>
    /// GetOrLoadConfig: возвращает null для невалидного JSON
    /// </summary>
    [Test]
    public void GetOrLoadConfig_InvalidJson_ReturnsNull()
    {
        // Arrange
        var filePath = Path.Combine(_testConfigDirectory, "invalid.json");
        File.WriteAllText(filePath, "{ invalid json }");

        // Act
        var config = _cacheService.GetOrLoadConfig<TestConfig>(filePath);

        // Assert
        Assert.That(config, Is.Null);
    }

    #endregion
}
