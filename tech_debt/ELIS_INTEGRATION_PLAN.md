# План интеграции ELIS в Vue Document Editor

**Дата создания**: 2025-11-06
**Статус**: В разработке
**Приоритет**: Высокий
**Версия**: 1.4.3+

## Контекст

В проекте TN_Doc уже реализована интеграция с системой ELIS (Единая Лабораторная Информационная Система) для старого HTML-редактора паспортов качества. Необходимо перенести эту функциональность на новый Vue Document Editor.

### Текущее состояние

#### ✅ Что уже готово (реализовано)

1. **Vue компоненты с поддержкой ELIS**
   - ✅ `useElisIntegration.ts` - композабл для получения данных через postMessage
   - ✅ `elis.types.ts` - TypeScript типы для данных ELIS
   - ✅ `DocumentPassportEditor.vue` - интегрирован useElisIntegration
   - ✅ `FormField.vue` - поддержка пропа `elisFilled` и CSS подсветки
   - ✅ `PassportParameterRow.vue` - условная подсветка ячеек таблицы
   - ✅ CSS стили для зеленой подсветки полей, заполненных из ELIS

2. **Типы данных**
   - ✅ `elisAlias` добавлен в `FormField` интерфейс
   - ✅ `elisAlias` добавлен в `PassportQualityParameterSchema`
   - ✅ Полная типизация данных ELIS (ElisPassportData, ElisParameter, ElisLabInfo)

3. **Обновлено главное окно**
   - ✅ `Common.js` - функция `FillPassportDataElis()` проверяет Vue редактор
   - ✅ Отправка данных через postMessage вместо прямого DOM манипулирования
   - ✅ Обратная совместимость с legacy HTML редактором

4. **Документация**
   - ✅ `ELIS_INTEGRATION.md` - подробная документация для разработчиков

#### ⚠️ Что нужно доработать

### Этап 0: Доработка компонента FormField (универсальная подсветка)

**Цель**: Добавить универсальный механизм подсветки фона элементов формы без привязки к конкретной бизнес-логике (ELIS).

**Приоритет**: Критический (блокирует Этап 1 и 2)

#### 0.1 Добавить опциональный проп `highlightColor` в FormField.vue

**Изменения в интерфейсе компонента:**

```typescript
// TN_Doc/Client/document-editor/src/components/FormField.vue

const props = defineProps<{
  field: FormField;
  modelValue: any;
  hideLabel?: boolean;
  invalidChars?: string[];
  highlightColor?: string;  // ← НОВЫЙ универсальный проп
}>();
```

**Описание:**
- `highlightColor` - опциональный проп для управления цветом фона элемента
- Принимает любое валидное CSS значение цвета: HEX, RGB, CSS переменную
- Примеры: `"#d4edda"`, `"rgb(212, 237, 218)"`, `"var(--md-elis-highlight)"`
- Если не передан - используется стандартный белый фон
- **Универсальность**: может использоваться не только для ELIS, но и для других случаев:
  - Подсветка автоматически заполненных полей
  - Подсветка изменённых полей
  - Подсветка полей с предупреждениями
  - Подсветка выбранных/сфокусированных полей

**Задачи:**
- [ ] Добавить проп `highlightColor?: string` в defineProps
- [ ] Добавить computed свойство для расчёта inline стилей
- [ ] Применить стили через `:style` binding на `.field-control`
- [ ] Убедиться, что стили применяются ко всем типам полей (text, number, select, date, datetime-local)
- [ ] Добавить поддержку для disabled состояния (приглушённый фон)

#### 0.2 Реализовать CSS стили с поддержкой highlightColor

**Подход**: Использовать inline стили вместо CSS классов для гибкости

```vue
<template>
  <div class="form-field" :class="{ compact: hideLabel }">
    <!-- ... label ... -->

    <!-- Применяем highlightColor через :style -->
    <InputText
      v-if="field.type === 'text'"
      :id="field.key"
      v-model="localValue"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      :style="fieldBackgroundStyle"
      class="field-control"
      @input="handleChange"
    />

    <!-- Аналогично для других типов полей -->
  </div>
</template>

<script setup lang="ts">
// Computed свойство для стилей фона
const fieldBackgroundStyle = computed(() => {
  if (!props.highlightColor) return {};

  return {
    backgroundColor: props.highlightColor,
    transition: 'background-color 0.2s ease-in-out'
  };
});
</script>
```

