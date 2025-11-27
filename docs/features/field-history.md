# История изменений полей документов

**Версия:** v1.4.4+
**Статус:** ✅ Реализовано
**Область применения:** Паспорт качества (Passport)

---

## ⚠️ КРИТИЧЕСКОЕ ТРЕБОВАНИЕ

**История изменений работает ТОЛЬКО при включенном ELIS в конфигурации приложения.**

В файле `TN_Doc/Cfg/CfgApp.json` устройства должна быть установлена настройка:
```json
{
  "IsUsedElis": true
}
```

Если ELIS выключен (`IsUsedElis = false`), функциональность истории изменений:
- ❌ Не сохраняется в базу данных
- ❌ Не передается на фронтенд
- ❌ Не отображается пользователю
- ❌ Индикаторы истории (FieldHistoryIndicator) не показываются

**Проверка:** После включения ELIS перезапустите приложение TN_Doc.

---

## Обзор

Система истории изменений отслеживает источник и время всех изменений полей в редакторе документов. Для каждого поля сохраняется до 10 последних изменений с информацией об источнике данных, авторе, времени и значениях.

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

**Триггер:** Наведение курсора (hover) → автоматически открывается FieldHistoryPopup

**Важно:** Popup открывается при наведении мыши на индикатор, а не при клике.

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
// useElisIntegration.ts (в DocumentPassportEditor.vue)
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

    // Для метода испытаний сохраняем только название (v1.4.4+)
    if (elisData.methods && elisData.methods[param.key]) {
      const methodData = elisData.methods[param.key];

      // Сохраняем полный JSON метода в formData
      store.updateField(`method.${param.key}`, JSON.stringify(methodData));

      // В историю записываем только читаемое название
      trackElisLoad(
        `method.${param.key}`,
        methodData.name,  // Только название, не весь объект
        elisData.protocolNumber
      );
    }
  });
};
```

**Результат для измерения:**
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

**Результат для метода испытаний (v1.4.4+):**
```json
{
  "source": "ELIS",
  "modifiedAt": "2025-01-14T10:00:00Z",
  "modifiedBy": "ELIS",
  "value": "ГОСТ 1756-2000",
  "previousValue": null,
  "comment": "Загружено из протокола ПР-2024-12345"
}
```

**Визуальная индикация:**
- Зелёная подсветка поля (#8fd19e)
- Зелёный индикатор "ЕЛИС" в правом углу
- Popup показывает номер протокола ELIS и **читаемое название метода**

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
import { useDocumentStore } from '@/stores/documentStore';
import { useFieldHistory } from '@/composables/useFieldHistory';

const store = useDocumentStore();
const { trackElisLoad, trackManualChange, getFieldHistory } = useFieldHistory();

// 1. Пользователь открывает документ Passport с id=12345 на устройстве 1
await store.loadConfig(1, 'Passport', 12345);
// История автоматически загружена из БД в store.formHistory

// 2. Нажимает кнопку "Загрузить из ЕЛИС"
// Данные приходят из протокола ПР-2024-12345
trackElisLoad('value.Density', '850.5', 'ПР-2024-12345');
// Создана запись истории: Source=ELIS, comment="Загружено из протокола ПР-2024-12345"

// 3. Вручную корректирует значение в поле (округление до 3 знаков)
trackManualChange('value.Density', '850.567', '850.5');
// Создана запись истории: Source=Manual, previousValue='850.5'

// 4. Сохраняет документ (Ctrl+S или кнопка "Сохранить")
await store.saveDocument(1, 'Passport', 12345);
// Payload включает __history['value.Density'] с 2 записями
// Backend сохраняет в DataARM.FieldHistoryMap (ТОЛЬКО если IsUsedElis=true)

// 5. Наводит курсор на индикатор "ЕЛИС" → видит popup с историей
const history = getFieldHistory('value.Density');
console.log(history);
// [
//   {
//     source: 'ELIS',
//     modifiedAt: '2025-01-14T10:00:00Z',
//     modifiedBy: 'ELIS',
//     value: '850.5',
//     previousValue: null,
//     comment: 'Загружено из протокола ПР-2024-12345'
//   },
//   {
//     source: 'Manual',
//     modifiedAt: '2025-01-14T10:32:00Z',
//     modifiedBy: 'Пользователь',
//     value: '850.567',
//     previousValue: '850.5',
//     comment: null
//   }
// ]
```

