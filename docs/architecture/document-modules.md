# Архитектура модулей документов

## Обзор

Система генерации документов построена на основе модульной архитектуры с использованием паттерна **Factory** и динамической загрузки модулей.

## Жизненный цикл документа

```mermaid
stateDiagram-v2
    [*] --> Request: Запрос документа
    Request --> ModuleSelection: deviceId + docId
    ModuleSelection --> ModuleCreation: Factory Pattern
    ModuleCreation --> DataRetrieval: GetViewDoc(recordId)
    DataRetrieval --> TemplateLoading: GetPathTemplateFile()
    TemplateLoading --> Rendering: FastReport Process
    Rendering --> Export: PDF/Excel/etc
    Export --> Storage: In-memory Buffer
    Storage --> [*]: Return to User
```

## Интерфейс модуля документа

Все модули документов реализуют стандартный интерфейс `IDocClass`:

```mermaid
classDiagram
    class IDocClass {
        <<interface>>
        +GetViewDoc(int id) string
        +GetPathTemplateFile() string
        +GetEditDoc(int id) string
        +SetDocFromJson(string json) void
        +GetNameDoc() string
        +GetDocTypeId() string
    }

    class PassportModule {
        -DbContext _context
        -string _templatePath
        +GetViewDoc(int id) string
        +GetPathTemplateFile() string
        +GetEditDoc(int id) string
        +SetDocFromJson(string json) void
    }

    class PoverkaModule {
        -DbContext _context
        -VerificationConfig _config
        +GetViewDoc(int id) string
        +GetPathTemplateFile() string
        +GetEditDoc(int id) string
        +SetDocFromJson(string json) void
    }

    class KMHModule {
        -DbContext _context
        -QualityConfig _config
        +GetViewDoc(int id) string
        +GetPathTemplateFile() string
        +GetEditDoc(int id) string
        +SetDocFromJson(string json) void
    }

    IDocClass <|.. PassportModule
    IDocClass <|.. PoverkaModule
    IDocClass <|.. KMHModule
```

## Фабрика документов

```mermaid
sequenceDiagram
    participant Controller
    participant AppConfigService
    participant Registry
    participant Activator
    participant Module

    Controller->>AppConfigService: GetDocumentClass(deviceId, docId)
    AppConfigService->>AppConfigService: Resolve document type
    AppConfigService->>Registry: Lookup Type by key
    Registry-->>AppConfigService: Type info

    alt Module found in registry
        AppConfigService->>Activator: CreateInstance(type, args)
        Activator->>Module: new()
        Module-->>AppConfigService: Instance
        AppConfigService-->>Controller: IDocClass instance
    else Module not found
        AppConfigService-->>Controller: throw Exception
    end
```

## Структура модулей документов

### Организация по типам

```mermaid
graph TB
    subgraph "Document Modules (43 libraries)"
        subgraph "Core Documents"
            Passport[Passport]
            Act[Act]
            Report[Report]
            Jornal[Jornal]
        end

        subgraph "Verification - Poverka (21)"
            P1974[Poverka1974 variants]
            P2816[Poverka2816 MI]
            P3151[Poverka3151 GOST]
            P3265[Poverka3265 variants]
            P3380[Poverka3380]
            PSikn[PoverkaSikn425 variants]
        end

        subgraph "Quality Control - KMH (14)"
            KMH3265[KMH3265 variants]
            KMH3288[KMH3288 variants]
            KMH3312[KMH3312 variants]
            KMHMI[KMH_MI2816]
            KMHPP[KMH_PP variants]
            KMHSikn[KMHSikn425 variants]
        end

        subgraph "Common Libraries"
            Common1974[CommonPoverka1974]
            CommonSikn[CommonSikn425]
        end
    end
```

### Категории документов

#### 1. Passport (Паспорта качества)
- **Назначение**: Сертификация качества нефтепродуктов
- **Стандарты**: ГОСТ Р 50.2.040, МИ 3532, EAC
- **Особенности**:
  - Интеграция с ELIS
  - Справочники показателей качества
  - Методы испытаний
  - **История изменений полей (v1.4.4+)**:
    - Отслеживание источника изменения (Manual/ELIS/IVK/Unknown)
    - Хранение до 10 последних изменений на поле
    - Визуальные индикаторы источников данных
    - Popup с детальной историей при наведении
    - Автоматическая миграция из старого флага `ElisFilled`