**Задачи:**
- [ ] Создать computed свойство `fieldBackgroundStyle`
- [ ] Применить `:style="fieldBackgroundStyle"` на все input компоненты
- [ ] Убедиться, что transition работает плавно
- [ ] Проверить, что стили не конфликтуют с `:disabled` состоянием
- [ ] Проверить, что стили не конфликтуют с `.p-invalid` состоянием

#### 0.3 Добавить поддержку для select (PrimeVue Select)

**Особенность**: PrimeVue Select использует вложенные элементы, нужно применить стили к `.p-select`

```vue
<!-- Select с поддержкой highlightColor -->
<Select
  v-if="field.type === 'select'"
  :id="field.key"
  v-model="localValue"
  :options="validSelectOptions"
  optionLabel="label"
  optionValue="value"
  :disabled="!field.editable"
  :class="{ 'p-invalid': !isValid }"
  :style="fieldBackgroundStyle"
  class="field-control"
  @change="handleChange"
/>
```

**Задачи:**
- [ ] Применить `:style="fieldBackgroundStyle"` на компонент `Select`
- [ ] Проверить, что стили применяются к `.p-select` контейнеру
- [ ] Убедиться, что dropdown (выпадающий список) не наследует подсветку
- [ ] Проверить работу с disabled состоянием

#### 0.4 Добавить поддержку для DatePicker

```vue
<!-- DatePicker с поддержкой highlightColor -->
<DatePicker
  v-if="field.type === 'date'"
  :id="field.key"
  v-model="localValue"
  :disabled="!field.editable"
  :class="{ 'p-invalid': !isValid }"
  :style="fieldBackgroundStyle"
  dateFormat="dd.mm.yy"
  class="field-control"
  @update:modelValue="handleChange"
/>
```

**Задачи:**
- [ ] Применить `:style="fieldBackgroundStyle"` на компонент `DatePicker`
- [ ] Проверить, что стили применяются к input внутри DatePicker
- [ ] Убедиться, что календарь (popup) не наследует подсветку
- [ ] Проверить работу с disabled состоянием

#### 0.5 Добавить поддержку для InputNumber

```vue
<!-- InputNumber с поддержкой highlightColor -->
<InputNumber
  v-if="field.type === 'number'"
  :id="field.key"
  v-model="localValue"
  :disabled="!field.editable"
  :class="{ 'p-invalid': !isValid }"
  :style="fieldBackgroundStyle"
  class="field-control"
  @input="handleChange"
/>
```

**Задачи:**
- [ ] Применить `:style="fieldBackgroundStyle"` на компонент `InputNumber`
- [ ] Проверить, что стили применяются к input внутри InputNumber
- [ ] Убедиться, что кнопки +/- не имеют подсветку
- [ ] Проверить работу с disabled состоянием

#### 0.6 Тестирование безопасности изменений FormField

**Критично**: Убедиться, что изменения не сломали существующее поведение в других редакторах.

**Тестовые сценарии:**

1. **DocumentEditor.vue** (общий редактор, без highlightColor):
   - [ ] Открыть любой документ типа Report/Journal
   - [ ] Проверить, что все поля рендерятся корректно
   - [ ] Проверить, что фон полей белый (стандартный)
   - [ ] Проверить работу валидации
   - [ ] Проверить работу disabled полей
   - [ ] Проверить работу select, date, number полей

2. **DocumentActEditor.vue** (редактор актов, без highlightColor):
   - [ ] Открыть документ типа Act
   - [ ] Проверить, что все поля рендерятся корректно
   - [ ] Проверить работу автозаполнения (useActAutoFill)
   - [ ] Проверить, что фон полей белый (стандартный)
   - [ ] Проверить работу всех типов полей

3. **DocumentPassportEditor.vue** (с highlightColor для ELIS):
   - [ ] Открыть документ типа Passport
   - [ ] Проверить, что поля без highlightColor имеют белый фон
   - [ ] Применить данные ELIS
   - [ ] Проверить, что поля с highlightColor имеют зелёный фон
   - [ ] Изменить поле вручную
   - [ ] Проверить, что highlightColor сбрасывается

**Команды для тестирования:**
```bash
# Запустить dev сервер
cd TN_Doc/Client
npm run dev:editor

# В браузере проверить все 3 редактора:
# http://localhost:5175/editor/1/Report/123
# http://localhost:5175/editor/1/Act/456
# http://localhost:5175/editor/1/Passport/789
```

