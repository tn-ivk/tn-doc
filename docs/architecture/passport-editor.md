# Паспорт качества: логика отображения, редактирования и сохранения (Document Editor)

Документ описывает фактическую реализацию паспорта качества в **Vue Document Editor** и бэкенд‑модуле **DocPassport**: отображение формы, применение данных ELIS, редактирование параметров, сохранение и этап обновления после подтверждения ИВК (OPC).

## Область применения

- **UI редактирования**: `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue`
- **API редактирования**: `TN_Doc/Controllers/DocumentEditController.cs`
- **Модуль паспорта (DLL)**: `tn.docgeneral/Passport/DocPassport.cs` + partial‑классы
  - `tn.docgeneral/Passport/DocPassport.Editor.cs` (GetEditConfig/SaveDocument)
  - `tn.docgeneral/Passport/DocPassport.Update.cs` (DocUpdate/BuildCorrectionData/нормализация/мерж истории)
  - сервисы обновления: `tn.docgeneral/Passport/Service/*`

## Технологии и компоненты

### Frontend (Document Editor)

- Vue 3 + TypeScript + Pinia + PrimeVue
- Ключевые файлы:
  - View: `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue`
  - Store: `TN_Doc/Client/document-editor/src/stores/documentStore.ts`
  - Composables:
    - `TN_Doc/Client/document-editor/src/composables/useDocumentEditor.ts` (загрузка, интеграция с родительским окном, SaveDoc)
    - `TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts` (логика редактирования таблицы качества)
    - `TN_Doc/Client/document-editor/src/composables/usePassportSave.ts` (сохранение паспорта с OPC polling и update)
    - `TN_Doc/Client/document-editor/src/composables/useElisIntegration.ts` (приём ELIS через `postMessage`)
    - `TN_Doc/Client/document-editor/src/composables/useFieldHistory.ts` (формирование истории на фронтенде)
  - Компоненты:
    - `FormFieldWithHistory` / `SignerFieldGroup` / `DateRangeFieldGroup`
    - `PassportQualityTable` и его вложенные компоненты (ввод измерений, выбор метода, модалки)

### Backend (ASP.NET Core + модуль документов)

- Контроллер API: `TN_Doc/Controllers/DocumentEditController.cs`
- Модуль паспорта: `tn.docgeneral/Passport/DocPassport.*.cs`
- Правила тегирования/флагов:
  - `tn.docgeneral/Passport/Extensions/PassportExtensions.cs` (`ResolveTag`, `ExtractElisFlag`)

## Контракты и ключи данных

### Маршрут редактора

- Редактор паспорта открывается по пути вида: `/document-editor/edit/{deviceId}/Passport/{id}`
- Внутри SPA тип документа фиксирован как `Passport` (`DocumentPassportEditor.vue`).

### API endpoints (фактическое использование)

- `GET /api/documents/{deviceId}/Passport/edit/{id}` → конфигурация формы + `initialValues`
- `POST /api/documents/{deviceId}/Passport/save/{id}` → “первичное” сохранение (без этапа подтверждения ИВК)
- `POST /api/documents/{deviceId}/Passport/update/{id}` → обновление после подтверждения ИВК (DocUpdate, история, ELIS протокол)

### Ключи полей (controlId)

#### AdditionalInfo

- Простой ключ, например: `ExportPermit`, `Sample`, `Laboratory_IOF`, `PassportPeriodDT.Begin`.
- Поля‑подписанты представлены как select:
  - Значение в форме: `Laboratory_IOF = "<userId>"`, а реальный текст хранится как `Laboratory_IOF__label = "И. О. Фамилия"`.

#### Параметры качества (таблица)

Ключи параметров всегда составные:

- `value.{ParameterKey}` — измерение (measurement)
- `result.{ParameterKey}` — результат для печати (print/result)
- `method.{ParameterKey}` — метод испытаний (JSON строки)
- `document.{ParameterKey}` — документ ELIS (JSON строки)

#### Метаданные и служебные ключи

