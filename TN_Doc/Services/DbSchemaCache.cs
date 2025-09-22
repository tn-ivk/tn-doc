using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TN_DocGeneral.Services;
using TN.Doc;
using TN.DocData;

namespace TN_Doc.Services;

public class DbSchemaCache : IDbSchemaCache
{
    private readonly ConcurrentDictionary<(int deviceId, string tableName), bool> _cache = new();
    private readonly IAppConfigService _appConfigService;
    private readonly ILogger<DbSchemaCache> _logger;

    public DbSchemaCache(IAppConfigService appConfigService, ILogger<DbSchemaCache> logger)
    {
        _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool HasDataArm(int deviceId, IdDoc idDoc)
    {
        _logger.LogDebug($"Проверка наличия колонки DataARM для устройства {_appConfigService.GetDeviceName(deviceId)}, документа {idDoc}");
        
        var tableName = GetTableName(idDoc);
        if (string.IsNullOrEmpty(tableName))
        {
            _logger.LogTrace($"Документ {idDoc} не поддерживает проверку DataARM");
            return false;
        }

        var key = (deviceId, tableName);
        
        // Проверяем, есть ли значение в кэше
        if (_cache.TryGetValue(key, out var cachedResult))
        {
            _logger.LogTrace($"Результат из кэша: устройство {_appConfigService.GetDeviceName(deviceId)}, таблица {tableName}, DataARM: {cachedResult}");
            return cachedResult;
        }
        
        // Ленивая инициализация: проверяем только нужную таблицу для нужного устройства
        _logger.LogDebug($"Значение не найдено в кэше, выполняется проверка БД для устройства {_appConfigService.GetDeviceName(deviceId)}, таблицы {tableName}");
        var result = CheckDataArmExists(deviceId, tableName);
        _cache.TryAdd(key, result);
        
        _logger.LogTrace($"Результат проверки DataARM для устройства {_appConfigService.GetDeviceName(deviceId)}, таблицы {tableName}: {result}");
        return result;
    }

    private bool CheckDataArmExists(int deviceId, string tableName)
    {
        try
        {
            _logger.LogDebug($"Выполнение проверки схемы БД для устройства {_appConfigService.GetDeviceName(deviceId)}, таблицы {tableName}");
            
            var cfgDevice = _appConfigService.GetDeviceCfg(deviceId);
            if (cfgDevice?.DBConnectionStrings == null || !cfgDevice.DBConnectionStrings.Any(x => x.Use))
            {
                _logger.LogWarning($"Устройство {_appConfigService.GetDeviceName(deviceId)} не имеет активных подключений к БД");
                return false;
            }

            var dbService = new DBtService(cfgDevice.DBConnectionStrings.Where(x => x.Use).ToList());
            var fields = dbService.GetTableInfo(tableName);

            var hasDataArm = fields.Any(f => string.Equals(f.Field, "DataARM", StringComparison.OrdinalIgnoreCase));
            _logger.LogTrace($"Проверка схемы БД завершена: устройство {_appConfigService.GetDeviceName(deviceId)}, таблица {tableName}, наличие DataARM: {hasDataArm}");
            return hasDataArm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при проверке схемы БД для устройства {_appConfigService.GetDeviceName(deviceId)}, таблицы {tableName}");
            return false;
        }
    }

    public void ClearCache()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogDebug($"Кэш схемы БД очищен. Удалено {count} записей");
    }

    public Dictionary<string, object> GetCacheStats()
    {
        return new Dictionary<string, object>
        {
            ["CacheSize"] = _cache.Count,
            ["CachedEntries"] = _cache.ToDictionary(
                kvp => $"Device{kvp.Key.deviceId}_{kvp.Key.tableName}",
                kvp => kvp.Value
            )
        };
    }

    private string GetTableName(IdDoc idDoc)
    {
        return idDoc switch
        {
            IdDoc.Report => "TableReport",
            IdDoc.Jornal => "TableMeasurementJornal",
            _ => null
        };
    }
}