#### 0.7 Обновить документацию компонента FormField

**Файл**: `TN_Doc/Client/document-editor/src/components/FormField.vue`

**Задачи:**
- [ ] Добавить JSDoc комментарий для пропа `highlightColor`
- [ ] Описать примеры использования в комментариях
- [ ] Добавить примеры валидных значений цвета

**Пример документации:**
```typescript
/**
 * Универсальный компонент для отображения полей формы
 *
 * @prop {FormField} field - Конфигурация поля
 * @prop {any} modelValue - Значение поля
 * @prop {boolean} [hideLabel] - Скрыть label
 * @prop {string[]} [invalidChars] - Список недопустимых символов
 * @prop {string} [highlightColor] - Цвет подсветки фона элемента
 *
 * @example
 * // Стандартное использование (белый фон)
 * <FormField :field="field" :modelValue="value" />
 *
 * @example
 * // С подсветкой зелёным (ELIS данные)
 * <FormField
 *   :field="field"
 *   :modelValue="value"
 *   highlightColor="var(--md-elis-highlight)"
 * />
 *
 * @example
 * // С подсветкой жёлтым (предупреждение)
 * <FormField
 *   :field="field"
 *   :modelValue="value"
 *   highlightColor="#fff3cd"
 * />
 */
```

#### 0.8 Обновить типы FormField (опционально)

**Файл**: `TN_Doc/Client/document-editor/src/types/document.types.ts`

Если нужно добавить `highlightColor` в интерфейс `FormField`:

```typescript
export interface FormField {
  key: string;
  label: string;
  type: 'text' | 'number' | 'select' | 'date' | 'datetime-local';
  tag?: string;
  required?: boolean;
  editable?: boolean;
  options?: SelectOption[];
  roundValue?: number;
  elisAlias?: string;
  highlightColor?: string;  // ← НОВОЕ (опционально)
}
```

**Примечание**: Этот шаг опционален, т.к. `highlightColor` может передаваться как отдельный проп, независимо от конфигурации поля.

**Задачи:**
- [ ] Обсудить, нужно ли добавлять `highlightColor` в интерфейс `FormField`
- [ ] Если да - обновить типы
- [ ] Если нет - оставить как отдельный проп компонента

#### 0.9 Критерии готовности Этапа 0

- [ ] Проп `highlightColor` добавлен в `FormField.vue`
- [ ] Computed свойство `fieldBackgroundStyle` реализовано
- [ ] Стили применены ко всем типам полей (text, number, select, date, datetime-local)
- [ ] Все 3 редактора протестированы (DocumentEditor, DocumentActEditor, DocumentPassportEditor)
- [ ] Нет регрессий в существующей функциональности
- [ ] Transition работает плавно
- [ ] Документация обновлена
- [ ] Code review пройден

**Время выполнения**: 0.5-1 день

---

### Этап 1: Проверка серверной конфигурации

**Цель**: Убедиться, что `ElisAlias` правильно передается из серверной конфигурации в Vue компонент.

#### 1.1 Проверить конфигурацию на сервере

```bash
# Файлы для проверки:
- TN_Doc/Cfg/CfgEditPassport.json
```

**Задачи**:
- [x] ✅ Проверить наличие атрибута `ElisAlias` в секции `AdditionalInfo` - **ПОДТВЕРЖДЕНО**
- [x] ✅ Проверить наличие атрибута `ElisAlias` в секции `Parameters` - **ПОДТВЕРЖДЕНО**
- [ ] Убедиться, что ключи `ElisAlias` соответствуют данным из ELIS API
- [x] ✅ Документировать формат `ElisAlias` - **это массив строк `["key1", "key2"]`**
- [ ] Проверить, что устаревшее поле `KeyELIS` не используется (оставлено для совместимости)

**⚠️ ВАЖНО: Реальный формат конфигурации**:

`ElisAlias` это **массив строк** (не строка с разделителем `|`):
```json
"ElisAlias": ["key1", "key2"]  // ← НЕ "key1|key2"
```

**Смешанная номенклатура ключей:**
- **AdditionalInfo**: использует **camelCase** (`labName`, `accreditationNumber`)
- **Parameters**: использует **русские полные названия** (`"Массовая доля воды(%)"`)

