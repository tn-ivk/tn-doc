# План миграции статической таблицы AdditionalInfo на Vue-компонент

**Дата создания:** 2025-10-15
**Статус:** Планирование
**Оценка:** 11-14 рабочих дней

## Обзор

Замена статической таблицы `AdditionalInfo` в формах редактирования документов на современный Vue 3 компонент с использованием паттерна workspace (аналогично statusbar/configurator).

**Затронутые файлы:**
- `/TN_Doc/wwwroot/HTML/DocEdit.html` - базовая форма
- `/TN_Doc/wwwroot/HTML/DocEditAct.html` - форма с валидацией
- `/TN_Doc/wwwroot/HTML/DocEditPassport.html` - форма с расширенной логикой

**Важно:** Таблица `Edit` (методики) остается без изменений, рефакторинг в будущем.

## Архитектура решения

### Структура проекта (паттерн statusbar/configurator)

```
TN_Doc/Client/
├── statusbar/              # Существующий workspace
├── configurator/           # Существующий workspace
├── form-editor/            # НОВЫЙ workspace
│   ├── src/
│   │   ├── components/
│   │   │   ├── AdditionalInfo.vue       # Основной компонент таблицы
│   │   │   ├── FormField.vue            # Input/Textarea поля
│   │   │   ├── SelectField.vue          # Select с автозаполнением
│   │   │   └── ValidationTooltip.vue    # Tooltips (PrimeVue)
│   │   ├── composables/
│   │   │   ├── useValidation.ts         # Логика валидации
│   │   │   └── useUserChange.ts         # Автозаполнение из справочников
│   │   ├── stores/
│   │   │   ├── formStore.ts             # Состояние формы (Pinia)
│   │   │   └── dictionaryStore.ts       # Справочники (Users, Licenses)
│   │   ├── types/
│   │   │   ├── field.types.ts           # Типы полей и конфигурации
│   │   │   └── validation.types.ts      # Типы валидации
│   │   ├── utils/
│   │   │   └── idGenerator.ts           # Генерация уникальных ID
│   │   ├── App.vue                      # Root компонент
│   │   └── main.ts                      # Entry point
│   ├── package.json                     # Dependencies
│   ├── vite.config.ts                   # Build config
│   ├── tsconfig.json                    # TypeScript config
│   └── index.html                       # Dev template
├── shared/                 # Общие утилиты (переиспользование)
└── package.json            # Root workspace config
```

### Build output

```
TN_Doc/wwwroot/
├── statusbar/
├── configurator/
└── form-editor/            # Новый output
    ├── main.js
    ├── style.css
    └── assets/
```

### Технологический стек

- **Vue 3.4+** - реактивный фреймворк
- **TypeScript** - типобезопасность
- **PrimeVue 4.2+** - UI компоненты (Tooltip)
- **Pinia** - state management
- **Vite** - сборщик и dev-сервер

## Этапы реализации

### Этап 1: Создание workspace form-editor

**Задачи:**
- [x] Создать директорию `TN_Doc/Client/form-editor/`
- [x] Скопировать конфигурацию из statusbar (package.json, vite.config.ts, tsconfig.json)
- [x] Установить зависимости: Vue 3, PrimeVue, Pinia, TypeScript
- [x] Настроить output в `../../wwwroot/form-editor/`
- [x] Добавить скрипты в root `package.json`
- [x] Добавить workspace в workspaces array

**package.json (form-editor):**
```json
{
  "name": "form-editor",
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vue-tsc && vite build",
    "type-check": "vue-tsc --noEmit"
  },
  "dependencies": {
    "vue": "^3.4.21",
    "primevue": "^4.2.0",
    "pinia": "^2.1.7"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^5.0.4",
    "typescript": "^5.2.2",
    "vite": "^5.2.0",
    "vue-tsc": "^2.0.6"
  }
}
```

**vite.config.ts:**
```typescript
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': resolve(__dirname, './src')
    }
  },
  build: {
    outDir: '../../wwwroot/form-editor',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        manualChunks: undefined
      }
    }
  }
})
```

**Root package.json (обновить scripts):**
```json
{
  "scripts": {
    "dev:form-editor": "npm run dev --workspace=form-editor",
    "build:form-editor": "npm run build --workspace=form-editor",
    "build:all": "npm run build && npm run build:configurator && npm run build:form-editor",
    "type-check": "npm run type-check --workspaces"
  },
  "workspaces": [
    "statusbar",
    "configurator",
    "form-editor"
  ]
}
```