### Сценарий 2: Округление ИВК

```typescript
import { useFieldHistory } from '@/composables/useFieldHistory';

const { trackManualChange, trackIVKRounding, getFieldHistory } = useFieldHistory();

// Пользователь ввёл значение плотности с избыточной точностью (6 знаков)
trackManualChange('value.Density', '850.123456', null);
// Создана запись: Source=Manual, value='850.123456'

// Система ИВК автоматически округлила до 2 знаков (согласно настройкам параметра)
trackIVKRounding('value.Density', '850.123456', '850.12', 2);
// Создана запись: Source=IVK, value='850.12', previousValue='850.123456'
// comment="Округлено системой ИВК до 2 знаков"

// Проверка истории
const history = getFieldHistory('value.Density');
console.log(history.length); // 2

// Индикатор теперь показывает "ИВК" (оранжевый) вместо ручного ввода
// Popup показывает обе записи: исходный ввод и округление
```

**Важно:** Округление ИВК происходит автоматически и не может быть отключено пользователем.

### Сценарий 3: FIFO очередь (автоматическое удаление старых записей)

```typescript
import { useFieldHistory } from '@/composables/useFieldHistory';

const { trackManualChange, getFieldHistory } = useFieldHistory();

// Симуляция многократного редактирования одного поля
// (например, пользователь несколько раз корректирует значение)
for (let i = 1; i <= 12; i++) {
  const previousValue = i === 1 ? null : `${850 + i - 1}`;
  trackManualChange('value.Density', `${850 + i}`, previousValue);

  console.log(`Изменение #${i}: ${previousValue || 'null'} → ${850 + i}`);
}

// История автоматически ограничена 10 записями
const history = getFieldHistory('value.Density');
console.log(history.length); // 10 (записи #1 и #2 автоматически удалены)

// Самая старая запись в истории - изменение #3
console.log(history[0].value); // "853"
console.log(history[0].previousValue); // "852"

// Самая новая запись - изменение #12
console.log(history[9].value); // "862"
console.log(history[9].previousValue); // "861"
```

**Примечание:** Ограничение в 10 записей задано константой `FieldHistoryEntry.MaxHistoryEntries = 10` и применяется как на фронтенде, так и на бэкенде.

---

## См. также

- [Architecture Overview - Field History](../architecture/overview.md#field-history-tracking-architecture-v144)
- [Document Modules - FieldHistoryMap](../architecture/document-modules.md)
- [API Endpoints - Field History API](../api/endpoints.md#field-history-api-v144)
- [UI Design - History Components](../ui-design.md#компоненты-истории-изменений-полей-v144)
- [ELIS Integration - History Tracking](../elis-summary.md)

---

## История изменений

**v1.4.4 (2025-11-27)** - Сохранение флага ELIS при редактировании метода
- ✅ **Исправлено сохранение флага ELIS при редактировании метода:**
  - Флаг ELIS метода сохраняется если название метода не изменялось
  - Флаг ELIS результата сохраняется если значение не изменялось
  - Изменение limitValue/limitValueActivate/limitValueString не сбрасывает флаг ELIS
  - При открытии диалога "Редактирование метода испытаний" и сохранении без изменения названия - индикатор ELIS сохраняется
- ✅ **Мгновенное обновление предупреждения "отсутствует в справочнике":**
  - После добавления метода в справочник через диалог предупреждение исчезает мгновенно
  - Добавлен callback `addMethodToLocalDictionary` для обновления локального списка методов
  - Не требуется перезагрузка страницы для скрытия предупреждения

**v1.4.4 (2025-01-20)** - Улучшения истории для методов испытаний
- ✅ **Оптимизация истории методов испытаний:**
  - История для поля "Метод испытаний" теперь сохраняет только название метода (поле `name`)
  - Ранее сохранялся полный JSON объект, что делало историю нечитаемой
  - Добавлена автоматическая запись истории при ручном изменении метода
  - Ранее история записывалась только при загрузке из ELIS
- ✅ Улучшена читаемость popup истории изменений

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
- ⚠️ **Требует IsUsedElis = true** - история не работает при выключенном ELIS
- ⚠️ **Реализовано только для Passport** - другие типы документов в roadmap
- ⚠️ **ФИО пользователя недоступно** - используется "Пользователь" для ручных изменений
- ⚠️ **Unit-тесты в разработке** - покрытие тестами запланировано

---

## Быстрый старт

### Для администраторов

**1. Включите ELIS в конфигурации:**
```bash
# Откройте TN_Doc/Cfg/CfgApp.json
nano /opt/TN_Doc/Cfg/CfgApp.json  # Linux
notepad TN_Doc\Cfg\CfgApp.json    # Windows
```

```json
{
  "Devices": [
    {
      "Id": 1,
      "Name": "ИВК-1",
      "IsUsedElis": true,  // ← Установите true
      // ... остальные настройки
    }
  ]
}
```

**2. Перезапустите TN_Doc:**
```bash
# Linux
sudo systemctl restart tn_doc

