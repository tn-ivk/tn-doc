# План: Визуальный конфигуратор CfgEditPassport

**Статус:** ✅ Выполнено (Декабрь 2025)

## Цель

Заменить ручное редактирование JSON на визуальный интерфейс для двух секций конфигурации паспорта качества:
- **Parameters** — параметры качества нефти
- **AdditionalInfo** — дополнительные текстовые поля паспорта

**Вне скоупа:** Methods (методы испытаний) — остаются только в JSON.

---

## Текущее состояние

Вкладка "Документы" в конфигураторе (`TN_Doc/Client/configurator/`) отображает JSON в Textarea:

```
DocumentsTab.vue
├── DocumentTree.vue        # Дерево документов/шаблонов
└── DocumentConfigEditor.vue # JSON Textarea редактор
```

При выборе узла с `CfgEditPassport*.json` открывается сырой JSON.

---

## Целевая архитектура

```
DocumentConfigEditor.vue (диспетчер)
│
├─ hasVisualEditor(configPath)?
│  │
│  ├─ YES → PassportConfigEditor.vue (визуальный)
│  │         ├── ParametersSection.vue
│  │         └── AdditionalFieldsSection.vue
│  │
│  └─ NO  → JsonConfigEditor.vue (текущий Textarea)
│
└─ SelectButton: [Визуальный | JSON] — переключатель режима
```

---

## Этап 1: Подготовка инфраструктуры

### 1.1 Типы TypeScript

**Файл:** `src/types/passport-config.types.ts`

```typescript
// Параметр качества
export interface PassportParameter {
  Id: number;
  Key: string;
  Name: string;
  Use: boolean;
  Edit: boolean;
  IsBallast: boolean;
  SlaveKey?: string;
  RequiredFill?: boolean;
  RoundValue?: number;
}

// Дополнительное поле
export interface PassportAdditionalField {
  Id: number;
  Use: boolean;
  Key: string;
  Type: 'text' | 'list' | 'datetime-local' | 'number';
  Name: string;
}

// Метод (readonly, не редактируется визуально)
export interface PassportMethod {
  Id: number;
  Use: boolean;
  IdParameter: number;
  Name: string;
  LimitValueActivate: boolean;
  LimitValue: number;
  LimitValueString: string;
  IsDefault?: boolean;
}

// Полная конфигурация
export interface PassportEditConfig {
  Parameters: PassportParameter[];
  AdditionalInfo: PassportAdditionalField[];
  Methods: PassportMethod[]; // Только для сериализации, не редактируется
}

// Типы полей для dropdown
export const FIELD_TYPES = [
  { label: 'Текст', value: 'text' },
  { label: 'Список (справочник)', value: 'list' },
  { label: 'Дата и время', value: 'datetime-local' },
  { label: 'Число', value: 'number' }
] as const;
```

### 1.2 Composable для определения редактора

**Файл:** `src/composables/useVisualEditor.ts`

```typescript
import { type Component } from 'vue';

export interface VisualEditorInfo {
  type: 'visual' | 'json';
  component: Component | null;
  label: string;
}

// Реестр визуальных редакторов (расширяемый)
const VISUAL_EDITOR_PATTERNS: Array<{
  pattern: RegExp;
  component: () => Promise<Component>;
  label: string;
}> = [
  {
    pattern: /CfgEditPassport.*\.json$/i,
    component: () => import('../components/visual-editors/PassportConfigEditor.vue'),
    label: 'Паспорт качества'
  }
  // Здесь можно добавить другие типы в будущем:
  // { pattern: /CfgEditKMH.*\.json$/i, component: () => import(...), label: 'КМХ' }
];

export function useVisualEditor() {

  function getEditorInfo(configPath: string): VisualEditorInfo {
    const filename = configPath.split(/[/\\]/).pop() || '';

    for (const entry of VISUAL_EDITOR_PATTERNS) {
      if (entry.pattern.test(filename)) {
        return {
          type: 'visual',
          component: null,
          label: entry.label
        };
      }
    }

    return { type: 'json', component: null, label: 'JSON' };
  }

  async function loadVisualEditor(configPath: string): Promise<Component | null> {
    const filename = configPath.split(/[/\\]/).pop() || '';

    for (const entry of VISUAL_EDITOR_PATTERNS) {
      if (entry.pattern.test(filename)) {
        const module = await entry.component();
        return module.default;
      }
    }

    return null;
  }

  function hasVisualEditor(configPath: string): boolean {
    return getEditorInfo(configPath).type === 'visual';
  }

  return { getEditorInfo, loadVisualEditor, hasVisualEditor };
}
```

---

## Этап 2: Рефакторинг DocumentConfigEditor

### 2.1 Выделить JSON-редактор

**Файл:** `src/components/JsonConfigEditor.vue`

