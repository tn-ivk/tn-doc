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

## Базовый класс модуля документа

Все модули документов наследуются от базового класса `DocGeneral` (namespace `TN.Doc`) и переопределяют ключевые методы доступа к данным и шаблону:

```mermaid
classDiagram
    class DocGeneral {
        +GetViewDoc(int id) object
        +GetViewDoc(int id, int protocolNumber) object
        +GetPathTemplateFile() string
        +GetEditDoc(int id) string
    }

    class PassportModule
    class PoverkaModule
    class KMHModule

    DocGeneral <|-- PassportModule
    DocGeneral <|-- PoverkaModule
    DocGeneral <|-- KMHModule
```

## Фабрика документов

```mermaid
sequenceDiagram
    participant Controller
    participant DocModuleLoader
    participant AppConfigService
    participant Activator
    participant Module

    Controller->>DocModuleLoader: LoadDocsModule(options, deviceId, docId, baseDir)
    DocModuleLoader->>AppConfigService: GetPathToDocDll(deviceId, docId)
    DocModuleLoader->>DocModuleLoader: Load assembly by path

    alt Module found in DLL
        DocModuleLoader->>Activator: CreateInstance(type, args)
        Activator->>Module: new DocGeneral(...)
        Module-->>DocModuleLoader: Instance
        DocModuleLoader-->>Controller: DocGeneral instance
    else Module not found
        DocModuleLoader-->>Controller: null/exception
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
    subgraph "Config"
        Cfg[CfgApp.json]
    end

    subgraph "Loader"
        Loader[DocModuleLoader]
        Dll[PathToDocDll]
        Assembly[Assembly.LoadFrom]
    end

    subgraph "Module"
        Instance[DocGeneral instance]
    end

    Cfg --> Loader
    Loader --> Dll
    Dll --> Assembly
    Assembly --> Instance
```

### Регистрация модулей

**Создание экземпляра модуля:**
```csharp
// Упрощенно (логика DocModuleLoader)
var assembly = Assembly.LoadFrom(pathToDll);
var docType = assembly.GetTypes()
    .Single(t => t.BaseType?.Name == nameof(DocGeneral));

var instance = (DocGeneral)assembly.CreateInstance(
    docType.FullName,
    false,
    BindingFlags.Default,
    null,
    new object[] { options, appConfig, configCache, deviceId, docId, baseDirectory },
    null,
    null);
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
public class NewDocModule : DocGeneral
{
    public NewDocModule(DbContextOptions<DocGeneral> options,
        IAppConfigService appConfig,
        IConfigurationCacheService configCache,
        int idDevice,
        IdDoc idDoc,
        string path)
        : base(options, appConfig, configCache, idDevice, idDoc, path) { }

    public override object GetViewDoc(int id) { /* ... */ }
    public override string GetEditDoc(int id) { /* ... */ }
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
    A[1. Create Module Class] --> B[2. Inherit DocGeneral]
    B --> C[3. Create Configurations]
    C --> D[4. Design FastReport Template]
    D --> E[5. Register in CfgApp.json]
    E --> F[6. Build & Deploy]
    F --> G[Module Ready]
```

## См. также

- [Architecture Overview](overview.md)
- [FastReport Templates Guide](../development/fastreport-templates.md)
- [Adding New Module Tutorial](../development/new-module-tutorial.md)