# Windows
Restart-Service TN_Doc
```

**3. Проверьте работу:**
- Откройте редактор документа (http://localhost:38509/document-editor)
- Откройте любой Passport
- Внесите изменение в поле
- Сохраните документ (Ctrl+S)
- Наведите курсор на индикатор → должен появиться popup с историей

### Для разработчиков

**Backend (C#):**
```csharp
// Добавление записи в историю
var entry = new FieldHistoryEntry
{
    Source = DataSource.Manual,
    ModifiedAt = DateTime.UtcNow,
    ModifiedBy = "Пользователь",
    Value = "850.5",
    PreviousValue = "850.4",
    Comment = null
};

dataArm.AddFieldHistoryEntry("value.Density", entry);

// Получение последнего источника
var lastSource = dataArm.GetLastSourceForControl("value.Density");
```

**Frontend (TypeScript):**
```typescript
import { useFieldHistory } from '@/composables/useFieldHistory';

const { trackManualChange, getFieldHistory } = useFieldHistory();

// Отследить изменение
trackManualChange('value.Density', '850.5', '850.4');

// Получить историю
const history = getFieldHistory('value.Density');
```

---

## Roadmap - Планы развития

### v1.4.5 (Q1 2025)
- 🔄 **Поддержка других типов документов:**
  - Act (Акты приёма-сдачи)
  - Report (Отчёты о качестве)
  - Jornal (Журналы регистрации)
- 🔄 **Расширенная история для методов испытаний** - отслеживание изменений в документах ELIS
- 🔄 **Unit-тесты** - полное покрытие backend и frontend

### v1.5.0 (Q2 2025)
- 🔄 **Аутентификация пользователей** - отслеживание ФИО оператора вместо "Пользователь"
- 🔄 **Фильтрация истории** - по источнику, дате, автору
- 🔄 **Экспорт истории** - в Excel/PDF для аудита

### v2.0.0 (Q3 2025)
- 🔄 **История изменений всего документа** - не только полей, но и структурных изменений
- 🔄 **Сравнение версий** - diff между двумя состояниями документа
- 🔄 **Восстановление из истории** - откат к предыдущему значению

---

## Troubleshooting - Устранение неполадок

### Проблема: Индикаторы истории не отображаются

**Причина:** ELIS выключен в конфигурации

**Решение:**
1. Откройте `TN_Doc/Cfg/CfgApp.json`
2. Найдите секцию устройства
3. Установите `"IsUsedElis": true`
4. Перезапустите приложение TN_Doc
5. Откройте документ - индикаторы должны появиться

**Проверка настройки:**
```bash
# Linux
grep -A 5 '"IsUsedElis"' /opt/TN_Doc/Cfg/CfgApp.json