Используются для флагов/интеграции и не превращаются напрямую в `EditData` при сборке `CorrectionData`:

- `{controlId}__elisFilled` — текущий флаг “значение соответствует ELIS/ReturnToELIS”
- `{controlId}__elisOriginal` — сохранённое “оригинальное” значение для механизма “возврата к ELIS”
- `{controlId}__elisMissing` — поле ожидалось из ELIS, но не найдено
- `{controlId}__label` — label для select (подписанты)
- `method.{key}__elisOption` — полный объект метода из ELIS (для возврата/сравнения)
- `result.{key}__manualOverride` — признак ручной правки результата (для non‑ballast)
- `__elisProtocol` — полный протокол ELIS (JSON), отправляется на бэкенд в update‑шаге
- `__history` — история изменений полей (JSON), отправляется на бэкенд в update‑шаге

## Frontend: логика отображения

### Состояния страницы

`DocumentPassportEditor.vue` показывает:

- `store.isLoading` → спиннер и текст “Загрузка формы…”
- `store.error` → `Message` с ошибкой
- `store.isReady` → редактор
- `store.isSaving` → оверлей “Сохранение…” поверх UI

### AdditionalInfo: таблица полей

`displayRows` в `DocumentPassportEditor.vue` формирует отображение **в исходном порядке из конфигурации**, но с группировками:

1. **Диапазон дат**: если встречается `PassportPeriodDT.Begin`, то `Begin` и `End` объединяются в `DateRangeFieldGroup`.
2. **Группы подписантов**: если встречается `*_Post`, то собирается тройка полей:
   - `{prefix}_Post`, `{prefix}_Factory`, `{prefix}_IOF` → один `SignerFieldGroup`
   - Заголовок строки берётся из label поля `*_IOF` и “очищается” от суффикса в скобках.
3. Остальные поля рендерятся как `FormFieldWithHistory`.

### Таблица качественных показателей

`PassportQualityTable` отображается, если `hasQualityParameters == true`.

Список строк таблицы формируется в `usePassportEditor` на базе `qualityParametersSchema` + `store.formData`:

- **Slave‑параметры исключены** из UI (`role === 'Slave'`) и из валидации (`documentStore.ts`).

## Frontend: интеграция с ELIS

### Как данные ELIS попадают в редактор

Встроенный редактор слушает `window.postMessage`:

- `useElisIntegration` ждёт сообщение формата `{ type: 'ELIS_DATA', payload: <ElisPassportData> }`
- После получения данные “обогащаются” (`enrichElisData`) и передаются в `handleElisData` (`DocumentPassportEditor.vue`).

### Применение ELIS данных (`handleElisData`)

Если конфигурация ещё не загружена (`store.config`/`store.fields` пусты), данные кладутся в `pendingElisData` и применяются после `loadDocument()`.

Дальше формируется bulk‑payload `updates` и применяется через `store.bulkUpdateFields(updates)`.

#### 1) AdditionalInfo

Для каждого поля с `elisAlias` выполняется поиск значения (fallback):

1) корень `elisData`, 2) `elisData.labInfo`, 3) `elisData.signers.laboratory`.

Особенности:

- Для `list`/`select` поля ищется опция по `label`. Если опции нет — она добавляется в `field.options`.
- Для `Laboratory_IOF` дополнительно извлекается `UserData` (post/factory) и кладётся в `option.data` для автозаполнения.

При успехе:

- выставляется `{key} = value` и `{key}__elisFilled = true`
- сохраняется `{key}__elisOriginal`
- пишется история `trackElisLoad(key, value, protocolNumber)`

Если значение не найдено:

- выставляется `{key}__elisMissing = true`
- пишется `trackElisMissing(key, protocolNumber)`

#### 2) Параметры качества

Для каждого `paramSchema`:

- `role === 'Slave'` → пропуск (не заполняется из ELIS)
- вычисляются ключи `valueKey/methodKey/resultKey/documentKey`
- параметр ищется в `elisData.parameters` по массиву `elisAlias`

