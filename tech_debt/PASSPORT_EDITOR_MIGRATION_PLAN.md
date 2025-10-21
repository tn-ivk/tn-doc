# План реализации Vue-редактора для документов Passport (Quality Certificates)

**Обновлено:** 2025-10-21
**Версия:** 2.0 (с учетом изменения структуры таблицы Edit)
**Статус:** Frontend реализован (требует обновления под новую структуру), Backend не реализован

---

## 📋 Оглавление

1. [Текущее состояние](#текущее-состояние)
2. [Изменение структуры таблицы Edit](#изменение-структуры-таблицы-edit)
3. [Обновления Frontend](#обновления-frontend)
4. [Backend реализация](#backend-реализация)
5. [Timeline и оценка](#timeline-и-оценка)
6. [Приоритеты](#приоритеты)
7. [Следующие шаги](#следующие-шаги)

---

## Текущее состояние

### ✅ Frontend - Реализовано (требует обновления)

**Основной компонент редактора:**
- ✅ `DocumentPassportEditor.vue` - главный компонент редактора
- ✅ Таблица AdditionalInfo (16 полей дополнительной информации)
- ⚠️ `PassportQualityTable.vue` - **ТРЕБУЕТ ОБНОВЛЕНИЯ** (8 колонок → 6 колонок)

**TypeScript типы:**
- ✅ `passport.types.ts` с интерфейсами
- ⚠️ **ТРЕБУЕТ ОБНОВЛЕНИЯ**: `ParameterValues` - убрать отдельные `ivk`/`hal`, добавить единое поле `measurement`

**Композабл логика:**
- ✅ `usePassportEditor.ts` - основная бизнес-логика
- ⚠️ **ТРЕБУЕТ ОБНОВЛЕНИЯ**: адаптировать под новую структуру данных

**Компоненты таблицы качественных параметров:**
- ⚠️ `PassportParameterRow.vue` - **ТРЕБУЕТ ОБНОВЛЕНИЯ** (8 ячеек → 6 ячеек)
- ✅ `PassportMethodSelect.vue` - выбор метода испытаний
- ✅ `PassportHalInput.vue` - ввод значения ХАЛ (переименовать в `PassportMeasurementInput.vue`)
- ⚠️ `PassportPrintCell.vue` - **ТРЕБУЕТСЯ АДАПТАЦИЯ** под колонку "Результат"
- ✅ `PassportDocumentField.vue` - отображение ELIS документов

**Роутинг:**
- ✅ Маршрут `/edit/:deviceId/Passport/:id` добавлен в `router/index.ts`

**Расположение:**
- Все компоненты в `/TN_Doc/Client/document-editor/`
- Использует существующий document-editor монорепозиторий (не отдельное приложение)

### ❌ Backend - Не реализовано (0%)

**Требуется реализация:**
- ❌ Интерфейс `IDocumentEditor` в `DocPassport.cs`
- ❌ Метод `GetEditConfig(int id)` для возврата конфигурации
- ❌ Классы моделей (QualityParameter, ParameterValues, etc.)
- ❌ Метод `BuildQualityParameters()` с логикой "Измерение"
- ❌ Метод `UpdateQualityParameters()`
- ❌ Метод `SaveEditConfig(object config)`

---

## Изменение структуры таблицы Edit

### Старая структура (8 колонок):
| № | Наименование | Метод | Документы* | Измерение ИВК | Измерение ХАЛ | Результат-Значение | Результат-Текст |

### Новая структура (6 колонок):
| № | Наименование | Метод | Документы* | Измерение | Результат |

_* Колонка "Документы" показывается только при использовании ELIS_

### Ключевые изменения:

1. **Колонки "Измерение ИВК" и "Измерение ХАЛ" объединены в "Измерение"**
   - Логика заполнения должна быть реализована в `DocPassport.cs`
   - Приоритет: ELIS → HAL → IVK → пусто

2. **Колонка "Результат-Значение" удалена**
   - Значение теперь только одно - в колонке "Результат"

3. **Колонка "Результат-Текст" переименована в "Результат"**
   - Всегда показывается, независимо от использования ELIS
   - Может быть редактируемой при определенных условиях (limitValueActivate)

4. **Структура данных изменена:**
   ```typescript
   // БЫЛО:
   interface ParameterValues {
     ivk: string;
     hal: string;
     result: string;
     printValue: string;
   }

   // СТАЛО:
   interface ParameterValues {
     measurement: string;  // Объединенное значение
     result: string;       // Результат (ранее printValue)
   }
   ```

---

## Обновления Frontend

### 1. Обновить `passport.types.ts` ⚠️

**Файл:** `/TN_Doc/Client/document-editor/src/types/passport.types.ts`

```typescript
/**
 * Значения параметра
 */
export interface ParameterValues {
  /** Измерение (объединенное значение) */
  measurement: string;
  /** Результат (ранее printValue) */
  result: string;
}

/**
 * Флаги заполнения из ELIS для параметра
 */
export interface ParameterElisFlags {
  measurement: boolean; // Измерение заполнено из ELIS (вместо hal)
  method: boolean;      // Метод испытаний заполнен из ELIS
  result: boolean;      // Результат заполнен из ELIS (вместо printValue)
  document: boolean;    // Документ заполнен из ELIS
}

/**
 * Событие обновления measurement
 */
export interface MeasurementUpdateEvent {
  paramKey: string;
  value: string;
}

/**
 * Событие обновления result
 */
export interface ResultUpdateEvent {
  paramKey: string;
  value: string;
}
```

### 2. Обновить `PassportQualityTable.vue` ⚠️

**Файл:** `/TN_Doc/Client/document-editor/src/components/passport/PassportQualityTable.vue`

**Изменить заголовок таблицы:**

```vue
<template>
  <div class="quality-table-container">
    <table id="Edit" class="quality-table">
      <!-- Определение ширины колонок -->
      <colgroup>
        <col class="col-num">          <!-- № -->
        <col class="col-name">         <!-- Наименование -->
        <col class="col-method">       <!-- Метод -->
        <col class="col-documents" v-if="isElisUsed">  <!-- Документы (условная) -->
        <col class="col-measurement">  <!-- Измерение -->
        <col class="col-result">       <!-- Результат -->
      </colgroup>

      <!-- Заголовок таблицы (ОДНА строка вместо двух) -->
      <thead>
        <tr>
          <th>№</th>
          <th>Наименование показателя</th>
          <th>Метод испытаний</th>
          <th v-if="isElisUsed" class="th-documents">Документы</th>
          <th>Измерение</th>
          <th>Результат</th>
        </tr>
      </thead>

      <!-- Тело таблицы -->
      <tbody>
        <PassportParameterRow
          v-for="(param, index) in parameters"
          :key="param.key"
          :parameter="param"
          :index="index + 1"
          :isElisUsed="isElisUsed"
          @update:method="$emit('update:method', $event)"
          @update:measurement="$emit('update:measurement', $event)"
          @update:result="$emit('update:result', $event)"
        />
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportParameterRow from './PassportParameterRow.vue';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  parameters: PassportQualityParameter[];
  isElisUsed: boolean;
}

const props = defineProps<Props>();

defineEmits<{
  'update:method': [event: { paramKey: string; methodName: string }];
  'update:measurement': [event: { paramKey: string; value: string }];
  'update:result': [event: { paramKey: string; value: string }];
}>();
</script>
```

### 3. Обновить `PassportParameterRow.vue` ⚠️

**Файл:** `/TN_Doc/Client/document-editor/src/components/passport/PassportParameterRow.vue`

**Изменить структуру ячеек (6 ячеек вместо 8):**

```vue
<template>
  <tr class="parameter-row">
    <!-- №: Номер строки -->
    <td class="cell-number">{{ index }}</td>

    <!-- Наименование показателя -->
    <td class="cell-name">{{ parameter.name }}</td>

    <!-- Метод испытаний -->
    <td class="cell-method">
      <PassportMethodSelect
        :parameter="parameter"
        @update:method="handleMethodUpdate"
      />
    </td>

    <!-- Документы (только если ELIS используется) -->
    <td v-if="isElisUsed" class="cell-documents td-documents">
      <PassportDocumentField :parameter="parameter" />
    </td>

    <!-- Измерение (объединенная колонка) -->
    <td class="cell-measurement">
      <PassportMeasurementInput
        :parameter="parameter"
        @update:measurement="handleMeasurementUpdate"
      />
    </td>

    <!-- Результат (может быть редактируемым) -->
    <td
      class="cell-result"
      :class="{ 'manual-input--disabled': !isResultEditable }"
    >
      <PassportResultCell
        :parameter="parameter"
        :isEditable="isResultEditable"
        @update:result="handleResultUpdate"
      />
    </td>
  </tr>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportMethodSelect from './PassportMethodSelect.vue';
import PassportDocumentField from './PassportDocumentField.vue';
import PassportMeasurementInput from './PassportMeasurementInput.vue';
import PassportResultCell from './PassportResultCell.vue';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

interface Props {
  parameter: PassportQualityParameter;
  index: number;
  isElisUsed: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:method': [event: { paramKey: string; methodName: string }];
  'update:measurement': [event: { paramKey: string; value: string }];
  'update:result': [event: { paramKey: string; value: string }];
}>();

/**
 * Определить, редактируема ли ячейка результата
 */
const isResultEditable = computed(() => {
  const selectedMethod = props.parameter.method.options.find(
    (m: MethodOption) => m.name === props.parameter.method.selected
  );

  if (!selectedMethod || !selectedMethod.limitValueActivate) {
    return false;
  }

  const measurementValue = parseFloat(props.parameter.values.measurement.replace(',', '.'));
  if (isNaN(measurementValue)) {
    return false;
  }

  return selectedMethod.limitValue !== undefined && measurementValue < selectedMethod.limitValue;
});

function handleMethodUpdate(methodName: string) {
  emit('update:method', { paramKey: props.parameter.key, methodName });
}

function handleMeasurementUpdate(value: string) {
  emit('update:measurement', { paramKey: props.parameter.key, value });
}

function handleResultUpdate(value: string) {
  emit('update:result', { paramKey: props.parameter.key, value });
}
</script>
```

### 4. Переименовать `PassportHalInput.vue` → `PassportMeasurementInput.vue` ⚠️

**Создать:** `/TN_Doc/Client/document-editor/src/components/passport/PassportMeasurementInput.vue`

```vue
<template>
  <InputNumber
    :modelValue="numericValue"
    :disabled="!parameter.editable"
    :class="[
      validationClass,
      { 'elis-filled': parameter.elisFlags.measurement },
      { 'manual-input--disabled': !parameter.editable }
    ]"
    :minFractionDigits="0"
    :maxFractionDigits="parameter.roundValue || 10"
    mode="decimal"
    locale="ru-RU"
    class="measurement-input"
    @update:modelValue="handleValueChange"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import InputNumber from 'primevue/inputnumber';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  parameter: PassportQualityParameter;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:measurement': [value: string];
}>();

const numericValue = computed(() => {
  if (!props.parameter.values.measurement) return null;
  const value = parseFloat(props.parameter.values.measurement.replace(',', '.'));
  return isNaN(value) ? null : value;
});

const validationClass = computed(() => {
  if (props.parameter.requiredFill) {
    if (!props.parameter.values.measurement || props.parameter.values.measurement === '') {
      return 'incorrect-value';
    }
  }

  if (props.parameter.roundValue && props.parameter.values.measurement) {
    const value = props.parameter.values.measurement.replace(',', '.');
    const parts = value.split('.');
    if (parts.length > 1 && parts[1].length > props.parameter.roundValue) {
      return 'incorrect-value';
    }
  }

  return 'correct-value';
});

function handleValueChange(value: number | null) {
  const stringValue = value !== null ? value.toString().replace('.', ',') : '';
  emit('update:measurement', stringValue);
}
</script>

<style scoped>
.measurement-input {
  width: 100%;
  text-align: center;
  font-size: 15px;
}

.measurement-input:deep(input) {
  text-align: center;
  font-size: 15px;
}

/* Валидация */
.correct-value {
  border-color: var(--md-outline, #CFD8DC);
}

.correct-value:deep(input) {
  border-color: var(--md-outline, #CFD8DC);
}

.incorrect-value {
  border-color: var(--md-error, #dc3545);
  background-color: #f8d7da;
}

.incorrect-value:deep(input) {
  border-color: var(--md-error, #dc3545);
  background-color: #f8d7da;
}

/* ELIS подсветка */
.elis-filled {
  background-color: #8fd19e !important;
}

.elis-filled:deep(input) {
  background-color: #8fd19e !important;
}

/* Disabled стиль */
.manual-input--disabled {
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
}

.manual-input--disabled:deep(input) {
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
}
</style>
```

### 5. Переименовать `PassportPrintCell.vue` → `PassportResultCell.vue` ⚠️

**Создать:** `/TN_Doc/Client/document-editor/src/components/passport/PassportResultCell.vue`

```vue
<template>
  <!-- Редактируемая ячейка результата -->
  <InputText
    v-if="isEditable"
    :modelValue="parameter.values.result"
    :class="{ 'elis-filled': parameter.elisFlags.result }"
    type="text"
    class="result-cell-input"
    @update:modelValue="handleValueChange"
  />

  <!-- Нередактируемая ячейка результата (просто текст) -->
  <span
    v-else
    :class="{ 'elis-filled-text': parameter.elisFlags.result }"
    class="result-cell-readonly"
  >
    {{ displayValue }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import InputText from 'primevue/inputtext';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  parameter: PassportQualityParameter;
  isEditable: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:result': [value: string];
}>();

const displayValue = computed(() => {
  if (!props.parameter.values.result) return '-';
  return props.parameter.values.result.replace('.', ',');
});

function handleValueChange(value: string) {
  emit('update:result', value);
  console.log(`[PassportResultCell] Result изменено: ${props.parameter.key} -> ${value}`);
}
</script>

<style scoped>
.result-cell-input {
  width: 100%;
  border: none;
  background: transparent;
  text-align: center;
  font-family: inherit;
  font-size: 15px;
  padding: 2px;
  outline: none;
  box-sizing: border-box;
  margin: 0;
}

.result-cell-input:focus {
  outline: none;
  background: #f8f9fa;
}

.result-cell-readonly {
  display: block;
  width: 100%;
  text-align: center;
  font-size: 15px;
}

/* ELIS подсветка */
.elis-filled {
  background-color: #8fd19e !important;
}

.elis-filled-text {
  background-color: #8fd19e;
  display: inline-block;
  width: 100%;
  padding: 2px;
}
</style>
```

### 6. Обновить `usePassportEditor.ts` ⚠️

**Файл:** `/TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts`

**Изменить типы событий и обработчики:**

```typescript
import { computed } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import type {
  PassportEditConfig,
  PassportQualityParameter,
  MethodOption,
  MeasurementUpdateEvent,
  MethodUpdateEvent,
  ResultUpdateEvent
} from '@/types/passport.types';

export function usePassportEditor() {
  const store = useDocumentStore();

  const passportConfig = computed<PassportEditConfig | null>(() => {
    if (!store.config || store.config.docType !== 'Passport') {
      return null;
    }
    return store.config as PassportEditConfig;
  });

  const qualityParameters = computed<PassportQualityParameter[]>(() => {
    return passportConfig.value?.qualityParameters || [];
  });

  const isElisUsed = computed<boolean>(() => {
    return passportConfig.value?.isElisUsed || false;
  });

  const hasQualityParameters = computed<boolean>(() => {
    return qualityParameters.value.length > 0;
  });

  function findParameter(paramKey: string): PassportQualityParameter | undefined {
    return qualityParameters.value.find(p => p.key === paramKey);
  }

  /**
   * Пересчитать результат на основе метода и значения measurement
   */
  function recalculateResult(param: PassportQualityParameter): string {
    const selectedMethod = param.method.options.find(
      (m: MethodOption) => m.name === param.method.selected
    );

    // Если есть результат из ELIS и measurement заполнено из ELIS, используем его
    if (param.elisFlags.result && param.elisFlags.measurement) {
      return param.values.result;
    }

    const measurementValue = parseFloat(param.values.measurement.replace(',', '.'));

    if (isNaN(measurementValue)) {
      return '-';
    }

    // Если у метода активирован лимит и значение ниже порога
    if (
      selectedMethod?.limitValueActivate &&
      selectedMethod.limitValue !== undefined &&
      measurementValue < selectedMethod.limitValue
    ) {
      return selectedMethod.limitValueString || '-';
    }

    return param.values.measurement;
  }

  /**
   * Определить, редактируема ли ячейка результата
   */
  function isResultEditable(param: PassportQualityParameter): boolean {
    const selectedMethod = param.method.options.find(
      (m: MethodOption) => m.name === param.method.selected
    );

    if (!selectedMethod || !selectedMethod.limitValueActivate) {
      return false;
    }

    const measurementValue = parseFloat(param.values.measurement.replace(',', '.'));
    if (isNaN(measurementValue)) {
      return false;
    }

    return selectedMethod.limitValue !== undefined && measurementValue < selectedMethod.limitValue;
  }

  /**
   * Обработчик обновления measurement
   */
  function handleMeasurementUpdate(event: MeasurementUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      console.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    param.values.measurement = event.value;
    param.values.result = recalculateResult(param);
    store.isDirty = true;

    console.log(`[usePassportEditor] Measurement обновлено: ${event.paramKey} = ${event.value}, Result = ${param.values.result}`);
  }

  /**
   * Обработчик обновления метода испытаний
   */
  function handleMethodUpdate(event: MethodUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      console.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    param.method.selected = event.methodName;
    param.values.result = recalculateResult(param);
    store.isDirty = true;

    console.log(`[usePassportEditor] Метод обновлен: ${event.paramKey} = ${event.methodName}, Result = ${param.values.result}`);
  }

  /**
   * Обработчик обновления результата (ручное редактирование)
   */
  function handleResultUpdate(event: ResultUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      console.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    param.values.result = event.value;
    store.isDirty = true;
    param.elisFlags.result = false;

    console.log(`[usePassportEditor] Result обновлено вручную: ${event.paramKey} = ${event.value}`);
  }

  return {
    passportConfig,
    qualityParameters,
    isElisUsed,
    hasQualityParameters,
    findParameter,
    recalculateResult,
    isResultEditable,
    handleMeasurementUpdate,
    handleMethodUpdate,
    handleResultUpdate
  };
}
```

---

## Backend реализация

### 1. Реализовать интерфейс `IDocumentEditor` в классе `DocPassport`

**Файл:** `tn.docgeneral/Passport/DocPassport.cs`

```csharp
using TN_Doc.Models.Interfaces;

namespace TN.DocGeneral.Passport
{
    public class DocPassport : IDocumentEditor
    {
        // Существующие методы и поля...

        /// <summary>
        /// Получить конфигурацию для редактирования паспорта качества
        /// </summary>
        public object GetEditConfig(int id)
        {
            // Загрузить документ из базы данных
            var doc = LoadDocumentById(id);

            // Построить конфигурацию
            return new PassportEditConfig
            {
                DocType = "Passport",
                DeviceId = IdDevice,
                DocumentId = id,
                IsElisUsed = IsElisUsed(),
                AdditionalInfo = BuildAdditionalInfoFields(doc),
                QualityParameters = BuildQualityParameters(doc)
            };
        }

        /// <summary>
        /// Сохранить изменения документа из JSON
        /// </summary>
        public void SaveEditConfig(object config)
        {
            var passportConfig = config as PassportEditConfig;
            if (passportConfig == null)
            {
                throw new ArgumentException("Неверный тип конфигурации");
            }

            // Обновить AdditionalInfo поля
            UpdateAdditionalInfoFields(passportConfig.AdditionalInfo);

            // Обновить качественные параметры
            UpdateQualityParameters(passportConfig.QualityParameters);

            // Сохранить изменения в базу данных
            SaveToDatabase();
        }
    }
}
```

### 2. Создать классы моделей для Backend

```csharp
/// <summary>
/// Конфигурация для редактирования паспорта качества
/// </summary>
public class PassportEditConfig
{
    public string DocType { get; set; } = "Passport";
    public int DeviceId { get; set; }
    public int DocumentId { get; set; }
    public bool IsElisUsed { get; set; }
    public Dictionary<string, AdditionalInfoField> AdditionalInfo { get; set; } = new();
    public List<QualityParameter> QualityParameters { get; set; } = new();
}

/// <summary>
/// Параметр качества нефти
/// </summary>
public class QualityParameter
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Name { get; set; }
    public bool Editable { get; set; }
    public bool RequiredFill { get; set; }
    public int? RoundValue { get; set; }
    public ElisData? ElisData { get; set; }
    public ParameterValues Values { get; set; }
    public ParameterMethod Method { get; set; }
    public ParameterDocument? Document { get; set; }
    public ParameterElisFlags ElisFlags { get; set; }
}

/// <summary>
/// Значения параметра (НОВАЯ СТРУКТУРА)
/// </summary>
public class ParameterValues
{
    /// <summary>
    /// Измерение (объединенное значение)
    /// Логика заполнения: ELIS → HAL → IVK → пусто
    /// </summary>
    public string Measurement { get; set; } = string.Empty;

    /// <summary>
    /// Результат (ранее PrintValue)
    /// Всегда показывается, независимо от ELIS
    /// </summary>
    public string Result { get; set; } = string.Empty;
}

/// <summary>
/// Метод испытаний
/// </summary>
public class ParameterMethod
{
    public string Selected { get; set; }
    public List<MethodOption> Options { get; set; } = new();
}

/// <summary>
/// Опция метода испытаний
/// </summary>
public class MethodOption
{
    public string Name { get; set; }
    public bool IsDefault { get; set; }
    public bool LimitValueActivate { get; set; }
    public double? LimitValue { get; set; }
    public string? LimitValueString { get; set; }
}

/// <summary>
/// Документ ELIS
/// </summary>
public class ParameterDocument
{
    public string Number { get; set; }
    public bool ElisFilled { get; set; }
}

/// <summary>
/// Флаги заполнения из ELIS (НОВАЯ СТРУКТУРА)
/// </summary>
public class ParameterElisFlags
{
    public bool Measurement { get; set; }  // вместо hal
    public bool Method { get; set; }
    public bool Result { get; set; }       // вместо printValue
    public bool Document { get; set; }
}

/// <summary>
/// ELIS метаданные
/// </summary>
public class ElisData
{
    public string KeyELIS { get; set; }
    public List<string>? ElisAlias { get; set; }
}
```

### 3. Реализовать метод `BuildQualityParameters()`

**ВАЖНО: Логика заполнения колонки "Измерение"**

```csharp
/// <summary>
/// Построить список качественных параметров
/// </summary>
private List<QualityParameter> BuildQualityParameters(PassportDocument doc)
{
    var parameters = new List<QualityParameter>();

    // Пример для одного параметра (нужно повторить для всех параметров)
    parameters.Add(new QualityParameter
    {
        Id = 1,
        Key = "TempCorrection",
        Name = "Температура приведения, °С",
        Editable = true,
        RequiredFill = false,
        RoundValue = 1,
        ElisData = new ElisData
        {
            KeyELIS = "TempCorrection",
            ElisAlias = new List<string> { "Температура приведения" }
        },
        Values = new ParameterValues
        {
            // ЛОГИКА ЗАПОЛНЕНИЯ MEASUREMENT:
            // 1. Если есть значение из ELIS - используем его
            // 2. Иначе если есть значение HAL из БД - используем его
            // 3. Иначе если есть значение IVK из БД - используем его
            // 4. Иначе пустая строка
            Measurement = GetMeasurementValue(doc, "TempCorrection"),

            // ЛОГИКА ЗАПОЛНЕНИЯ RESULT:
            // 1. Если есть ValueString из ELIS - используем его
            // 2. Иначе если есть PrintValue из БД - используем его
            // 3. Иначе пересчитываем на основе measurement и метода
            Result = GetResultValue(doc, "TempCorrection")
        },
        Method = new ParameterMethod
        {
            Selected = doc.TempCorrectionMethod,
            Options = GetMethodOptions("TempCorrection")
        },
        Document = IsElisUsed() ? new ParameterDocument
        {
            Number = doc.TempCorrectionDocNumber ?? string.Empty,
            ElisFilled = doc.TempCorrectionElisDocFilled
        } : null,
        ElisFlags = new ParameterElisFlags
        {
            Measurement = doc.TempCorrectionElisMeasurementFilled,
            Method = doc.TempCorrectionElisMethodFilled,
            Result = doc.TempCorrectionElisResultFilled,
            Document = doc.TempCorrectionElisDocFilled
        }
    });

    // Повторить для всех остальных параметров качества...

    return parameters;
}

/// <summary>
/// Получить значение measurement для параметра
/// ЛОГИКА: ELIS → HAL → IVK → пусто
/// </summary>
private string GetMeasurementValue(PassportDocument doc, string paramKey)
{
    // TODO: Реализовать логику на основе конкретных требований
    switch (paramKey)
    {
        case "TempCorrection":
            if (!string.IsNullOrEmpty(doc.TempCorrectionElisValue))
                return doc.TempCorrectionElisValue;
            if (!string.IsNullOrEmpty(doc.TempCorrectionHalValue))
                return doc.TempCorrectionHalValue;
            if (!string.IsNullOrEmpty(doc.TempCorrectionIvkValue))
                return doc.TempCorrectionIvkValue;
            return string.Empty;
        // ... остальные параметры
        default:
            return string.Empty;
    }
}

/// <summary>
/// Получить значение result для параметра
/// ЛОГИКА: ValueString из ELIS → PrintValue из БД → пересчёт
/// </summary>
private string GetResultValue(PassportDocument doc, string paramKey)
{
    switch (paramKey)
    {
        case "TempCorrection":
            if (!string.IsNullOrEmpty(doc.TempCorrectionElisValueString))
                return doc.TempCorrectionElisValueString;
            if (!string.IsNullOrEmpty(doc.TempCorrectionPrintValue))
                return doc.TempCorrectionPrintValue;
            return RecalculateResult(doc, paramKey);
        // ... остальные параметры
        default:
            return string.Empty;
    }
}

/// <summary>
/// Пересчитать результат на основе measurement и метода
/// </summary>
private string RecalculateResult(PassportDocument doc, string paramKey)
{
    var measurementValue = GetMeasurementValue(doc, paramKey);
    if (string.IsNullOrEmpty(measurementValue))
        return "-";

    var method = GetSelectedMethod(doc, paramKey);
    if (method == null || !method.LimitValueActivate)
        return measurementValue;

    if (double.TryParse(measurementValue.Replace(',', '.'), out var value))
    {
        if (method.LimitValue.HasValue && value < method.LimitValue.Value)
        {
            return method.LimitValueString ?? "-";
        }
    }

    return measurementValue;
}
```

### 4. Реализовать метод `UpdateQualityParameters()`

```csharp
/// <summary>
/// Обновить качественные параметры в документе
/// </summary>
private void UpdateQualityParameters(List<QualityParameter> parameters)
{
    foreach (var param in parameters)
    {
        switch (param.Key)
        {
            case "TempCorrection":
                UpdateMeasurementValue("TempCorrection", param.Values.Measurement);
                UpdateResultValue("TempCorrection", param.Values.Result);
                UpdateMethodValue("TempCorrection", param.Method.Selected);
                break;
            // Повторить для всех остальных параметров...
        }
    }
}

private void UpdateMeasurementValue(string paramKey, string value)
{
    // TODO: Сохранить в соответствующее поле БД
}

private void UpdateResultValue(string paramKey, string value)
{
    // TODO: Сохранить в соответствующее поле БД
}

private void UpdateMethodValue(string paramKey, string methodName)
{
    // TODO: Сохранить в соответствующее поле БД
}
```

### 5. Реализовать метод `BuildAdditionalInfoFields()`

```csharp
/// <summary>
/// Построить поля дополнительной информации (AdditionalInfo таблица)
/// </summary>
private Dictionary<string, AdditionalInfoField> BuildAdditionalInfoFields(PassportDocument doc)
{
    return new Dictionary<string, AdditionalInfoField>
    {
        ["Field1"] = new AdditionalInfoField
        {
            Label = "Месторождение",
            Value = doc.Field1Value ?? string.Empty,
            Editable = true,
            Type = "text"
        },
        ["Field2"] = new AdditionalInfoField
        {
            Label = "Пункт приёма",
            Value = doc.Field2Value ?? string.Empty,
            Editable = true,
            Type = "text"
        },
        // ... остальные 14 полей
    };
}
```

---

## Timeline и оценка

### Frontend обновления (1-2 дня)
- [ ] Обновить `passport.types.ts` - изменить ParameterValues (1 час)
- [ ] Обновить `PassportQualityTable.vue` - изменить заголовок (1 час)
- [ ] Обновить `PassportParameterRow.vue` - 6 ячеек вместо 8 (2 часа)
- [ ] Переименовать `PassportHalInput.vue` → `PassportMeasurementInput.vue` (30 мин)
- [ ] Переименовать `PassportPrintCell.vue` → `PassportResultCell.vue` (30 мин)
- [ ] Обновить `usePassportEditor.ts` - новые обработчики (2 часа)
- [ ] Обновить `DocumentPassportEditor.vue` (1 час)
- [ ] Тестирование frontend (2 часа)

### Backend реализация (5-7 дней)
- [ ] Реализовать интерфейс `IDocumentEditor` (1 день)
- [ ] Создать классы моделей (1 день)
- [ ] Реализовать `BuildQualityParameters()` с логикой "Измерение" (2-3 дня)
- [ ] Реализовать `UpdateQualityParameters()` (1-2 дня)
- [ ] Реализовать `BuildAdditionalInfoFields()` (1 день)
- [ ] Тестирование backend (1 день)

### Интеграционное тестирование (2 дня)
- [ ] Проверить загрузку данных из БД
- [ ] Проверить ELIS интеграцию
- [ ] Проверить сохранение изменений
- [ ] Проверить валидацию полей
- [ ] Проверить редактируемость ячейки "Результат"

**Итого: 8-11 дней (1.5-2 недели)**

---

## Приоритеты

### 🔴 Критично (обязательно)
1. Обновить структуру таблицы с 8 на 6 колонок (Frontend)
2. Реализовать логику колонки "Измерение" в DocPassport.cs (Backend)
3. Реализовать GetEditConfig() в DocPassport.cs (Backend)
4. Обновить типы TypeScript под новую структуру

### 🟡 Важно (желательно)
1. ELIS интеграция с подсветкой #8fd19e
2. Валидация полей с визуализацией ошибок
3. Динамическая редактируемость ячейки "Результат"
4. Проверка округления значений

### 🟢 Опционально (можно отложить)
1. Анимации переходов
2. Расширенное логирование событий
3. Unit тесты для композаблов
4. E2E тесты

---

## Следующие шаги

1. **Обновить Frontend компоненты** под новую 6-колоночную структуру (1-2 дня)
2. **Уточнить с пользователем логику колонки "Измерение"** - как именно формировать значение
3. **Реализовать Backend GetEditConfig()** в DocPassport.cs (5-7 дней)
4. **Интеграционное тестирование** (2 дня)
5. **Деплой и production тестирование** (1 день)

---

## Архитектурные решения

### Интеграция с существующей системой

**Используемый подход:**
- ✅ Использование существующего `document-editor` монорепозитория
- ✅ Интеграция с `DocumentEditController` (без создания нового контроллера)
- ✅ Использование интерфейса `IDocumentEditor` для единообразия
- ✅ Следование паттерну из `DocumentActEditor.vue`
- ✅ Роутинг через Vue Router (`/edit/:deviceId/Passport/:id`)

**НЕ используемый подход (из старого плана):**
- ❌ Создание отдельного приложения `/TN_Doc/Client/passport-editor/`
- ❌ Создание нового `PassportController`
- ❌ Отдельные endpoint'ы для паспортов

### Технологический стек

**Frontend:**
- Vue 3.4.21 с Composition API
- TypeScript для типобезопасности
- PrimeVue 4.2+ для UI компонентов
- Pinia для управления состоянием
- Vite как сборщик

**Backend:**
- ASP.NET Core 8.0
- Интерфейс `IDocumentEditor`
- Существующий `DocumentEditController`
- NLog для логирования

### ELIS интеграция

**Подсветка данных из ELIS:**
- Цвет: `#8fd19e` (светло-зеленый)
- Применяется к полям с флагом `elisFlags.<field> === true`
- Условное отображение колонки "Документы" при `isElisUsed === true`

**Флаги ELIS:**
- `measurement` - измерение заполнено из ELIS
- `method` - метод испытаний заполнен из ELIS
- `result` - результат заполнен из ELIS
- `document` - документ заполнен из ELIS

---

## Связанные документы

- `/tech_debt/DOCUMENT_EDITOR_POC.md` - Архитектура Document Editor POC
- `/tech_debt/IMPLEMENTATION_STATUS.md` - Статус реализации Stage 1 и Stage 2
- `/tech_debt/MIGRATION_PATTERN.md` - Общий паттерн миграции документов
- `/tech_debt/VUE_EDITOR_INTEGRATION.md` - Интеграция Vue редактора

---

**Последнее обновление:** 2025-10-21
**Автор:** TN_Doc Development Team
**Статус:** В разработке (Frontend готов, Backend требует реализации)