# Windows PowerShell
Select-String -Path "TN_Doc\Cfg\CfgApp.json" -Pattern "IsUsedElis" -Context 0,5
```

### Проблема: История не сохраняется после редактирования

**Причина 1:** Значение не изменилось (одинаковые значения с учётом нормализации)

**Решение:** Убедитесь, что новое значение отличается от старого. Числа сравниваются с заменой запятой на точку.

**Причина 2:** Ошибка при сохранении документа

**Решение:**
1. Откройте консоль браузера (F12)
2. Проверьте вкладку Network → запрос `/api/documents/.../save/...`
3. Проверьте логи TN_Doc: `logs/tn_doc.log` (Linux: `/opt/TN_Doc/logs/`)
4. Найдите ошибки с текстом "FieldHistory" или "DocUpdate"

### Проблема: Popup истории пустой

**Причина 1:** Поле ещё не редактировалось

**Решение:** Это нормально для новых документов. Внесите изменение в поле.

**Причина 2:** История не загрузилась из БД

**Решение:**
1. Проверьте логи: `grep "FieldHistoryMap" logs/tn_doc.log`
2. Убедитесь, что JSON в БД не повреждён:
```sql
SELECT JSON_EXTRACT(DataARMJson, '$.FieldHistoryMap')
FROM TN_Doc
WHERE Id = <document_id>;
```

### Проблема: После обновления с v1.4.3 индикаторы не показывают ELIS

**Причина:** Миграция из старого флага `ElisFilled` не сработала

**Решение:**
1. Откройте документ в редакторе
2. Сохраните документ без изменений (миграция произойдёт автоматически)
3. Перезагрузите страницу - индикаторы ELIS должны появиться

**Проверка миграции в логах:**
```bash
grep "Миграция из ElisFilled" logs/tn_doc.log
```

### Проблема: Индикатор показывает "Unknown" вместо "ELIS"

**Причина:** Старые данные без детальной истории

**Решение:**
1. Загрузите данные из ELIS заново (кнопка "Загрузить из ЕЛИС")
2. Новые значения будут помечены правильно
3. Сохраните документ

### Проблема: Popup открывается при клике, а не при наведении

**Причина:** Использована старая версия компонента

**Решение:**
1. Проверьте версию: `cat TN_Doc/Client/document-editor/package.json | grep version`
2. Убедитесь, что установлена v1.4.4+
3. Пересоберите фронтенд: `cd TN_Doc/Client && npm run build:editor`

### Получение диагностической информации

**Backend (логи):**
```bash
# Последние записи об истории изменений
tail -n 100 logs/tn_doc.log | grep -i "history"

# Ошибки при сохранении
grep "ERROR.*FieldHistory" logs/tn_doc.log
```

**Frontend (консоль браузера):**
```javascript
// Проверка хранилища истории
const store = useDocumentStore();
console.log(store.formHistory);

// Проверка истории конкретного поля
const { getFieldHistory } = useFieldHistory();
console.log(getFieldHistory('value.Density'));

// Проверка настройки ELIS
console.log(store.config?.isElisUsed);
```

### Проблема: История работает локально, но не на сервере

**Причина:** Разные настройки в Development и Production конфигурациях

**Решение:**
1. Проверьте конфигурацию на сервере:
```bash
# Linux
cat /opt/TN_Doc/Cfg/CfgApp.json | grep -A 10 '"IsUsedElis"'

# Windows
type C:\TN_Doc\Cfg\CfgApp.json | findstr /C:"IsUsedElis"
```

2. Убедитесь, что `IsUsedElis = true` для нужного устройства

3. Проверьте логи после перезапуска службы:
```bash
# Linux (systemd)
sudo systemctl restart tn_doc
sudo journalctl -u tn_doc -n 50

# Windows (служба)
Restart-Service TN_Doc
Get-EventLog -LogName Application -Source TN_Doc -Newest 50
```

### Быстрая диагностика через API

**Проверка включения ELIS для устройства:**
```bash
curl http://localhost:38509/api/config/devices
# Найдите ваше устройство и проверьте IsUsedElis
```

**Получение конфигурации редактирования документа:**
```bash
curl http://localhost:38509/api/documents/1/Passport/edit/12345 | jq '.initialValues | keys | map(select(endswith("__history")))'
# Должен вернуть список полей с суффиксом __history
```

**Проверка сохранения истории:**
```bash
# Отправьте тестовое изменение
curl -X POST http://localhost:38509/api/documents/1/Passport/save/12345 \
  -H "Content-Type: application/json" \
  -d '{
    "ExportPermit": "TEST123",
    "__history": {
      "ExportPermit": [{
        "source": "Manual",
        "modifiedAt": "'$(date -Iseconds)'",
        "modifiedBy": "Тест",
        "value": "TEST123"
      }]
    }
  }'
```
