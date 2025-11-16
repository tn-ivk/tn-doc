# История изменений полей документов

**Версия:** v1.4.4+
**Статус:** ✅ Реализовано
**Область применения:** Паспорт качества (Passport)

---

## Обзор

Система истории изменений отслеживает источник и время всех изменений полей в редакторе документов. Для каждого поля сохраняется до 10 последних изменений с информацией об источнике данных, авторе, времени и значениях.

**⚠️ ВАЖНО: История изменений работает ТОЛЬКО при включенном ELIS в конфигурации приложения (CfgApp.json).**
Если ELIS выключен (`IsUsedElis = false`), история изменений:
- Не сохраняется в базу данных
- Не передается на фронтенд
- Не отображается пользователю
- Индикаторы истории (FieldHistoryIndicator) не показываются

**Основные возможности:**
- Отслеживание источника данных (ELIS, ручное редактирование, округление ИВК)
- Визуальная индикация источника в UI (цветные значки)
- Детальная история изменений в popup окне
- Автоматическая миграция старых данных
- FIFO очередь (максимум 10 записей на поле)

---

## Источники данных

### DataSource enum

| Источник | Значение | Описание | Цвет индикатора |
|----------|----------|----------|-----------------|
| **ELIS** | `ELIS` | Загружено из протокола ЕЛИС | Зелёный `#4CAF50` |
| **Manual** | `Manual` | Ручное редактирование пользователем | Синий `#2196F3` |
| **IVK** | `IVK` | Округление системой ИВК | Оранжевый `#FF9800` |
| **Unknown** | `Unknown` | Неизвестный источник (старые данные) | Серый `#9E9E9E` |

---

## Архитектура

### Backend (C#)

#### FieldHistoryEntry - Запись истории

```csharp
public class FieldHistoryEntry
{
    public DataSource Source { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }  // "Пользователь", "ELIS", "IVK"
    public string Value { get; set; }
    public string? PreviousValue { get; set; }
    public string? Comment { get; set; }

    public const int MaxHistoryEntries = 10;
}
```

#### DataARM.FieldHistoryMap - Хранилище истории

```csharp
public partial class DataARM
{
    // Словарь: controlId → список записей истории
    public Dictionary<string, List<FieldHistoryEntry>> FieldHistoryMap { get; set; }
        = new Dictionary<string, List<FieldHistoryEntry>>(StringComparer.OrdinalIgnoreCase);

    // Добавить запись в историю
    public void AddFieldHistoryEntry(string controlId, FieldHistoryEntry entry)
    {
        // ... реализация с FIFO (удаление старых записей при превышении 10)
    }

    // Получить последний источник изменений
    public DataSource GetLastSourceForControl(string controlId)
    {
        // ... возвращает Source последней записи или Unknown
    }
}
```

#### Ключи истории

**AdditionalInfo поля (простые ключи):**
- `ExportPermit`, `Sample`, `Laboratory_IOF`, `ExportPermit_Date`, `Sample_Date`, `Laboratory_Date`

**Параметры качества (составные ключи):**
- `value.{ParameterKey}` - измеренное значение
- `result.{ParameterKey}` - результат для печати
- `method.{ParameterKey}` - метод испытаний (JSON string)
- `document.{ParameterKey}` - документ ELIS (JSON string)

**Важно:** Каждый тип поля имеет раздельную историю (одновременное изменение value и method создаёт 2 записи).

### Frontend (TypeScript)

#### Типы данных

```typescript
enum DataSource {
  Unknown = 'Unknown',
  ELIS = 'ELIS',
  Manual = 'Manual',
  IVK = 'IVK'
}

interface FieldHistoryEntry {
  source: DataSource;
  modifiedAt: string;  // ISO 8601
  modifiedBy: string;
  value: string;
  previousValue?: string;
  comment?: string;
}
```

#### Composable useFieldHistory

```typescript
const {
  trackManualChange,    // Отследить ручное изменение
  trackElisLoad,        // Отследить загрузку из ELIS
  trackIVKRounding,     // Отследить округление ИВК
  getFieldHistory,      // Получить историю поля
  getLastSource,        // Получить последний источник
  clearFieldHistory     // Очистить историю
} = useFieldHistory();

// Пример: отследить ручное изменение
trackManualChange('value.Density', '850.567', '850.5');

// Пример: отследить загрузку из ELIS
trackElisLoad('value.Density', '850.5', 'ПР-2024-12345');
```

