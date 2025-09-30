using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TN_Doc.Services;

namespace TN_Doc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IStatusProvider _statusProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StatusController> _logger;

    private const string CacheKey = "status_data";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(5);

    public StatusController(
        IStatusProvider statusProvider,
        IMemoryCache cache,
        ILogger<StatusController> logger)
    {
        _statusProvider = statusProvider;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        var requestStart = DateTime.UtcNow;
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        _logger.LogDebug("Status request from {ClientIP}", clientIp);

        try
        {
            if (_cache.TryGetValue(CacheKey, out StatusResponse cachedData))
            {
                var cacheHitDuration = DateTime.UtcNow - requestStart;
                _logger.LogTrace("Status served from cache in {Duration}ms to {ClientIP}",
                    cacheHitDuration.TotalMilliseconds, clientIp);
                return Ok(cachedData);
            }

            var response = await _statusProvider.GetStatusAsync(HttpContext.RequestAborted);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                Priority = CacheItemPriority.High
            };
            _cache.Set(CacheKey, response, cacheOptions);

            var totalDuration = DateTime.UtcNow - requestStart;
            _logger.LogInformation("Status request completed in {Duration}ms for {ClientIP}",
                totalDuration.TotalMilliseconds, clientIp);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Status request from {ClientIP} was cancelled", clientIp);
            return StatusCode(499, "Request was cancelled");
        }
        catch (Exception ex)
        {
            var errorDuration = DateTime.UtcNow - requestStart;
            _logger.LogError(ex, "Failed to retrieve status for {ClientIP} after {Duration}ms: {ErrorMessage}",
                clientIp, errorDuration.TotalMilliseconds, ex.Message);
            return StatusCode(500, new { error = "Failed to retrieve system status", details = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshStatus()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        _logger.LogInformation("Status refresh requested by {ClientIP}", clientIp);

        try
        {
            _cache.Remove(CacheKey);
            var response = await _statusProvider.GetStatusAsync(HttpContext.RequestAborted);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                Priority = CacheItemPriority.High
            };
            _cache.Set(CacheKey, response, cacheOptions);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh status for {ClientIP}: {ErrorMessage}", clientIp, ex.Message);
            return StatusCode(500, new { error = "Failed to refresh status", details = ex.Message });
        }
    }
}