**Время:** 1 день

---

### Этап 2: Базовый компонент (DocEdit.html)

**Задачи:**
- [x] Создать `main.ts` с инициализацией Vue + Pinia
- [x] Создать `App.vue` как контейнер
- [x] Реализовать `AdditionalInfo.vue` с рендерингом таблицы
- [x] Создать `FormField.vue` для input/textarea
- [x] Создать `SelectField.vue` с событием change
- [x] Реализовать `useUserChange.ts` для автозаполнения
- [x] Создать stores (formStore, dictionaryStore)
- [x] Реализовать типы (field.types.ts)

**Ключевые компоненты:**

**main.ts:**
```typescript
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Tooltip from 'primevue/tooltip'
import App from './App.vue'

// Глобальная инициализация
window.initFormEditor = (config: FormEditorConfig) => {
  const app = createApp(App, { config })
  const pinia = createPinia()

  app.use(pinia)
  app.use(PrimeVue)
  app.directive('tooltip', Tooltip)

  app.mount('#form-editor-root')
}
```

**App.vue:**
```vue
<template>
  <AdditionalInfo :config="config" />
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import AdditionalInfo from './components/AdditionalInfo.vue'
import { useFormStore } from './stores/formStore'
import { useDictionaryStore } from './stores/dictionaryStore'

const props = defineProps<{
  config: FormEditorConfig
}>()

onMounted(() => {
  const formStore = useFormStore()
  const dictStore = useDictionaryStore()

  formStore.initFields(props.config.fields, props.config.data)
  dictStore.init(props.config.dictionaries)

  if (props.config.invalidChars) {
    formStore.setInvalidChars(props.config.invalidChars)
  }
})
</script>
```

**AdditionalInfo.vue:**
```vue
<template>
  <table id="AdditionalInfo">
    <tbody>
      <tr v-for="field in fields" :key="field.name">
        <td>{{ field.label }}</td>
        <td>
          <SelectField
            v-if="field.type === 'select'"
            v-model="formData[field.name]"
            :field="field"
          />
          <FormField
            v-else
            v-model="formData[field.name]"
            :field="field"
          />
        </td>
      </tr>
    </tbody>
  </table>
</template>

<script setup lang="ts">
import { storeToRefs } from 'pinia'
import FormField from './FormField.vue'
import SelectField from './SelectField.vue'
import { useFormStore } from '../stores/formStore'

const formStore = useFormStore()
const { fields, formData } = storeToRefs(formStore)
</script>
```

**useUserChange.ts:**
```typescript
import { useFormStore } from '../stores/formStore'
import { useDictionaryStore } from '../stores/dictionaryStore'

export function useUserChange() {
  const formStore = useFormStore()
  const dictStore = useDictionaryStore()

  const handleUserChange = (selectElement: HTMLSelectElement, field: FieldConfig) => {
    const userId = Number(selectElement.value)

    if (field.name === 'Delive_IOF') {
      const user = dictStore.users.find(u => u.Id === userId)
      if (user) {
        formStore.updateField('Delive_Post', user.Post || '')
        formStore.updateField('Delive_Factory', user.Factory || '')
      }
    } else if (field.name === 'Receive_IOF') {
      const user = dictStore.users.find(u => u.Id === userId)
      if (user) {
        formStore.updateField('Receive_Post', user.Post || '')
        formStore.updateField('Receive_Factory', user.Factory || '')
      }
    }
  }

  return { handleUserChange }
}
```

**Время:** 2-3 дня

---

### Этап 3: Валидация и tooltips (DocEditAct.html)

**Задачи:**
- [x] Реализовать `useValidation.ts` composable
- [x] Добавить валидацию в FormField и SelectField
- [x] Интегрировать PrimeVue Tooltip
- [x] Реализовать `postMessage` для ButtonSaveOn/ButtonSaveOff
- [x] Добавить стили валидации (correct-value, incorrect-value)
- [x] Реализовать стили manual-input--disabled