**Пример реальной структуры (из CfgEditPassport_GOSTR50.2.040(I).json)**:
```json
{
  "AdditionalInfo": [
    {
      "Key": "Laboratory",
      "Type": "text",
      "Name": "Лаборатория предприятия",
      "ElisAlias": ["labName"]
    },
    {
      "Key": "Laboratory_IOF",
      "Type": "list",
      "Name": "Представитель испытательной лаборатории (ИОФ)",
      "ElisAlias": ["chiefLabShortSign"]
    },
    {
      "Key": "PassportPeriodDT.Begin",
      "Type": "datetime-local",
      "Name": "Дата и время отбора пробы (начало)",
      "ElisAlias": ["startPeriodTime"]
    }
  ],
  "Parameters": [
    {
      "Key": "MassWaterFracCorrection",
      "Name": "Массовая доля воды, %",
      "ElisAlias": ["Массовая доля воды(%)"],
      "RequiredFill": true,
      "RoundValue": 2,
      "Edit": true
    },
    {
      "Key": "Chloride_Salts.MassFraction",
      "Name": "Массовая концентрация хлористых солей, %",
      "ElisAlias": [
        "Массовая концентрация хлористых солей(%)",
        "Массовая доля хлористых солей(%)"
      ],
      "Edit": true
    },
    {
      "Key": "Mass_fraction_of_hydrogen_sulfide",
      "Name": "Массовая доля сероводорода, млнˉ¹ (ppm)",
      "ElisAlias": [
        "Массовая доля сероводорода(млн⁻¹)",
        "Массовая доля сероводорода(млн⁻¹ (ppm))"
      ],
      "RequiredFill": true,
      "RoundValue": 1,
      "Edit": true
    }
  ]
}
```

**Механизм fallback:**
- Композабл `useElisIntegration` должен перебирать массив `ElisAlias`
- Использовать первое найденное значение из ELIS данных
- Логировать, какой именно алиас сработал

#### 1.2 Проверить десериализацию на сервере

**Файлы для проверки**:
- `TN_Doc/Controllers/HomeController.cs` - метод `GetDocEdit()`
- `tn.docgeneral/Passport/PassportConfig.cs` - класс конфигурации

**Задачи**:
- [ ] Убедиться, что `ElisAlias` правильно десериализуется из JSON как **массив строк**
- [ ] Проверить объявление свойства в C# классе:
  ```csharp
  public string[] ElisAlias { get; set; }  // ← массив, не string!
  ```
- [ ] Проверить, что `ElisAlias` передается в API ответе как массив
- [ ] Убедиться, что camelCase/PascalCase конвертация работает корректно:
  - Сервер: `ElisAlias` (PascalCase)
  - Клиент: `elisAlias` (camelCase)
- [ ] Проверить, что устаревшее поле `KeyELIS` не мешает (backward compatibility)

**Проверка через DevTools**:
```javascript
// В Vue DevTools проверить store.fields:
store.fields.forEach(field => {
  if (field.elisAlias) {
    console.log(`Field ${field.key} -> elisAlias:`, field.elisAlias);
    console.log(`  Type: ${Array.isArray(field.elisAlias) ? 'Array' : typeof field.elisAlias}`);
  }
});

// В Vue DevTools проверить qualityParametersSchema:
store.config.qualityParametersSchema.forEach(param => {
  if (param.elisAlias) {
    console.log(`Param ${param.key} -> elisAlias:`, param.elisAlias);
    console.log(`  Type: ${Array.isArray(param.elisAlias) ? 'Array' : typeof param.elisAlias}`);
  }
});
```

#### 1.3 Обновить TypeScript типы

**Файл**: `TN_Doc/Client/document-editor/src/types/document.types.ts`

**Задачи**:
- [ ] Изменить тип `elisAlias` с `string?` на `string[]?` в интерфейсе `FormField`
- [ ] Изменить тип `elisAlias` в интерфейсе `PassportQualityParameterSchema`
- [ ] Обновить JSDoc комментарии с примерами массива

**Ожидаемые изменения**:
```typescript
export interface FormField {
  key: string;
  label: string;
  type: 'text' | 'number' | 'select' | 'date' | 'datetime-local' | 'list';
  tag?: string;
  required?: boolean;
  editable?: boolean;
  options?: SelectOption[];
  roundValue?: number;
  elisAlias?: string[];  // ← массив, не строка!
  highlightColor?: string;
}

export interface PassportQualityParameterSchema {
  id: number;
  key: string;
  name: string;
  elisAlias?: string[];  // ← массив, не строка!
  requiredFill?: boolean;
  methodRequiredFill?: boolean;
  roundValue?: number;
  methods: PassportQualityMethod[];
}
```