Measurement (`value.*`):

- нормализуется по `roundValue`: если знаков меньше — дополняется нулями, если больше — оставляется “как есть” (чтобы валидация подсветила ошибку)
- ставится `__elisFilled = true`, сохраняется `__elisOriginal`, создаётся история `ELIS`

Result (`result.*`):

- если параметр **балластный** (`IsBallast`), результат считается автозаполнением:
  - `result.__elisFilled = false`
  - история: `trackAutoFill`
- если не балластный:
  - если `valueString` отличается от measurement (с учётом нормализации) → `result.__elisFilled = true` и `trackElisLoad`
  - иначе → `result.__elisFilled = false` и `trackAutoFill`

Method (`method.*`):

- создаётся из `testMethodName` + парсинг `valueString` (limitValue/operator)
- если метод отсутствует в локальном списке — создаётся временная `MethodOption`
- сохраняется JSON‑строка метода, `__elisFilled = true`, `__elisOriginal = <name>`, `__elisOption = <object>`
- история: `trackElisLoad(methodKey, methodName, protocolNumber)` (в историю пишется только имя)

Document (`document.*`):

- собирается JSON `{Number, Type?, Date?}`
- `__elisFilled = true`, `__elisOriginal = payload`, история `ELIS`

Если параметр не найден в ELIS — выставляются `__elisMissing` для value/method/document, и `trackElisMissing` для соответствующих controlId.

#### 3) Сохранение “полного ELIS протокола”

В `updates` всегда добавляется:

- `__elisProtocol = JSON.stringify(elisData)`

Этот ключ:

- **не участвует** в построении `CorrectionData` (игнорируется как служебный),
- **извлекается** на бэкенде в update‑шаге (`DocPassportUpdatePayloadService`).

## Frontend: логика редактирования

### Базовые операции со значениями

`documentStore.updateField()`:

- обновляет `formData[key]`
- ставит `isDirty = true`
- если существует `{key}__elisOriginal`, то сравнивает нормализованные значения и выставляет `{key}__elisFilled` (механизм “возврата к ELIS”)

`documentStore.bulkUpdateFields()`:

- массово обновляет `formData` (используется при применении ELIS данных и при синхронизации связанных полей)

### Изменение measurement (`usePassportEditor.handleMeasurementUpdate`)

- Для **балластных** параметров:
  - используется `store.syncBallastParameter(paramKey, value, { source })`
  - measurement и result синхронизируются
  - `result.__elisFilled` принудительно `false`
  - пишется история (Manual или ReturnToELIS) + автозаполнение результата
- Для **обычных** параметров:
  - measurement обновляется, пишется история Manual/ReturnToELIS
  - если параметр редактируемый — пересчитывается result, сбрасывается `result.__manualOverride`, пишется история AutoFill

### Изменение результата (`usePassportEditor.handleResultUpdate`)

- Используется `store.markManualOverride(paramKey, value, { source: Manual })`:
  - выставляет `result.__manualOverride = true`
  - корректно проставляет `result.__elisFilled` (в т.ч. ReturnToELIS)
  - пишет историю результата (Manual/ReturnToELIS)

### Изменение метода (`usePassportEditor.handleMethodUpdate`)

- Метод хранится как JSON строка в `store.formData['method.{key}']`.
- В истории фиксируется **только имя** метода.
- Если в схеме есть `linkedParameter`, метод синхронизируется в связанный параметр (на фронте) и помечается в истории как AutoFill для ведомого.
- Отдельно поддерживается механизм “метод отсутствует в справочнике” через `localMethodNames` и `addMethodToLocalDictionary()`.

## Сохранение паспорта: фактический двухэтапный процесс (Save → OPC → Update)

Сохранение вызывается **не кнопкой внутри SPA**, а главным окном:

- `useDocumentEditor.exposeSaveDoc()` регистрирует `window.SaveDoc()`
- главное окно включает/выключает кнопку сохранения по `postMessage`:
  - `'ButtonSaveOn'` / `'ButtonSaveOff'` (на основе `store.canSave`)