#### 2. Poverka (Протоколы поверки) - 21 модуль
- **Назначение**: Поверка измерительных систем
- **Стандарты**:
  - ГОСТ Р 8.1011-2022 (4 варианта 1974)
  - МИ 2816
  - ГОСТ 3151, 3189, 3265, 3267, 3272, 3287, 3288, 3312, 3380
  - SIKN-425 (2 варианта)

#### 3. KMH (Контроль метрологических характеристик) - 14 модулей
- **Назначение**: Текущий контроль точности измерений
- **Типы**:
  - По давлению (PR_PU, PR_PR)
  - По плотности (PP, PP_Areom)
  - По массе/объему (PW, PV)
  - По температуре (TPR)
  - SIKN-425 (2 варианта)

#### 4. Act (Акты приема-сдачи)
- **Назначение**: Документирование приема-передачи нефтепродуктов
- **Особенности**: Автоматическое заполнение из паспортов

#### 5. Report & Jornal (Отчеты и журналы)
- **Назначение**: Сводные отчеты и журналы учета
- **Типы**: По периодам, по показателям

## Конфигурация модулей

### Структура конфигурационных файлов

```mermaid
graph LR
    subgraph "Configuration Files"
        CFG[Cfg{DocType}.json]
        EDIT[CfgEdit{DocType}.json]
        TEMPLATE[{Number}_{DocType}.frx]
    end

    subgraph "Configuration Data"
        PATH[Template Path]
        EXPORT[Export Settings]
        FIELDS[Form Fields]
        VALIDATION[Validation Rules]
    end

    CFG --> PATH
    CFG --> EXPORT
    EDIT --> FIELDS
    EDIT --> VALIDATION
    TEMPLATE --> PATH
```

### Пример конфигурации

**CfgPassport.json:**
```json
{
  "PathTemplateFile": "Doc/Passport/Passport_GOSTR50.2.040(I).frx",
  "ShowEditButton": true,
  "EnableELISIntegration": true,
  "ExportFormats": ["PDF", "Excel"]
}
```

**CfgEditPassport.json:**
```json
{
  "Fields": [
    {
      "Name": "PassportNumber",
      "Type": "String",
      "Required": true,
      "MaxLength": 50
    },
    {
      "Name": "ProductName",
      "Type": "Dictionary",
      "DictionarySource": "Products"
    }
  ]
}
```

## Процесс генерации документа

```mermaid
flowchart TD
    Start([Запрос документа]) --> LoadModule[Загрузка модуля]
    LoadModule --> GetData[GetViewDoc: Извлечение данных]

    GetData --> Query1[SQL запрос к БД ИВК]
    Query1 --> Transform[Трансформация в JSON]

    Transform --> Template[GetPathTemplateFile]
    Template --> LoadFRX[Загрузка .frx шаблона]

    LoadFRX --> Inject[Инъекция JSON в параметр JsonDoc]
    Inject --> Execute[Выполнение скриптов шаблона]

    Execute --> Render{Формат экспорта}
    Render -->|PDF| ExportPDF[Export to PDF]
    Render -->|Excel| ExportExcel[Export to Excel]
    Render -->|ODS| ExportODS[Export to ODS]

    ExportPDF --> Memory[MemoryStream]
    ExportExcel --> Memory
    ExportODS --> Memory

    Memory --> Buffer[ReportBuffer]
    Buffer --> Client[Возврат клиенту]
    Client --> End([Завершение])
```

## Загрузка модулей

### Стратегия загрузки

```mermaid
graph TB
    subgraph "Startup"
        Scan[Assembly Scanner]
        Discovery[Module Discovery]
    end

    subgraph "Sources"
        DLL[Dll/ Directory]
        Compiled[Compiled Assemblies]
        Runtime[Runtime Compilation]
    end

    subgraph "Registry"
        Cache[Type Cache]
        Mapping[Device→Doc→Type Mapping]
    end

    Scan --> DLL
    Scan --> Compiled
    Scan --> Runtime

    DLL --> Discovery
    Compiled --> Discovery
    Runtime --> Discovery

    Discovery --> Cache
    Cache --> Mapping
```

### Регистрация модулей

