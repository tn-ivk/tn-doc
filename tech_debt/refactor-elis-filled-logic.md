# Рефакторинг логики определения __elisFilled

## Проблема

Дублирование логики определения флага `__elisFilled` между бэкендом и фронтендом.

### Текущая реализация (неправильная)

**Бэкенд** (`tn.docgeneral/Passport/DocPassport.Editor.cs`):
- `FillAdditionalInfoInitialValues` (строки 859-862)
- `FillQualityParametersInitialValues` (строки 916-933)

```csharp
// Определяем флаги ELIS на основе истории изменений (GetLastSourceForControl)
var lastValueSource = dataArm.GetLastSourceForControl(valueControlId);
var valueElisFlag = lastValueSource == DataSource.ELIS || lastValueSource == DataSource.ReturnToELIS;
values[$"value.{key}__elisFilled"] = valueElisFlag; // ❌ Бэкенд отправляет готовый флаг
```

**Фронтенд** (`TN_Doc/Client/document-editor/src/composables/useDocumentEditor.ts`):
- Получает готовый `__elisFilled` с бэкенда
- Использует его в `restoreElisOriginals()` для восстановления `__elisOriginal`

### Почему это проблема

1. **Дублирование логики**: Одна и та же логика определения источника данных реализована в двух местах
2. **Риск рассинхронизации**: При изменении логики нужно обновлять и бэкенд, и фронтенд
3. **Нарушение принципа единого источника правды**: История (`__history`) — единственный источник правды, флаг должен вычисляться из неё
4. **Избыточная передача данных**: Флаг можно вычислить на клиенте, не нужно передавать с сервера

## Решение

Перенести вычисление `__elisFilled` полностью на фронтенд.

### Правильная архитектура

**Бэкенд**:
- Отправляет только `__history` (источник правды)
- НЕ вычисляет и НЕ отправляет `__elisFilled`

**Фронтенд**:
- Вычисляет `__elisFilled` из последней записи `__history`
- Использует `__elisFilled` для `restoreElisOriginals()`

### План реализации

#### 1. Создать функцию `restoreElisFilledFromHistory()` на фронтенде

**Файл**: `TN_Doc/Client/document-editor/src/composables/useDocumentEditor.ts`

```typescript
/**
 * Восстанавливает флаги __elisFilled из истории изменений при загрузке документа.
 *
 * Логика: берёт последний Source из __history и устанавливает:
 * - __elisFilled = true, если Source === 'ELIS' или 'ReturnToELIS'
 * - __elisFilled = false для остальных источников (Manual, IVK, Auto)
 */
const restoreElisFilledFromHistory = () => {
  const formData = store.formData;

  // Найти все ключи с историей изменений
  const historyKeys = Object.keys(formData)
    .filter(key => key.endsWith('__history') && Array.isArray(formData[key]) && formData[key].length > 0);

  let restoredCount = 0;

  for (const historyKey of historyKeys) {
    const baseKey = historyKey.replace('__history', '');
    const history = formData[historyKey] as Array<{ Source: string }>;

    // Получаем последний источник из истории
    const lastEntry = history[history.length - 1];
    const lastSource = lastEntry?.Source;

    if (!lastSource) {
      continue;
    }

    // Устанавливаем флаг на основе источника
    const elisFilled = lastSource === 'ELIS' || lastSource === 'ReturnToELIS';
    formData[`${baseKey}__elisFilled`] = elisFilled;
    restoredCount++;

    logger.debug('[restoreElisFilledFromHistory] Восстановлен __elisFilled из истории', {
      key: baseKey,
      lastSource,
      elisFilled
    });
  }

  if (restoredCount > 0) {
    logger.info('[restoreElisFilledFromHistory] Восстановлено __elisFilled флагов из истории', {
      count: restoredCount
    });
  }
};
```

**Вызов** в `loadDocument()`:
```typescript
await store.loadConfig(deviceId, docType, id);

// 1. Сначала вычисляем __elisFilled из истории
restoreElisFilledFromHistory();

// 2. Затем восстанавливаем __elisOriginal для полей с __elisFilled=true
restoreElisOriginals();
```

#### 2. Удалить установку `__elisFilled` на бэкенде

**Файл**: `tn.docgeneral/Passport/DocPassport.Editor.cs`

##### 2.1. В `FillAdditionalInfoInitialValues` (строки 859-862)