**useValidation.ts:**
```typescript
export interface ValidationResult {
  valid: boolean
  message?: string
}

export interface ValidationRules {
  required?: boolean
  invalidChars?: string[]
  roundValue?: number
}

export function useValidation(invalidChars: string[] = []) {
  const checkEmpty = (value: string, required: boolean): ValidationResult => {
    if (!required) return { valid: true }
    if (!value || value.trim() === '') {
      return { valid: false, message: 'Поле должно быть заполнено!' }
    }
    return { valid: true }
  }

  const checkInvalidChars = (value: string): ValidationResult => {
    if (!value || invalidChars.length === 0) return { valid: true }

    for (const char of invalidChars) {
      if (value.includes(char)) {
        return { valid: false, message: `Некорректный символ: ${char}` }
      }
    }
    return { valid: true }
  }

  const validate = (value: string, rules: ValidationRules): ValidationResult => {
    const emptyCheck = checkEmpty(value, rules.required || false)
    if (!emptyCheck.valid) return emptyCheck

    return checkInvalidChars(value)
  }

  return { validate }
}
```

**FormField с валидацией:**
```vue
<template>
  <component
    :is="componentType"
    v-model="internalValue"
    :id="fieldId"
    :name="field.name"
    :disabled="field.disabled"
    :class="validationClass"
    @input="handleInput"
    v-tooltip="tooltipConfig"
  />
</template>

<script setup lang="ts">
const validationClass = computed(() => ({
  'correct-value': validationResult.value.valid,
  'incorrect-value': !validationResult.value.valid,
  'manual-input--disabled': props.field.disabled
}))

const tooltipConfig = computed(() => {
  if (!validationResult.value.valid && validationResult.value.message) {
    return {
      value: validationResult.value.message,
      disabled: false
    }
  }
  return { disabled: true }
})

const notifyParentWindow = () => {
  const allValid = formStore.isAllFieldsValid
  window.top?.postMessage(
    allValid ? 'ButtonSaveOn' : 'ButtonSaveOff',
    '*'
  )
}
</script>
```

**Время:** 2 дня

---

### Этап 4: Логика Passport (DocEditPassport.html)

**Задачи:**
- [x] Расширить валидацию для числовых полей с округлением
- [x] Добавить обработку для Laboratory_IOF в useUserChange
- [x] Реализовать генерацию уникальных ID (utils/idGenerator.ts)
- [x] Обработка ELIS данных (если применимо к AdditionalInfo)

**Расширенная валидация:**
```typescript
export function useValidation(invalidChars: string[] = []) {
  // ... предыдущий код ...

  const checkRounding = (value: string, roundValue?: number): ValidationResult => {
    if (!roundValue || roundValue < 1) return { valid: true }

    const pattern = `^-?[0-9]+(?:\\.[0-9]{1,${roundValue}})?$`
    if (value.match(new RegExp(pattern, 'g'))) {
      return { valid: true }
    }
    return {
      valid: false,
      message: 'Кол-во знаков после запятой не соответствует заданным правилам!'
    }
  }

  const validateNumber = (value: string, rules: ValidationRules): ValidationResult => {
    const emptyCheck = checkEmpty(value, rules.required || false)
    if (!emptyCheck.valid) return emptyCheck

    return checkRounding(value, rules.roundValue)
  }

  return { validate, validateNumber }
}
```

**idGenerator.ts:**
```typescript
let idCounter = 0

export function generateUniqueId(prefix: string): string {
  idCounter++
  const timestamp = Date.now()
  const random = Math.random().toString(36).substr(2, 9)
  return `${prefix}_${timestamp}_${idCounter}_${random}`
}

export function resetIdCounter() {
  idCounter = 0
}
```

**useUserChange расширенный:**
```typescript
if (field.name === 'Laboratory_IOF') {
  const user = dictStore.users.find(u => u.Id === userId)
  if (user) {
    formStore.updateField('Laboratory_Post', user.Post || '')
    formStore.updateField('Laboratory_Factory', user.Factory || '')
  }
}
```

**Время:** 1-2 дня

---

### Этап 5: Обновление C# кода

**Задачи:**
- [ ] Создать базовый класс `FormEditorConfigBuilder` в TN.DocGeneral
- [ ] Создать класс `FieldConfig` для конфигурации полей
- [ ] Обновить методы `GetEditDoc` во всех документных библиотеках:
  - [ ] Act
  - [ ] Passport
  - [ ] Report
  - [ ] Jornal
  - [ ] Poverka* (все 21 библиотека)
  - [ ] KMH* (все 14 библиотек)
- [ ] Создать метод `GenerateVueFormHtml` для генерации HTML с Vue
- [ ] Удалить старые методы генерации HTML таблиц