#### DocumentStore

```typescript
const store = useDocumentStore();

// История всех полей
store.formHistory: Record<string, FieldHistoryEntry[]>

// Загрузка истории из API
await store.loadConfig(deviceId, docType, id);

// Сохранение с историей
await store.saveDocument(deviceId, docType, id);
// → payload.__history содержит formHistory
```

---

## UI Компоненты

### FieldHistoryIndicator - Индикатор источника

**Расположение:** `TN_Doc/Client/document-editor/src/components/history/FieldHistoryIndicator.vue`

**Визуальные характеристики:**
- Позиция: правый верхний угол поля (absolute, top: 4px, right: 4px)
- Размер текста "ЕЛИС"/"ИВК": 6px
- Размер иконок (Manual/Unknown): 14px
- Фон: rgba(255, 255, 255, 0.9)
- Тень: 0 1px 3px rgba(0, 0, 0, 0.1)
- Hover: scale(1.05), увеличенная тень

**Отображение по источникам:**
- ELIS → Текст "ЕЛИС" зелёный
- Manual → Иконка `pi-user-edit` синяя
- IVK → Текст "ИВК" оранжевый
- Unknown → Иконка `pi-question-circle` серая

**Триггер:** Наведение курсора → открывается FieldHistoryPopup

### FieldHistoryPopup - Детальная история

**Расположение:** `TN_Doc/Client/document-editor/src/components/history/FieldHistoryPopup.vue`

**Технология:** PrimeVue OverlayPanel

**Структура:**
1. **Заголовок:**
   - "История изменений" (16px, font-weight: 600, по центру)
   - Название поля (13px, secondary color, по центру)
2. **Список записей (обратный порядок):**
   - Иконка/текст источника + Описание + Дата/время (с секундами)
   - Старое → Новое значение (старое зачёркнуто красным, новое зелёным)
   - Цветовая индикация: левая граница 3px (зелёная/синяя/оранжевая)
3. **Пустое состояние:**
   - Иконка `pi-info-circle` + "История изменений отсутствует"

**Пример:**
```
┌─────────────────────────────────────────┐
│         История изменений               │
│    Массовая доля воды, %                │
├─────────────────────────────────────────┤
│ ЕЛИС Загружено из протокола ЕЛИС       │
│      14.01.2025 10:00:35                │
│      850.5 → 850.567                    │
│                                         │
│ [✎] Отредактировано вручную            │
│     14.01.2025 10:32:18                 │
│     850.567 → 850.57                    │
└─────────────────────────────────────────┘
```

### Компоненты-обёртки

- **FormFieldWithHistory** - для AdditionalInfo полей
- **PassportMeasurementInputWithHistory** - для value.* (измерения)
- **PassportMethodSelectWithHistory** - для method.* (методы испытаний)
- **PassportResultCellWithHistory** - для result.* (результаты)

---

## API

### GET /api/documents/{deviceId}/{docType}/edit/{id}

**Response:** История передаётся в двух форматах

**1. Для AdditionalInfo полей (суффикс `__history`):**
```json
{
  "initialValues": {
    "ExportPermit": "АБВ123",
    "ExportPermit__history": [
      {
        "source": "Manual",
        "modifiedAt": "2025-01-14T09:00:00Z",
        "modifiedBy": "Пользователь",
        "value": "АБВ123",
        "previousValue": null,
        "comment": null
      }
    ]
  }
}
```

**2. Для параметров качества (в схеме параметра):**
```json
{
  "qualityParametersSchema": [
    {
      "key": "Density",
      "valueHistory": [ /* история value.Density */ ],
      "resultHistory": [ /* история result.Density */ ],
      "methodHistory": [ /* история method.Density */ ],
      "documentHistory": [ /* история document.Density */ ]
    }
  ]
}
```

### POST /api/documents/{deviceId}/{docType}/save/{id}

**Request:** История передаётся в поле `__history`

