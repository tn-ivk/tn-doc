using System;
using System.Collections.Concurrent;
using System.Linq;
using TN_DocGeneral.Services;
using TN.Doc;
using TN.DocData;

namespace TN_Doc.Services;

public class DbSchemaCache : IDbSchemaCache
{
    private readonly ConcurrentDictionary<(int deviceId, string tableName), bool> _cache = new();
    private readonly IAppConfigService _appConfigService;

    public DbSchemaCache(IAppConfigService appConfigService)
    {
        _appConfigService = appConfigService;
    }

    public bool HasDataArm(int deviceId, IdDoc idDoc)
    {
        var tableName = GetTableName(idDoc);
        if (string.IsNullOrEmpty(tableName))
            return false;

        var key = (deviceId, tableName);

        return _cache.GetOrAdd(key, _ => CheckDataArmExists(deviceId, tableName));
    }

    private bool CheckDataArmExists(int deviceId, string tableName)
    {
        try
        {
            var cfgDevice = _appConfigService.GetDeviceCfg(deviceId);
            if (cfgDevice?.DBConnectionStrings == null || !cfgDevice.DBConnectionStrings.Any(x => x.Use))
                return false;

            var dbService = new DBtService(cfgDevice.DBConnectionStrings.Where(x => x.Use).ToList());
            var fields = dbService.GetTableInfo(tableName);

            return fields.Any(f => string.Equals(f.Field, "DataARM", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
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