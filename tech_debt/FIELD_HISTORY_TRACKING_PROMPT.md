# Промпт: Реализация истории изменений полей формы редактирования паспорта качества

**Дата создания:** 2025-01-14
**Целевая версия:** v1.5.0
**Приоритет:** Высокий

---

## Контекст проекта

Приложение **TN_Doc** - это ASP.NET Core 8.0 веб-приложение для генерации технических документов из данных измерительно-вычислительного комплекса (ИВК).

**Текущая реализация:**
- Форма редактирования паспорта качества реализована на Vue 3 + TypeScript + PrimeVue
- Подсветка полей, заполненных из ELIS (зеленый фон) - **УЖЕ РЕАЛИЗОВАНА**
- Данные сохраняются в БД MySQL в поле `DataARM` (JSON) таблицы `TableActAndPassportData`
- Текущий флаг `ElisFilled` (boolean) - **НЕДОСТАТОЧЕН** для полного отслеживания


> Эти материалы отражают прежнюю логику (история внутри `LabInfo`). Для реализации новой схемы истории ориентируемся на текущий документ; ссылки выше использовать лишь как исторический контекст.

---

## Постановка задачи

### Цель

Реализовать **полную историю изменений** для каждого поля формы редактирования паспорта качества с визуальной индикацией источника данных и возможностью просмотра истории изменений.

### Требования

#### 1. Отслеживание источника изменений

Для каждого поля необходимо отслеживать:

**Источник данных (DataSource enum):**
- 🤚 **Manual** - ручное редактирование пользователем
- ⚙️ **IVK** - автоматическое изменение системой ИВК (округление значений)
- 📊 **ELIS** - загрузка из протокола ЕЛИС
- ❓ **Unknown** - неизвестный источник (для старых данных)

**Метаданные изменения:**
- **Дата и время** изменения (ISO 8601 формат)
- **Автор изменения** (пользователь, "IVK", "ELIS")
- Для ручных правок backend записывает `ModifiedBy = "Пользователь"` (ФИО недоступно)
- **Новое значение** (фиксируем явное итоговое значение поля)
- **Предыдущее значение** (для истории изменения)
- **Комментарий** к изменению

#### 2. Визуальная индикация

**В правом верхнем углу каждого поля ввода отображается маленькая иконка:**