```json
{
  "ExportPermit": "АБВ123",
  "value.Density": "850.567",
  "__history": {
    "ExportPermit": [
      {
        "source": "Manual",
        "modifiedAt": "2025-01-14T09:00:00Z",
        "modifiedBy": "Пользователь",
        "value": "АБВ123",
        "previousValue": null,
        "comment": null
      }
    ],
    "value.Density": [
      {
        "source": "ELIS",
        "modifiedAt": "2025-01-14T10:00:00Z",
        "modifiedBy": "ELIS",
        "value": "850.5",
        "previousValue": null,
        "comment": "Загружено из протокола ПР-2024-12345"
      },
      {
        "source": "Manual",
        "modifiedAt": "2025-01-14T10:32:00Z",
        "modifiedBy": "Пользователь",
        "value": "850.567",
        "previousValue": "850.5",
        "comment": "Скорректировано вручную"
      }
    ]
  }
}
```

**Backend обработка:**
```csharp
// DocPassport.DocUpdate()
// ВАЖНО: История сохраняется ТОЛЬКО если ELIS включен
var isElisUsed = _appConfig.IsUsedElis(_deviceId);

foreach (var item in correctionData.Values)
{
    // Проверка isElisUsed перед сохранением истории
    if (isElisUsed && item.History != null && item.History.Count > 0)
    {
        foreach (var historyEntry in item.History)
        {
            // Автоматическое проставление автора для Manual
            if (historyEntry.Source == DataSource.Manual &&
                string.IsNullOrWhiteSpace(historyEntry.ModifiedBy))
            {
                historyEntry.ModifiedBy = "Пользователь";
            }

            dataArm.AddFieldHistoryEntry(item.Key, historyEntry);
        }
    }
}
```

---

## Интеграция с ELIS

При загрузке данных из протокола ELIS автоматически создаются записи истории:

```typescript
// useElisIntegration.ts
const applyElisData = (elisData: ElisProtocol) => {
  // Для каждого заполненного поля
  qualityParams.forEach(param => {
    const value = elisData.parameters[param.elisAlias];
    if (value) {
      // Обновляем значение
      store.updateField(`value.${param.key}`, value);

      // Создаём запись истории
      trackElisLoad(
        `value.${param.key}`,
        value,
        elisData.protocolNumber  // Например, "ПР-2024-12345"
      );
    }
  });
};
```

**Результат:**
```json
{
  "source": "ELIS",
  "modifiedAt": "2025-01-14T10:00:00Z",
  "modifiedBy": "ELIS",
  "value": "850.5",
  "previousValue": null,
  "comment": "Загружено из протокола ПР-2024-12345"
}
```