### Алгоритм сохранения (frontend)

Реализация: `TN_Doc/Client/document-editor/src/composables/usePassportSave.ts`

1) **Save**: `POST /api/documents/{deviceId}/Passport/save/{id}`
   - отправляется **только** `store.formData`
2) **OPC trigger + polling** (если переданы `opcParams` в URL):
   - запись `ARM.ARM_FillActAndPassport = true`
   - ожидание увеличения `ARM.ARM_FillActAndPassportResult` (timeout 10s)
3) **Merge localStorage**:
   - добавляются данные из `localStorage['dataPassport']` (наследие старого клиента)
4) **Update**: `POST /api/documents/{deviceId}/Passport/update/{id}`
   - в payload добавляется `__history` (схлопнутая до последней записи на поле)
   - `__elisProtocol` попадает автоматически, т.к. хранится в `store.formData`

Если `opcParams` отсутствуют или polling завершился по таймауту — update‑шаг **не выполняется**.

## Backend: отображение (GetEditConfig)

Точка входа: `DocumentEditController.GetEditConfig()` → `DocPassport.GetEditConfig()`.

`DocPassport.GetEditConfig()` (`tn.docgeneral/Passport/DocPassport.Editor.cs`):

1) Загружает документ через `GetViewDoc(id)`.
2) Загружает `CfgEditPassport` и вычисляет роли параметров (`ResolveParameterRoles()`).
3) Формирует:
   - `Fields` (AdditionalInfo) через `BuildAdditionalInfoFields()`
   - `QualityParametersSchema` через `BuildQualityParametersSchema()` (роль Master/Slave/Independent, `linkedParameter`, доступные методы, `ElisAlias` только для non‑Slave)
   - `InitialValues` через `ExtractInitialValues()`:
     - `value.* / result.* / method.* / document.*`
     - `{controlId}__elisFilled` — на основе `DataARM.GetLastSourceForControl()` (ELIS и ReturnToELIS считаются ELIS‑источником для UI)
     - `{controlId}__history` — возвращается только при включенном ELIS
     - `__elisProtocol` (если сохранён в DataARM)

### Что считается «не заполненным» паспортом (IsFilled = 0)

В контексте **редактора** “пустой/ещё не заполненный паспорт” — это документ, у которого в данных ИВК/БД выставлено:

- `TablePassport.IsFilled == 0` (поле копируется в `GetViewDoc()` из списка документов: `TableActAndPassportList.IsFilled → TablePassport.IsFilled`).

Этот флаг влияет на вычисление значений при первоначальной загрузке формы (см. ниже) и на служебную историю методов (DefaultMethod).

### Диаграмма: открытие редактора и получение initialValues

```mermaid
sequenceDiagram
    autonumber
    participant UI as Vue: DocumentPassportEditor
    participant Store as Pinia: documentStore
    participant API as ASP.NET: DocumentEditController
    participant Mod as DLL: DocPassport.GetEditConfig
    participant DB as БД: TableActAndPassport*

    UI->>Store: loadDocument(deviceId,"Passport",id)
    Store->>API: GET /api/documents/{deviceId}/Passport/edit/{id}
    API->>Mod: editor.GetEditConfig(id)
    Mod->>DB: GetViewDoc(id)\nLoad ActAndPassport/AdditionalData/PassportResult/DataARM\nSet TablePassport.IsFilled
    Mod->>Mod: BuildQualityParametersSchema()
    Mod->>Mod: ExtractInitialValues()\nFillQualityParametersInitialValues()
    Mod-->>API: PassportEditConfig(fields, schema, initialValues)
    API-->>Store: config + initialValues
    Store->>Store: formData = initialValues\nformHistory = initialValues[...__history]
    Store-->>UI: qualityParameters = schema + formData
    UI-->>UI: render table\n(Измерение=value.*, Предпросмотр=result.*)
    Note over Store: restoreElisOriginals() после loadConfig\nвосстанавливает __elisOriginal для __elisFilled=true
```