#### 1.4 Обновить композабл useElisIntegration

**Файл**: `TN_Doc/Client/document-editor/src/composables/useElisIntegration.ts` (создать)

**Задачи**:
- [ ] Реализовать функцию `findElisValue()` для поиска по массиву `elisAlias`
- [ ] Учесть смешанную номенклатуру:
  - AdditionalInfo: искать в `elisData.labInfo.*`, `elisData.signers.*` (camelCase)
  - Parameters: искать в `elisData.parameters["Русское название"]`
- [ ] Логировать, какой из алиасов был найден (для отладки)
- [ ] Логировать предупреждения, если ни один алиас не найден

**Пример реализации функции поиска**:
```typescript
/**
 * Ищет значение в ELIS данных по массиву алиасов (fallback механизм)
 * @param elisData - данные из ELIS
 * @param elisAlias - массив возможных ключей
 * @param searchPath - путь для поиска ('labInfo', 'parameters', etc.)
 * @returns найденное значение или undefined
 */
function findElisValue(
  elisData: ElisPassportData,
  elisAlias?: string[],
  searchPath?: string
): any {
  if (!elisAlias || elisAlias.length === 0) return undefined;

  // Определить корневой объект для поиска
  const root = searchPath
    ? _.get(elisData, searchPath)
    : elisData;

  if (!root) {
    console.warn(`[ELIS] Путь поиска "${searchPath}" не найден в данных ELIS`);
    return undefined;
  }

  // Перебрать все алиасы
  for (const alias of elisAlias) {
    const value = root[alias];
    if (value !== undefined && value !== null) {
      console.log(`[ELIS] Найдено значение по алиасу "${alias}" в "${searchPath || 'root'}"`);
      return value;
    }
  }

  console.warn(`[ELIS] Не найдено значение для алиасов: [${elisAlias.join(', ')}] в "${searchPath || 'root'}"`);
  return undefined;
}

// Пример использования для AdditionalInfo
const labName = findElisValue(elisData, field.elisAlias, 'labInfo');

// Пример использования для Parameters
const waterContent = findElisValue(elisData, param.elisAlias, 'parameters');
```

### Этап 2: Тестирование интеграции

**Цель**: Проверить работу интеграции ELIS с реальными данными.

#### 2.1 Локальное тестирование

**Предварительные требования**:
- [ ] TN.ElisConnector запущен на `http://localhost:5050`
- [ ] Доступ к тестовому API ELIS
- [ ] Тестовое устройство с `isElisUsed: true`

**Сценарий тестирования**:

1. **Открыть редактор паспорта**
   ```
   URL: http://localhost:38509/
   Устройство: Выбрать устройство с ELIS
   Документ: Passport
   Создать новый или открыть существующий
   ```

2. **Запросить данные ELIS**
   - [ ] Нажать кнопку "ЕЛИС" в главном окне
   - [ ] Проверить, что отображается список протоколов испытаний
   - [ ] Выбрать протокол из списка
   - [ ] Проверить, что протокол сохранился в localStorage

3. **Применить данные ELIS**
   - [ ] Нажать кнопку "Применить"
   - [ ] Проверить в DevTools Console:
     ```javascript
     // Главное окно
     localStorage.getItem('dataPassport')
     localStorage.getItem('labInfo')
     ```
   - [ ] Проверить, что postMessage отправлен в iframe:
     ```javascript
     // Ожидаемое сообщение в логах:
     "Обнаружен Vue редактор, отправка данных ЕЛИС через postMessage"
     ```

4. **Проверить заполнение формы**
   - [ ] Проверить, что поля AdditionalInfo заполнены
   - [ ] Проверить, что поля имеют зеленый фон
   - [ ] Проверить, что качественные параметры заполнены:
     - Measurement (Измерение)
     - Method (Метод испытаний)
     - Result (Результат)
   - [ ] Проверить, что все заполненные поля имеют зеленый фон

5. **Проверить ручное редактирование**
   - [ ] Изменить вручную любое поле, заполненное из ELIS
   - [ ] Убедиться, что зеленый фон исчез
   - [ ] Убедиться, что флаг `__elisFilled` сброшен