**Автоматическая регистрация:**
```csharp
// При старте приложения
foreach (var assembly in LoadedAssemblies)
{
    var documentTypes = assembly.GetTypes()
        .Where(t => typeof(IDocClass).IsAssignableFrom(t))
        .Where(t => !t.IsInterface && !t.IsAbstract);

    foreach (var type in documentTypes)
    {
        var attribute = type.GetCustomAttribute<DocumentTypeAttribute>();
        _registry[attribute.TypeId] = type;
    }
}
```

## Работа с данными

### Извлечение данных из БД

```mermaid
sequenceDiagram
    participant Module
    participant DbContext
    participant MySQL
    participant JsonConverter

    Module->>DbContext: Query measurements
    DbContext->>MySQL: SELECT * FROM measurements WHERE id=?
    MySQL-->>DbContext: ResultSet
    DbContext-->>Module: Entity objects

    Module->>Module: Business logic processing
    Module->>JsonConverter: Convert to JSON
    JsonConverter-->>Module: JSON string

    Note over Module: JSON содержит:<br/>- Измерения<br/>- Метаданные<br/>- Вычисленные значения
```

### Формат JSON данных

```json
{
  "DocType": "Passport",
  "Header": {
    "Number": "ПК-2025-001",
    "Date": "2025-10-02",
    "Device": "ИВК-1"
  },
  "Measurements": [
    {
      "Parameter": "Density",
      "Value": 850.5,
      "Unit": "kg/m³",
      "Method": "ГОСТ 3900"
    }
  ],
  "QualityIndicators": {
    "Viscosity": {
      "Value": 5.2,
      "Norm": "5.0 - 6.0",
      "Result": "Соответствует"
    }
  }
}
```

## FastReport Integration

```mermaid
graph TB
    subgraph "Template Structure"
        FRX[.frx File]
        Bands[Report Bands]
        DataSources[Data Sources]
        Scripts[C# Scripts]
    end

    subgraph "Data Binding"
        JsonParam[JsonDoc Parameter]
        Deserialize[JSON Deserialization]
        Objects[.NET Objects]
    end

    subgraph "Processing"
        PrepareReport[PrepareReport]
        CalcFields[Calculated Fields]
        Totals[Totals & Aggregates]
    end

    FRX --> Bands
    FRX --> DataSources
    FRX --> Scripts

    JsonParam --> Deserialize
    Deserialize --> Objects
    Objects --> DataSources

    DataSources --> PrepareReport
    Scripts --> CalcFields
    PrepareReport --> Totals
```

## Расширение системы новым модулем

### Шаги добавления нового модуля

1. **Создать класс модуля:**
```csharp
[DocumentType("NewDoc")]
public class NewDocModule : IDocClass
{
    public string GetViewDoc(int id) { /* ... */ }
    public string GetPathTemplateFile() { /* ... */ }
    public string GetEditDoc(int id) { /* ... */ }
    public void SetDocFromJson(string json) { /* ... */ }
}
```

2. **Создать конфигурацию:**
   - `Cfg/CfgNewDoc.json`
   - `Cfg/CfgEditNewDoc.json`

3. **Создать шаблон FastReport:**
   - `Doc/NewDoc/{Number}_NewDoc.frx`

4. **Зарегистрировать в CfgApp.json:**
```json
{
  "Devices": [
    {
      "IdDevice": "IVK-1",
      "Documents": [
        {
          "IdDoc": "NewDoc",
          "DisplayName": "Новый документ",
          "ModuleAssembly": "TN.NewDoc.dll"
        }
      ]
    }
  ]
}
```

```mermaid
flowchart LR
    A[1. Create Module Class] --> B[2. Implement IDocClass]
    B --> C[3. Create Configurations]
    C --> D[4. Design FastReport Template]
    D --> E[5. Register in CfgApp.json]
    E --> F[6. Build & Deploy]
    F --> G[Module Ready]
```

## Field History Tracking (v1.4.4+)

### Структура DataARM с историей