### Как заполняются «Измерение» и «Предпросмотр» при загрузке (initialValues)

В таблице качественных показателей колонки привязаны к ключам:

- **Измерение** → `value.{ParameterKey}`
- **Предпросмотр** → `result.{ParameterKey}`

Эти значения **первично** приходят с бэкенда в составе `initialValues` (формируются в `FillQualityParametersInitialValues()`).

#### Алгоритм выбора значений из данных ИВК/БД (BuildParameterValues)

Логика реализована в `BasePassportQualityStrategy.cs` (`tn.docgeneral/Passport/PassportQualityStrategy/`) и использует два метода:

1. **ResolveMeasurementValue** - определяет значение для колонки "Измерение" (`value.*`)
2. **ResolveResultValue** - определяет значение для колонки "Предпросмотр" (`result.*`)

##### Структура входных данных (ParameterValuesContext)

```csharp
public record ParameterValuesContext(
    string ParameterKey,           // Ключ параметра (например, "DensCorrection")
    LegalValue Raw,                // Raw значение: { Value: double, Legal: int }
    LegalValue Correction,         // Correction значение: { Value: double, Legal: int }
    string Result,                 // Значение из PassportResult
    LabInfo LabInfo,               // Данные из DataARM.LabInfo (пользовательский ввод)
    long IsFilled                  // Флаг заполненности паспорта (0 или 1)
);
```

**Важно**: `Legal > 0` означает, что значение было подтверждено ИВК (прошло валидацию). `Legal = 0` означает неподтверждённое значение.

##### Алгоритм ResolveMeasurementValue (колонка "Измерение")

```mermaid
flowchart TD
    A[ResolveMeasurementValue] --> B{IsFilled == 0?<br/>Паспорт НЕ заполнен}

    B -->|Да| C{Correction.Legal > 0?}
    C -->|Да| C1[Вернуть Correction.Value<br/>source = correction]
    C -->|Нет| D{Raw.Legal > 0?}
    D -->|Да| D1[Вернуть Raw.Value<br/>source = raw]
    D -->|Нет| E{Result - число?<br/>CanParseToFloat}

    B -->|Нет<br/>Паспорт заполнен| E

    E -->|Да| E1[Вернуть Result<br/>source = result]
    E -->|Нет| F[Вернуть пустую строку<br/>source = empty]

    style C1 fill:#90EE90
    style D1 fill:#90EE90
    style E1 fill:#90EE90
    style F fill:#FFB6C1
```

**Логика (приоритет сверху вниз):**

1. **Если паспорт НЕ заполнен** (`IsFilled = 0`):
   - `Correction.Value` (если `Legal > 0`) — подтверждённое ИВК значение с коррекцией
   - `Raw.Value` (если `Legal > 0`) — подтверждённое ИВК сырое значение
   - `Result` (если является числом) — значение из PassportResult
   - Пустая строка

2. **Если паспорт заполнен** (`IsFilled > 0`):
   - `Result` (если является числом) — значение из PassportResult
   - Пустая строка

##### Алгоритм ResolveResultValue (колонка "Предпросмотр")

```mermaid
flowchart TD
    A[ResolveResultValue] --> B{IsFilled == 0?<br/>Паспорт НЕ заполнен}

    B -->|Да| C{Correction.Legal > 0?}
    C -->|Да| C1[Вернуть Correction.Value<br/>source = correction]
    C -->|Нет| D{Raw.Legal > 0?}
    D -->|Да| D1[Вернуть Raw.Value<br/>source = raw]
    D -->|Нет| G{Result - число?}

    B -->|Нет<br/>Паспорт заполнен| E{LabInfo.Value существует<br/>И НЕ число<br/>И НЕ содержит '-'?}

    E -->|Да| E1[Вернуть LabInfo.Value<br/>source = labInfo<br/>Текстовое значение<br/>из пользовательского ввода]
    E -->|Нет| G

    G -->|Да| G1[Вернуть Result<br/>source = result]
    G -->|Нет| H[Вернуть пустую строку<br/>source = empty]

    style C1 fill:#90EE90
    style D1 fill:#90EE90
    style E1 fill:#87CEEB
    style G1 fill:#90EE90
    style H fill:#FFB6C1
```

