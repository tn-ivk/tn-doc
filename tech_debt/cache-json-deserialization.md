# План доработки кэширования конфигураций: переход на JSON

## Описание проблемы

Сервис `ConfigurationCacheService` кэширует десериализованные C#‑объекты конфигураций. При обращении к `LoadCfg<T>` мы получаем ту же ссылку, которую могли изменить другой код: например, `DocPassport` изменяет `Doc.Doc.Settings`, `TablePassport.DataARM` и т.д., и эти правки остаются в кэше. В результате повторное открытие паспорта подтягивает «грязные» данные и игнорирует флаги `IsDefault`.

**Симптомы:**
- После открытия одного паспорта и перехода к другому, методы по умолчанию не выбираются
- В форме редактирования отображаются методы из предыдущего паспорта
- Проблема исчезает только после перезапуска сервера

**Корневая причина:**
Кэш хранит ссылку на объект, который модифицируется в процессе работы. При повторном обращении возвращается тот же изменённый объект.

## Задача

Переписать кэш так, чтобы он хранил исходный JSON (строку/`JToken`). Каждый запрос к конфигурации должен возвращать свежий десериализованный объект, исключая утечки состояния, при этом чтение с диска выполняется только один раз. Обновление файлов должно синхронизироваться с кэшем.

## План выполнения

### 1. Подготовка
- Проанализировать всех потребителей `ConfigurationCacheService` и `LoadCfg<T>`, чтобы учесть особенности сериализации (`TypeNameHandling.All`)
- Убедиться, что методы, изменяющие конфигурации (`SetDirectoriesJsonAsync`, `SetQpConfigFromJsonAsync`, т.п.) вызывают `ClearCache` или смогут обновлять кэш вручную
- Провести аудит всех мест использования `LoadCfg<T>` в проекте

### 2. Модификация кэша
- Изменить `ConfigCacheEntry`, чтобы оно хранило `string RawJson` вместо `object Configuration`
- При первом чтении файла сохранить в кэш именно строку (использовать `File.ReadAllText`), параллельно десериализовав объект для возврата вызвавшему коду
- При последующих запросах десериализовать `RawJson` через `JsonConvert.DeserializeObject<T>(rawJson, settings)`

### 3. Обновление кэша при записи
- Для операций, которые вызывают `CfgFileRW.SaveCfg`, после успешной записи сразу обновлять запись в кэше:
  ```csharp
  var raw = JsonConvert.SerializeObject(obj, Formatting.Indented);
  _configCache.Update(filePath, raw);
  ```
- Если синхронное обновление сложно, оставить операцию `ClearCache()`; убедиться, что все места её вызывают после записи

### 4. Рефактор `LoadCfg<T>`
- Обновить `DocGeneral.LoadCfg`, чтобы получать строку из кэша и десериализовать её локально:
  ```csharp
  var rawJson = _configCache?.GetOrLoadRaw(resolved);
  return rawJson != null
      ? JsonConvert.DeserializeObject<T>(rawJson, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })
      : CfgFileRW.LoadCfg<T>(resolved);
  ```
- Если сервис отдаёт уже десериализованный объект, скорректировать сигнатуру (например, добавить перегрузку `GetOrLoadRaw`)

### 5. Тестирование
- Добавить юнит/интеграционные тесты: открыть два разных паспорта подряд, убедиться, что второй стартует с дефолтных методов
- Проверить, что `ClearCache` и `GetStatistics` корректно работают с новым форматом (количество байт, загрузка, счётчики)
- Протестировать производительность: сравнить время загрузки конфигураций до и после изменений

### 6. Оптимизация/финализация
- При необходимости внедрить кэш `JsonSerializer` (reuse настроек) для снижения аллокаций
- Перепроверить логи: заменить сообщения, где упоминался объект, на размер/путь JSON
- Обновить документацию по использованию кэша

## Примеры кода