**FormEditorConfigBuilder.cs:**
```csharp
// TN.DocGeneral/Services/FormEditorConfigBuilder.cs
namespace TN.DocGeneral.Services
{
    public class FormEditorConfigBuilder
    {
        public object BuildConfig(
            IEnumerable<FieldConfig> fields,
            object data,
            object dictionaries,
            string[] invalidChars = null)
        {
            return new
            {
                fields = fields,
                data = data,
                dictionaries = dictionaries,
                invalidChars = invalidChars ?? Array.Empty<string>()
            };
        }
    }

    public class FieldConfig
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Type { get; set; } // "text", "number", "select", "textarea"
        public bool Required { get; set; }
        public bool Disabled { get; set; }
        public int? RoundValue { get; set; } // Для числовых полей
        public string[] AutoFill { get; set; } // Поля для автозаполнения
    }
}
```

**Пример GetEditDoc (Act):**
```csharp
public string GetEditDoc(int id)
{
    try
    {
        var doc = GetDocumentData(id);
        var configBuilder = new FormEditorConfigBuilder();

        var fields = new[]
        {
            new FieldConfig
            {
                Name = "Delive_IOF",
                Label = "Сдал (ФИО)",
                Type = "select",
                Required = true,
                AutoFill = new[] { "Delive_Factory", "Delive_FIO", "Delive_Lic_Date", "Delive_Lic_Number" }
            },
            new FieldConfig
            {
                Name = "Delive_Factory",
                Label = "Предприятие (сдал)",
                Type = "text",
                Required = true,
                Disabled = true
            },
            new FieldConfig
            {
                Name = "Delive_FIO",
                Label = "ФИО (сдал)",
                Type = "text",
                Required = true,
                Disabled = true
            },
            // ... остальные поля
        };

        var dictionaries = new
        {
            Users = GetUsers(),
            Licenses = GetLicenses()
        };

        var config = configBuilder.BuildConfig(fields, doc, dictionaries, GetInvalidChars());
        var configJson = JsonConvert.SerializeObject(config);

        string htmlPath = Path.Combine(_wwwrootPath, "HTML", "html.html");
        var html = GenerateVueFormHtml(configJson);
        File.WriteAllText(htmlPath, html);

        _logger.Trace($"HTML форма документа {IdDoc} (id={id}) сохранена: {htmlPath}");

        return html;
    }
    catch (Exception ex)
    {
        _logger.Error(ex, $"Ошибка при создании формы редактирования документа Act (id={id})");
        throw;
    }
}

private string GenerateVueFormHtml(string configJson)
{
    return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Редактирование документа</title>
    <link rel=""stylesheet"" href=""/form-editor/style.css"">
    <link rel=""stylesheet"" href=""/css/commonEditForm.css"">
</head>
<body>
    <div id=""form-editor-root""></div>

    <script type=""module"" src=""/form-editor/main.js""></script>
    <script>
        window.addEventListener('DOMContentLoaded', () => {{
            if (window.initFormEditor) {{
                window.initFormEditor({HttpUtility.JavaScriptStringEncode(configJson, true)});
            }}
        }});
    </script>
</body>
</html>";
}
```

**Документные библиотеки для обновления (42 штуки):**

**Core (4):**
- Act
- Passport
- Report
- Jornal

**Poverka (21):**
- Poverka1974, Poverka1974_04, Poverka1974_89, Poverka1974_95
- Poverka2816
- Poverka3151, Poverka3189
- Poverka3265_PR_PU, Poverka3265_UPR_PR, Poverka3265_UPR_PU
- Poverka3266, Poverka3267, Poverka3272
- Poverka3287, Poverka3288
- Poverka3312_PR_PU, Poverka3312_UPR_PR
- Poverka3380
- PoverkaSikn425_PR_PR, PoverkaSikn425_PR_PU

**KMH (14):**
- KMH3265_PR_PU, KMH3265_UPR_PR
- KMH3288_MPR_TPR
- KMH3312_PR_PU, KMH3312_UPR_PR
- KMH_MI2816
- KMH_MPR_MPR, KMH_MPR_PU, KMH_MPR_TPR
- KMH_PP, KMH_PP_Areom
- KMH_PR_PR, KMH_PR_PU
- KMH_PV, KMH_PW
- KMX_Sikn425_PR_PR, KMX_Sikn425_PR_PU

**Common (3):**
- CommonPoverka1974
- CommonSikn425

**Важно:** Каждая библиотека требует:
1. Определения конфигурации полей специфичной для документа
2. Обновления метода GetEditDoc
3. Удаления старого кода генерации HTML

**Время:** 2-3 дня

---

### Этап 6: Удаление legacy кода