Удалить блок:
```csharp
// Определяем флаг __elisFilled на основе последнего источника в истории
// Это необходимо для восстановления __elisOriginal через restoreElisOriginals()
var lastSource = dataArm.GetLastSourceForControl(fieldKey);
values[$"{fieldKey}__elisFilled"] = lastSource == DataSource.ELIS || lastSource == DataSource.ReturnToELIS;
```

Оставить только:
```csharp
foreach (var fieldKey in additionalInfoKeys)
{
    if (dataArm?.FieldHistoryMap.TryGetValue(fieldKey, out var history) == true && history.Count > 0)
    {
        values[$"{fieldKey}__history"] = history;
        // __elisFilled будет вычислен на фронтенде из истории
    }
}
```

##### 2.2. В `FillQualityParametersInitialValues` (строки 916-933, 942-943, 958)

Удалить блок вычисления флагов:
```csharp
// Определяем флаги ELIS на основе истории изменений (GetLastSourceForControl)
// ВАЖНО: ReturnToELIS визуально отображается как ELIS, поэтому учитываем оба источника
var lastValueSource = dataArm.GetLastSourceForControl(valueControlId);
var lastResultSource = dataArm.GetLastSourceForControl(resultControlId);
var lastMethodSource = dataArm.GetLastSourceForControl(methodControlId);
var lastDocumentSource = dataArm.GetLastSourceForControl(documentControlId);

var valueElisFlag = lastValueSource == DataSource.ELIS || lastValueSource == DataSource.ReturnToELIS;
var resultElisFlag = lastResultSource == DataSource.ELIS || lastResultSource == DataSource.ReturnToELIS;
var methodElisFlag = lastMethodSource == DataSource.ELIS || lastMethodSource == DataSource.ReturnToELIS;
var documentElisFlag = lastDocumentSource == DataSource.ELIS || lastDocumentSource == DataSource.ReturnToELIS;
```

Удалить установку флагов:
```csharp
values[$"value.{key}"] = parameterValues.Measurement;
values[$"value.{key}__elisFilled"] = valueElisFlag; // ❌ Удалить

values[$"result.{key}"] = parameterValues.Result;
values[$"result.{key}__elisFilled"] = resultElisFlag; // ❌ Удалить

values[$"method.{key}"] = JsonSerializeObject(selectedOption).ToString();
values[$"method.{key}__elisFilled"] = methodElisFlag; // ❌ Удалить

values[$"document.{key}"] = documentPayload;
values[$"document.{key}__elisFilled"] = documentElisFlag; // ❌ Удалить
```

Оставить только передачу истории:
```csharp
// Возвращаем историю для фронтенда ТОЛЬКО если ELIS включен
if (isElisUsed)
{
    if (dataArm.FieldHistoryMap.TryGetValue(valueControlId, out var valueHistory) && valueHistory.Count > 0)
    {
        values[$"{valueControlId}__history"] = valueHistory;
        // __elisFilled будет вычислен на фронтенде из истории
    }

    // ... аналогично для result, method, document
}
```

#### 3. Тестирование

- [ ] Проверить AdditionalInfo поля: возврат к значениям ELIS работает
- [ ] Проверить параметры качества (value, result, method, document): возврат к ELIS работает
- [ ] Проверить, что индикаторы источника данных отображаются корректно
- [ ] Проверить сохранение и повторное открытие документа

## Выгоды

1. ✅ **Единый источник правды**: История (`__history`) — единственные данные с сервера
2. ✅ **Нет дублирования**: Логика определения `elisFilled` только на фронтенде
3. ✅ **Проще поддерживать**: Изменения логики только в одном месте
4. ✅ **Меньше данных передаётся**: Флаги вычисляются на клиенте
5. ✅ **Согласованность**: Невозможна рассинхронизация между бэкендом и фронтендом

## Связанные файлы

### Бэкенд
- `tn.docgeneral/Passport/DocPassport.Editor.cs` - методы `FillAdditionalInfoInitialValues`, `FillQualityParametersInitialValues`
- `tn.docgeneral/Passport/DataIVKDoc.cs` - класс `DataARM`, метод `GetLastSourceForControl`

### Фронтенд
- `TN_Doc/Client/document-editor/src/composables/useDocumentEditor.ts` - функции `restoreElisFilledFromHistory`, `restoreElisOriginals`
- `TN_Doc/Client/document-editor/src/components/FormFieldWithHistory.vue` - обработчик `handleChange`, проверка возврата к ELIS
- `TN_Doc/Client/document-editor/src/stores/documentStore.ts` - хранилище `formData`

## Приоритет

**Средний** - текущая реализация работает, но создаёт технический долг и усложняет поддержку.

## Дата создания

2025-12-04