### Новый объект кэша
```csharp
internal class ConfigCacheEntry {
    public string RawJson { get; set; }
    public string FilePath { get; set; }
    public Type ConfigType { get; set; }
    public DateTime LoadedAt { get; set; }
    public DateTime LastAccessTime { get; set; }
    public long AccessCount { get; set; }
}
```

### Обновлённый GetOrLoadConfig<T>
```csharp
public T GetOrLoadConfig<T>(string filePath) where T : class
{
    var normalizedPath = Path.GetFullPath(filePath);
    var cacheKey = new ConfigCacheKey(normalizedPath, typeof(T));

    if (_cache.TryGetValue(cacheKey, out var entry))
    {
        Interlocked.Increment(ref _cacheHits);
        entry.LastAccessTime = DateTime.UtcNow;
        entry.AccessCount++;
        
        return JsonConvert.DeserializeObject<T>(entry.RawJson, _serializerSettings);
    }

    // Загружаем с диска (первое обращение)
    Interlocked.Increment(ref _cacheMisses);
    _logger.Debug($"Загрузка конфигурации с диска (первое обращение): {normalizedPath}");

    var rawJson = File.ReadAllText(normalizedPath, Encoding.UTF8);
    var config = JsonConvert.DeserializeObject<T>(rawJson, _serializerSettings);
    
    if (config == null)
    {
        _logger.Warn($"Не удалось загрузить конфигурацию: {normalizedPath}");
        return null;
    }

    // Сохраняем в кэш
    var newEntry = new ConfigCacheEntry
    {
        RawJson = rawJson,
        FilePath = normalizedPath,
        ConfigType = typeof(T),
        LoadedAt = DateTime.UtcNow,
        LastAccessTime = DateTime.UtcNow,
        AccessCount = 1
    };

    _cache[cacheKey] = newEntry;
    _logger.Debug($"Конфигурация закэширована: {Path.GetFileName(normalizedPath)} (всего в кэше: {_cache.Count})");
    
    return config;
}
```

### Обновление кэша после сохранения
```csharp
public void Update(string filePath, string newRawJson)
{
    var normalizedPath = Path.GetFullPath(filePath);
    var key = new ConfigCacheKey(normalizedPath, typeof(object));
    
    _cache[key] = new ConfigCacheEntry
    {
        RawJson = newRawJson,
        FilePath = normalizedPath,
        ConfigType = typeof(object),
        LoadedAt = DateTime.UtcNow,
        LastAccessTime = DateTime.UtcNow,
        AccessCount = 0
    };
    
    _logger.Debug($"Кэш обновлён для файла: {Path.GetFileName(normalizedPath)}");
}
```

### Использование в CfgFileRW.SaveCfg
```csharp
public static bool SaveCfg(string filePath, object obj)
{
    // ... существующая логика сохранения ...
    
    try
    {
        var jsonContent = JsonConvert.SerializeObject(obj, Formatting.Indented);
        File.WriteAllText(filePath, jsonContent, Encoding.UTF8);
        
        // Обновляем кэш после успешного сохранения
        ConfigurationCacheService.Instance?.Update(filePath, jsonContent);
        
        _logger.Trace($"Выполнено сохранение файла {filePath}");
        return true;
    }
    catch (Exception ex)
    {
        _logger.Error($"Ошибка сохранения файла {filePath}: {ex.Message}");
        return false;
    }
}
```

## Ожидаемые результаты

- **Решение проблемы:** Каждый вызов `LoadCfg<T>` будет возвращать чистый объект без следов предыдущих изменений
- **Производительность:** Чтение с диска выполняется только один раз, последующие обращения используют кэшированный JSON
- **Совместимость:** Существующий код продолжит работать без изменений
- **Надёжность:** Синхронизация кэша с файловой системой предотвратит расхождения

## Риски и ограничения

- **CPU нагрузка:** Десериализация на каждый запрос увеличит нагрузку на процессор
- **Память:** Кэш будет занимать больше места (JSON строки vs объекты)
- **Сложность:** Необходимо обеспечить синхронизацию кэша при всех операциях записи
- **Тестирование:** Требуется тщательное тестирование всех сценариев использования кэша