**Расширенный формат JSON:**
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
  "FieldHistoryMap": {
    "ExportPermit": [
      {
        "Source": "Manual",
        "ModifiedAt": "2025-01-14T09:00:00",
        "ModifiedBy": "Пользователь",
        "Value": "АБВ123",
        "PreviousValue": null,
        "Comment": null
      }
    ],
    "value.Density": [
      {
        "Source": "ELIS",
        "ModifiedAt": "2025-01-14T10:00:00",
        "ModifiedBy": "ELIS",
        "Value": "850.5",
        "PreviousValue": null,
        "Comment": "Загружено из протокола ПР-2024-12345"
      },
      {
        "Source": "Manual",
        "ModifiedAt": "2025-01-14T10:32:00",
        "ModifiedBy": "Пользователь",
        "Value": "850.567",
        "PreviousValue": "850.5",
        "Comment": "Скорректировано вручную"
      },
      {
        "Source": "IVK",
        "ModifiedAt": "2025-01-14T10:35:00",
        "ModifiedBy": "IVK",
        "Value": "850.57",
        "PreviousValue": "850.567",
        "Comment": "Округлено системой ИВК"
      }
    ],
    "method.Density": [
      {
        "Source": "ELIS",
        "ModifiedAt": "2025-01-14T10:00:00",
        "ModifiedBy": "ELIS",
        "Value": "{\"Name\":\"ГОСТ 3900-85\",\"Id\":5}",
        "PreviousValue": null,
        "Comment": "Метод из протокола ELIS"
      }
    ]
  },
  "ElisProtocol": {...}
}
```

### Ключевые особенности реализации

**Backend (C#):**
- `DataSource` enum: Unknown, ELIS, Manual, IVK
- `FieldHistoryEntry` класс с полями Source, ModifiedAt, ModifiedBy, Value, PreviousValue, Comment
- `DataARM.FieldHistoryMap` - Dictionary<string, List<FieldHistoryEntry>>
- `DataARM.AddFieldHistoryEntry()` - автоматический FIFO (max 10 записей)
- `DataARM.GetLastSourceForControl()` - получение последнего источника

**Frontend (Vue 3 + TypeScript):**
- `useFieldHistory.ts` композабл для работы с историей
- `FieldHistoryIndicator.vue` - индикатор источника (14-16px badge)
- `FieldHistoryPopup.vue` - popup с детальной историей (PrimeVue OverlayPanel)
- `FormFieldWithHistory.vue` - обёртка для AdditionalInfo полей
- Специальные компоненты для таблицы параметров качества:
  - `PassportMeasurementInputWithHistory.vue` (для value.*)
  - `PassportMethodSelectWithHistory.vue` (для method.*)
  - `PassportResultCellWithHistory.vue` (для result.*)

**Раздельные ключи истории:**
- AdditionalInfo: прямые ключи (`ExportPermit`, `Sample`, `Laboratory_IOF` и т.д.)
- Параметры качества:
  - `value.{ParameterKey}` - измеренное значение
  - `result.{ParameterKey}` - результат для печати
  - `method.{ParameterKey}` - метод испытаний
  - `document.{ParameterKey}` - номер документа ELIS

**Миграция:**
- Автоматическое создание записи истории из старого `ElisFilled` при первой загрузке
- Обратная совместимость: `ElisFilled` автоматически пересчитывается на основе последнего источника

**Примеры использования:**

```typescript
// Frontend: Отслеживание ручного изменения
import { useFieldHistory } from '@/composables/useFieldHistory';

const { trackManualChange } = useFieldHistory();

function handleValueChange(fieldKey: string, newValue: any, oldValue: any) {
  trackManualChange(fieldKey, newValue, oldValue);
  // ... остальная логика
}

// Frontend: Отслеживание загрузки из ELIS
const { trackElisLoad } = useFieldHistory();

function loadFromElis(fieldKey: string, elisValue: any, protocolNumber: string) {
  trackElisLoad(fieldKey, elisValue, protocolNumber);
  // ... остальная логика
}
```

```csharp
// Backend: Добавление записи истории в DocUpdate
foreach (var item in correctionData.Values.Where(x => x.Tag == "Value"))
{
    // ... обработка значения

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
            _logger.Info($"История параметра {parameterKey}: {historyEntry.Source} от {historyEntry.ModifiedBy}");
        }
    }
}
```

## См. также

- [Architecture Overview](overview.md)
- [FastReport Templates Guide](../development/fastreport-templates.md)
- [Adding New Module Tutorial](../development/new-module-tutorial.md)
- [Field History Implementation Plan](../../tech_debt/FIELD_HISTORY_IMPLEMENTATION_PLAN.md)
- [Field History Tracking Prompt](../../tech_debt/FIELD_HISTORY_TRACKING_PROMPT.md)