Перенести текущую логику Textarea из `DocumentConfigEditor.vue`:
- Props: `content: string`
- Emit: `update:content`, `validation-error`
- Валидация JSON в реальном времени

### 2.2 Модифицировать DocumentConfigEditor

**Изменения в** `src/components/DocumentConfigEditor.vue`:

```vue
<template>
  <div class="config-editor">
    <!-- Заголовок с переключателем режима -->
    <div class="editor-header">
      <div class="header-info">
        <i :class="selectedNode.icon" class="header-icon"></i>
        <div class="header-text">
          <h3 class="header-title">{{ selectedNode.label }}</h3>
          <span class="header-subtitle">{{ currentConfigPath }}</span>
        </div>
      </div>

      <!-- Переключатель режима (только если есть визуальный редактор) -->
      <SelectButton v-if="hasVisualEditor"
                    v-model="editorMode"
                    :options="editorModes"
                    optionLabel="label"
                    optionValue="value"
                    class="mode-toggle" />
    </div>

    <!-- Контент редактора -->
    <div class="editor-content">
      <!-- Визуальный редактор -->
      <Suspense v-if="editorMode === 'visual' && hasVisualEditor">
        <template #default>
          <component :is="visualEditor"
                     :config="parsedConfig"
                     :config-path="currentConfigPath"
                     @update:config="handleVisualUpdate" />
        </template>
        <template #fallback>
          <div class="loading-state">
            <i class="pi pi-spinner pi-spin"></i>
            <span>Загрузка редактора...</span>
          </div>
        </template>
      </Suspense>

      <!-- JSON редактор -->
      <JsonConfigEditor v-else
                        v-model:content="editedContent"
                        @validation-error="validationError = $event" />
    </div>
  </div>
</template>
```

---

## Этап 3: Визуальный редактор паспорта

### 3.1 Структура компонентов

```
src/components/visual-editors/
├── PassportConfigEditor.vue      # Основной контейнер с TabView
├── ParametersSection.vue         # Таблица параметров качества
└── AdditionalFieldsSection.vue   # Таблица дополнительных полей
```

### 3.2 PassportConfigEditor.vue

**Файл:** `src/components/visual-editors/PassportConfigEditor.vue`

```vue
<template>
  <div class="passport-config-editor">
    <TabView>
      <!-- Вкладка параметров -->
      <TabPanel>
        <template #header>
          <i class="pi pi-list mr-2"></i>
          <span>Параметры ({{ config.Parameters.length }})</span>
        </template>
        <ParametersSection
          :parameters="config.Parameters"
          @update:parameters="updateParameters" />
      </TabPanel>

      <!-- Вкладка дополнительных полей -->
      <TabPanel>
        <template #header>
          <i class="pi pi-info-circle mr-2"></i>
          <span>Дополнительные поля ({{ config.AdditionalInfo.length }})</span>
        </template>
        <AdditionalFieldsSection
          :fields="config.AdditionalInfo"
          @update:fields="updateAdditionalFields" />
      </TabPanel>
    </TabView>
  </div>
</template>
```

### 3.3 ParametersSection.vue

**Колонки таблицы:**

| Колонка | Элемент | Поле | Описание |
|---------|---------|------|----------|
| Ключ | Text (readonly) | `Key` | Идентификатор параметра |
| Название | Text (readonly) | `Name` | Отображаемое имя |
| Вкл | Checkbox | `Use` | Включён ли параметр |
| Редакт. | Checkbox | `Edit` | Можно ли редактировать в Document Editor |
| Балласт | Checkbox + Tag | `IsBallast` | Result = Measurement автоматически |
| Slave-связь | Dropdown / Tag | `SlaveKey` | Выбор slave-параметра |
| Округл. | InputNumber | `RoundValue` | Знаков после запятой |
| Обяз. | Checkbox | `RequiredFill` | Обязательно для заполнения |

**Логика Master-Slave:**
- Параметры, на которые ссылается `SlaveKey` другого параметра, показывают Tag "← MasterKey"
- Dropdown для `SlaveKey` содержит только параметры, которые ещё не являются slave
- Параметр не может быть одновременно master и slave

```vue
<script setup lang="ts">
// Карта: slave key → master key
const slaveToMaster = computed(() => {
  const map = new Map<string, string>();
  for (const p of props.parameters) {
    if (p.SlaveKey) {
      map.set(p.SlaveKey, p.Key);
    }
  }
  return map;
});

function isSlaveOf(key: string): boolean {
  return slaveToMaster.value.has(key);
}

function getMasterKey(slaveKey: string): string {
  return slaveToMaster.value.get(slaveKey) || '';
}

function getAvailableSlaves(currentKey: string) {
  return props.parameters
    .filter(p =>
      p.Key !== currentKey &&  // Не сам себя
      !isSlaveOf(p.Key) &&     // Не уже slave другого
      !p.SlaveKey              // Не является мастером
    )
    .map(p => ({ label: p.Key, value: p.Key }));
}
</script>
```

