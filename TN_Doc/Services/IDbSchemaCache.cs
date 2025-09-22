using System.Collections.Generic;
using TN.DocData;

namespace TN_Doc.Services;

public interface IDbSchemaCache
{
    bool HasDataArm(int deviceId, IdDoc idDoc);
    
    /// <summary>
    /// Очистка кэша схемы
    /// </summary>
    void ClearCache();
    
    /// <summary>
    /// Получение статистики кэша
    /// </summary>
    Dictionary<string, object> GetCacheStats();
}