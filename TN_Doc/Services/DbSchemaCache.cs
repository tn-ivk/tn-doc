using System;
using System.Collections.Concurrent;
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
        _logger.LogDebug($"Проверка наличия колонки DataARM для устройства {deviceId}, документа {idDoc}");
        
        var tableName = GetTableName(idDoc);
        if (string.IsNullOrEmpty(tableName))
        {
            _logger.LogTrace("Документ {IdDoc} не поддерживает проверку DataARM", idDoc);
            return false;
        }

        var key = (deviceId, tableName);
        var result = _cache.GetOrAdd(key, _ => CheckDataArmExists(deviceId, tableName));
        _logger.LogTrace($"Результат проверки DataARM для устройства {deviceId}, таблицы {tableName}: {result}");
        return result;
    }

    private bool CheckDataArmExists(int deviceId, string tableName)
    {
        try
        {
            _logger.LogDebug($"Выполнение проверки схемы БД для устройства {deviceId}, таблицы {tableName}");
            
            var cfgDevice = _appConfigService.GetDeviceCfg(deviceId);
            if (cfgDevice?.DBConnectionStrings == null || !cfgDevice.DBConnectionStrings.Any(x => x.Use))
            {
                _logger.LogWarning($"Устройство {deviceId} не имеет активных подключений к БД");
                return false;
            }

            var dbService = new DBtService(cfgDevice.DBConnectionStrings.Where(x => x.Use).ToList());
            var fields = dbService.GetTableInfo(tableName);

            var hasDataArm = fields.Any(f => string.Equals(f.Field, "DataARM", StringComparison.OrdinalIgnoreCase));
            _logger.LogTrace($"Проверка схемы БД завершена: устройство {deviceId}, таблица {tableName}, наличие DataARM: {hasDataArm}");
            return hasDataArm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при проверке схемы БД для устройства {deviceId}, таблицы {tableName}");
            return false;
        }
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