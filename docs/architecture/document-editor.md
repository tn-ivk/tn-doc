# Document Editor Architecture

## Обзор

Document Editor — это веб-приложение для редактирования документов непосредственно в браузере, построенное на **Vue 3 + TypeScript + PrimeVue + Vue Router**. Компонент предоставляет продвинутый UI для редактирования паспортов качества с поддержкой интеграции с ELIS, OPC, системой истории изменений и автозаполнением зависимых полей.

**Статус**: 🚧 **В активной разработке** (v1.4.4)
**Ветка**: `developWork`
**URL**: `/document-editor/edit/{IdDevice}/{IdDoc}/{id}`
**Порт dev-сервера**: 5175
**Production Build**: `TN_Doc/wwwroot/document-editor/`
**Технологии**: Vue 3.5, TypeScript 5.6, PrimeVue 4.2, Pinia 2.2, Vue Router 4.5

**Последние обновления (Ноябрь 2025):**
- ✅ **Объединение полей даты отбора пробы** - компонент DateRangeFieldGroup для отображения двух datetime-local полей в одной строке
- ✅ **Исправлена обработка datetime-local полей** - устранены искажения времени, корректное создание истории изменений
- ✅ Добавлены модальные окна для редактирования результатов и методов
- ✅ Компоненты с историей изменений (FieldHistoryIndicator, FieldHistoryPopup)
- ✅ Обновлена структура таблицы качества (одна строка заголовка, ширина колонок 150px)
- ✅ Встроенные иконки редактирования в ячейки таблицы
- ✅ Composable useFieldHistory для работы с историей изменений
- ✅ Улучшена обработка методов испытаний (поддержка null, ручной ввод)
- ✅ **Визуальная индикация методов вне справочника** (желтая рамка #f5c24c для нестандартных методов)
- ✅ **Отключена автоматическая подстановка методов** (убраны флаги IsDefault, явный выбор оператора)
- ✅ **Исправлен порядок отображения полей подписантов** (поля отображаются в правильной позиции согласно CfgEditPassport.json)

## Ключевые возможности

### Реализовано в v1.4.4 ✅
- ✅ **Production-ready редактор паспортов качества** - полнофункциональная форма с таблицей параметров
- ✅ **Модальные окна редактирования**:
  - `ResultEditDialog` - редактирование результата для печати
  - `ManualMethodDialog` - ручной ввод метода испытаний с граничными значениями
- ✅ **Компоненты с историей изменений**:
  - `FieldHistoryIndicator` - визуальный индикатор источника данных
  - `FieldHistoryPopup` - popup с детальной историей до 10 записей
  - `PassportMeasurementInputWithHistory` - поле измерения с историей
  - `PassportMethodSelectWithHistory` - выбор метода с историей
  - `PassportResultCellWithHistory` - ячейка результата с историей
- ✅ **Специализированные компоненты форм**:
  - `DateRangeFieldGroup` - горизонтальное отображение двух datetime-local полей в одной строке
- ✅ **Улучшенная таблица качества**:
  - Одна строка заголовка вместо двух
  - Ширина колонок увеличена до 150px
  - Встроенные иконки редактирования
  - Условная колонка "Документы" (только при ELIS)
- ✅ **Composable useFieldHistory** - управление историей изменений полей
- ✅ **Валидация в реальном времени** - проверка обязательных полей, формата, округления, некорректных символов
- ✅ **Production build pipeline** - автоматическая сборка через npm workspaces
- ✅ **Parent window integration** - postMessage API для взаимодействия с главным окном

### В разработке 🚧
- 🚧 **Интеграция с ELIS** - загрузка лабораторных данных из протоколов с визуальной индикацией
- 🚧 **Интеграция с OPC** - прямое чтение данных с измерительных приборов ИВК
- 🚧 **Автозаполнение зависимых параметров** - автоматический расчёт связанных полей с отслеживанием в истории
- 🚧 **Система истории изменений полей** - полная трассировка источника данных (ELIS, Manual, IVK)

### Планируется в v1.4.5+
- 🔄 **Редактирование актов** - специализированный редактор для документов типа Act
- 🔄 **Редактирование других типов документов** - универсальная форма для Report, Journal
- 🔄 **История изменений для всех типов документов** - расширение за пределы Passport
- 🔄 **Улучшенная валидация с подсказками** - контекстные подсказки при ошибках ввода

## Архитектура компонента

```mermaid
graph TB
    subgraph "Frontend - Vue 3"
        Router[Vue Router]
        PassportEditor[PassportEditor.vue]
        ActEditor[ActEditor.vue]
        GenericEditor[DocumentEditor.vue]
        Store[DocumentStore]
        Composables[Composables]
        Components[UI Components]
    end

    subgraph "Composables Layer"
        UseDocEditor[useDocumentEditor]
        UsePassportEditor[usePassportEditor]
        UsePassportAutoFill[usePassportAutoFill]
        UseElisIntegration[useElisIntegration]
        UseOpcIntegration[useOpcIntegration]
        UseFieldHistory[useFieldHistory]
    end

    subgraph "Backend - ASP.NET Core"
        DocumentController[DocumentController]
        DocPassport[DocPassport Library]
        DocAct[DocAct Library]
        ElisConnector[ELIS Connector]
        OpcClient[OPC Client]
    end

    subgraph "External Systems"
        ELIS[ELIS LabHub]
        OpcServer[OPC DA/UA Server]
        DB[(MySQL Database)]
    end

    Router --> PassportEditor
    Router --> ActEditor
    Router --> GenericEditor

    PassportEditor --> UseDocEditor
    PassportEditor --> UsePassportEditor
    PassportEditor --> UsePassportAutoFill

    UseDocEditor --> Store
    UsePassportEditor --> Store
    UsePassportAutoFill --> Store

    UseElisIntegration --> ElisConnector
    UseOpcIntegration --> OpcClient
    UseFieldHistory --> Store

    Store --> DocumentController
    DocumentController --> DocPassport
    DocumentController --> DocAct

    DocPassport --> DB
    ElisConnector --> ELIS
    OpcClient --> OpcServer
```

## Routing Structure

### URL Pattern

```
/document-editor/edit/{IdDevice}/{IdDoc}/{id}
```

**Примеры:**
- `/document-editor/edit/1/Passport/12345` - Паспорт качества для ИВК-1, документ #12345
- `/document-editor/edit/2/Act/98765` - Акт для ИВК-2, документ #98765
- `/document-editor/edit/3/Report/5555` - Отчет для ИВК-3, документ #5555

### Route Configuration

```typescript
// router/index.ts
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      // Специализированный редактор паспортов качества
      path: '/edit/:deviceId/Passport/:id',
      name: 'passport-editor',
      component: DocumentPassportEditor,
      props: true
    },
    {
      // Специализированный редактор актов
      path: '/edit/:deviceId/Act/:id',
      name: 'act-editor',
      component: DocumentActEditor,
      props: true
    },
    {
      // Общий редактор для других типов документов
      path: '/edit/:deviceId/:docType/:id',
      name: 'editor',
      component: DocumentEditor,
      props: true
    },
    {
      // Страница ошибки
      path: '/error',
      name: 'error',
      component: ErrorPage
    }
  ]
});
```

## State Management (Pinia)

### DocumentStore Architecture

```mermaid
classDiagram
    class DocumentStore {
        +config: DocumentEditConfig
        +formData: Record~string, any~
        +formHistory: Record~string, FieldHistoryEntry[]~
        +isDirty: boolean
        +isLoading: boolean
        +isSaving: boolean
        +error: string
        +isReady: computed
        +hasUnsavedChanges: computed
        +hasValidationErrors: computed
        +canSave: computed
        +loadConfig() Promise
        +saveDocument() Promise
        +updateField() void
        +getFieldHistory() FieldHistoryEntry[]
    }

    class DocumentEditConfig {
        +docType: string
        +title: string
        +fields: FormField[]
        +initialValues: Record~string, any~
        +invalidChars: string[]
    }

    class PassportEditConfig {
        +qualityParametersSchema: ParameterSchema[]
        +elisConfig: ElisConfig
        +opcConfig: OpcConfig
    }

    class FieldHistoryEntry {
        +source: DataSource
        +modifiedAt: string
        +modifiedBy: string
        +value: string
        +previousValue: string
        +comment: string
    }

    DocumentStore --> DocumentEditConfig
    PassportEditConfig --|> DocumentEditConfig
    DocumentStore --> FieldHistoryEntry
```

### State Flow

```mermaid
stateDiagram-v2
    [*] --> Loading: loadConfig()
    Loading --> Ready: Config loaded
    Loading --> Error: Load failed

    Ready --> Dirty: User edit
    Dirty --> Validating: Click Save
    Validating --> Saving: Validation OK
    Validating --> Dirty: Validation failed

    Saving --> Ready: Save success
    Saving --> Error: Save failed

    Dirty --> Ready: Cancel changes
    Error --> Ready: Retry
```

## Component Hierarchy

### Passport Editor View

```mermaid
graph TD
    PassportEditor[DocumentPassportEditor.vue]

    PassportEditor --> AdditionalInfo[Additional Info Section]
    PassportEditor --> QualityTable[PassportQualityTable]
    PassportEditor --> SavingOverlay[Saving Overlay]

    AdditionalInfo --> FormFieldWithHistory1[FormFieldWithHistory: ExportPermit]
    AdditionalInfo --> FormFieldWithHistory2[FormFieldWithHistory: Sample]
    AdditionalInfo --> FormFieldWithHistory3[FormFieldWithHistory: Laboratory_IOF]

    QualityTable --> ParameterRow1[PassportParameterRow: Density]
    QualityTable --> ParameterRow2[PassportParameterRow: WaterContent]
    QualityTable --> ParameterRowN[PassportParameterRow: ...]

    ParameterRow1 --> MeasurementInput[PassportMeasurementInputWithHistory]
    ParameterRow1 --> MethodSelect[PassportMethodSelectWithHistory]
    ParameterRow1 --> ResultCell[PassportResultCellWithHistory]

    MeasurementInput --> HistoryIndicator1[FieldHistoryIndicator]
    MethodSelect --> HistoryIndicator2[FieldHistoryIndicator]
    ResultCell --> HistoryIndicator3[FieldHistoryIndicator]

    HistoryIndicator1 --> HistoryPopup[FieldHistoryPopup]
    HistoryIndicator2 --> HistoryPopup
    HistoryIndicator3 --> HistoryPopup
```

## Key UI Components

### PassportQualityTable

**Расположение:** `components/passport/PassportQualityTable.vue`

**Функции:**
- Отображение таблицы параметров качества (Edit таблица)
- Управление строками параметров
- Интеграция с ELIS и OPC
- Валидация измерений
- **Новое:** Модальные окна для редактирования результатов и методов
- **Новое:** Встроенные иконки редактирования

**Структура таблицы (обновлено в v1.4.4):**

| № | Наименование показателя | Метод испытаний | Документы* | Измерение | Результат |
|---|------------------------|----------------|-----------|-----------|-----------|
| 1 | Плотность при 20°C | [Select + 📝 Кнопка ручного ввода] | [Link]* | [Input: 850.567 + 🟢] | [Display: 850.57 + ✏️] |
| 2 | Массовая доля воды | [Select + 📝] | [Link]* | [Input: 0.035 + 🔵] | [Display: 0.03 + ✏️] |

**Примечание:** * Колонка "Документы" отображается только при включенном ELIS (`isElisUsed = true`)

**Изменения в структуре (v1.4.4):**
- ✅ Заголовок таблицы теперь содержит одну строку (вместо двух)
- ✅ Ширина колонок увеличена до 150px для лучшей читаемости
- ✅ Исправлено переполнение контента в ячейках
- ✅ Колонка "Документы" условная (только при ELIS)
- ✅ **Встроенные иконки редактирования**:
  - ✏️ в колонке "Результат" - открывает `ResultEditDialog`
  - 📝 возле селектора методов - открывает `ManualMethodDialog`
- ✅ **Индикаторы истории**:
  - 🟢 ELIS - данные загружены из лаборатории
  - 🔵 Manual - данные введены вручную
  - 🟡 IVK - данные округлены системой ИВК
  - ⚪ Unknown - источник неизвестен

### PassportParameterRow

**Расположение:** `components/passport/PassportParameterRow.vue`

**Props:**
- `parameter: ParameterSchema` - Схема параметра из конфигурации
- `isElisUsed: boolean` - Доступна ли интеграция с ELIS

**Компоненты-потомки:**
1. **PassportMethodSelectWithHistory** - выбор метода испытаний
2. **PassportMeasurementInputWithHistory** - ввод измеренного значения
3. **PassportResultCellWithHistory** - результат для печати (readonly)

### PassportMethodSelect (Обновлено в Ноябре 2025)

**Расположение:** `components/passport/PassportMethodSelect.vue`

**Функции:**
- Выбор метода испытаний из справочника
- Ручной ввод метода через модальное окно
- **Визуальная индикация методов вне справочника**

**Визуальная индикация нестандартных методов:**

Методы, которые отсутствуют в локальном справочнике `CfgEditPassport.json`, отображаются с визуальным предупреждением:

- **Желтая рамка** вокруг поля выбора (цвет: `#f5c24c`)
- **Текстовое предупреждение** под полем: "отсутствует в справочнике"
- CSS класс: `.unknown-method`

**Логика определения нестандартных методов:**

```typescript
// usePassportEditor.ts
const showDictionaryWarning = computed(() => {
  const selectedMethod = parameter.method.selected;

  // Если метод не выбран, предупреждение не показывается
  if (!selectedMethod || selectedMethod.trim() === '') {
    return false;
  }

  // Проверка наличия метода в справочнике (регистронезависимое сравнение)
  const methodOptions = paramSchema.methodOptions || [];
  const isInDictionary = methodOptions.some(option =>
    option.name?.toLowerCase() === selectedMethod.toLowerCase()
  );

  return !isInDictionary;
});
```

**Типы данных:**

```typescript
interface ParameterMethod {
  selected: string | null;          // Выбранное название метода
  options: MethodOption[];          // Доступные методы из справочника
  isInDictionary?: boolean;         // Флаг наличия в справочнике (новое)
  requiredFill: boolean;            // Обязательность заполнения
}
```

**Сценарии использования:**

1. **Метод из справочника** - нормальное отображение, без предупреждений
2. **Метод из ELIS, не зарегистрированный локально** - желтая рамка + предупреждение
3. **Ручной ввод нестандартного метода** - желтая рамка + предупреждение
4. **Метод не выбран** - placeholder "Метод не выбран", без предупреждения

**Важно:** Визуальное предупреждение не блокирует сохранение документа, а служит для информирования оператора о необходимости актуализации справочника

### FormFieldWithHistory

**Расположение:** `components/FormFieldWithHistory.vue`

**Функции:**
- Универсальная обёртка для полей с историей изменений
- Поддержка типов: text, number, date, textarea, select
- Валидация некорректных символов
- Отображение индикатора истории

**Типы полей:**
```typescript
type FieldType = 'text' | 'number' | 'date' | 'textarea' | 'select';

interface FormField {
  key: string;
  label: string;
  type: FieldType;
  required: boolean;
  options?: { label: string; value: any }[];
  placeholder?: string;
}
```

### DateRangeFieldGroup (Новое в v1.4.4)

**Расположение:** `components/DateRangeFieldGroup.vue`

**Назначение:** Компонент для горизонтального отображения двух полей datetime-local в одной строке

**Функции:**
- Отображение двух связанных полей даты в одной строке (начало и окончание периода)
- Визуальный разделитель между полями (дефис "-")
- Равномерное распределение полей по ширине контейнера
- Интеграция с системой истории изменений через FormFieldWithHistory
- Скрытие индивидуальных label для экономии пространства

**Props:**
```typescript
interface Props {
  beginField: FormField;           // Конфигурация поля "Начало"
  endField: FormField;             // Конфигурация поля "Окончание"
  beginValue: string;              // Значение начала периода (ISO datetime)
  endValue: string;                // Значение окончания периода (ISO datetime)
  invalidChars?: string[];         // Список недопустимых символов для валидации
}
```

**Events:**
```typescript
{
  'update:begin': (value: string) => void;   // Изменение начала периода
  'update:end': (value: string) => void;     // Изменение окончания периода
}
```

**Использование:**

Компонент используется в `DocumentPassportEditor.vue` для отображения полей "Дата и время отбора пробы":
- `PassportPeriodDT.Begin` - Дата и время отбора пробы (начало)
- `PassportPeriodDT.End` - Дата и время отбора пробы (окончание)

```typescript
// В displayRows computed property
if (field.Key === 'PassportPeriodDT.Begin' || field.Key === 'PassportPeriodDT.End') {
  // Группируем оба поля в одну строку с типом dateRange
  displayRows.value.push({
    type: 'dateRange',
    label: 'Дата и время отбора пробы',
    fields: {
      begin: beginField,
      end: endField
    }
  });
}
```

**Преимущества:**
- Экономия вертикального пространства в форме (2 поля → 1 строка)
- Визуальная связь между началом и окончанием периода
- Сохранение всех функций FormFieldWithHistory (валидация, история изменений)
- Адаптивное распределение ширины полей

### FieldHistoryIndicator & FieldHistoryPopup

См. детальное описание в [История изменений полей](../features/field-history.md)

**FieldHistoryIndicator** - компактный индикатор источника:
- Позиция: правый верхний угол поля
- Размер: 6px для текста, 14px для иконок
- Цвета: Зелёный (ELIS), Синий (Manual), Оранжевый (IVK), Серый (Unknown)

**FieldHistoryPopup** - детальная история в popup:
- Технология: PrimeVue OverlayPanel
- Триггер: Hover на FieldHistoryIndicator
- Содержимое: до 10 последних изменений с датами и значениями

### ResultEditDialog (Новое в v1.4.4)

**Расположение:** `components/passport/ResultEditDialog.vue`

**Назначение:** Модальное окно для редактирования результата для печати

**Функции:**
- Ручное редактирование значения результата параметра качества
- Валидация вводимого значения
- Подтверждение или отмена изменений

**Props:**
```typescript
interface Props {
  visible: boolean;              // Видимость диалога
  parameterName?: string;        // Название параметра
  initialValue: string;          // Начальное значение результата
}
```

**Events:**
```typescript
{
  'update:visible': (value: boolean) => void;   // Изменение видимости
  'confirm': (value: string) => void;           // Подтверждение нового значения
}
```

**Использование:**
- Открывается при клике на иконку редактирования в колонке "Результат"
- Позволяет изменить результат для печати независимо от измерения
- Применяется для случаев, когда нужно скорректировать значение вручную

### ManualMethodDialog (Новое в v1.4.4)

**Расположение:** `components/passport/ManualMethodDialog.vue`

**Назначение:** Модальное окно для ручного ввода метода испытаний

**Функции:**
- Ручной ввод названия метода испытаний (текстовое поле)
- Ввод граничного значения (опционально)
- Ввод текстового представления результата (например, "Менее 4,0")
- Сброс метода к стандартному значению

**Props:**
```typescript
interface Props {
  visible: boolean;              // Видимость диалога
  parameterName?: string;        // Название параметра
}
```

**Events:**
```typescript
{
  'update:visible': (value: boolean) => void;   // Изменение видимости
  'confirm': (payload: ManualMethodPayload) => void;  // Подтверждение нового метода
  'reset': () => void;                          // Сброс к стандартному методу
}

interface ManualMethodPayload {
  name: string;                  // Название метода
  limitValue?: number;           // Граничное значение (опционально)
  limitValueString?: string;     // Текстовое представление результата
}
```

**Использование:**
- Открывается при клике на кнопку "Ввести вручную" в ячейке метода
- Позволяет ввести произвольный метод испытаний, отсутствующий в списке
- Поддерживает ввод граничных значений для сравнения
- Кнопка "Сбросить" возвращает стандартный метод из конфигурации

## Composables Architecture

### useDocumentEditor

**Основная логика редактирования документов**

```typescript
export function useDocumentEditor() {
  const store = useDocumentStore();
  const route = useRoute();

  // Загрузка документа
  async function loadDocument() {
    const { deviceId, docType, id } = route.params;
    await store.loadConfig(Number(deviceId), String(docType), Number(id));
  }

  // Сохранение документа
  async function saveDocument() {
    const { deviceId, docType, id } = route.params;
    await store.saveDocument(Number(deviceId), String(docType), Number(id));
  }

  // Экспорт saveDoc для вызова из родительского окна
  function exposeSaveDoc() {
    (window as any).saveDoc = saveDocument;
  }

  // Уведомление родительского окна о состоянии сохранения
  function notifyParentAboutSaveState(canSave: boolean) {
    if (window.parent !== window) {
      window.parent.postMessage({
        action: 'updateSaveButtonState',
        canSave
      }, '*');
    }
  }

  // Предупреждение о несохранённых изменениях
  function setupBeforeUnloadHandler() {
    window.addEventListener('beforeunload', (e) => {
      if (store.hasUnsavedChanges) {
        e.preventDefault();
        e.returnValue = '';
      }
    });
  }

  return {
    store,
    loadDocument,
    saveDocument,
    exposeSaveDoc,
    notifyParentAboutSaveState,
    setupBeforeUnloadHandler
  };
}
```

### usePassportEditor

**Специфичная логика для паспортов качества**

**Обновлено в v1.4.4:**
- Добавлена поддержка модальных окон для редактирования результатов и методов
- **Улучшена логика выбора методов:** убрана автоматическая подстановка, явная индикация незаполненных методов
- **Визуальная индикация нестандартных методов:** проверка наличия метода в справочнике

```typescript
export function usePassportEditor() {
  const store = useDocumentStore();

  // Computed свойства для параметров качества
  const qualityParameters = computed(() => {
    const config = store.config as PassportEditConfig;
    return config?.qualityParametersSchema || [];
  });

  const isElisUsed = computed(() => {
    const config = store.config as PassportEditConfig;
    return config?.elisConfig?.isUsed || false;
  });

  const hasQualityParameters = computed(() => qualityParameters.value.length > 0);

  // Обработчики изменений
  function handleMeasurementUpdate(parameterKey: string, value: string) {
    const fieldKey = `value.${parameterKey}`;
    const { trackManualChange } = useFieldHistory();

    const previousValue = store.formData[fieldKey];
    store.updateField(fieldKey, value);
    trackManualChange(fieldKey, value, previousValue);
  }

  function handleMethodUpdate(parameterKey: string, methodData: MethodOption | null) {
    const fieldKey = `method.${parameterKey}`;
    const { trackManualChange } = useFieldHistory();

    // Получаем предыдущий метод для истории
    const previousMethodJson = store.formData[fieldKey] || '';
    const previousMethod = previousMethodJson ? JSON.parse(previousMethodJson) : null;
    const previousMethodName = previousMethod?.name || '';

    // Сохраняем полный JSON метода в formData
    const newValue = methodData ? JSON.stringify(methodData) : null;
    store.updateField(fieldKey, newValue);

    // В историю записываем только название метода (v1.4.4+)
    const newMethodName = methodData?.name || '';
    if (newMethodName !== previousMethodName) {
      trackManualChange(fieldKey, newMethodName, previousMethodName || undefined);
    }
  }

  function handleResultUpdate(parameterKey: string, value: string) {
    const fieldKey = `result.${parameterKey}`;
    const { trackManualChange } = useFieldHistory();

    const previousValue = store.formData[fieldKey];
    store.updateField(fieldKey, value);
    trackManualChange(fieldKey, value, previousValue);
  }

  // Новое в v1.4.4: Работа с модальными окнами
  function handleResultEdit(parameterKey: string, newValue: string) {
    handleResultUpdate(parameterKey, newValue);
  }

  function handleManualMethodConfirm(parameterKey: string, payload: ManualMethodPayload) {
    const methodOption: MethodOption = {
      id: Date.now(), // Генерация уникального ID
      use: true,
      idParameter: 0, // Будет установлено на бэкенде
      name: payload.name,
      isDefault: false,
      limitValueActivate: !!payload.limitValue,
      limitValue: payload.limitValue,
      limitValueString: payload.limitValueString
    };

    handleMethodUpdate(parameterKey, methodOption);
  }

  function handleManualMethodReset(parameterKey: string) {
    // Сброс к стандартному методу из конфигурации
    const param = qualityParameters.value.find(p => p.key === parameterKey);
    if (param && param.methodOptions.length > 0) {
      const defaultMethod = param.methodOptions.find(m => m.isDefault) || param.methodOptions[0];
      handleMethodUpdate(parameterKey, defaultMethod);
    }
  }

  return {
    qualityParameters,
    isElisUsed,
    hasQualityParameters,
    handleMeasurementUpdate,
    handleMethodUpdate,
    handleResultUpdate,
    handleResultEdit,           // Новое
    handleManualMethodConfirm,  // Новое
    handleManualMethodReset     // Новое
  };
}
```

### usePassportAutoFill

**Автозаполнение зависимых параметров**

```typescript
export function usePassportAutoFill() {
  const store = useDocumentStore();

  function setupAutoFillWatchers() {
    const config = store.config as PassportEditConfig;
    const parameters = config?.qualityParametersSchema || [];

    // Пример: Автозаполнение результата для печати при изменении измерения
    parameters.forEach(param => {
      if (param.roundValue) {
        watch(
          () => store.formData[`value.${param.key}`],
          (newValue) => {
            if (newValue) {
              const rounded = roundToDecimals(newValue, param.roundValue);
              store.updateField(`result.${param.key}`, rounded);
            }
          }
        );
      }
    });

    // Пример: Зависимые параметры (плотность при 15°C → плотность при 20°C)
    if (parameters.some(p => p.key === 'Density15')) {
      watch(
        () => store.formData['value.Density15'],
        (newValue) => {
          if (newValue) {
            // Расчёт плотности при 20°C через таблицу пересчёта
            const density20 = convertDensity15to20(parseFloat(newValue));
            store.updateField('value.Density20', density20.toString());
          }
        }
      );
    }
  }

  return { setupAutoFillWatchers };
}
```

### useElisIntegration

**Интеграция с ELIS для загрузки лабораторных данных**

```typescript
export function useElisIntegration() {
  const store = useDocumentStore();
  const { trackElisLoad } = useFieldHistory();

  async function loadElisData(protocolNumber: string) {
    try {
      const elisData = await elisApi.getProtocol(protocolNumber);

      // Заполнение параметров из ELIS
      elisData.parameters.forEach(param => {
        const parameterKey = mapElisAliasToKey(param.alias);
        if (parameterKey) {
          const fieldKey = `value.${parameterKey}`;
          store.updateField(fieldKey, param.value);

          // Отследить загрузку из ELIS
          trackElisLoad(fieldKey, param.value, protocolNumber);

          // Обновить метод испытаний (v1.4.4+)
          if (param.methodName) {
            const methodKey = `method.${parameterKey}`;
            const methodData = createMethodFromElisData(param);

            // Сохраняем полный JSON метода в formData
            store.updateField(methodKey, JSON.stringify(methodData));

            // В историю записываем только читаемое название
            trackElisLoad(methodKey, methodData.name, protocolNumber);
          }

          // Обновить документ ELIS
          if (param.documentNumber) {
            const documentKey = `document.${parameterKey}`;
            const documentData = {
              Number: param.documentNumber,
              Type: param.documentType,
              Date: param.documentDate
            };
            store.updateField(documentKey, JSON.stringify(documentData));
          }
        }
      });

      return { success: true, loadedCount: elisData.parameters.length };
    } catch (error) {
      return { success: false, error: error.message };
    }
  }

  return { loadElisData };
}
```

### useOpcIntegration

**Интеграция с OPC для чтения данных с приборов**

```typescript
export function useOpcIntegration() {
  const store = useDocumentStore();

  async function readOpcTag(tagName: string, parameterKey: string) {
    try {
      const opcData = await opcApi.readTag(tagName);

      const fieldKey = `value.${parameterKey}`;
      store.updateField(fieldKey, opcData.value);

      // Отследить как загрузку из ИВК
      const { trackIVKLoad } = useFieldHistory();
      trackIVKLoad(fieldKey, opcData.value, tagName);

      return { success: true, value: opcData.value };
    } catch (error) {
      return { success: false, error: error.message };
    }
  }

  return { readOpcTag };
}
```

### useFieldHistory

**Управление историей изменений полей**

См. детальное описание в [История изменений полей](../features/field-history.md)

```typescript
export function useFieldHistory() {
  const store = useDocumentStore();

  // Отследить ручное изменение
  function trackManualChange(fieldKey: string, newValue: any, previousValue?: any) {
    const entry: FieldHistoryEntry = {
      source: DataSource.Manual,
      modifiedAt: new Date().toISOString(),
      modifiedBy: 'Пользователь',
      value: normalizeValue(newValue),
      previousValue: previousValue !== undefined ? normalizeValue(previousValue) : undefined,
      comment: 'Отредактировано вручную'
    };

    addHistoryEntry(fieldKey, entry);
  }

  // Отследить загрузку из ELIS
  function trackElisLoad(fieldKey: string, value: any, protocolNumber: string) {
    const entry: FieldHistoryEntry = {
      source: DataSource.ELIS,
      modifiedAt: new Date().toISOString(),
      modifiedBy: 'ELIS',
      value: normalizeValue(value),
      comment: `Загружено из протокола ${protocolNumber}`
    };

    addHistoryEntry(fieldKey, entry);
  }

  // Отследить округление ИВК
  function trackIVKRounding(fieldKey: string, originalValue: any, roundedValue: any, decimals: number) {
    const entry: FieldHistoryEntry = {
      source: DataSource.IVK,
      modifiedAt: new Date().toISOString(),
      modifiedBy: 'ИВК',
      value: normalizeValue(roundedValue),
      previousValue: normalizeValue(originalValue),
      comment: `Округлено до ${decimals} знаков`
    };

    addHistoryEntry(fieldKey, entry);
  }

  return {
    trackManualChange,
    trackElisLoad,
    trackIVKRounding,
    getFieldHistory: (fieldKey: string) => store.formHistory[fieldKey] || [],
    getLastSource: (fieldKey: string) => {
      const history = store.formHistory[fieldKey];
      return history && history.length > 0 ? history[history.length - 1].source : DataSource.Unknown;
    }
  };
}
```

## Data Flow

### Document Loading Flow

```mermaid
sequenceDiagram
    participant User
    participant Parent as Parent Window
    participant Editor as Document Editor
    participant Store as DocumentStore
    participant API as DocumentController
    participant DocLib as DocPassport Library
    participant DB as MySQL Database

    User->>Parent: Click "Редактировать"
    Parent->>Editor: Open in iframe/modal
    Editor->>Store: loadConfig(deviceId, docType, id)
    Store->>API: GET /api/documents/{deviceId}/{docType}/edit/{id}
    API->>DocLib: GetEditConfig(id)
    DocLib->>DB: SELECT * FROM dataarm WHERE IdDoc = ?
    DB-->>DocLib: DataARM record
    DocLib->>DocLib: Build EditConfig with fields & history
    DocLib-->>API: DocumentEditConfig JSON
    API-->>Store: Config + initialValues + history
    Store->>Store: Populate formData & formHistory
    Store-->>Editor: isReady = true
    Editor-->>User: Show edit form
```

### Document Saving Flow

```mermaid
sequenceDiagram
    participant User
    participant Editor as Document Editor
    participant Store as DocumentStore
    participant History as useFieldHistory
    participant API as DocumentController
    participant DocLib as DocPassport Library
    participant DB as MySQL Database

    User->>Editor: Edit field value
    Editor->>Store: updateField(key, value)
    Store->>Store: formData[key] = value
    Store->>Store: isDirty = true
    Editor->>History: trackManualChange(key, value, previousValue)
    History->>Store: Add entry to formHistory[key]

    User->>Editor: Click "Сохранить"
    Editor->>Store: saveDocument()
    Store->>Store: Validate formData

    alt Validation Failed
        Store-->>Editor: Show validation errors
    else Validation Success
        Store->>API: POST /api/documents/{deviceId}/{docType}/save/{id}
        Note over Store,API: Payload: { ...formData, __history: formHistory }
        API->>DocLib: DocUpdate(correctionData)
        DocLib->>DocLib: Parse __history & update FieldHistoryMap
        DocLib->>DB: UPDATE dataarm SET ... WHERE IdDoc = ?
        DB-->>DocLib: Success
        DocLib-->>API: Success
        API-->>Store: 200 OK
        Store->>Store: isDirty = false
        Store-->>Editor: Success
        Editor-->>User: Show success message
    end
```

### ELIS Integration Flow

```mermaid
sequenceDiagram
    participant User
    participant Editor as Document Editor
    participant ElisComp as useElisIntegration
    participant History as useFieldHistory
    participant Store as DocumentStore
    participant ELIS as ELIS LabHub

    User->>Editor: Enter protocol number
    User->>Editor: Click "Загрузить из ELIS"
    Editor->>ElisComp: loadElisData(protocolNumber)
    ElisComp->>ELIS: GET /api/protocols/{protocolNumber}
    ELIS-->>ElisComp: Protocol data (parameters, methods, documents)

    loop For each parameter
        ElisComp->>Store: updateField(`value.${key}`, param.value)
        ElisComp->>History: trackElisLoad(`value.${key}`, value, protocolNumber)
        History->>Store: Add ELIS entry to formHistory

        alt Has method
            ElisComp->>Store: updateField(`method.${key}`, methodData)
            ElisComp->>History: trackElisLoad(`method.${key}`, methodData, protocolNumber)
        end

        alt Has document
            ElisComp->>Store: updateField(`document.${key}`, documentData)
        end
    end

    ElisComp-->>Editor: { success: true, loadedCount: N }
    Editor-->>User: "Загружено N параметров из ELIS"
    Note over Editor: Поля подсвечены зелёным, индикаторы "ЕЛИС"
```

## Validation System

### Client-side Validation Rules

```typescript
// DocumentStore validation
const hasValidationErrors = computed(() => {
  if (!config.value) return false;

  const invalidChars = config.value.invalidChars || [];

  for (const field of config.value.fields) {
    const value = formData.value[field.key];

    // 1. Обязательные поля
    if (field.required && (value === null || value === undefined || value === '')) {
      return true;
    }

    // 2. Некорректные символы
    if (field.type === 'text' && value && typeof value === 'string') {
      for (const char of invalidChars) {
        if (value.includes(char)) {
          return true;
        }
      }
    }
  }

  // 3. Валидация параметров качества (для Passport)
  if (config.value.docType === 'Passport') {
    const passportConfig = config.value as PassportEditConfig;
    const parametersSchema = passportConfig.qualityParametersSchema || [];

    for (const paramSchema of parametersSchema) {
      const measurement = (formData.value[`value.${paramSchema.key}`] ?? '').toString();

      // 3a. Обязательные измерения
      if (paramSchema.requiredFill && !measurement) {
        return true;
      }

      // 3b. Количество знаков после запятой
      if (paramSchema.roundValue && measurement) {
        const normalized = measurement.replace(',', '.');
        const parts = normalized.split('.');
        if (parts.length > 1 && parts[1].length > paramSchema.roundValue) {
          return true;
        }
      }
    }
  }

  return false;
});
```

### Validation Error Display

```mermaid
flowchart TD
    Input[User Input] --> RequiredCheck{Required?}
    RequiredCheck -->|Yes, Empty| ShowRequired[Show "Обязательное поле"]
    RequiredCheck -->|No/Filled| InvalidCharsCheck{Invalid Chars?}

    InvalidCharsCheck -->|Found| ShowInvalidChars["Show 'Недопустимые символы: ...'"]
    InvalidCharsCheck -->|None| NumberCheck{Number field?}

    NumberCheck -->|Yes| DecimalCheck{Too many decimals?}
    DecimalCheck -->|Yes| ShowDecimals["Show 'Максимум N знаков после запятой'"]
    DecimalCheck -->|No| Valid[Valid]

    NumberCheck -->|No| Valid

    ShowRequired --> DisableSave[Disable Save Button]
    ShowInvalidChars --> DisableSave
    ShowDecimals --> DisableSave
    Valid --> EnableSave[Enable Save Button]
```

## Parent Window Integration

Document Editor обычно открывается во вложенном окне (iframe или модальное окно). Связь с родительским окном осуществляется через `postMessage`.

### Messages from Editor to Parent

```typescript
// Уведомление о состоянии кнопки "Сохранить"
window.parent.postMessage({
  action: 'updateSaveButtonState',
  canSave: true  // или false
}, '*');

// Уведомление об успешном сохранении
window.parent.postMessage({
  action: 'documentSaved',
  documentId: 12345
}, '*');

// Запрос на закрытие окна редактора
window.parent.postMessage({
  action: 'closeEditor'
}, '*');
```

### Messages from Parent to Editor

```typescript
// Родительское окно вызывает функцию saveDoc() в iframe
(window.frames[0] as any).saveDoc?.();

// Или через postMessage
editorIframe.contentWindow.postMessage({
  action: 'saveDocument'
}, '*');
```

### Integration Example

```typescript
// В родительском окне (например, главная страница TN_Doc)
const editorIframe = document.getElementById('editor-iframe');

// Обработка сообщений от редактора
window.addEventListener('message', (event) => {
  if (event.data.action === 'updateSaveButtonState') {
    const saveButton = document.getElementById('save-btn');
    saveButton.disabled = !event.data.canSave;
  }

  if (event.data.action === 'documentSaved') {
    // Закрыть модальное окно и обновить список документов
    closeEditorModal();
    refreshDocumentList();
  }
});

// Нажатие на кнопку "Сохранить" в родительском окне
saveButton.addEventListener('click', () => {
  editorIframe.contentWindow.saveDoc();
});
```

## Performance Optimizations

### 1. Lazy Loading Components

```typescript
// Ленивая загрузка редакторов
const DocumentPassportEditor = defineAsyncComponent(() =>
  import('@/views/DocumentPassportEditor.vue')
);

const DocumentActEditor = defineAsyncComponent(() =>
  import('@/views/DocumentActEditor.vue')
);
```

### 2. Debounced Auto-Fill

```typescript
import { debounce } from 'lodash';

// Отложенный расчёт зависимых полей
const debouncedAutoFill = debounce((parameterKey: string, value: string) => {
  // Автозаполнение результата для печати
  const rounded = roundToDecimals(value, param.roundValue);
  store.updateField(`result.${parameterKey}`, rounded);
}, 300);

watch(() => store.formData['value.Density'], (newValue) => {
  debouncedAutoFill('Density', newValue);
});
```

### 3. History Entry Batching

```typescript
// Батчинг записей истории при массовой загрузке из ELIS
const historyBatch: Record<string, FieldHistoryEntry> = {};

elisData.parameters.forEach(param => {
  // Накопление записей
  historyBatch[`value.${param.key}`] = createElisHistoryEntry(param);
});

// Одновременное добавление всех записей
store.addHistoryBatch(historyBatch);
```

## Production Deployment

### Build Process

```bash
# Development с hot reload
cd TN_Doc/Client
npm run dev:editor
# Доступен на http://localhost:5174

# Production build
cd TN_Doc/Client
npm run build:editor

# Или собрать все Vue компоненты одновременно
npm run build:all
```

### Build Output Location

После успешной сборки артефакты располагаются в:
```
TN_Doc/wwwroot/document-editor/
├── index.html
├── assets/
│   ├── index-[hash].js       # Main bundle (~150 KB gzipped)
│   ├── index-[hash].css      # Styles (~50 KB gzipped)
│   ├── vendor-[hash].js      # Vue, PrimeVue, etc. (~200 KB gzipped)
│   └── [lazy-chunks].js      # Route-based code splitting
└── favicon.ico
```

**Важно:**
- Production build автоматически обслуживается ASP.NET Core через `app.UseStaticFiles()`
- При деплое на production сервер необходимо запустить `npm run build:all` перед `dotnet publish`
- Build артефакты включены в `.gitignore` - каждый разработчик собирает локально

### Deployment Checklist

**Перед деплоем на production:**
1. ✅ Убедитесь, что все тесты проходят: `dotnet test`
2. ✅ Соберите Vue компоненты: `cd TN_Doc/Client && npm run build:all`
3. ✅ Проверьте наличие артефактов в `TN_Doc/wwwroot/document-editor/`
4. ✅ Соберите ASP.NET Core приложение: `dotnet publish -c Release`
5. ✅ Проверьте конфигурацию ELIS в `CfgApp.json` (`IsUsedElis = true` для работы истории)
6. ✅ Убедитесь, что база данных имеет актуальную схему с поддержкой `FieldHistoryMap`

### Build Output

```mermaid
flowchart LR
    Source[Vue Source Files] --> TypeScript[TypeScript Compiler]
    TypeScript --> Vite[Vite Bundler]

    Vite --> CSS[document-editor.css]
    Vite --> JS[document-editor.js]
    Vite --> Chunks[Lazy Chunks]
    Vite --> Assets[Assets]

    CSS --> Output[wwwroot/document-editor/]
    JS --> Output
    Chunks --> Output
    Assets --> Output

    Output --> ASPNet[ASP.NET Core Static Files]
```

### Integration with ASP.NET Core

**Views/Documents/Editor.cshtml**:
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Редактор документов</title>
    <link rel="stylesheet" href="~/dist/document-editor/document-editor.css" />
</head>
<body>
    <div id="app"></div>
    <script src="~/dist/document-editor/document-editor.js"></script>
</body>
</html>
```

**Startup.cs**:
```csharp
app.UseStaticFiles(); // Serves wwwroot/document-editor/*

app.MapControllerRoute(
    name: "editor",
    pattern: "editor/{*path}",
    defaults: new { controller = "Documents", action = "Editor" }
);
```

## Error Handling

```mermaid
flowchart TD
    Error[Error Occurred] --> Type{Error Type}

    Type -->|Network| NetworkError[Connection error]
    Type -->|Validation| ValidationError[Validation failed]
    Type -->|Server| ServerError[Server error]
    Type -->|ELIS| ElisError[ELIS unavailable]
    Type -->|OPC| OpcError[OPC read failed]

    NetworkError --> Toast1[Toast: "Нет связи с сервером"]
    ValidationError --> Inline[Inline error under field]
    ServerError --> Toast2[Toast: Show server message]
    ElisError --> Toast3[Toast: "ELIS недоступен"]
    OpcError --> Toast4[Toast: "Не удалось прочитать данные с ИВК"]

    Toast1 --> Log[Log to console]
    Inline --> Log
    Toast2 --> Log
    Toast3 --> Log
    Toast4 --> Log

    Log --> Retry{Retryable?}
    Retry -->|Yes| RetryBtn[Show Retry Button]
    Retry -->|No| Support[Show Support Contact]
```

## Security Considerations

### 1. Input Sanitization

```typescript
// Защита от XSS при отображении значений
function sanitizeHtml(value: string): string {
  const div = document.createElement('div');
  div.textContent = value;
  return div.innerHTML;
}

// Валидация недопустимых символов
const invalidChars = ['<', '>', '&', '"', "'", '\\'];
function containsInvalidChars(value: string): boolean {
  return invalidChars.some(char => value.includes(char));
}
```

### 2. CSRF Protection

```typescript
// API Service добавляет CSRF токен к запросам
const documentApi = {
  async saveDocument(deviceId: number, docType: string, id: number, data: any) {
    return axios.post(
      `/api/documents/${deviceId}/${docType}/save/${id}`,
      data,
      {
        headers: {
          'X-CSRF-TOKEN': getCsrfToken()
        }
      }
    );
  }
};
```

### 3. Access Control

```csharp
// Backend проверяет права доступа
[Authorize]
[HttpPost("api/documents/{deviceId}/{docType}/save/{id}")]
public async Task<IActionResult> SaveDocument(int deviceId, string docType, int id)
{
    // Проверка прав на редактирование документов для данного устройства
    if (!User.HasPermission($"documents.{deviceId}.edit"))
    {
        return Forbid();
    }

    // ...
}
```

## Testing Strategy

### Unit Tests

```typescript
// DocumentStore tests
describe('DocumentStore', () => {
  it('should mark document as dirty after field update', () => {
    const store = useDocumentStore();
    store.loadConfig(1, 'Passport', 12345);

    store.updateField('ExportPermit', 'New Value');

    expect(store.isDirty).toBe(true);
  });

  it('should validate required fields', () => {
    const store = useDocumentStore();
    store.config = {
      fields: [{ key: 'ExportPermit', required: true }]
    };
    store.formData = { ExportPermit: '' };

    expect(store.hasValidationErrors).toBe(true);
  });
});

// useFieldHistory tests
describe('useFieldHistory', () => {
  it('should track manual change', () => {
    const { trackManualChange, getFieldHistory } = useFieldHistory();

    trackManualChange('value.Density', '850.567', '850.5');

    const history = getFieldHistory('value.Density');
    expect(history.length).toBe(1);
    expect(history[0].source).toBe(DataSource.Manual);
  });
});
```

### Integration Tests

```typescript
describe('ELIS Integration', () => {
  it('should load data from ELIS and track history', async () => {
    const { loadElisData } = useElisIntegration();
    const { getFieldHistory } = useFieldHistory();

    const result = await loadElisData('ПР-2024-12345');

    expect(result.success).toBe(true);
    const history = getFieldHistory('value.Density');
    expect(history[0].source).toBe(DataSource.ELIS);
    expect(history[0].comment).toContain('ПР-2024-12345');
  });
});
```

## Known Limitations

### v1.4.4 Production Release

**Функциональные ограничения:**
- ⚠️ **История изменений требует включенного ELIS**
  - Необходимо `IsUsedElis = true` в `CfgApp.json`
  - При выключенном ELIS история не сохраняется и не отображается
  - Индикаторы `FieldHistoryIndicator` скрываются автоматически
- ⚠️ **Реализовано только для документа Passport**
  - Act, Report, Journal - в планах на v1.4.5+
  - Универсальный редактор `DocumentEditor.vue` существует, но требует доработки
- ⚠️ **ФИО пользователя недоступно**
  - В истории изменений используется "Пользователь" вместо реального имени
  - Требует интеграции с системой аутентификации ASP.NET Core

**Технические ограничения:**
- ⚠️ **Офлайн режим не поддерживается** - требуется постоянное подключение к серверу
- ⚠️ **Нет мобильной версии** - UI оптимизирован только для desktop (минимум 1024px)
- ⚠️ **Отсутствует Undo/Redo** - нельзя отменить изменения внутри сессии редактирования
- ⚠️ **История ограничена 10 записями** - старые изменения удаляются автоматически (FIFO)

## Roadmap

### ✅ v1.4.4 (В активной разработке - Ноябрь 2025)

**Завершено:**
- ✅ Production-ready редактор паспортов качества (DocumentPassportEditor)
- ✅ Модальные окна редактирования:
  - `ResultEditDialog` для редактирования результата для печати
  - `ManualMethodDialog` для ручного ввода метода испытаний
- ✅ Компоненты с историей изменений:
  - `FieldHistoryIndicator` - визуальные индикаторы источника данных
  - `FieldHistoryPopup` - popup с детальной историей
  - `PassportMeasurementInputWithHistory`, `PassportMethodSelectWithHistory`, `PassportResultCellWithHistory`
- ✅ Улучшения таблицы качества:
  - Одна строка заголовка, ширина колонок 150px
  - Встроенные иконки редактирования
  - Условная колонка "Документы"
- ✅ Composable useFieldHistory для управления историей
- ✅ Валидация в реальном времени
- ✅ Composables-based архитектура (useDocumentEditor, usePassportEditor)
- ✅ Parent window integration (postMessage API)
- ✅ Production build pipeline
- ✅ **Визуальная индикация методов вне справочника** (желтая рамка #f5c24c для нестандартных методов)
- ✅ **Улучшенная логика выбора методов** (отключены флаги IsDefault, явная индикация незаполненных методов)

**В разработке:**
- 🚧 Система истории изменений полей (ELIS, Manual, IVK tracking)
- 🚧 Интеграция с ELIS (загрузка протоколов)
- 🚧 Интеграция с OPC (чтение данных с ИВК)
- 🚧 Автозаполнение зависимых параметров

### 🔄 v1.4.5 (Q1 2025)
- [ ] Редактор актов (DocumentActEditor) - production ready
- [ ] Общий редактор для Report/Journal (DocumentEditor refactoring)
- [ ] История изменений для Act, Report, Journal
- [ ] Улучшенная валидация с контекстными подсказками
- [ ] Интеграция с ASP.NET Core Identity для отображения реального ФИО пользователя

### 🔮 v1.4.6+ (Q2 2025+)
- [ ] Темная тема (dark mode support)
- [ ] Офлайн режим с синхронизацией при восстановлении связи
- [ ] Адаптивная мобильная версия (responsive design)
- [ ] Расширенная интеграция с OPC UA (подписки на изменения)
- [ ] Экспорт/импорт данных из Excel
- [ ] Полный аудит логирование всех изменений
- [ ] Undo/Redo функциональность (локальная история действий)
- [ ] Массовое редактирование документов (bulk edit)
- [ ] Поддержка нескольких языков (i18n)

## См. также

### Внутренняя документация
- [История изменений полей](../features/field-history.md) - детальное описание системы истории изменений
- [StatusBar Architecture](./statusbar.md) - архитектура строки состояния
- [Configurator Architecture](./configurator.md) - архитектура конфигуратора
- [ELIS Integration](../integration/elis.md) - интеграция с ЕЛИС
- [API Endpoints](../api/endpoints.md) - документация API endpoints

### Внешние ресурсы
- [Vue 3 Documentation](https://vuejs.org/) - официальная документация Vue 3
- [PrimeVue Components](https://primevue.org/) - библиотека UI компонентов
- [Pinia State Management](https://pinia.vuejs.org/) - state management для Vue
- [Vue Router](https://router.vuejs.org/) - официальный роутер для Vue
- [TypeScript Documentation](https://www.typescriptlang.org/docs/) - документация TypeScript

---

## История изменений документа

**2025-11-22 - Исправлена обработка datetime-local полей**
- ✅ **Исправлена конвертация Date в локальное время:** ранее использовался `toISOString()` возвращающий UTC, что вызывало сдвиг времени
- ✅ **Добавлена специализированная функция handleDateTimeChange():** для обработки событий PrimeVue DatePicker
- ✅ **Реализована защита от дублирования событий:** предотвращение повторной обработки одинаковых значений
- ✅ **Корректное создание истории изменений:** записи истории теперь создаются для datetime-local полей
- ✅ **Индикаторы источников данных работают:** корректное отображение иконок ELIS/Manual для datetime-local полей
- ✅ **Устранены искажения даты/времени:** проблема с часовым поясом решена переходом на локальное время
- 📝 Компоненты: `FormField.vue`, `FormFieldWithHistory.vue`, `DateRangeFieldGroup.vue`
- 📝 Модули: `useFieldHistory.ts`, `documentStore.ts`
- 📝 Корневая причина: PrimeVue DatePicker не вызывал @update:modelValue для datetime-local + неправильная UTC конвертация

**2025-11-21 - Объединение полей даты отбора пробы**
- ✅ **Новый компонент DateRangeFieldGroup.vue:** для горизонтального отображения двух полей datetime-local
- ✅ **Объединение полей даты:** "Дата и время отбора пробы (начало)" и "(окончание)" теперь в одной строке
- ✅ **Визуальный разделитель:** дефис "-" между двумя полями для ясности
- ✅ **Адаптивное распределение:** оба поля равномерно распределены по ширине
- ✅ **Интеграция с историей:** полная поддержка системы истории изменений через FormFieldWithHistory
- 📝 Обновлен `DocumentPassportEditor.vue` с типом строки `dateRange` в displayRows
- 📝 Документация: добавлена детальная секция "DateRangeFieldGroup" в Key UI Components
- 📝 Документация: обновлен раздел "Последние обновления"

**2025-11-21 - Визуальная индикация методов вне справочника**
- ✅ **Визуальная индикация нестандартных методов:** желтая рамка (#f5c24c) и предупреждение "отсутствует в справочнике"
- ✅ **Улучшена логика выбора методов:** убрана автоматическая подстановка первого метода из справочника
- ✅ **Отключены флаги IsDefault:** для параметров с Id: 11, 13, 15, 17, 19, 21, 23, 33, 35, 37 в CfgEditPassport_GOSTR50.2.040(I).json
- ✅ **Явная индикация незаполненных методов:** placeholder "Метод не выбран" вместо автовыбора
- ✅ **Новое поле isInDictionary:** в типе ParameterMethod для отслеживания методов вне справочника
- 📝 Обновлен `PassportMethodSelect.vue` с CSS классом `.unknown-method`
- 📝 Обновлен `usePassportEditor.ts` с логикой проверки наличия метода в справочнике
- 📝 Обновлен подмодуль `tn.docgeneral` до версии с улучшенной логикой выбора методов
- 📝 Документация: добавлена секция "PassportMethodSelect" с детальным описанием визуальной индикации
- 📝 Документация: обновлены разделы "Последние обновления" и "Roadmap"

**2025-01-20 - Оптимизация истории для методов испытаний**
- ✅ **История методов испытаний:** теперь сохраняется только название метода (`name`) вместо полного JSON объекта
- ✅ **Автоматическая запись истории:** добавлена запись истории при ручном изменении метода (ранее только при загрузке из ELIS)
- 📝 Обновлен `handleMethodUpdate` в usePassportEditor с логикой сохранения только `name` в историю
- 📝 Обновлена документация с примерами кода

**2025-11-20 - Актуализация документации в соответствии с последними изменениями**
- 📝 Обновлен статус компонента (v1.4.4 в активной разработке)
- 📝 Добавлена информация о реализованных UI компонентах
- 📝 Обновлена структура таблицы качества с индикаторами истории
- 📝 Добавлены детали модальных окон (ResultEditDialog, ManualMethodDialog)
- 📝 Обновлен раздел Roadmap с разделением на "Завершено" и "В разработке"

**2025-01-20 - Обновление UI компонентов и архитектуры (v1.4.4)**
- ✅ **Новые UI компоненты:**
  - Добавлен `ResultEditDialog` - модальное окно для редактирования результатов
  - Добавлен `ManualMethodDialog` - модальное окно для ручного ввода методов
  - Встроенные иконки редактирования в ячейки таблицы качества
  - Добавлены компоненты с историей: `FieldHistoryIndicator`, `FieldHistoryPopup`
  - Специализированные компоненты: `PassportMeasurementInputWithHistory`, `PassportMethodSelectWithHistory`, `PassportResultCellWithHistory`
- ✅ **Улучшения PassportQualityTable:**
  - Структура таблицы обновлена (одна строка заголовка вместо двух)
  - Ширина колонок увеличена до 150px
  - Исправлено переполнение контента
  - Колонка "Документы" условная (только при ELIS)
- ✅ **Обновлён usePassportEditor composable:**
  - Добавлены обработчики для модальных окон (`handleResultEdit`, `handleManualMethodConfirm`, `handleManualMethodReset`)
  - Улучшена обработка изменений методов (поддержка `null`)
  - Добавлено логирование изменений результатов через систему истории
- ✅ **Новый composable useFieldHistory:**
  - `trackManualChange()` - отслеживание ручных изменений
  - `trackElisLoad()` - отслеживание загрузки из ELIS
  - `trackIVKRounding()` - отслеживание округления ИВК
  - `getFieldHistory()`, `getLastSource()` - получение истории
- ✅ **Рефакторинг бэкенда DocPassport:**
  - Разделение на partial классы (DocPassport.cs, DocPassport.Editor.cs, DocPassport.Listing.cs, DocPassport.Update.cs)
  - Внедрены сервисы `DocPassportUpdatePayloadService` и `DocPassportDataArmService`
  - Добавлена поддержка передачи полного протокола ELIS с фронтенда

**2025-01-17 - Актуализация для Production Ready (v1.4.4)**
- ✅ Изменён статус с "В активной разработке" на "Production Ready"
- ✅ Переработана секция "Реализовано в v1.4.4" - добавлены детали всех завершённых функций
- ✅ Добавлена секция "Known Limitations" с разделением на функциональные и технические ограничения
- ✅ Обновлён Roadmap - перенесены завершённые задачи из "Планируется" в "Реализовано"
- ✅ Добавлена секция "Production Deployment" с deployment checklist и build output
- ✅ Добавлена ссылка на документацию системы истории изменений полей
- ✅ Актуализирована информация о ветке разработки (developWork)
- ✅ Добавлена информация о версиях технологий (Vue 3.5, TypeScript 5.6, etc.)

**2024-12-15 - Первоначальная версия**
- Базовая архитектурная документация
- Описание composables и компонентов
- Mermaid диаграммы
- Примеры кода