**Задачи:**
- [ ] Создать backup старых HTML файлов
- [ ] Удалить `<script id="ElementChangeEventHandling">` из HTML
- [ ] Удалить jQuery UI инициализацию
- [ ] Удалить funcOnLoad(), UserChangeEvent(), InputStringCheck() и др.
- [ ] Очистить `/js/EditDoc.js` от неиспользуемых функций
- [ ] Удалить inline стили (перенести в CSS если нужно)

**Backup команды:**
```bash
cd TN_Doc/wwwroot/HTML
mkdir backup_$(date +%Y%m%d)
cp DocEdit*.html backup_$(date +%Y%m%d)/
```

**Удаляемые функции из HTML:**
- `funcOnLoad()`
- `UserChangeEvent(selectObject)`
- `InputStringCheck(Object)`
- `CheckEmpty(Object)`
- `CheckInvalidChars(element)`
- `CheckRounding(object)` (Passport)
- `SendingMessageStateButtonSave()`
- `updateTooltip(element)`
- `TogglePrintCellEditable(selectElement)` (Passport, только для AdditionalInfo)
- `calculatePrintValue(parameterKey, metod)` (Passport)
- `updatePrintValueOnHalChange(halInput)` (Passport)
- `generateUniqueId(prefix)` (Passport)

**Удаляемые зависимости:**
- jQuery UI tooltip (`<link rel="stylesheet" href="/lib/jquery-ui-1.13.1.custom/jquery-ui.min.css">`)
- jQuery UI script (`<script src="/lib/jquery-ui-1.13.1.custom/jquery-ui.min.js"></script>`)

**Примечание:** jQuery может остаться если используется в других частях (например, таблица Edit), но только базовый jquery.min.js

**Время:** 1 день

---

### Этап 7: Тестирование

**Задачи:**
- [ ] Функциональное тестирование всех трех типов форм
- [ ] Проверка автозаполнения при выборе пользователя
- [ ] Проверка валидации (пустые поля, недопустимые символы, округление)
- [ ] Проверка tooltips при ошибках валидации
- [ ] Проверка отправки сообщений ButtonSaveOn/ButtonSaveOff
- [ ] Проверка disabled полей
- [ ] Тестирование на реальных данных из БД
- [ ] Проверка работы с разными типами документов
- [ ] Регрессионное тестирование сохранения данных

**Тест-кейсы:**

**TC-1: Загрузка формы**
1. Открыть форму редактирования документа Act
2. Проверить загрузку всех полей с данными из БД
3. Проверить корректное отображение select с пользователями
4. Повторить для Passport и Report

**TC-2: Автозаполнение (Delive_IOF)**
1. Открыть форму Act
2. Выбрать пользователя в поле "Сдал (ФИО)"
3. Проверить автозаполнение: Delive_Factory, Delive_FIO, Delive_Lic_Date, Delive_Lic_Number
4. Проверить что поля стали disabled

**TC-3: Автозаполнение (Receive_IOF)**
1. Открыть форму Act
2. Выбрать пользователя в поле "Принял (ФИО)"
3. Проверить автозаполнение: Receive_Factory, Receive_FIO, Receive_Lic_Date, Receive_Lic_Number
4. Проверить что поля стали disabled

**TC-4: Валидация пустых полей**
1. Открыть форму Act
2. Очистить обязательное поле
3. Проверить появление класса "incorrect-value"
4. Проверить появление tooltip с сообщением "Поле должно быть заполнено!"
5. Проверить отправку postMessage "ButtonSaveOff"

**TC-5: Валидация недопустимых символов**
1. Открыть форму Act
2. Ввести недопустимый символ в текстовое поле
3. Проверить появление tooltip с сообщением "Некорректный символ: X"
4. Проверить класс "incorrect-value"
5. Проверить отправку postMessage "ButtonSaveOff"

**TC-6: Валидация округления (Passport)**
1. Открыть форму Passport
2. Ввести число с большим количеством знаков после запятой
3. Проверить tooltip "Кол-во знаков после запятой не соответствует заданным правилам!"
4. Ввести корректное число
5. Проверить исчезновение ошибки

**TC-7: Корректное заполнение**
1. Открыть форму любого типа
2. Заполнить все обязательные поля корректно
3. Проверить что все поля имеют класс "correct-value"
4. Проверить отправку postMessage "ButtonSaveOn"

