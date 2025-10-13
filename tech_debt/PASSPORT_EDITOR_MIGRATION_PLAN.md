# План миграции формы редактирования паспорта на Vue.js

## 📋 Оглавление

1. [Архитектура и технологический стек](#1-архитектура-и-технологический-стек)
2. [Изменения на серверной стороне](#2-изменения-на-серверной-стороне)
3. [Компонентная структура Vue](#3-компонентная-структура-vue)
4. [Composables (переиспользуемая логика)](#4-composables-переиспользуемая-логика)
5. [Pinia Store](#5-pinia-store)
6. [Интеграция с существующей системой](#6-интеграция-с-существующей-системой)
7. [Этапы реализации](#7-этапы-реализации)
8. [Преимущества миграции](#8-преимущества-миграции)
9. [Риски и митигация](#9-риски-и-митигация)

---

## 1. АРХИТЕКТУРА И ТЕХНОЛОГИЧЕСКИЙ СТЕК

### Использовать существующий стек из statusbar:
- Vue 3.4+ с Composition API
- TypeScript
- PrimeVue 4.2+ (готовые компоненты)
- Pinia для управления состоянием
- Vite как сборщик

### Структура проекта:
```
TN_Doc/Client/passport-editor/
├── src/
│   ├── components/
│   │   ├── AdditionalInfoTable.vue     # Таблица дополнительной информации
│   │   ├── ParametersTable.vue          # Основная таблица параметров
│   │   ├── ValidationTooltip.vue        # Компонент для подсказок валидации
│   │   └── PrintCell.vue                # Редактируемая ячейка печати
│   ├── composables/
│   │   ├── useValidation.ts             # Логика валидации
│   │   ├── useParentCommunication.ts    # Связь с родительским окном
│   │   └── useLogger.ts                 # Клиентское логирование
│   ├── stores/
│   │   └── passportStore.ts             # Pinia store для данных паспорта
│   ├── types/
│   │   ├── passport.types.ts            # TypeScript типы
│   │   └── validation.types.ts
│   ├── services/
│   │   ├── api.service.ts               # API запросы
│   │   └── validation.service.ts        # Сервис валидации
│   ├── App.vue                           # Главный компонент
│   └── main.ts
├── public/
├── package.json
├── tsconfig.json
└── vite.config.ts
```

---

## 2. ИЗМЕНЕНИЯ НА СЕРВЕРНОЙ СТОРОНЕ

### 2.1 Модификация DocPassport.cs

**Текущее состояние:**
- `GetEditDoc(id)` генерирует HTML с помощью HtmlAgilityPack
- Сохраняет в `/wwwroot/HTML/html.html`

**Новый подход:**
```csharp
// Новый метод для возврата JSON данных вместо HTML
public object GetEditDocData(int id)
{
    _logger.Trace($"Получение JSON данных для редактирования документа {IdDoc} с ID {id}");
    GetViewDoc(id);

    var isElisUsed = _appConfig.IsUsedElis(_deviceId);
    var editDoc = LoadCfg<CfgEditPassport>(PathToDocEditConfigFile);
    var dataArm = ((DataIVKDoc)Doc.Doc.DataIVK).TablePassport.DataARM;

    var result = new
    {
        DocId = id,
        IsElisUsed = isElisUsed,
        AdditionalInfo = BuildAdditionalInfoData(editDoc, dataArm),
        Parameters = BuildParametersData(editDoc, dataArm),
        Dictionaries = new
        {
            Users = Doc.Doc?.Settings?.Dictionarys?.Users,
            InvalidChars = GetInvalidChars()
        }
    };

    return result;
}

private List<object> BuildAdditionalInfoData(CfgEditPassport editDoc, DataARM dataArm)
{
    var items = new List<object>();
    foreach (var item in editDoc.AdditionalInfo.Where(x => x.Use))
    {
        var fieldData = new
        {
            Key = item.Key,
            Name = item.Name,
            Type = item.Type,
            Edit = item.Edit,
            RequiredFill = item.RequiredFill,
            Value = GetAdditionalInfoValue(item),
            KeyELIS = item.KeyELIS,
            ElisAlias = item.ElisAlias,
            ElisFilled = false,
            Options = item.Type == "list" ? GetUserOptions(item.Key) : null
        };
        items.Add(fieldData);
    }
    return items;
}

private List<object> BuildParametersData(CfgEditPassport editDoc, DataARM dataArm)
{
    var parameters = new List<object>();
    foreach (var param in editDoc.Parameters.Where(x => x.Use))
    {
        var metodName = GetJsonTokenSafety($"Doc.DataIVK.TablePassport.TableActAndPassport.Passport.{param.Key.Replace("Correction", "Result")}.Desc");
        var valueResult = GetJsonTokenSafety($"Doc.DataIVK.TablePassport.PassportResult.{param.Key.Replace("Correction", "Result")}");
        var valueRaw = GetJsonTokenSafety($"Doc.DataIVK.TablePassport.TableActAndPassport.Passport.{param.Key.Replace("Correction", "Raw")}.Value", NotEditLabel);
        // ... остальные данные

        var labInfo = dataArm?.LabInfo?.FirstOrDefault(x => x.ParameterKey == param.Key);
        var methods = GetMetods(param, editDoc, labInfo);

        var paramData = new
        {
            Key = param.Key,
            Name = param.Name,
            Id = param.Id,
            Edit = param.Edit,
            RequiredFill = param.RequiredFill,
            RoundValue = param.RoundValue,
            KeyELIS = param.KeyELIS,
            ElisAlias = param.ElisAlias,
            ValueIVK = valueRaw,
            ValueHAL = valueCorrection,
            ValueResult = valueResult,
            PrintValue = GetPrintValue(param.Key, valueResult, dataArm),
            Metods = methods,
            SelectedMetod = metodName,
            Document = labInfo?.Document,
            ElisFilled = false
        };
        parameters.Add(paramData);
    }
    return parameters;
}
```

### 2.2 Новый контроллер endpoint

```csharp
// В HomeController или новом PassportController
[HttpGet]
public IActionResult GetPassportEditData(Guid idDevice, int idDoc)
{
    try
    {
        var doc = _appConfig.GetDocumentClass(idDevice, IdDoc.Passport);
        var data = ((DocPassport)doc).GetEditDocData(idDoc);
        return Ok(data);
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Ошибка получения данных для редактирования паспорта");
        return StatusCode(500, new { error = ex.Message });
    }
}
```

---

## 3. КОМПОНЕНТНАЯ СТРУКТУРА VUE

### 3.1 Главный компонент App.vue

```vue
<template>
  <div class="passport-editor">
    <AdditionalInfoTable
      :fields="additionalInfo"
      :dictionaries="dictionaries"
      @field-change="handleFieldChange"
    />

    <ParametersTable
      :parameters="parameters"
      :is-elis-used="isElisUsed"
      @parameter-change="handleParameterChange"
      @metod-change="handleMetodChange"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { usePassportStore } from '@/stores/passportStore';
import { useParentCommunication } from '@/composables/useParentCommunication';
import AdditionalInfoTable from '@/components/AdditionalInfoTable.vue';
import ParametersTable from '@/components/ParametersTable.vue';

const store = usePassportStore();
const { notifyValidationState } = useParentCommunication();

const additionalInfo = computed(() => store.additionalInfo);
const parameters = computed(() => store.parameters);
const isElisUsed = computed(() => store.isElisUsed);
const dictionaries = computed(() => store.dictionaries);

onMounted(async () => {
  const urlParams = new URLSearchParams(window.location.search);
  const docId = parseInt(urlParams.get('id') || '0');
  const deviceId = urlParams.get('deviceId') || '';

  await store.loadPassportData(deviceId, docId);
});

const handleFieldChange = (key: string, value: any) => {
  store.updateAdditionalInfo(key, value);
  notifyValidationState(store.isValid);
};

const handleParameterChange = (key: string, tag: string, value: any) => {
  store.updateParameter(key, tag, value);
  notifyValidationState(store.isValid);
};

const handleMetodChange = (key: string, metod: any) => {
  store.updateMetod(key, metod);
};
</script>
```

### 3.2 AdditionalInfoTable.vue

```vue
<template>
  <table id="AdditionalInfo">
    <tbody>
      <tr v-for="field in fields" :key="field.Key">
        <td>{{ field.Name }}</td>
        <td>
          <!-- Dropdown для списков пользователей -->
          <Dropdown
            v-if="field.Type === 'list'"
            v-model="field.Value"
            :options="field.Options"
            optionLabel="IOF"
            optionValue="Id"
            :class="getValidationClass(field)"
            :disabled="!field.Edit"
            @change="handleChange(field)"
          />

          <!-- Calendar для дат -->
          <Calendar
            v-else-if="field.Type === 'datetime-local'"
            v-model="field.Value"
            showTime
            :class="getValidationClass(field)"
            :required="field.RequiredFill"
            @update:modelValue="handleChange(field)"
          />

          <!-- InputText для текста -->
          <InputText
            v-else
            v-model="field.Value"
            :class="getValidationClass(field)"
            :disabled="!field.Edit"
            :required="field.RequiredFill"
            @input="handleChange(field)"
          />
        </td>
      </tr>
    </tbody>
  </table>
</template>

<script setup lang="ts">
import { useValidation } from '@/composables/useValidation';
import Dropdown from 'primevue/dropdown';
import Calendar from 'primevue/calendar';
import InputText from 'primevue/inputtext';

const props = defineProps<{
  fields: any[];
  dictionaries: any;
}>();

const emit = defineEmits<{
  fieldChange: [key: string, value: any];
}>();

const { validateField, getValidationClass } = useValidation();

const handleChange = (field: any) => {
  validateField(field);
  emit('fieldChange', field.Key, field.Value);

  // Автозаполнение должности и предприятия при выборе пользователя
  if (field.Key.includes('_IOF')) {
    autoFillUserData(field);
  }
};

const autoFillUserData = (field: any) => {
  // Логика из UserChangeEvent()
  // ...
};
</script>
```

### 3.3 ParametersTable.vue

```vue
<template>
  <table id="Edit" :class="{ 'no-elis': !isElisUsed }">
    <colgroup>
      <col class="col-num">
      <col class="col-name">
      <col class="col-method">
      <col class="col-ivk">
      <col v-if="isElisUsed" class="col-elis">
      <col class="col-hal">
      <col class="col-result-value">
      <col class="col-result-text">
    </colgroup>

    <thead>
      <tr>
        <th rowspan="2">№</th>
        <th rowspan="2">Наименование показателя</th>
        <th rowspan="2">Метод испытаний</th>
        <th rowspan="2">Измерение ИВК</th>
        <th v-if="isElisUsed" rowspan="2">Документы</th>
        <th rowspan="2">Измерение ХАЛ</th>
        <th colspan="2">Результат</th>
      </tr>
      <tr>
        <th>Значение</th>
        <th>Текст</th>
      </tr>
    </thead>

    <tbody>
      <tr v-for="(param, index) in parameters" :key="param.Key">
        <td style="text-align: center">{{ index + 1 }}</td>
        <td>{{ param.Name }}</td>

        <!-- Метод испытаний -->
        <td>
          <Dropdown
            v-model="param.SelectedMetod"
            :options="param.Metods"
            optionLabel="Name"
            optionValue="Name"
            :class="getValidationClass(param, 'Metod')"
            @change="handleMetodChange(param)"
          />
        </td>

        <!-- Измерение ИВК -->
        <td class="manual-input--disabled" style="text-align: center">
          {{ param.ValueIVK }}
        </td>

        <!-- Документы (если ELIS) -->
        <td v-if="isElisUsed">
          <InputText
            :value="param.Document?.Number"
            disabled
            class="manual-input--disabled"
            style="text-align: center"
          />
        </td>

        <!-- Измерение ХАЛ -->
        <td>
          <InputNumber
            v-model="param.ValueHAL"
            :class="getValidationClass(param, 'Value')"
            :disabled="!param.Edit"
            :maxFractionDigits="param.RoundValue ? parseInt(param.RoundValue) : 10"
            :useGrouping="false"
            @input="handleHALChange(param)"
          />
        </td>

        <!-- Результат - Значение -->
        <td class="manual-input--disabled" style="text-align: center">
          {{ param.ValueResult }}
        </td>

        <!-- Результат - Текст -->
        <PrintCell
          :parameter="param"
          @value-change="handlePrintValueChange"
        />
      </tr>
    </tbody>
  </table>
</template>

<script setup lang="ts">
import { useValidation } from '@/composables/useValidation';
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import PrintCell from './PrintCell.vue';

const props = defineProps<{
  parameters: any[];
  isElisUsed: boolean;
}>();

const emit = defineEmits<{
  parameterChange: [key: string, tag: string, value: any];
  metodChange: [key: string, metod: any];
}>();

const { validateField, getValidationClass } = useValidation();

const handleMetodChange = (param: any) => {
  emit('metodChange', param.Key, param.SelectedMetod);
  // Логика из TogglePrintCellEditable()
};

const handleHALChange = (param: any) => {
  validateField(param, 'Value');
  emit('parameterChange', param.Key, 'Value', param.ValueHAL);
  // Логика из updatePrintValueOnHalChange()
};
</script>
```

### 3.4 PrintCell.vue

```vue
<template>
  <td
    :class="cellClasses"
    style="text-align: center"
    :data-parameter-key="parameter.Key"
  >
    <InputText
      v-if="isEditable"
      v-model="printValue"
      class="print-cell-input"
      :class="getValidationClass(parameter, 'PrintValue')"
      @input="handleInput"
    />
    <span v-else>{{ printValue }}</span>
  </td>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import InputText from 'primevue/inputtext';

const props = defineProps<{
  parameter: any;
}>();

const emit = defineEmits<{
  valueChange: [key: string, value: string];
}>();

const printValue = ref(props.parameter.PrintValue);

const isEditable = computed(() => {
  // Логика из TogglePrintCellEditable()
  const metod = props.parameter.Metods.find(m => m.Name === props.parameter.SelectedMetod);
  const halValue = parseFloat(props.parameter.ValueHAL);

  return metod?.LimitValueActivate && !isNaN(halValue) && halValue < metod.LimitValue;
});

const cellClasses = computed(() => ({
  'print-cell-editable': isEditable.value,
  'manual-input--disabled': !isEditable.value,
  'elis-filled-cell': props.parameter.ElisFilled
}));

watch(() => props.parameter, (newParam) => {
  printValue.value = calculatePrintValue(newParam);
}, { deep: true });

const calculatePrintValue = (param: any) => {
  // Логика из calculatePrintValue()
  // ...
};

const handleInput = () => {
  emit('valueChange', props.parameter.Key, printValue.value);
};
</script>
```

---

## 4. COMPOSABLES (ПЕРЕИСПОЛЬЗУЕМАЯ ЛОГИКА)

### 4.1 useValidation.ts

```typescript
import { ref } from 'vue';
import type { ValidationRule } from '@/types/validation.types';

export function useValidation() {
  const invalidChars = ref<string[]>([]);

  const checkEmpty = (field: any): boolean => {
    if (!field.RequiredFill) return true;

    if (!field.Value || field.Value === '') {
      field.ValidationError = 'Поле должно быть заполнено!';
      return false;
    }

    field.ValidationError = null;
    return true;
  };

  const checkInvalidChars = (field: any): boolean => {
    if (!invalidChars.value || invalidChars.value.length === 0) return true;

    const value = String(field.Value);
    for (const char of invalidChars.value) {
      if (value.includes(char)) {
        field.ValidationError = `Некорректный символ: ${char}`;
        return false;
      }
    }

    field.ValidationError = null;
    return true;
  };

  const checkRounding = (field: any): boolean => {
    if (!field.RoundValue) return true;

    const round = Number(field.RoundValue);
    if (isNaN(round) || round < 1) return true;

    const pattern = `^-?[0-9]+(?:\\.[0-9]{1,${round}})?$`;
    const regex = new RegExp(pattern);

    if (regex.test(String(field.Value))) {
      field.ValidationError = null;
      return true;
    }

    field.ValidationError = 'Кол-во знаков после запятой не соответствует заданным правилам!';
    return false;
  };

  const validateField = (field: any, tag?: string): boolean => {
    let isValid = true;

    if (tag === 'Value') {
      isValid = checkEmpty(field) && checkRounding(field);
    } else {
      isValid = checkEmpty(field) && checkInvalidChars(field);
    }

    return isValid;
  };

  const getValidationClass = (field: any, tag?: string) => {
    return {
      'correct-value': !field.ValidationError,
      'incorrect-value': !!field.ValidationError
    };
  };

  return {
    invalidChars,
    validateField,
    getValidationClass,
    checkEmpty,
    checkInvalidChars,
    checkRounding
  };
}
```

### 4.2 useParentCommunication.ts

```typescript
export function useParentCommunication() {
  const notifyValidationState = (isValid: boolean) => {
    const message = isValid ? 'ButtonSaveOn' : 'ButtonSaveOff';
    window.top?.postMessage(message, '*');
  };

  const saveDocument = async (data: any) => {
    // Вызов SaveDoc через window.parent
    return new Promise((resolve, reject) => {
      // Логика из SaveDoc()
    });
  };

  return {
    notifyValidationState,
    saveDocument
  };
}
```

### 4.3 useLogger.ts

```typescript
export function useLogger() {
  const logToServer = async (level: string, message: string) => {
    try {
      await fetch('/api/ClientLog/logging', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ level, message })
      });
    } catch (error) {
      console.warn('Не удалось отправить лог на сервер:', message);
    }
  };

  return {
    logTrace: (msg: string) => logToServer('Trace', msg),
    logDebug: (msg: string) => logToServer('Debug', msg),
    logInfo: (msg: string) => logToServer('Info', msg),
    logWarn: (msg: string) => logToServer('Warn', msg),
    logError: (msg: string) => logToServer('Error', msg)
  };
}
```

---

## 5. PINIA STORE

```typescript
// stores/passportStore.ts
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { PassportData } from '@/types/passport.types';
import { apiService } from '@/services/api.service';

export const usePassportStore = defineStore('passport', () => {
  const docId = ref<number>(0);
  const deviceId = ref<string>('');
  const additionalInfo = ref<any[]>([]);
  const parameters = ref<any[]>([]);
  const dictionaries = ref<any>(null);
  const isElisUsed = ref<boolean>(false);

  const isValid = computed(() => {
    const additionalValid = additionalInfo.value.every(f => !f.ValidationError);
    const parametersValid = parameters.value.every(p => !p.ValidationError);
    return additionalValid && parametersValid;
  });

  const loadPassportData = async (devId: string, docNumber: number) => {
    deviceId.value = devId;
    docId.value = docNumber;

    const data = await apiService.getPassportEditData(devId, docNumber);

    additionalInfo.value = data.AdditionalInfo;
    parameters.value = data.Parameters;
    dictionaries.value = data.Dictionaries;
    isElisUsed.value = data.IsElisUsed;
  };

  const updateAdditionalInfo = (key: string, value: any) => {
    const field = additionalInfo.value.find(f => f.Key === key);
    if (field) {
      field.Value = value;
    }
  };

  const updateParameter = (key: string, tag: string, value: any) => {
    const param = parameters.value.find(p => p.Key === key);
    if (param) {
      if (tag === 'Value') param.ValueHAL = value;
      else if (tag === 'PrintValue') param.PrintValue = value;
    }
  };

  const updateMetod = (key: string, metodName: string) => {
    const param = parameters.value.find(p => p.Key === key);
    if (param) {
      param.SelectedMetod = metodName;
    }
  };

  return {
    docId,
    deviceId,
    additionalInfo,
    parameters,
    dictionaries,
    isElisUsed,
    isValid,
    loadPassportData,
    updateAdditionalInfo,
    updateParameter,
    updateMetod
  };
});
```

---

## 6. ИНТЕГРАЦИЯ С СУЩЕСТВУЮЩЕЙ СИСТЕМОЙ

### 6.1 Модификация родительской страницы

```javascript
// В родительском окне (Index.cshtml или другая страница)
function openPassportEditor(deviceId, docId) {
  const iframe = document.getElementById('editFrame');
  iframe.src = `/passport-editor/?deviceId=${deviceId}&id=${docId}`;

  // Слушатель сообщений от Vue приложения
  window.addEventListener('message', (event) => {
    if (event.data === 'ButtonSaveOn') {
      enableSaveButton();
    } else if (event.data === 'ButtonSaveOff') {
      disableSaveButton();
    }
  });
}
```

### 6.2 Endpoint для сохранения

```csharp
[HttpPost]
public async Task<IActionResult> SavePassport([FromBody] SavePassportRequest request)
{
    try
    {
        var doc = _appConfig.GetDocumentClass(request.DeviceId, IdDoc.Passport);
        var success = doc.SaveDoc(JsonConvert.SerializeObject(request.Data));

        return Ok(new { success });
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Ошибка сохранения паспорта");
        return StatusCode(500, new { error = ex.Message });
    }
}
```

---

## 7. ЭТАПЫ РЕАЛИЗАЦИИ

### Этап 1: Подготовка инфраструктуры (1-2 дня)
- [ ] Создать Vue проект в `/TN_Doc/Client/passport-editor/`
- [ ] Настроить TypeScript, Vite, PrimeVue
- [ ] Создать базовую структуру папок
- [ ] Настроить интеграцию сборки в ASP.NET Core

### Этап 2: Серверная часть (2-3 дня)
- [ ] Создать `GetEditDocData()` в `DocPassport.cs`
- [ ] Добавить endpoint в контроллер
- [ ] Протестировать возврат JSON данных
- [ ] Добавить endpoint для сохранения

### Этап 3: Базовые компоненты (3-4 дня)
- [ ] Реализовать `AdditionalInfoTable.vue`
- [ ] Реализовать `ParametersTable.vue`
- [ ] Создать `PrintCell.vue`
- [ ] Базовая валидация

### Этап 4: Логика валидации (2-3 дня)
- [ ] Composable `useValidation`
- [ ] Проверка обязательных полей
- [ ] Проверка недопустимых символов
- [ ] Проверка округления
- [ ] Tooltips для ошибок

### Этап 5: Pinia Store и API (2 дня)
- [ ] Создать store
- [ ] Сервис API запросов
- [ ] Загрузка данных
- [ ] Сохранение данных

### Этап 6: Специфичная логика (3-4 дня)
- [ ] Автозаполнение пользовательских данных
- [ ] Логика `TogglePrintCellEditable`
- [ ] Расчет `PrintValue`
- [ ] Обработка ELIS данных

### Этап 7: Интеграция с родительским окном (1-2 дня)
- [ ] `useParentCommunication` composable
- [ ] Уведомления о валидности
- [ ] Интеграция кнопки сохранения

### Этап 8: Стили и UX (2-3 дня)
- [ ] Портировать CSS стили
- [ ] ELIS подсветка
- [ ] Адаптация под PrimeVue
- [ ] Тестирование UI

### Этап 9: Тестирование (3-5 дней)
- [ ] Unit тесты composables
- [ ] Component тесты
- [ ] E2E тесты
- [ ] Тестирование интеграции

### Этап 10: Документация и развертывание (1-2 дня)
- [ ] Документация компонентов
- [ ] README для разработчиков
- [ ] Настройка CI/CD
- [ ] Развертывание

**Общая оценка: 20-30 рабочих дней**

---

## 8. ПРЕИМУЩЕСТВА МИГРАЦИИ

✅ **Реактивность**: Автоматическое обновление UI при изменении данных
✅ **Типобезопасность**: TypeScript предотвращает ошибки на этапе разработки
✅ **Компонентность**: Переиспользуемые изолированные компоненты
✅ **Тестируемость**: Легче писать unit и integration тесты
✅ **Поддержка**: Современный код проще поддерживать
✅ **PrimeVue**: Готовые компоненты с accessibility и валидацией
✅ **Производительность**: Virtual DOM и оптимизации Vue 3

---

## 9. РИСКИ И МИТИГАЦИЯ

⚠️ **Риск**: Сложность логики `TogglePrintCellEditable`
   - **Митигация**: Детальный анализ и unit тесты

⚠️ **Риск**: Интеграция с OPC тегами через `EditDoc.js`
   - **Митигация**: Создать отдельный сервис для OPC

⚠️ **Риск**: Обратная совместимость с существующими формами
   - **Митигация**: Поэтапная миграция, feature flag

⚠️ **Риск**: ELIS интеграция
   - **Митигация**: Сохранить существующую логику, обернуть в API

---

## Файлы для анализа

### Зависимые файлы проекта:
- `/TN_Doc/wwwroot/HTML/DocEditPassport.html` - HTML форма редактирования
- `/tn.docgeneral/Passport/DocPassport.cs` - Серверная логика
- `/TN_Doc/wwwroot/css/commonEditForm.css` - Общие стили
- `/TN_Doc/wwwroot/css/elisEditForm.css` - Стили ELIS
- `/TN_Doc/wwwroot/js/EditDoc.js` - JavaScript логика
- `/TN_Doc/wwwroot/js/Logger.js` - Клиентское логирование
- `/TN_Doc/wwwroot/js/loading-spinner.js` - Спиннер загрузки

### Конфигурационные файлы:
- `/TN_Doc/Cfg/CfgEditPassport.json` - Конфигурация формы редактирования