**Визуальная индикация:**
- Зелёная подсветка поля (#8fd19e)
- Зелёный индикатор "ЕЛИС" в правом углу
- Popup показывает номер протокола ELIS

---

## Миграция старых данных

### Проблема

Старые документы использовали булевый флаг `ElisFilled` без детальной истории.

### Решение

При загрузке документа (GetEditConfig) автоматически создаётся запись истории:

```csharp
// DocPassport.GetEditConfig()
foreach (var labInfo in dataArm.LabInfo)
{
    var controlId = $"value.{labInfo.ParameterKey}";

    // Миграция: если ElisFilled = true, но нет истории
    if (labInfo.ElisFilled &&
        !dataArm.FieldHistoryMap.ContainsKey(controlId))
    {
        dataArm.AddFieldHistoryEntry(controlId, new FieldHistoryEntry
        {
            Source = DataSource.ELIS,
            ModifiedAt = DateTime.MinValue,  // Неизвестная дата
            ModifiedBy = "ELIS",
            Value = labInfo.Value,
            Comment = "Миграция из ElisFilled"
        });
    }
}
```

**Обратная совместимость:**

Флаг `ElisFilled` помечен как `[Obsolete]`, но сохранён и автоматически пересчитывается:

```csharp
// После сохранения истории
existingLabInfo.ElisFilled =
    dataArm.GetLastSourceForControl($"value.{parameterKey}") == DataSource.ELIS;
```

---

## Ограничения и оптимизация

### Лимиты

- **Максимум 10 записей** на поле (константа `FieldHistoryEntry.MaxHistoryEntries`)
- **FIFO очередь:** при добавлении 11-й записи удаляется самая старая
- **Размер записи:** ~150-200 байт в JSON

### Производительность

- История хранится в JSON поле `DataARM` в БД MySQL
- В памяти: Dictionary для быстрого доступа
- Индексация не требуется
- Передаётся только для изменённых полей

### Frontend оптимизация

**Нормализация значений:**

Чтобы избежать ложных срабатываний при изменении десятичного разделителя:

```typescript
// useFieldHistory.ts
const normalizeValue = (value: any): string => {
  const str = String(value).trim();
  // Замена запятой на точку для числовых значений
  return /^-?\d+[,.]?\d*$/.test(str) ? str.replace(',', '.') : str;
};

// Сравнение с нормализацией
const trackManualChange = (fieldKey: string, newValue: any, previousValue?: any) => {
  const newNormalized = normalizeValue(newValue);
  const prevNormalized = previousValue !== undefined
    ? normalizeValue(previousValue)
    : undefined;

  // Создаём запись только если значение реально изменилось
  if (newNormalized !== prevNormalized) {
    // ... создать запись истории
  }
};
```

---

## Примеры использования

### Сценарий 1: Загрузка из ELIS + ручное редактирование

```typescript
// 1. Пользователь открывает документ
await store.loadConfig(1, 'Passport', 12345);
// История загружена из БД

// 2. Загружает данные из ELIS
trackElisLoad('value.Density', '850.5', 'ПР-2024-12345');
// Создана запись: Source=ELIS

// 3. Вручную корректирует значение
trackManualChange('value.Density', '850.567', '850.5');
// Создана запись: Source=Manual

// 4. Сохраняет документ
await store.saveDocument(1, 'Passport', 12345);
// Передано: __history['value.Density'] с 2 записями

// 5. Просматривает историю
const history = getFieldHistory('value.Density');
// [
//   { source: 'ELIS', value: '850.5', ... },
//   { source: 'Manual', value: '850.567', previousValue: '850.5', ... }
// ]
```

### Сценарий 2: Округление ИВК

```typescript
// Пользователь ввёл значение с избыточной точностью
trackManualChange('value.Density', '850.123456', null);

// ИВК округлило до 2 знаков
trackIVKRounding('value.Density', '850.123456', '850.12', 2);

// История содержит 2 записи:
// 1. Manual: 850.123456
// 2. IVK: 850.12 (previousValue: 850.123456)
```

### Сценарий 3: FIFO очередь

```typescript
// Поле изменялось 12 раз
for (let i = 1; i <= 12; i++) {
  trackManualChange('value.Density', `${850 + i}`, `${850 + i - 1}`);
}

// История содержит только последние 10 записей
const history = getFieldHistory('value.Density');
console.log(history.length); // 10 (записи #1 и #2 удалены)
```

---

## См. также

- [Architecture Overview - Field History](../architecture/overview.md#field-history-tracking-architecture-v144)
- [Document Modules - FieldHistoryMap](../architecture/document-modules.md)
- [API Endpoints - Field History API](../api/endpoints.md#field-history-api-v144)
- [UI Design - History Components](../ui-design.md#компоненты-истории-изменений-полей-v144)
- [ELIS Integration - History Tracking](../elis-summary.md)

---

## История изменений

**v1.4.4 (2025-01-15)** - Первый релиз
- ✅ Базовая функциональность истории изменений
- ✅ Backend: DataSource, FieldHistoryEntry, FieldHistoryMap
- ✅ Frontend: useFieldHistory, UI компоненты
- ✅ Интеграция с ELIS
- ✅ Миграция из ElisFilled
- ✅ Нормализация значений для корректного сравнения
- ✅ Компактный UI (индикатор 6px для текста, 14px для иконок)
- ✅ Hover-триггер для popup
- ✅ Раздельная история для value/method/result полей

**Известные ограничения:**
- ⚠️ ФИО пользователя недоступно (используется "Пользователь")
- ⚠️ Реализовано только для документа Passport
- ⚠️ Unit-тесты в разработке