### 3.4 AdditionalFieldsSection.vue

**Колонки таблицы:**

| Колонка | Элемент | Поле | Описание |
|---------|---------|------|----------|
| Ключ | Text (readonly) | `Key` | Идентификатор поля |
| Название | InputText | `Name` | Отображаемое имя (редактируемое) |
| Тип | Dropdown | `Type` | text, list, datetime-local, number |
| Вкл | Checkbox | `Use` | Включено ли поле |

---

## Этап 4: Интеграция и сериализация

### 4.1 Парсинг и сериализация

```typescript
// В DocumentConfigEditor.vue

// Парсинг JSON в типизированный объект
const parsedConfig = computed<PassportEditConfig | null>(() => {
  try {
    return JSON.parse(editedContent.value);
  } catch {
    return null;
  }
});

// Обработка изменений из визуального редактора
function handleVisualUpdate(newConfig: PassportEditConfig) {
  // Сериализуем обратно в JSON с форматированием
  editedContent.value = JSON.stringify(newConfig, null, 2);

  // Помечаем как изменённый
  configStore.markDocumentConfigDirty(currentConfigPath.value, editedContent.value);
}
```

**Примечание:** Комментарии в JSON (`/* ... */`) игнорируются и будут удалены при сохранении через визуальный редактор.

---

## Этап 5: Стилизация

### CSS для таблиц

```css
/* Компактные таблицы */
.parameters-table :deep(.p-datatable-tbody > tr > td) {
  padding: 0.5rem;
}

/* Код ключей */
.param-key, .field-key {
  font-family: 'Consolas', monospace;
  font-size: 0.85rem;
  background: var(--md-surface-variant);
  padding: 0.2rem 0.4rem;
  border-radius: 4px;
}

/* Ячейка балласта */
.ballast-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

/* Dropdown для slave */
.slave-dropdown {
  width: 100%;
}

/* Поле округления */
.round-input {
  width: 60px;
}

/* Поле названия */
.name-input {
  width: 100%;
}

/* Переключатель режима */
.mode-toggle {
  flex-shrink: 0;
}
```

---

## Сводка файлов

| Файл | Действие | Описание |
|------|----------|----------|
| `src/types/passport-config.types.ts` | Создать | Типы для CfgEditPassport |
| `src/composables/useVisualEditor.ts` | Создать | Логика выбора редактора |
| `src/components/JsonConfigEditor.vue` | Создать | Вынести текущий JSON-редактор |
| `src/components/DocumentConfigEditor.vue` | Изменить | Добавить диспетчер редакторов |
| `src/components/visual-editors/PassportConfigEditor.vue` | Создать | Контейнер с TabView |
| `src/components/visual-editors/ParametersSection.vue` | Создать | Таблица параметров |
| `src/components/visual-editors/AdditionalFieldsSection.vue` | Создать | Таблица доп. полей |

---

## Порядок реализации

| Шаг | Задача | Зависимости |
|-----|--------|-------------|
| 1 | Создать `passport-config.types.ts` | — |
| 2 | Создать `useVisualEditor.ts` | — |
| 3 | Выделить `JsonConfigEditor.vue` из `DocumentConfigEditor.vue` | — |
| 4 | Создать `ParametersSection.vue` | Шаг 1 |
| 5 | Создать `AdditionalFieldsSection.vue` | Шаг 1 |
| 6 | Создать `PassportConfigEditor.vue` | Шаги 4, 5 |
| 7 | Модифицировать `DocumentConfigEditor.vue` | Шаги 2, 3, 6 |
| 8 | Тестирование на нескольких CfgEditPassport*.json | Все |

---

## Критерии готовности

- [x] Переключатель "Визуальный / JSON" работает
- [x] Таблица параметров отображает все поля
- [x] Редактирование Checkbox/Dropdown/InputNumber сохраняется
- [x] Master-Slave связи корректно отображаются
- [x] Таблица дополнительных полей работает
- [x] Изменения сериализуются в валидный JSON
- [x] Сохранение через общую кнопку "Сохранить" работает
- [x] Fallback на JSON-редактор для неподдерживаемых типов

---

## Расширяемость

Для добавления визуального редактора другого типа документа:

1. Создать типы в `types/{type}-config.types.ts`
2. Создать компоненты в `visual-editors/{Type}ConfigEditor.vue`
3. Добавить паттерн в `VISUAL_EDITOR_PATTERNS` в `useVisualEditor.ts`

```typescript
// Пример добавления KMH
{
  pattern: /CfgEditKMH.*\.json$/i,
  component: () => import('../components/visual-editors/KMHConfigEditor.vue'),
  label: 'КМХ'
}
```