**Логика (приоритет сверху вниз):**

1. **Если паспорт НЕ заполнен** (`IsFilled = 0`):
   - `Correction.Value` (если `Legal > 0`) — подтверждённое ИВК значение с коррекцией
   - `Raw.Value` (если `Legal > 0`) — подтверждённое ИВК сырое значение
   - `Result` (если является числом) — значение из PassportResult
   - Пустая строка

2. **Если паспорт заполнен** (`IsFilled > 0`):
   - `LabInfo.Value` (если текстовое значение без "-") — **пользовательский ввод** из прошлых редактирований
   - `Result` (если является числом) — значение из PassportResult
   - Пустая строка

**Особенность LabInfo.Value:**
- Это поле используется для хранения **текстовых** значений результата (например, "соответствует", "не соответствует")
- Проверяется только для заполненных паспортов (`IsFilled > 0`)
- Игнорируется, если значение является числом или содержит символ "-"

##### Вспомогательная функция CanParseToFloat

```csharp
protected static bool CanParseToFloat(string value)
{
    return !string.IsNullOrWhiteSpace(value)
        && float.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out _);
}
```

Проверяет, является ли строка числом (с учётом замены запятой на точку для совместимости с русской локалью).

##### Итоговая обработка значений

```csharp
public virtual ParameterValues BuildParameterValues(ParameterValuesContext context)
{
    var measurementValue = ResolveMeasurementValue(context, out var measurementSource);
    var resultValue = ResolveResultValue(context, out var resultSource);

    _logger.Trace($"BuildParameterValues [{context.ParameterKey}]: measurement={measurementValue} ({measurementSource}) | result={resultValue} ({resultSource})");

    return new ParameterValues
    {
        Measurement = measurementValue?.Replace('.', ',') ?? string.Empty,  // Точка → запятая для русской локали
        Result = resultValue?.Replace('.', ',') ?? string.Empty
    };
}
```

**Примечания:**

- Значения извлекаются из разных источников (`jsonDoc`, `DataARM`) в методе `GetRawValue/GetCorrectionValue` (`DocPassport.Editor.cs`)
- В конце значения приводятся к русской записи: `'.' → ','`
- Источник значения логируется для диагностики (уровень Trace)

### Как «Измерение» и «Предпросмотр» переписываются из ELIS (после получения протокола)

После загрузки формы значения могут быть обновлены сообщением ELIS (frontend `handleElisData()`).

Ключевой момент: ELIS заполняет **те же** controlId (`value.*` и `result.*`) и выставляет служебные флаги `__elisFilled/__elisOriginal`, влияющие на подсветку и механику “возврата к ELIS”.

```mermaid
flowchart TD
    A[ELIS postMessage] --> B{config/fields загружены?}
    B -->|нет| B1[pendingElisData\nприменить после loadDocument]
    B -->|да| C[handleElisData()]
    C --> D[для каждого параметра schema]
    D --> E{role == Slave?}
    E -->|да| D
    E -->|нет| F{elisAlias есть?}
    F -->|нет| D
    F -->|да| G{найден elisParam?}
    G -->|нет| H[__elisMissing\nvalue/method/document]
    G -->|да| I[value.<key>=elisParam.value\nvalue.__elisFilled=true\nvalue.__elisOriginal=value]
    I --> J{valueString есть?}
    J -->|нет| D
    J -->|да| K{isBallast?}
    K -->|да| L[result.<key>=valueString\nresult.__elisFilled=false\nhistory AutoFill]
    K -->|нет| M{valueString != measurement?}
    M -->|да| N[result.__elisFilled=true\nresult.__elisOriginal=valueString\nhistory ELIS]
    M -->|нет| O[result.__elisFilled=false\nhistory AutoFill]
    H --> D
    L --> D
    N --> D
    O --> D
```

