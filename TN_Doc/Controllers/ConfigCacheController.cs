using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using TN_DocGeneral.Services;

namespace TN_Doc.Controllers;

/// <summary>
/// Контроллер для управления и мониторинга кэша конфигураций документов
/// </summary>
[ApiController]
[Route("api/config-cache")]
public class ConfigCacheController : ControllerBase
{
    private readonly IConfigurationCacheService _configCache;
    private readonly ILogger<ConfigCacheController> _logger;

    public ConfigCacheController(IConfigurationCacheService configCache, ILogger<ConfigCacheController> logger)
    {
        _configCache = configCache;
        _logger = logger;
    }

    /// <summary>
    /// Получить статистику использования кэша конфигураций
    /// GET /api/config-cache/statistics
    /// </summary>
    /// <returns>Статистика кэша с информацией о попаданиях, промахах и закэшированных файлах</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ConfigCacheStatistics), 200)]
    [ProducesResponseType(500)]
    public ActionResult<ConfigCacheStatistics> GetStatistics()
    {
        try
        {
            var stats = _configCache.GetStatistics();
            _logger.LogDebug($"Запрошена статистика кэша конфигураций: {stats.CachedConfigsCount} файлов, Hit Rate: {stats.HitRate:F2}%");
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения статистики кэша конфигураций");
            return StatusCode(500, new { error = "Ошибка получения статистики кэша", details = ex.Message });
        }
    }

    /// <summary>
    /// Очистить весь кэш конфигураций
    /// POST /api/config-cache/clear
    /// </summary>
    /// <remarks>
    /// Используйте этот метод после ручного изменения конфигурационных файлов документов.
    /// Конфигурации будут перезагружены с диска при следующем обращении.
    /// </remarks>
    /// <returns>Сообщение об успешной очистке кэша</returns>
    [HttpPost("clear")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public IActionResult ClearCache()
    {
        try
        {
            _configCache.ClearCache();
            _logger.LogInformation("Кэш конфигураций очищен по запросу пользователя");
            return Ok(new
            {
                success = true,
                message = "Кэш конфигураций успешно очищен. Все конфигурации будут перезагружены с диска при следующем обращении.",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка очистки кэша конфигураций");
            return StatusCode(500, new { error = "Ошибка очистки кэша", details = ex.Message });
        }
    }

    /// <summary>
    /// Проверка работоспособности сервиса кэширования
    /// GET /api/config-cache/health
    /// </summary>
    /// <returns>Статус работоспособности сервиса</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult HealthCheck()
    {
        try
        {
            var stats = _configCache.GetStatistics();
            return Ok(new
            {
                status = "healthy",
                service = "ConfigurationCacheService",
                cachedConfigs = stats.CachedConfigsCount,
                totalRequests = stats.TotalRequests,
                hitRate = $"{stats.HitRate:F2}%",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка проверки работоспособности кэша конфигураций");
            return StatusCode(500, new
            {
                status = "unhealthy",
                service = "ConfigurationCacheService",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