6. **Проверить сохранение**
   - [ ] Нажать кнопку "Сохранить"
   - [ ] Убедиться, что данные сохранены на сервере
   - [ ] Убедиться, что флаги `__elisFilled` не сохраняются в базу данных

#### 2.2 Тестирование граничных случаев

**Сценарии**:

1. **Отсутствующие данные в ELIS**
   - [ ] Применить протокол с неполными данными
   - [ ] Убедиться, что форма заполнена частично
   - [ ] Проверить, что нет ошибок в консоли

2. **Несоответствие ElisAlias**
   - [ ] Применить данные с ключами, не совпадающими с ElisAlias
   - [ ] Убедиться, что поля остались пустыми
   - [ ] Проверить логи на наличие предупреждений

3. **Множественные алиасы через `|`**
   - [ ] Проверить поле с `elisAlias: "density|Density20"`
   - [ ] Убедиться, что используется первый найденный ключ
   - [ ] Проверить, что fallback работает корректно

4. **Парсинг методов испытаний**
   - [ ] Проверить парсинг "Менее 4,0" → `limitValue: 4.0`
   - [ ] Проверить парсинг "Более 10" → `limitValue: 10.0`
   - [ ] Проверить парсинг "Не более 5,5" → `limitValue: 5.5`
   - [ ] Убедиться, что fallback работает для нераспознанных форматов

5. **Форматирование ФИО**
   - [ ] Проверить преобразование:
     ```
     givenName: "Иван"
     middleName: "Петрович"
     familyName: "Сидоров"
     → "И. П. Сидоров"
     ```
   - [ ] Проверить fallback на старое поле `iof`

### Этап 3: Интеграция с OPC (опционально)

**Примечание**: Старая версия использовала OPC теги для синхронизации с ИВК при сохранении паспорта (DocGUID == 1).

**Задачи**:
- [ ] Проверить, нужна ли интеграция с OPC для Vue редактора
- [ ] Если да, реализовать через composable `useOpcIntegration`
- [ ] Добавить запись тега `ARM.ARM_FillActAndPassport` при сохранении
- [ ] Добавить опрос тега `ARM.ARM_FillActAndPassportResult`

**Файл для справки**: `TN_Doc/wwwroot/js/EditDoc.js:99-155`

### Этап 4: Оптимизация и улучшения

#### 4.1 Производительность

**Задачи**:
- [ ] Оптимизировать `bulkUpdateFields()` для больших объемов данных
- [ ] Добавить debounce для ручного редактирования полей
- [ ] Кэшировать результаты парсинга методов испытаний

#### 4.2 UX улучшения

**Задачи**:
- [ ] Добавить индикатор загрузки при применении данных ELIS
- [ ] Добавить Toast уведомление об успешном применении
- [ ] Добавить диалог подтверждения при перезаписи существующих данных
- [ ] Добавить возможность отменить применение ELIS данных (Undo)

#### 4.3 Отладка и логирование

**Задачи**:
- [ ] Добавить подробное логирование маппинга полей
- [ ] Логировать несопоставленные ключи ElisAlias
- [ ] Добавить DevTools panel для отладки ELIS интеграции

### Этап 5: Документация

#### 5.1 Документация для пользователей

**Файл**: `docs/user/ELIS_USER_GUIDE.md`

**Содержание**:
- [ ] Как запросить данные из ELIS
- [ ] Как применить протокол испытаний
- [ ] Как редактировать данные после применения ELIS
- [ ] Часто задаваемые вопросы (FAQ)

#### 5.2 Документация для администраторов

**Файл**: `docs/admin/ELIS_CONFIGURATION.md`

**Содержание**:
- [ ] Настройка TN.ElisConnector
- [ ] Конфигурация ElisAlias в CfgEditPassport.json
- [ ] Маппинг полей между ELIS и TN_Doc
- [ ] Устранение неполадок

#### 5.3 Обновление CHANGELOG

**Задачи**:
- [ ] Добавить запись в CHANGELOG.md о новой функциональности
- [ ] Упомянуть breaking changes (если есть)
- [ ] Добавить примеры использования

### Этап 6: Подготовка к Production

#### 6.1 Security

**Задачи**:
- [ ] Проверить origin в postMessage handler
- [ ] Добавить Content Security Policy для iframe
- [ ] Валидация данных из ELIS перед применением
- [ ] Sanitization HTML контента (если есть)