**TC-8: Сохранение данных**
1. Открыть форму, изменить данные
2. Сохранить форму
3. Проверить сохранение в БД
4. Открыть форму снова
5. Проверить что данные загрузились корректно

**TC-9: Laboratory_IOF (Passport)**
1. Открыть форму Passport
2. Выбрать пользователя в поле "Лаборатория"
3. Проверить автозаполнение Laboratory_Post и Laboratory_Factory

**TC-10: Disabled поля**
1. Открыть любую форму с disabled полями
2. Проверить класс "manual-input--disabled"
3. Проверить что поле неактивно для редактирования
4. Проверить серый фон (CSS переменная --disabled-bg-color)

**TC-11: Регрессия - разные типы документов**
1. Протестировать формы для разных типов документов:
   - Act (несколько ID)
   - Passport (несколько ID)
   - Report (несколько ID)
   - Jornal
   - Poverka* (минимум 3 разных)
   - KMH* (минимум 3 разных)
2. Проверить что все формы работают корректно

**TC-12: Hot reload во время разработки**
1. Запустить `npm run dev:form-editor`
2. Внести изменения в компонент
3. Проверить автоматическое обновление в браузере

**Критерии приемки:**
- ✅ Все тест-кейсы проходят успешно
- ✅ Нет регрессий в функциональности
- ✅ Валидация работает идентично старой версии
- ✅ Автозаполнение работает корректно
- ✅ Tooltips отображаются при ошибках
- ✅ Данные сохраняются в БД корректно
- ✅ Нет ошибок в консоли браузера
- ✅ Нет ошибок в логах сервера

**Время:** 2 дня

---

## Оценка трудозатрат

| Этап | Описание | Время |
|------|----------|-------|
| 1 | Workspace setup | 1 день |
| 2 | Базовый компонент (DocEdit) | 2-3 дня |
| 3 | Валидация (DocEditAct) | 2 дня |
| 4 | Passport логика | 1-2 дня |
| 5 | C# обновление (42 библиотеки) | 2-3 дня |
| 6 | Удаление legacy | 1 день |
| 7 | Тестирование | 2 дня |
| **Итого** | | **11-14 дней** |

## Преимущества решения

✅ **Консистентность** - использует паттерн statusbar/configurator
✅ **Современный стек** - Vue 3 + TypeScript + PrimeVue
✅ **Изолированная разработка** - hot reload, отдельный dev-сервер
✅ **Типобезопасность** - TypeScript для всех компонентов
✅ **Централизованная валидация** - единая логика в composables
✅ **Переиспользование** - shared утилиты из монорепо
✅ **Удаление jQuery UI** - современные PrimeVue tooltips
✅ **Реактивность** - автоматическое обновление UI
✅ **Тестируемость** - легко писать unit тесты

## Риски и митигация

### Риск 1: Большое количество документных библиотек (42 штуки)

**Вероятность:** Высокая
**Влияние:** Среднее

**Митигация:**
- Создать шаблонный код для GetEditDoc
- Начать с простых документов (Act, Report)
- Автоматизировать генерацию FieldConfig там где возможно

### Риск 2: Потеря функциональности при портировании

**Вероятность:** Средняя
**Влияние:** Высокое

**Митигация:**
- Детальное тестирование каждого этапа
- Функциональные тесты для критичных сценариев
- Backup старых HTML файлов
- Возможность отката через git

### Риск 3: Проблемы с интеграцией старых компонентов

**Вероятность:** Низкая
**Влияние:** Среднее

**Митигация:**
- Сохранить EditDoc.js для других функций
- Изолировать Vue-компонент от остального кода
- Тестировать интеграцию на каждом этапе

### Риск 4: Увеличение bundle size

**Вероятность:** Низкая
**Влияние:** Низкое

**Митигация:**
- Использовать tree-shaking в Vite
- Минимизировать зависимости
- Переиспользовать PrimeVue из других workspace

## Следующие шаги

1. **Ревью плана** - обсудить с командой
2. **Создание задач** - разбить на подзадачи в трекере
3. **Старт Этапа 1** - создание workspace
4. **Регулярные демо** - показывать прогресс после каждого этапа

## Примечания

- Таблица `Edit` (методики Passport) остается без изменений
- Рефакторинг таблицы `Edit` запланирован на следующую итерацию
- jQuery остается для других частей приложения (если используется)
- Старые HTML файлы будут сохранены в backup директории

---

**Последнее обновление:** 2025-10-15
**Автор:** Команда разработки TN_Doc
**Версия:** 1.0