| Источник | Иконка | Цвет | Описание |
|----------|--------|------|----------|
| Manual | 🤚 или `pi-user-edit` | Синий (#2196F3) | Рука или иконка редактирования |
| IVK | Текст "ИВК" или `pi-cog` | Синий (#2196F3) | Надпись или иконка шестеренки |
| ELIS | Текст "ЕЛИС" или `pi-database` | Зеленый (#4CAF50) | Надпись или иконка базы данных |
| Unknown | `pi-question-circle` | Серый (#9E9E9E) | Иконка вопроса |

**Размер иконки:** 14-16px
**Позиция:** Абсолютное позиционирование в правом верхнем углу внутри элемента ввода
**Отступ от края:** 4-6px

#### 3. Popup с историей изменений

**Триггер:** Наведение курсора мыши (hover) на иконку источника

**Содержимое popup:**
```
┌─────────────────────────────────────────┐
│ История изменений: Плотность при 20°С  │
├─────────────────────────────────────────┤
│                                         │
│ 📊 Загружено из ELIS                   │
│ 14.01.2025 10:00                        │
│ Протокол: ПР-2024-12345                │
│ 850.5 → 850.567                        │
│                                         │
│ ─────────────────────────────────────── │
│                                         │
│ 🤚 Отредактировано вручную             │
│ 14.01.2025 10:30                        │
│ 850.5 → 850.567                         │
│                                         │
│ ─────────────────────────────────────── │
│                                         │
│ ⚙️ Округлено системой ИВК              │
│ 14.01.2025 10:35                        │
│ 850.567 → 850.57                        │
│                                         │
└─────────────────────────────────────────┘
```

**Компонент:** PrimeVue `OverlayPanel` или `Tooltip` (расширенный)
**Ширина:** 300-400px
**Анимация:** Fade in/out (200ms)
**Максимальная высота:** 400px с прокруткой

#### 4. Сохранение истории изменений

**Структура данных в БД (DataARM JSON):**

```json
{
  "ExportPermit": "АБВ123",
  "Sample": "Образец №1",
  "LabInfo": [
    {
      "ParameterKey": "Density",
      "Value": "850.57",
      "Metod": {...},
      "Document": {...},
      "ElisFilled": true
    }
  ],
  "FieldHistory": {
    "ExportPermit": [
      {
        "Source": "Manual",
        "ModifiedAt": "2025-01-14T09:00:00",
        "ModifiedBy": "Пользователь",
        "Value": "АБВ123",
        "Comment": null
      }
    ],
    "Sample": [
      {
        "Source": "ELIS",
        "ModifiedAt": "2025-01-14T09:00:00",
        "ModifiedBy": "ELIS",
        "Value": "Образец №1",
        "Comment": "Загружено из протокола ПР-2024-12345"
      }
    ],
    "value.Density": [
      {
        "Source": "Manual",
        "ModifiedAt": "2025-01-14T10:32:00",
        "ModifiedBy": "Пользователь",
        "Value": "850.567",
        "PreviousValue": "850.5",
        "Comment": "Скорректировано вручную"
      }
    ]
  },
  "ElisProtocol": {...}
}
```

**Ограничения:**
- Максимум **10 последних изменений** на поле (FIFO очередь)
- Общий размер истории не должен превышать **50% от размера DataARM**

---

## Архитектура решения

### Структура компонентов

```
DocumentPassportEditor.vue
├── FormFieldWithHistory.vue (НОВЫЙ)
│   ├── FormField.vue (существующий, используется внутри)
│   ├── FieldHistoryIndicator.vue (НОВЫЙ)
│   └── FieldHistoryPopup.vue (НОВЫЙ)
│
├── PassportQualityTable.vue
│   ├── PassportMeasurementInput.vue → PassportMeasurementInputWithHistory.vue (НОВЫЙ)
│   ├── PassportResultCell.vue → PassportResultCellWithHistory.vue (НОВЫЙ)
│   ├── PassportMethodSelect.vue → PassportMethodSelectWithHistory.vue (НОВЫЙ)
│   └── FieldHistoryIndicator.vue (переиспользуется)
│
└── useFieldHistory.ts (НОВЫЙ композабл)
```

### Поток данных

```
1. Пользователь изменяет поле
   ↓
2. useFieldHistory.trackChange() создает запись истории
   ↓
3. documentStore обновляет formData и formHistory
   ↓
4. При сохранении: история передается в DocPassport.DocUpdate()
   ↓
5. Бэкенд сохраняет историю в DataARM JSON
   ↓
6. При загрузке: история восстанавливается из DataARM
   ↓
7. Компонент отображает иконку источника и popup с историей
```

---

## Детальная реализация

### Этап 1: Бэкенд (C#) - Структуры данных

#### 1.1. Создать enum DataSource

**Файл:** `tn.docgeneral/Passport/Models/DataSource.cs`

```csharp
namespace TN.Doc.Passport.Models;

/// <summary>
/// Источник данных поля
/// </summary>
public enum DataSource
{
    /// <summary>
    /// Неизвестный источник (для старых данных)
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Загружено из протокола ЕЛИС
    /// </summary>
    ELIS = 1,

    /// <summary>
    /// Отредактировано вручную пользователем
    /// </summary>
    Manual = 2,

    /// <summary>
    /// Изменено системой ИВК (округление, автозаполнение)
    /// </summary>
    IVK = 3
}
```

#### 1.2. Создать класс FieldHistoryEntry

**Файл:** `tn.docgeneral/Passport/Models/FieldHistoryEntry.cs`

```csharp
using System;

namespace TN.Doc.Passport.Models;

/// <summary>
/// Запись истории изменения поля
/// </summary>
public class FieldHistoryEntry
{
    /// <summary>
    /// Источник данных
    /// </summary>
    public DataSource Source { get; set; }

    /// <summary>
    /// Дата и время изменения (UTC)
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Автор изменения ("Пользователь" для ручных правок, "IVK", "ELIS")
    /// </summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// Значение после изменения
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Предыдущее значение (для отката)
    /// </summary>
    public string? PreviousValue { get; set; }

    /// <summary>
    /// Комментарий к изменению
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Максимальное количество записей истории на поле
    /// </summary>
    public const int MaxHistoryEntries = 10;
}
```

#### 1.3. LabInfo (без изменений структуры)

**Файл:** `tn.docgeneral/Passport/DocPassport.cs` (класс LabInfo)

```csharp
public partial class LabInfo
{
    public string ParameterKey { get; set; } = string.Empty;
    public Metod Metod { get; set; } = new();
    public LabDocumentInfo Document { get; set; } = new();
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Флаг, сигнализирующий что значение получено из ELIS (оставляем для обратной совместимости)
    /// </summary>
    public bool ElisFilled { get; set; } = false;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{nameof(ParameterKey)}: {ParameterKey ?? "Нет данных"}");
        sb.AppendLine($"{nameof(Value)}: {Value ?? "Нет данных"}");
        sb.AppendLine($"{nameof(ElisFilled)}: {ElisFilled}");
        return sb.ToString();
    }
}
```

> Историю больше не храним в `LabInfo`. Все события фиксируются в едином словаре `FieldHistory` (см. п. 1.4), где ключом выступает controlId с фронтенда.

#### 1.4. Расширить DataARM

**Файл:** `tn.docgeneral/Passport/DocPassport.cs` (класс DataARM)

```csharp
public partial class DataARM
{
    // ... существующие поля ...

    /// <summary>
    /// История изменений по идентификатору элемента формы
    /// Ключ - конкретный controlId с фронтенда (например, "ExportPermit", "value.Density")
    /// Значение - список записей истории (до 10 последних записей)
    /// </summary>
    public Dictionary<string, List<FieldHistoryEntry>> FieldHistoryMap { get; set; }
        = new Dictionary<string, List<FieldHistoryEntry>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Добавить запись в историю конкретного элемента формы
    /// </summary>
    public void AddFieldHistoryEntry(string controlId, FieldHistoryEntry entry)
    {
        if (string.IsNullOrWhiteSpace(controlId) || entry is null)
            return;

        if (!FieldHistoryMap.TryGetValue(controlId, out var history))
        {
            history = new List<FieldHistoryEntry>();
            FieldHistoryMap[controlId] = history;
        }

        history.Add(entry);

        // Ограничиваем размер истории
        if (history.Count > FieldHistoryEntry.MaxHistoryEntries)
        {
            history.RemoveAt(0);
        }
    }

    /// <summary>
    /// Получить последний источник изменений для элемента формы
    /// </summary>
    public DataSource GetLastSourceForControl(string controlId)
    {
        if (string.IsNullOrWhiteSpace(controlId) ||
            !FieldHistoryMap.TryGetValue(controlId, out var history) ||
            history.Count == 0)
            return DataSource.Unknown;

        return history[^1].Source;
    }
}
```

#### 1.5. Обновить логику сохранения

**Файл:** `tn.docgeneral/Passport/DocPassport.cs` (метод DocUpdate)

```csharp
// В методе DocUpdate, при обработке параметров качества (Value)
foreach (var item in correctionData.Values.Where(x => x.Tag == "Value"))
{
    var parameterKey = item.Key.Replace("value.", "");
    var existingLabInfo = dataArm.LabInfo.FirstOrDefault(x => x.ParameterKey == parameterKey);

    if (existingLabInfo != null)
    {
        // Сохраняем предыдущее значение
        var previousValue = existingLabInfo.Value;

        // Обновляем значение
        existingLabInfo.Value = item.Value;

        // Добавляем запись в историю (если передана)
        if (item.History != null && item.History.Count > 0)
        {
            foreach (var historyEntry in item.History)
            {
                if (historyEntry.Source == DataSource.Manual &&
                    string.IsNullOrWhiteSpace(historyEntry.ModifiedBy))
                {
                    historyEntry.ModifiedBy = "Пользователь";
                }

                dataArm.AddFieldHistoryEntry(item.Key, historyEntry);
                _logger.Info($"История параметра {parameterKey}: {historyEntry.Source} от {historyEntry.ModifiedBy}, {previousValue} → {historyEntry.Value}");
            }
        }

        // Обратная совместимость
        existingLabInfo.ElisFilled = dataArm.GetLastSourceForControl(item.Key) == DataSource.ELIS;
    }
    else
    {
        // Создаем новый LabInfo
        var newLabInfo = new LabInfo
        {
            ParameterKey = parameterKey,
            Value = item.Value
        };

        if (item.History != null && item.History.Count > 0)
        {
            foreach (var historyEntry in item.History)
            {
                dataArm.AddFieldHistoryEntry(item.Key, historyEntry);
            }
        }

        newLabInfo.ElisFilled = dataArm.GetLastSourceForControl(item.Key) == DataSource.ELIS;
        dataArm.LabInfo.Add(newLabInfo);
    }
}

// Аналогично для AdditionalInfo
foreach (var item in correctionData.Values.Where(x => x.Tag == "AdditionalInfo"))
{
    if (string.IsNullOrEmpty(item.Value))
        continue;

    // Обновляем значение
    switch (item.Key)
    {
        case "ExportPermit":
            dataArm.ExportPermit = item.Value;
            break;
        case "Sample":
            dataArm.Sample = item.Value;
            break;
        // ... остальные поля
    }

    // Добавляем запись в историю
    if (item.History != null && item.History.Count > 0)
    {
        foreach (var historyEntry in item.History)
        {
            if (historyEntry.Source == DataSource.Manual &&
                string.IsNullOrWhiteSpace(historyEntry.ModifiedBy))
            {
                historyEntry.ModifiedBy = "Пользователь";
            }

            dataArm.AddFieldHistoryEntry(item.Key, historyEntry);
            _logger.Info($"История AdditionalInfo {item.Key}: {historyEntry.Source} от {historyEntry.ModifiedBy}");
        }
    }
}
```

#### 1.6. Обновить EditData

**Файл:** `tn.docgeneral/Passport/Models/EditData.cs`

```csharp
using System.Collections.Generic;

namespace TN.Doc;

public class EditData
{
    public string Key { get; set; }
    public string Tag { get; set; }
    public string Value { get; set; }

    /// <summary>
    /// УСТАРЕЛО: Используйте History
    /// </summary>
    [Obsolete("Используйте History для передачи полной информации об изменениях")]
    public bool ElisFilled { get; set; } = false;

    /// <summary>
    /// История изменений поля
    /// При сохранении передаются только НОВЫЕ записи истории (добавленные с момента последней загрузки)
    /// </summary>
    public List<FieldHistoryEntry> History { get; set; } = new List<FieldHistoryEntry>();
}
```

---

### Этап 2: Фронтенд (Vue 3 + TypeScript) - Типы

#### 2.1. Создать типы

**Файл:** `TN_Doc/Client/document-editor/src/types/history.types.ts`

```typescript
/**
 * Источник данных поля
 */
export enum DataSource {
  /** Неизвестный источник (для старых данных) */
  Unknown = 'Unknown',
  /** Загружено из протокола ЕЛИС */
  ELIS = 'ELIS',
  /** Отредактировано вручную пользователем */
  Manual = 'Manual',
  /** Изменено системой ИВК (округление) */
  IVK = 'IVK'
}

/**
 * Запись истории изменения поля
 */
export interface FieldHistoryEntry {
  /** Источник данных */
  source: DataSource;
  /** Дата и время изменения (ISO 8601) */
  modifiedAt: string;
  /** Автор изменения ("Пользователь" для ручных правок) */
  modifiedBy: string;
  /** Значение после изменения */
  value: string;
  /** Предыдущее значение */
  previousValue?: string;
  /** Комментарий */
  comment?: string;
}

/**
 * Конфигурация отображения источника
 */
export interface SourceDisplayConfig {
  /** Иконка PrimeVue (например, 'pi-user-edit') */
  icon: string;
  /** Цвет иконки */
  color: string;
  /** Текст для отображения вместо иконки (опционально) */
  text?: string;
  /** Описание для tooltip */
  description: string;
}

/**
 * Маппинг источников на конфигурацию отображения
 */
export const SOURCE_DISPLAY_CONFIG: Record<DataSource, SourceDisplayConfig> = {
  [DataSource.Unknown]: {
    icon: 'pi-question-circle',
    color: '#9E9E9E',
    description: 'Неизвестный источник'
  },
  [DataSource.ELIS]: {
    icon: 'pi-database',
    color: '#4CAF50',
    text: 'ЕЛИС',
    description: 'Загружено из протокола ЕЛИС'
  },
  [DataSource.Manual]: {
    icon: 'pi-user-edit',
    color: '#2196F3',
    description: 'Отредактировано вручную'
  },
  [DataSource.IVK]: {
    icon: 'pi-cog',
    color: '#FF9800',
    text: 'ИВК',
    description: 'Округлено системой ИВК'
  }
};

/**
 * Максимальное количество записей истории
 */
export const MAX_HISTORY_ENTRIES = 10;
```

---

### Этап 3: Фронтенд - Композабл для работы с историей

#### 3.1. Создать useFieldHistory

**Файл:** `TN_Doc/Client/document-editor/src/composables/useFieldHistory.ts`

```typescript
import { computed } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import { DataSource, type FieldHistoryEntry, MAX_HISTORY_ENTRIES } from '@/types/history.types';
import { logger } from '@tn-doc/shared';

/**
 * Композабл для работы с историей изменений полей
 */
export function useFieldHistory() {
  const store = useDocumentStore();

  /**
   * Константа автора вручную внесённых изменений
   * (ФИО недоступно на момент редактирования)
   */
  const MANUAL_AUTHOR = 'Пользователь';

  /**
   * Создать запись истории
   */
  const createHistoryEntry = (
    source: DataSource,
    value: string,
    previousValue?: string,
    comment?: string
  ): FieldHistoryEntry => {
    const modifiedBy = source === DataSource.ELIS
      ? 'ELIS'
      : source === DataSource.IVK
      ? 'IVK'
      : MANUAL_AUTHOR;

    return {
      source,
      modifiedAt: new Date().toISOString(),
      modifiedBy,
      value,
      previousValue,
      comment
    };
  };

  /**
   * Добавить запись в историю поля
   */
  const addHistoryEntry = (fieldKey: string, entry: FieldHistoryEntry) => {
    if (!store.formHistory[fieldKey]) {
      store.formHistory[fieldKey] = [];
    }

    store.formHistory[fieldKey].push(entry);

    // Ограничиваем размер истории
    if (store.formHistory[fieldKey].length > MAX_HISTORY_ENTRIES) {
      store.formHistory[fieldKey].shift(); // Удаляем самую старую запись
    }

    logger.debug('[useFieldHistory] Добавлена запись в историю', {
      fieldKey,
      source: entry.source,
      modifiedBy: entry.modifiedBy
    });
  };

  /**
   * Отследить ручное изменение поля
   */
  const trackManualChange = (fieldKey: string, newValue: any, previousValue?: any) => {
    const entry = createHistoryEntry(
      DataSource.Manual,
      String(newValue),
      previousValue !== undefined ? String(previousValue) : undefined,
      'Отредактировано вручную'
    );

    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Отследить загрузку из ELIS
   */
  const trackElisLoad = (fieldKey: string, value: any, protocolNumber?: string) => {
    const comment = protocolNumber
      ? `Загружено из протокола ${protocolNumber}`
      : 'Загружено из протокола ЕЛИС';

    const entry = createHistoryEntry(
      DataSource.ELIS,
      String(value),
      undefined,
      comment
    );

    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Отследить округление ИВК
   */
  const trackIVKRounding = (
    fieldKey: string,
    originalValue: any,
    roundedValue: any,
    roundDigits: number
  ) => {
    const comment = `Округлено: ${originalValue} → ${roundedValue} (${roundDigits} знаков)`;

    const entry = createHistoryEntry(
      DataSource.IVK,
      String(roundedValue),
      String(originalValue),
      comment
    );

    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Получить историю поля
   */
  const getFieldHistory = (fieldKey: string): FieldHistoryEntry[] => {
    return store.formHistory[fieldKey] || [];
  };

  /**
   * Получить последний источник изменений
   */
  const getLastSource = (fieldKey: string): DataSource => {
    const history = getFieldHistory(fieldKey);
    if (history.length === 0) {
      return DataSource.Unknown;
    }
    return history[history.length - 1].source;
  };

  /**
   * Получить последнего автора изменений
   */
  const getLastModifiedBy = (fieldKey: string): string | undefined => {
    const history = getFieldHistory(fieldKey);
    if (history.length === 0) {
      return undefined;
    }
    return history[history.length - 1].modifiedBy;
  };

  /**
   * Очистить историю поля
   */
  const clearFieldHistory = (fieldKey: string) => {
    delete store.formHistory[fieldKey];
    logger.debug('[useFieldHistory] История поля очищена', { fieldKey });
  };

  return {
    createHistoryEntry,
    addHistoryEntry,
    trackManualChange,
    trackElisLoad,
    trackIVKRounding,
    getFieldHistory,
    getLastSource,
    getLastModifiedBy,
    clearFieldHistory
  };
}
```
> Пока не внедрена передача ФИО, фронтенд тоже записывает автора ручных правок как `"Пользователь"` (константа `MANUAL_AUTHOR`), чтобы совпадать с бэкендом.

---

### Этап 4: Фронтенд - Компоненты

#### 4.1. Создать FieldHistoryIndicator

**Файл:** `TN_Doc/Client/document-editor/src/components/history/FieldHistoryIndicator.vue`

```vue
<template>
  <div
    v-if="source !== DataSource.Unknown"
    class="field-history-indicator"
    @click="$emit('click')"
  >
    <!-- Текстовая метка (для ELIS и ИВК) -->
    <span
      v-if="displayConfig.text"
      class="indicator-text"
      :style="{ color: displayConfig.color }"
    >
      {{ displayConfig.text }}
    </span>

    <!-- Иконка (для Manual и Unknown) -->
    <i
      v-else
      :class="['pi', displayConfig.icon, 'indicator-icon']"
      :style="{ color: displayConfig.color }"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { DataSource, SOURCE_DISPLAY_CONFIG } from '@/types/history.types';

const props = defineProps<{
  /** Источник данных */
  source: DataSource;
}>();

defineEmits<{
  (e: 'click'): void;
}>();

const displayConfig = computed(() => {
  return SOURCE_DISPLAY_CONFIG[props.source];
});
</script>

<style scoped>
.field-history-indicator {
  position: absolute;
  top: 4px;
  right: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  z-index: 10;
  padding: 2px 4px;
  border-radius: 3px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.field-history-indicator:hover {
  transform: scale(1.05);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
}

.indicator-text {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.5px;
  line-height: 1;
}

.indicator-icon {
  font-size: 14px;
  line-height: 1;
}
</style>
```

#### 4.2. Создать FieldHistoryPopup

**Файл:** `TN_Doc/Client/document-editor/src/components/history/FieldHistoryPopup.vue`

```vue
<template>
  <OverlayPanel
    ref="overlayPanel"
    :dismissable="true"
    class="field-history-popup"
  >
    <div class="history-header">
      <h3 class="history-title">История изменений</h3>
      <p class="history-field-name">{{ fieldLabel }}</p>
    </div>

    <div v-if="history.length === 0" class="history-empty">
      <i class="pi pi-info-circle" />
      <p>История изменений отсутствует</p>
    </div>

    <div v-else class="history-list">
      <div
        v-for="(entry, index) in reversedHistory"
        :key="index"
        class="history-entry"
        :class="`source-${entry.source.toLowerCase()}`"
      >
        <!-- Иконка источника -->
        <div class="entry-icon">
          <span
            v-if="SOURCE_DISPLAY_CONFIG[entry.source].text"
            class="entry-icon-text"
            :style="{ color: SOURCE_DISPLAY_CONFIG[entry.source].color }"
          >
            {{ SOURCE_DISPLAY_CONFIG[entry.source].text }}
          </span>
          <i
            v-else
            :class="['pi', SOURCE_DISPLAY_CONFIG[entry.source].icon]"
            :style="{ color: SOURCE_DISPLAY_CONFIG[entry.source].color }"
          />
        </div>

        <!-- Информация -->
        <div class="entry-content">
          <div class="entry-description">
            {{ SOURCE_DISPLAY_CONFIG[entry.source].description }}
          </div>

          <div class="entry-meta">
            <span class="entry-date">{{ formatDate(entry.modifiedAt) }}</span>
            <span class="entry-separator">•</span>
            <span class="entry-author">{{ entry.modifiedBy }}</span>
          </div>

          <div v-if="entry.previousValue" class="entry-change">
            <span class="change-old">{{ entry.previousValue }}</span>
            <i class="pi pi-arrow-right change-arrow" />
            <span class="change-new">{{ entry.value }}</span>
          </div>
          <div v-else class="entry-value">
            Значение: <strong>{{ entry.value }}</strong>
          </div>

          <div v-if="entry.comment" class="entry-comment">
            {{ entry.comment }}
          </div>
        </div>
      </div>
    </div>
  </OverlayPanel>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import OverlayPanel from 'primevue/overlaypanel';
import type { FieldHistoryEntry } from '@/types/history.types';
import { SOURCE_DISPLAY_CONFIG } from '@/types/history.types';

const props = defineProps<{
  /** История изменений поля */
  history: FieldHistoryEntry[];
  /** Название поля */
  fieldLabel: string;
}>();

const overlayPanel = ref<InstanceType<typeof OverlayPanel>>();

/**
 * История в обратном порядке (последнее изменение сверху)
 */
const reversedHistory = computed(() => {
  return [...props.history].reverse();
});

/**
 * Форматирование даты
 */
const formatDate = (isoDate: string): string => {
  const date = new Date(isoDate);
  return date.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

/**
 * Показать popup
 */
const show = (event: Event) => {
  overlayPanel.value?.show(event);
};

/**
 * Скрыть popup
 */
const hide = () => {
  overlayPanel.value?.hide();
};

defineExpose({ show, hide });
</script>

<style scoped>
.field-history-popup {
  max-width: 400px;
  max-height: 450px;
  overflow: auto;
}

.history-header {
  padding-bottom: 12px;
  border-bottom: 1px solid var(--md-border-light);
  margin-bottom: 12px;
}

.history-title {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--md-text);
}

.history-field-name {
  margin: 4px 0 0 0;
  font-size: 13px;
  color: var(--md-text-secondary);
}

.history-empty {
  text-align: center;
  padding: 24px;
  color: var(--md-text-muted);
}

.history-empty .pi {
  font-size: 32px;
  margin-bottom: 8px;
  opacity: 0.5;
}

.history-empty p {
  margin: 0;
  font-size: 14px;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-entry {
  display: flex;
  gap: 12px;
  padding: 12px;
  border-radius: var(--md-radius);
  background: var(--md-surface);
  border: 1px solid var(--md-border-light);
}

.entry-icon {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.05);
}

.entry-icon-text {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.entry-icon .pi {
  font-size: 16px;
}

.entry-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.entry-description {
  font-size: 14px;
  font-weight: 500;
  color: var(--md-text);
}

.entry-meta {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--md-text-secondary);
}

.entry-separator {
  opacity: 0.5;
}

.entry-change {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 8px;
  background: rgba(0, 0, 0, 0.03);
  border-radius: 4px;
  font-size: 13px;
}

.change-old {
  color: var(--md-error);
  text-decoration: line-through;
}

.change-arrow {
  font-size: 12px;
  color: var(--md-text-muted);
}

.change-new {
  color: var(--green-600);
  font-weight: 500;
}

.entry-value {
  font-size: 13px;
  color: var(--md-text-secondary);
}

.entry-value strong {
  color: var(--md-text);
}

.entry-comment {
  font-size: 12px;
  font-style: italic;
  color: var(--md-text-muted);
  padding: 4px 8px;
  background: rgba(0, 0, 0, 0.02);
  border-radius: 4px;
  border-left: 2px solid var(--md-border);
}

/* Цветовая индикация для разных источников */
.history-entry.source-elis {
  border-left: 3px solid #4CAF50;
}

.history-entry.source-manual {
  border-left: 3px solid #2196F3;
}

.history-entry.source-ivk {
  border-left: 3px solid #FF9800;
}

.history-entry.source-unknown {
  border-left: 3px solid #9E9E9E;
}
</style>
```

#### 4.3. Создать FormFieldWithHistory

**Файл:** `TN_Doc/Client/document-editor/src/components/FormFieldWithHistory.vue`

```vue
<template>
  <div class="form-field-with-history">
    <FormField
      :field="field"
      :modelValue="modelValue"
      :hideLabel="hideLabel"
      :invalidChars="invalidChars"
      :highlightColor="highlightColor"
      @update:modelValue="handleChange"
    />

    <!-- Индикатор истории (поверх поля) -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown"
      :source="lastSource"
      @click="showHistory"
    />

    <!-- Popup с историей -->
    <FieldHistoryPopup
      ref="historyPopup"
      :history="fieldHistory"
      :fieldLabel="field.label"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import FormField from '@/components/FormField.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { DataSource } from '@/types/history.types';
import type { FormField as FormFieldType } from '@/types/document.types';

const props = defineProps<{
  field: FormFieldType;
  modelValue: any;
  hideLabel?: boolean;
  invalidChars?: string[];
  highlightColor?: string;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
}>();

const {
  getFieldHistory,
  getLastSource,
  trackManualChange
} = useFieldHistory();

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();

/**
 * История поля
 */
const fieldHistory = computed(() => {
  return getFieldHistory(props.field.key);
});

/**
 * Последний источник изменений
 */
const lastSource = computed(() => {
  return getLastSource(props.field.key);
});

/**
 * Обработка изменения значения
 */
const handleChange = (newValue: any) => {
  // Отслеживаем ручное изменение
  trackManualChange(props.field.key, newValue, props.modelValue);

  // Передаем изменение дальше
  emit('update:modelValue', newValue);
};

/**
 * Показать историю
 */
const showHistory = (event: Event) => {
  historyPopup.value?.show(event);
};
</script>

<style scoped>
.form-field-with-history {
  position: relative;
}
</style>
```

---

### Этап 5: Интеграция в DocumentPassportEditor

**Файл:** `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue`

```vue
<template>
  <!-- ... -->

  <!-- Таблица AdditionalInfo с историей -->
  <div class="editor-container additional-info-section">
    <div class="editor-table-wrapper">
      <table class="editor-table">
        <tbody>
          <tr v-for="field in store.fields" :key="field.key">
            <td class="editor-label-cell">
              <div class="label-wrapper">
                <span class="label-text">{{ field.label }}</span>
                <span v-if="field.required" class="required-mark">*</span>
              </div>
            </td>
            <td class="editor-input-cell">
              <!-- НОВЫЙ компонент с историей -->
              <FormFieldWithHistory
                :field="field"
                :modelValue="store.formData[field.key]"
                :hide-label="true"
                :invalidChars="store.config?.invalidChars || []"
                :highlightColor="getFieldHighlightColor(field.key)"
                @update:modelValue="(value) => handleFieldUpdate(field.key, value)"
              />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>

  <!-- ... -->
</template>

<script setup lang="ts">
// ... существующие импорты ...
import FormFieldWithHistory from '@/components/FormFieldWithHistory.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';

// ... существующий код ...

const { getLastSource } = useFieldHistory();

/**
 * Получить цвет подсветки для поля (на основе ELIS заполнения)
 */
const getFieldHighlightColor = (fieldKey: string): string | undefined => {
  const elisFilledFlag = store.formData[`${fieldKey}__elisFilled`];
  if (elisFilledFlag === true) {
    return 'var(--md-elis-highlight)';
  }
  return undefined;
};

/**
 * Обработка обновления поля с отслеживанием истории
 */
const handleFieldUpdate = (fieldKey: string, value: any) => {
  store.updateField(fieldKey, value);
};

// ... остальной код ...
</script>
```

---

### Этап 6: Обновление documentStore

**Файл:** `TN_Doc/Client/document-editor/src/stores/documentStore.ts`

```typescript
import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { FieldHistoryEntry } from '@/types/history.types';

export const useDocumentStore = defineStore('document', () => {
  // ... существующие reactive переменные ...

  /**
   * История изменений полей
   * Ключ - название поля, значение - массив записей истории
   */
  const formHistory = ref<Record<string, FieldHistoryEntry[]>>({});

  // ... существующие функции ...

  /**
   * Загрузить конфигурацию документа (расширенная версия)
   */
  const loadConfig = async (deviceId: number, docType: string, id: number) => {
    try {
      isLoading.value = true;
      error.value = null;

      // ... существующий код загрузки ...

      // Загрузить историю из конфигурации (если есть)
      if (response.data.fieldHistory) {
        formHistory.value = response.data.fieldHistory;
      }

      // Загрузить историю параметров качества
      if (response.data.qualityParametersSchema) {
        response.data.qualityParametersSchema.forEach((param: any) => {
          if (param.history && param.history.length > 0) {
            // Для параметров качества используем составные ключи
            formHistory.value[`value.${param.key}`] = param.history;
            formHistory.value[`result.${param.key}`] = param.history;
            formHistory.value[`method.${param.key}`] = param.history;
          }
        });
      }

      logger.info('[documentStore] История изменений загружена', {
        fieldsWithHistory: Object.keys(formHistory.value).length
      });
    } catch (err: any) {
      // ... обработка ошибок ...
    } finally {
      isLoading.value = false;
    }
  };

  /**
   * Сохранить документ (расширенная версия)
   */
  const saveDocument = async (deviceId: number, docType: string, id: number) => {
    try {
      isSaving.value = true;

      // Подготовить данные для сохранения (включая историю)
      const payload = {
        ...formData.value,
        __history: formHistory.value // Передаем историю
      };

      // ... существующий код сохранения ...

      logger.info('[documentStore] Документ сохранен с историей', {
        fieldsWithHistory: Object.keys(formHistory.value).length
      });

      return true;
    } catch (error: any) {
      // ... обработка ошибок ...
    } finally {
      isSaving.value = false;
    }
  };

  return {
    // ... существующие exports ...
    formHistory,
    loadConfig,
    saveDocument
  };
});
```

---

## Критерии приемки

### Функциональные требования

- [ ] **История изменений:** Для каждого поля сохраняется до 10 последних изменений
- [ ] **Визуальная индикация:** В правом верхнем углу каждого поля отображается иконка источника
- [ ] **Popup с историей:** При клике на иконку открывается popup с полной историей изменений
- [ ] **Отслеживание источников:**
  - [ ] Ручное редактирование пользователем → Manual
  - [ ] Загрузка из ELIS → ELIS
  - [ ] Округление ИВК → IVK
- [ ] **Сохранение в БД:** История сохраняется в DataARM JSON
- [ ] **Загрузка из БД:** История восстанавливается при открытии документа
- [ ] **Обратная совместимость:** Старые документы работают корректно (ElisFilled → FieldHistory миграция)

### Технические требования

- [ ] **TypeScript:** Все компоненты и типы корректно типизированы
- [ ] **Композабл:** useFieldHistory переиспользуется во всех компонентах
- [ ] **Производительность:** История не замедляет загрузку/сохранение документа
- [ ] **Размер DataARM:** История занимает не более 50% от размера DataARM
- [ ] **Логирование:** Все изменения логируются на уровне INFO

### UI/UX требования

- [ ] **Иконки:** Размер 14-16px, позиция в правом верхнем углу
- [ ] **Цвета:** Соответствуют SOURCE_DISPLAY_CONFIG
- [ ] **Popup:** Анимация fade in/out 200ms
- [ ] **Адаптивность:** Popup корректно отображается на разных разрешениях
- [ ] **Доступность:** Все элементы доступны с клавиатуры

### Тестовые сценарии

- [ ] **Сценарий 1:** Создание нового паспорта → Ручное заполнение → Сохранение → История содержит 1 запись Manual
- [ ] **Сценарий 2:** Загрузка из ELIS → Сохранение → История содержит записи ELIS
- [ ] **Сценарий 3:** Загрузка из ELIS → Ручное редактирование → Сохранение → История содержит ELIS + Manual
- [ ] **Сценарий 4:** Ручное редактирование → Округление ИВК → История содержит Manual + IVK
- [ ] **Сценарий 5:** Открытие старого паспорта (без истории) → Миграция из ElisFilled
- [ ] **Сценарий 6:** 15 изменений поля → История содержит только последние 10
- [ ] **Сценарий 7:** Popup с историей → Клик вне области → Popup закрывается

---

## Порядок выполнения

### Этап 1: Бэкенд (5-7 дней)
1. Создать структуры данных (DataSource, FieldHistoryEntry)
2. Расширить DataARM (FieldHistory)
3. Обновить EditData
4. Обновить логику сохранения DocPassport.DocUpdate()
5. Обновить логику загрузки GetEditConfig()
6. Написать unit-тесты

### Этап 2: Фронтенд - Типы и композабл (2-3 дня)
1. Создать типы (history.types.ts)
2. Создать useFieldHistory композабл
3. Обновить documentStore
4. Написать unit-тесты для композабла

### Этап 3: Фронтенд - Компоненты (4-5 дней)
1. Создать FieldHistoryIndicator
2. Создать FieldHistoryPopup
3. Создать FormFieldWithHistory
4. Создать аналогичные компоненты для таблицы параметров качества
5. Написать unit-тесты для компонентов

### Этап 4: Интеграция (3-4 дня)
1. Обновить DocumentPassportEditor.vue
2. Интегрировать отслеживание изменений
3. Интегрировать загрузку/сохранение истории
4. Тестирование E2E

### Этап 5: Полировка и документация (2-3 дня)
1. Оптимизация производительности
2. Доработка UI/UX
3. Написание документации
4. Обновление CHANGELOG

**Общая оценка:** 16-22 дня

---

## Дополнительные замечания

### Миграция старых данных

При загрузке документа с `ElisFilled = true`, но без записей в `FieldHistory`:

```csharp
// В методе GetEditConfig
var controlId = $"value.{labInfo.ParameterKey}";
if (labInfo.ElisFilled && dataArm.FieldHistoryMap.ContainsKey(controlId) == false)
{
    dataArm.AddFieldHistoryEntry(controlId, new FieldHistoryEntry
    {
        Source = DataSource.ELIS,
        ModifiedAt = DateTime.MinValue, // Неизвестная дата
        ModifiedBy = "ELIS",
        Value = labInfo.Value,
        Comment = "Миграция из старого формата (ElisFilled)"
    });
}
```

### Оптимизация размера истории (опционально)

⚠️ **Статус:** Отложено до этапа 8 (после замеров реального размера DataARM)

Возможные оптимизации для минимизации размера DataARM JSON:

1. **Не хранить timestamp в миллисекундах** - использовать формат `2025-01-14T10:30:00` (без миллисекунд и часового пояса)
2. **Сокращать имена полей** в JSON при сериализации (только если размер критичен):
   - `Source` → `s`
   - `ModifiedAt` → `t`
   - `ModifiedBy` → `a`
   - `Value` → `v`
3. **Удалять null/undefined поля** перед сериализацией

### Расширение функционала (будущие версии)

- **Откат к предыдущему значению** - кнопка в popup для восстановления старого значения
- **Экспорт истории** - выгрузка истории всех полей в CSV/Excel
- **Фильтрация истории** - поиск по автору, дате, источнику
- **Сравнение версий** - визуальное сравнение двух версий документа

---

**Дата последнего обновления:** 2025-01-14
**Версия промпта:** 1.1
**Изменения v1.1:**
- Удалено избыточное поле `NewValue` из `FieldHistoryEntry`
- Компактная сериализация JSON отложена до этапа 8
- Popup открывается по hover (уточнение триггера)