#### 6.2 Обработка ошибок

**Задачи**:
- [ ] Добавить try-catch блоки во все критические места
- [ ] Добавить fallback для недоступного ELIS API
- [ ] Добавить retry механизм для сетевых запросов
- [ ] Логировать все ошибки с контекстом

#### 6.3 Тестирование на production-подобной среде

**Задачи**:
- [ ] Тестирование на реальных устройствах
- [ ] Тестирование с реальными данными ELIS
- [ ] Нагрузочное тестирование (множественные одновременные запросы)
- [ ] Тестирование на разных браузерах

## Известные ограничения

1. **Только для Passport** - интеграция работает только для документов типа `Passport`
2. **Требуется ELIS** - устройство должно иметь `isElisUsed: true` в конфигурации
3. **postMessage origin** - в production следует проверять `event.origin` для безопасности
4. **ElisAlias обязателен** - поля без `ElisAlias` не будут заполнены из ELIS
5. **Экспериментальный статус** - Vue Document Editor помечен как POC

## Риски и митигация

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| Несоответствие ключей ElisAlias и ELIS API | Высокая | Высокое | Подробное логирование, валидация на этапе конфигурации |
| ELIS API недоступен | Средняя | Среднее | Fallback на ручное заполнение, retry механизм |
| Проблемы с postMessage между окнами | Низкая | Высокое | Тестирование на разных браузерах, fallback на legacy |
| Производительность при больших объемах данных | Низкая | Среднее | Оптимизация bulkUpdateFields, debounce |

## Timeline (приблизительный)

- **Этап 0**: 0.5-1 день (доработка FormField - универсальная подсветка) ⭐ **КРИТИЧНЫЙ**
- **Этап 1**: 1-2 дня (проверка конфигурации)
- **Этап 2**: 2-3 дня (тестирование)
- **Этап 3**: 1-2 дня (OPC интеграция, опционально)
- **Этап 4**: 2-3 дня (оптимизация и улучшения)
- **Этап 5**: 1 день (документация)
- **Этап 6**: 2-3 дня (подготовка к production)

**Итого**: 10-15 рабочих дней

## Зависимости

- TN.ElisConnector (внешний сервис)
- TN_MessagingService (для OPC интеграции)
- Vue Document Editor должен быть стабилен

## Критерии готовности (Definition of Done)

- [ ] Все чек-листы из Этапов 1-6 выполнены
- [ ] Документация обновлена
- [ ] Code review пройден
- [ ] Unit тесты написаны и проходят
- [ ] Integration тесты написаны и проходят
- [ ] Протестировано на production-подобной среде
- [ ] Нет критических багов
- [ ] Performance приемлемый (< 1 секунды на применение ELIS)

## Следующие шаги

1. ⭐ **СЕЙЧАС**: Начать с Этапа 0 - доработка FormField (универсальная подсветка)
2. Начать с Этапа 1.1 - проверка серверной конфигурации
3. Создать тестовую среду с TN.ElisConnector
4. Запросить доступ к тестовому API ELIS
5. Назначить ответственных за каждый этап

## История изменений

- **2025-11-06**: Создан первоначальный план интеграции
- **2025-11-06**: Добавлен Этап 0 - доработка FormField с универсальным пропом `highlightColor`
  - Сделан более абстрактный подход к подсветке элементов формы
  - Проп `highlightColor` может использоваться не только для ELIS, но и для других случаев
  - Добавлены детальные подзадачи для каждого типа поля (text, number, select, date, datetime-local)
  - Добавлены тестовые сценарии для проверки безопасности изменений
  - Обновлён таймлайн: добавлено 0.5-1 день на Этап 0
- **2025-11-06**: Обновлён Этап 1 на основе анализа реальной конфигурации
  - ⚠️ **КРИТИЧНО**: `ElisAlias` это массив строк `["key1", "key2"]`, не строка `"key1|key2"`
  - Обнаружена смешанная номенклатура: camelCase для AdditionalInfo, русские названия для Parameters
  - Добавлен подэтап 1.3: Обновить TypeScript типы (`elisAlias?: string[]`)
  - Добавлен подэтап 1.4: Реализовать композабл useElisIntegration с функцией `findElisValue()`
  - Обновлены примеры конфигурации из реального файла `CfgEditPassport_GOSTR50.2.040(I).json`
  - Механизм fallback реализован через массив, а не разделитель `|`