## Backend: первичное сохранение (SaveDocument)

Точка входа: `DocumentEditController.SaveDocument()` → `DocPassport.SaveDocument()` (`tn.docgeneral/Passport/DocPassport.Editor.cs`).

### Что делает SaveDocument

1) Строит `CorrectionData` из плоского payload:
   - теги определяются `PassportExtensions.ResolveTag()` (`value.*` → `Value`, `method.*` → `Metod`, …)
   - служебные ключи, содержащие `__`, игнорируются при сборке `EditData` (`DocPassport.Update.cs`)
   - `ElisFilled` для `EditData` читается из `{controlId}__elisFilled` (`PassportExtensions.ExtractElisFlag`)
2) Нормализует некоторые значения:
   - для полей подписантов (IOF) выбирается текст IOF по userId/`__label` (`DocPassport.Update.cs: NormalizeValue`)
3) Обновляет:
   - `FillingTableActAndPassport` (Correction/AdditionalData, бинарные поля)
   - `TableActAndPassportData.DataARM` (JSON)
4) Для параметров качества выставляет `Legal = 0` если:
   - параметр является **Slave** (значение вычисляется от Master),
   - или параметр нередактируемый (`Edit == false`).
5) Синхронизирует метод по `LinkedParameter` на бэкенде (ведущий → ведомый), если у ведущего **нет** `SlaveKey`.

### Важное граничное условие: даты PassportPeriodDT

При сохранении `PassportPeriodDT.Begin/End`:

- если значение пустое — обновление **пропускается** (старое значение в БД сохраняется).

## Backend: обновление после подтверждения ИВК (DocUpdate / update endpoint)

Точка входа: `DocumentEditController.UpdateDocument()` → `DocPassport.DocUpdate()` (`tn.docgeneral/Passport/DocPassport.Update.cs`).

### Подготовка payload

`DocPassportUpdatePayloadService` (`tn.docgeneral/Passport/Service/DocPassportUpdatePayloadService.cs`):

- извлекает `__elisProtocol` и десериализует в `QualityPassport`
- извлекает `__history` (только если `IsUsedElis=true`)
- строит `CorrectionData` через `BuildCorrectionData(id, values, historyFromFrontend)`

### Обработка DataARM и истории

`DocPassportDataArmService` (`tn.docgeneral/Passport/Service/DocPassportDataArmService.cs`):

- применяет значения по тегам `AdditionalInfo/Metod/Value/PrintValue/DocNum`
- добавляет записи истории в `DataARM.FieldHistoryMap` (только при ELIS)
- вычисляет `IsFullyFilledFromElis` (все поля с `ElisAlias` должны иметь последний источник строго `ELIS`)
- выполняет проверки/дополнения для payload балластных параметров и “manual overrides ELIS”

`DocPassport.DocUpdate()` дополнительно:

- при отсутствии `__elisProtocol` в запросе сохраняет предыдущий протокол из БД,
- делает `MergeFieldHistory()` — защищает историю от потери, т.к. фронт отправляет только “последнюю” запись на поле после схлопывания.

## Граничные условия и наблюдения (как есть в коде)

- История изменений работает только при `IsUsedElis=true` (на фронте индикаторы/загрузка истории, на бэке сохранение/мерж).
- Двухэтапное сохранение: без вызова `POST .../update/{id}` (нет OPC параметров или polling timeout) история и ELIS протокол могут не сохраниться/не обновиться.
- Slave‑параметры:
  - не показываются в таблице,
  - не валидируются,
  - не заполняются из ELIS,
  - имеют `Legal=0`.
- `LinkedParameter` работает только если у параметра нет `SlaveKey` (приоритет `SlaveKey`).
- Поле документа ELIS (`document.*`) приходит/уходит как JSON; на бэкенде парсинг tolerant (объект/строка/число).
