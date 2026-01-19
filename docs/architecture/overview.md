# Архитектура TN_Doc

## Обзор

TN_Doc построен на основе многослойной архитектуры с четким разделением ответственности между компонентами.

**Текущая версия**: 1.3.8
**Основные компоненты**: ASP.NET Core 8.0 backend + Vue 3 frontend (Configurator, Document Editor)

**Ключевые возможности**:
- **Configurator (Vue SPA)** для управления конфигурацией приложения
- **Document Editor (Vue SPA)** для редактирования документов (в разработке)
- **FastReport** для генерации PDF отчётов
- **Интеграция с ELIS** для загрузки лабораторных данных
- **Система модулей документов** (~48 библиотек)

## Общая архитектура системы

```mermaid
graph TB
    subgraph "Клиент"
        Browser[Web Browser]
        Configurator[Configurator Vue 3]
        DocumentEditor[Document Editor Vue 3]
    end

    subgraph "Web Layer - ASP.NET Core"
        Controllers[Controllers]
        Middleware[Middleware]
    end

    subgraph "Business Logic Layer"
        AppConfig[AppConfigService]
        DocModuleLoader[DocModuleLoader]
        Services[Business Services]
        FastReport[FastReport Engine]
    end

    subgraph "Data Access Layer"
        EF[Entity Framework Core]
        DbContext[DbContext]
    end

    subgraph "Document Modules"
        Passport[Passport Module]
        Poverka[Poverka* Modules]
        KMH[KMH* Modules]
        Other[Other Modules]
    end

    subgraph "External Systems"
        MySQL[(MySQL/MariaDB)]
        OPC[OPC DA/UA Servers]
        ELIS[ELIS Lab System]
        MessagingService[TN_MessagingService]
    end

    Browser --> Controllers
    Configurator --> Controllers
    DocumentEditor --> Controllers
    Controllers --> Services
    Services --> AppConfig
    AppConfig --> DocModuleLoader[DocModuleLoader]
    DocModuleLoader --> Passport
    DocModuleLoader --> Poverka
    DocModuleLoader --> KMH
    DocModuleLoader --> Other
    Passport --> FastReport
    Poverka --> FastReport
    KMH --> FastReport
    Services --> EF
    EF --> DbContext
    DbContext --> MySQL
    Services --> OPC
    Services --> ELIS
    Services --> MessagingService
```

## Слои приложения

### 1. Presentation Layer (Представление)

**Компоненты:**
- ASP.NET Core MVC Controllers
- Razor Views
- Vue 3 Components (Configurator, Document Editor)

**Vue Components:**
- **Configurator** (`TN_Doc/Client/configurator/`) - веб-интерфейс управления конфигурацией
  - Framework: Vue 3 + TypeScript + PrimeVue
  - Dev server: port 5174
  - Build output: `wwwroot/configurator/`
  - Вкладки: General, Devices, Documents, OPC Connections, ELIS Connections
  - Статус: Production
- **Document Editor** (`TN_Doc/Client/document-editor/`) - редактирование документов
  - Framework: Vue 3 + TypeScript + PrimeVue
  - Dev server: port 5175
  - Build output: `wwwroot/document-editor/`
  - Функции: редактирование полей, ELIS интеграция
  - Статус: В активной разработке

**Ответственность:**
- Обработка HTTP запросов
- Рендеринг пользовательского интерфейса
- Валидация входных данных

```mermaid
graph LR
    A[HTTP Request] --> B{Router}
    B --> C[Controller Action]
    C --> D[Service Layer]
    D --> E[Response]
    E --> F[View/JSON]
```

### 2. Business Logic Layer (Бизнес-логика)

**Компоненты:**
- `AppConfigService` - управление конфигурацией приложения
- `PrinterService` - управление печатью
- `DirectoryService` - работа с файловой системой
- `LoggingPathService` - централизованное управление путями логов

**Ответственность:**
- Бизнес-правила генерации документов
- Управление конфигурацией приложения
- Управление печатью документов
- Работа с файловой системой

```mermaid
classDiagram
    class AppConfigService {
        -CfgApp _config
        +LoadConfiguration() void
        +GetDeviceConfig(idDevice) DeviceConfig
        +GetDeviceName(idDevice) string
    }

    class PrinterService {
        -AbsPrinter _printer
        +PrintDocument(filePath) void
    }

    class DirectoryService {
        +GetBaseDirectory() string
        +EnsureDirectoryExists(path) void
    }

    class LoggingPathService {
        +GetLogDirectory() string
    }

    class DocGeneral {
        <<abstract>>
        +GetViewDoc(id) object
        +GetPathTemplateFile() string
        +GetList(UTBegin, UTEnd) List~RequestListDocs~
    }

    PrinterService --> AppConfigService : uses
    DirectoryService --> AppConfigService : uses
```

### 3. Data Access Layer (Доступ к данным)

**Компоненты:**
- Entity Framework Core
- DbContext implementations
- Repository Pattern (опционально)

**Ответственность:**
- Взаимодействие с базами данных
- ORM mapping
- Миграции схемы

### 4. Document Generation Layer

**Архитектура генерации документов (через HomeController):**

```mermaid
sequenceDiagram
    participant User
    participant HomeController
    participant DocModule as DocGeneral (DLL)
    participant FastReport

    User->>HomeController: Запрос документа (deviceId, docId, recordId)
    HomeController->>HomeController: Загрузка модуля через Reflection
    Note over HomeController: Assembly.LoadFrom + CreateInstance

    HomeController->>DocModule: GetViewDoc(recordId)
    DocModule->>DocModule: Загрузить данные из БД
    DocModule->>DocModule: Подготовить JSON
    DocModule-->>HomeController: JSON данные

    HomeController->>DocModule: GetPathTemplateFile()
    DocModule-->>HomeController: Путь к .frx шаблону

    HomeController->>FastReport: LoadTemplate(path)
    HomeController->>FastReport: SetDataSource(json)
    HomeController->>FastReport: PrepareReport()
    HomeController->>FastReport: ExportToPDF(MemoryStream)

    FastReport-->>User: PDF документ
```

**Архитектура редактирования документов (через DirEditorController):**

```mermaid
sequenceDiagram
    participant User as Document Editor Vue
    participant API as DirEditorController
    participant DocModule as DocGeneral (DLL)
    participant DB as MySQL DataARM

    User->>API: GET /api/documents/{deviceId}/{docType}/edit/{id}
    API->>API: Загрузка модуля через Reflection
    Note over API: Assembly.LoadFrom + CreateInstance

    API->>API: Проверить, что doc реализует IDocumentEditor
    API->>DocModule: GetEditConfig(id)
    DocModule->>DB: SELECT данные для редактирования
    DB-->>DocModule: JSON данные + история полей
    DocModule-->>API: EditConfig (schema + initialValues + __history)
    API-->>User: JSON конфигурация формы

    User->>API: POST /api/documents/{deviceId}/{docType}/save/{id}
    API->>API: Загрузка модуля через Reflection
    API->>DocModule: SaveDocument(id, values)
    DocModule->>DB: UPDATE документа
    DB-->>DocModule: Success
    DocModule-->>API: true
    API-->>User: {success: true}
```

## Dependency Injection Architecture

```mermaid
graph TB
    subgraph "Startup.cs"
        SC[Service Collection]
    end

    subgraph "Singleton Services"
        AppInfo[AppInfoProvider]
    end

    subgraph "Scoped Services"
        DbCtx[DocGeneral DbContext]
    end

    subgraph "Transient Services"
        Printer[AbsPrinter<br/>WindowsPrinter/LinuxPrinter]
        PrinterService[PrinterService]
    end

    SC --> AppInfo
    SC --> DbCtx
    SC --> Printer
    SC --> PrinterService
```

## Configuration Architecture

```mermaid
graph TB
    subgraph "Configuration Files"
        AS[appsettings.json]
        ASE[appsettings.Environment.json]
        CA[CfgApp.json]
        CD[Cfg{DocType}.json]
        CE[CfgEdit{DocType}.json]
    end

    subgraph "Configuration Loading"
        Builder[ConfigurationBuilder]
        Options[IOptions Pattern]
    end

    subgraph "Application"
        Services[Services]
        DocModules[Document Modules]
    end

    AS --> Builder
    ASE --> Builder
    CA --> Builder
    Builder --> Options
    Options --> Services
    CD --> DocModules
    CE --> DocModules
```

### Иерархия конфигурации

1. **appsettings.json** - базовые настройки ASP.NET Core
   - Kestrel настройки
   - Logging конфигурация
   - CORS policies

2. **CfgApp.json** - основная конфигурация приложения
   - Настройки устройств ИВК
   - Строки подключения к БД
   - ELIS интеграция
   - OPC серверы
   - Флаги безопасности

3. **Cfg{DocType}.json** - конфигурация типа документа
   - Путь к шаблону
   - Настройки отчета
   - Параметры экспорта

4. **CfgEdit{DocType}.json** - конфигурация форм редактирования
   - Поля формы
   - Валидация
   - Маппинг данных
   - Настройки истории изменений полей

**Примечание**: В текущей версии проекта отсутствуют централизованные сервисы для кэширования конфигурации и загрузки модулей. Модули документов загружаются напрямую в контроллерах через Reflection.

## Контроллеры приложения

```mermaid
sequenceDiagram
    participant Client as Web Client
    participant Controller as HomeController
    participant Service as Business Service
    participant DB as Database
    participant External as External Systems

    Note over Service: Business Logic Layer

    Client->>Controller: HTTP Request
    Controller->>Service: Call business method
    Service->>DB: Query data
        Provider->>External: Check OPC servers
        Provider->>External: Check ELIS
        Provider->>External: Check MessagingService
        External-->>Provider: Status responses
        Provider-->>Monitor: StatusResponse
        Monitor->>Hub: BroadcastStatus(data)
        Hub->>SignalR: statusUpdated event
        SignalR->>Vue: Update UI
    end

    Vue->>Vue: Manual refresh (click)
    Vue->>Provider: Fetch /api/status
    Provider-->>Vue: StatusResponse
```

## Document Editor Architecture (v1.4.4+)

```mermaid
sequenceDiagram
    participant User
    participant Editor as Document Editor Vue
    participant API as DirEditorController
    participant DocModule as DocPassport (DLL)
    participant DB as MySQL DataARM
    participant Parent as Parent Window (ELIS)

    User->>Editor: Открыть документ для редактирования
    Editor->>API: GET /api/documents/{deviceId}/{docType}/edit/{id}
    API->>API: Загрузка модуля через Reflection
    API->>DocModule: GetEditConfig(id)
    DocModule->>DB: SELECT DataARM + FieldHistoryMap
    DocModule-->>API: DocumentEditConfig (initialValues + __history)
    API-->>Editor: Конфигурация формы

    Editor->>Editor: Отобразить поля и индикаторы истории

    Parent-->>Editor: postMessage { type: 'ELIS_DATA', payload }
    Editor->>Editor: Автозаполнение полей и истории (trackElisLoad)

    User->>Editor: Изменить поле вручную
    Editor->>Editor: trackManualChange(fieldKey, value)

    User->>Editor: Сохранить документ
    Editor->>API: POST /api/documents/{deviceId}/{docType}/save/{id}
    Note over Editor,API: payload включает __history
    API->>DocModule: SaveDocument(id, values)
    DocModule->>DB: UPDATE DataARM.FieldHistoryMap
    DocModule-->>API: true
    API-->>Editor: {success: true}

    User->>Editor: Обновить паспорт после подтверждения от ИВК
    Editor->>API: POST /api/documents/{deviceId}/Passport/update/{id}
    API->>DocModule: DocUpdate(id, values)
    DocModule->>DB: UPDATE DataARM
    API-->>Editor: {success: true}
```

## Module Loading Architecture

```mermaid
graph TB
    subgraph "Controller Layer"
        Controller[DirEditorController / HomeController]
    end

    subgraph "Reflection & Assembly Loading"
        LoadAsm[Assembly.LoadFrom]
        FindType[GetTypes + BaseType check]
        CreateInst[assembly.CreateInstance]
    end

    subgraph "DLL Files (tn.docgeneral)"
        PassportDll[DocPassport.dll]
        PovDll[DocPoverka*.dll]
        KmhDll[DocKmh*.dll]
        OtherDll[Other Doc DLLs]
    end

    subgraph "Runtime Instance"
        Instance[DocGeneral Instance]
    end

    Controller --> LoadAsm
    LoadAsm --> PassportDll
    LoadAsm --> PovDll
    LoadAsm --> KmhDll
    LoadAsm --> OtherDll
    LoadAsm --> FindType
    FindType --> CreateInst
    CreateInst --> Instance
    Instance --> Controller
```

## UI Theme & Styling Architecture (v1.4.3+)

### Централизация цветов через CSS переменные

```mermaid
graph TB
    subgraph "Централизованное управление цветами"
        Material3[material3.css - :root]
    end

    subgraph "CSS переменные"
        Primary["--md-primary, --md-primary-active, --md-primary-light"]
        Gray["--md-gray-600, --md-gray-700, --md-gray-800"]
        Elis["--md-elis-highlight"]
        Error["--md-error-bootstrap, --md-error-light"]
        Border["--md-border, --md-border-light"]
        Text["--md-text-muted, --md-text-light"]
        Disabled["--md-disabled-bg, --md-gray-light"]
    end

    subgraph "Использование в стилях"
        Site[site.css]
        NewStyle[newstyle.css]
        Elis_CSS[elisRequestWindow.css]
        Error_CSS[errorDialogWindow.css]
        LeftPanel[LeftPanel.css]
        MenuDropdown[menu-dropdown.css]
        ElisEditForm[elisEditForm.css]
        CommonEditForm[commonEditForm.css]
    end

    Material3 --> Primary
    Material3 --> Gray
    Material3 --> Elis
    Material3 --> Error
    Material3 --> Border
    Material3 --> Text
    Material3 --> Disabled

    Primary --> Site
    Primary --> NewStyle
    Gray --> Site
    Gray --> NewStyle
    Gray --> MenuDropdown
    Elis --> Elis_CSS
    Elis --> ElisEditForm
    Error --> Error_CSS
    Border --> LeftPanel
    Border --> Site
    Text --> Site
    Text --> NewStyle
    Disabled --> CommonEditForm
```

**Принципы:**
- Все цвета определены в `/TN_Doc/wwwroot/css/material3.css` в блоке `:root`
- Запрещено использовать hardcoded HEX-коды в других файлах стилей
- Изменение темы оформления - единственная точка редактирования (material3.css)
- CSS переменные используются через синтаксис `var(--md-variable-name)`

**Ключевые CSS переменные:**
- `--md-primary` (#1976D2) - основной цвет приложения
- `--md-primary-active` (#1565C0) - активное состояние
- `--md-gray-*` (#616161, #757575, #9E9E9E) - градации серого
- `--md-elis-highlight` (#e8f5e9) - подсветка данных ELIS
- `--md-error-*` (#d32f2f, #ffebee) - состояния ошибок
- `--md-border-*` (#e0e0e0, #f5f5f5) - границы элементов

## Security & Error Handling

```mermaid
graph TB
    subgraph "Security Features"
        Encryption[Password Encryption]
        Validation[Input Validation]
        FileAccess[File Access Control]
        SecurityFlag[UseSecurityFeatures Flag]
    end

    subgraph "Error Handling"
        GlobalError[Global Exception Handler]
        NLog[NLog Logger]
        UserError[User-friendly Errors]
    end

    subgraph "Logging Levels"
        Dev[Development: Detailed]
        Prod[Production: Minimal]
    end

    SecurityFlag -.-> Encryption
    SecurityFlag -.-> Validation
    SecurityFlag -.-> FileAccess

    GlobalError --> NLog
    GlobalError --> UserError
    NLog --> Dev
    NLog --> Prod
```

## Platform-specific Architecture

```mermaid
graph TB
    subgraph "Platform Detection"
        Runtime[RuntimeInformation]
    end

    subgraph "Windows"
        WinService[Windows Service]
        WinPrinter[winprutil.exe]
        WinLogs[TN_Doc/logs]
    end

    subgraph "Linux"
        Systemd[Systemd Service]
        CUPS[CUPS Printing]
        LinuxLogs[/opt/TN_Doc/logs]
    end

    Runtime --> WinService
    Runtime --> Systemd
    WinService --> WinPrinter
    WinService --> WinLogs
    Systemd --> CUPS
    Systemd --> LinuxLogs
```

## Field History Tracking Architecture (v1.4.4+)

Система отслеживания истории изменений полей паспорта качества для аудита источников данных.

**Требования:**
- ⚠️ **Требует включенного ELIS** в конфигурации (`CfgApp.json`: `Elis.Use = true`)
- Работает с паспортами качества (Passport document type)
- История хранится в поле `FieldHistoryMap` таблицы `DataARM` (JSON)
- Максимум 10 записей на поле (FIFO очередь)

```mermaid
graph TB
    subgraph "Frontend - Vue Components"
        FieldIndicator[FieldHistoryIndicator.vue]
        FieldWrapper[FormFieldWithHistory.vue]
        MeasurementInput[PassportMeasurementInputWithHistory.vue]
        MethodSelect[PassportMethodSelectWithHistory.vue]
        ResultCell[PassportResultCellWithHistory.vue]
    end

    subgraph "Frontend - Composables"
        UseFieldHistory[useFieldHistory.ts]
        DocumentStore[documentStore - formHistory]
    end

    subgraph "Backend - Models"
        DataSource[DataSource enum]
        HistoryEntry[FieldHistoryEntry]
        DataARM[DataARM.FieldHistoryMap]
    end

    subgraph "Backend - Processing"
        DocUpdate[DocPassport.DocUpdate]
        GetEditConfig[DocPassport.GetEditConfig]
    end

    FieldWrapper --> FieldIndicator
    MeasurementInput --> FieldIndicator
    MethodSelect --> FieldIndicator
    ResultCell --> FieldIndicator

    FieldWrapper --> UseFieldHistory
    MeasurementInput --> UseFieldHistory
    MethodSelect --> UseFieldHistory
    ResultCell --> UseFieldHistory

    UseFieldHistory --> DocumentStore
    DocumentStore --> DocUpdate
    DocUpdate --> DataARM

    GetEditConfig --> DataARM
    DataARM --> DocumentStore
```

### Структура данных истории

```mermaid
classDiagram
    class DataSource {
        <<enumeration>>
        Unknown
        ELIS
        Manual
        IVK
        ElisMissing
        Auto
        ReturnToELIS
        DefaultMethod
    }

    class FieldHistoryEntry {
        +DataSource Source
        +DateTime ModifiedAt
        +string ModifiedBy
        +string Value
        +string? PreviousValue
        +string? Comment
        +const int MaxHistoryEntries = 10
    }

    class DataARM {
        +Dictionary~string, List~FieldHistoryEntry~~ FieldHistoryMap
        +AddFieldHistoryEntry(controlId, entry)
        +GetLastSourceForControl(controlId) DataSource
    }

    FieldHistoryEntry --> DataSource
    DataARM --> FieldHistoryEntry
```

### Поток данных истории изменений

```mermaid
sequenceDiagram
    participant User
    participant Vue as Vue Component
    participant Composable as useFieldHistory
    participant Store as documentStore
    participant Backend as DocPassport
    participant DB as MySQL DataARM

    User->>Vue: Изменяет поле значения
    Vue->>Composable: trackManualChange(fieldKey, newValue)
    Composable->>Composable: createHistoryEntry(Manual, value)
    Composable->>Store: formHistory[fieldKey].push(entry)

    User->>Vue: Кликает "Сохранить"
    Vue->>Backend: POST saveDocument({ __history: formHistory })
    Backend->>Backend: DocUpdate(correctionData)
    Backend->>DB: UPDATE DataARM.FieldHistoryMap

    Note over Backend: FIFO: макс 10 записей на поле

    User->>Vue: Открывает документ снова
    Vue->>Backend: GET getEditConfig(id)
    Backend->>DB: SELECT DataARM
    DB-->>Backend: JSON с FieldHistoryMap
    Backend->>Backend: GetEditConfig()
    Backend-->>Vue: initialValues с __history
    Vue->>Store: Загрузить formHistory
    Store->>Vue: Отобразить индикаторы

    User->>Vue: Наводит на индикатор
    Vue->>Vue: Показать tooltip с источником данных
    Vue-->>User: Краткое описание источника
```

### Ключи истории для разных полей

```mermaid
graph TB
    subgraph "AdditionalInfo Fields"
        AI1["ExportPermit"]
        AI2["Sample"]
        AI3["Laboratory_IOF"]
    end

    subgraph "Quality Parameters"
        QP1["value.{ParameterKey}"]
        QP2["result.{ParameterKey}"]
        QP3["method.{ParameterKey}"]
        QP4["document.{ParameterKey}"]
    end

    subgraph "FieldHistoryMap Keys"
        HM1["ExportPermit → List~Entry~"]
        HM2["Sample → List~Entry~"]
        HM3["Laboratory_IOF → List~Entry~"]
        HM4["value.Density → List~Entry~"]
        HM5["result.Density → List~Entry~"]
        HM6["method.Density → List~Entry~"]
        HM7["document.Density → List~Entry~"]
    end

    AI1 --> HM1
    AI2 --> HM2
    AI3 --> HM3
    QP1 --> HM4
    QP2 --> HM5
    QP3 --> HM6
    QP4 --> HM7
```

### UI Компоненты истории

**FieldHistoryIndicator (14-16px badge):**
- Отображает последний источник изменения
- Цвета: ELIS (зелёный #4CAF50), Manual (синий #2196F3), IVK (оранжевый #FF9800)
- Позиция: правый верхний угол поля (absolute, top: 4px, right: 4px)
- Подсказка отображается через `v-tooltip` (краткое описание источника)

### Миграция из ElisFilled

```mermaid
flowchart LR
    OldData[labInfo.ElisFilled = true] --> Check{Есть запись в\nFieldHistoryMap?}
    Check -->|Нет| Create[Создать запись:\nSource=ELIS\nModifiedAt=MinValue\nComment=Миграция]
    Check -->|Да| Skip[Пропустить]
    Create --> Store[Сохранить в\nvalue.{ParameterKey}]
    Store --> Flag[Обновить\nElisFilled из истории]
```

**Логика миграции в GetEditConfig:**
```csharp
if (labInfo.ElisFilled && !dataArm.FieldHistoryMap.ContainsKey($"value.{parameterKey}"))
{
    dataArm.AddFieldHistoryEntry($"value.{parameterKey}", new FieldHistoryEntry
    {
        Source = DataSource.ELIS,
        ModifiedAt = DateTime.MinValue,
        ModifiedBy = "ELIS",
        Value = labInfo.Value,
        Comment = "Миграция из ElisFilled"
    });
}
```

### Ограничения и оптимизация

**Лимиты:**
- Максимум 10 записей истории на поле (FIFO очередь)
- При добавлении 11-й записи удаляется самая старая

**Производительность:**
- История хранится в JSON поле DataARM
- Индексация не требуется (в памяти Dictionary)
- Размер записи ~150-200 байт

**Обратная совместимость:**
- Поле `ElisFilled` (bool) помечено как `[Obsolete]` но сохранено
- Автоматический пересчёт `ElisFilled` на основе последнего источника в истории
- Миграция старых документов при первой загрузке

## Recent Changes

### v1.4.4 (Текущая версия - в разработке)

**Архитектурные улучшения:**
- ✅ **Рефакторинг DocPassport** - разделение на partial классы:
  - `DocPassport.cs` - основная логика и конструктор
  - `DocPassport.Editor.cs` - методы редактирования (GetEditConfig, SaveDocument)
  - `DocPassport.Listing.cs` - методы получения списков документов
  - `DocPassport.Update.cs` - методы обновления данных (DocUpdate)
- ✅ **Оптимизация DocUpdate** - выделение сервисов обработки обновлений в pipeline
- ✅ **Рефакторинг Poverka** - разнесение сервисов DocUpdate для всех модулей поверки
- ✅ **Улучшение HomeController** - использование IDocModuleLoader для унифицированной загрузки модулей

**Document Editor:**
- ✅ Редактор паспортов качества и актов (Vue 3 SPA)
- ✅ Модальные окна для редактирования результатов и методов (ResultEditDialog, ManualMethodDialog)
- ✅ Компоненты с историей изменений (FieldHistoryIndicator + tooltip)
- ✅ ELIS автозаполнение через postMessage
- ✅ Автозаполнение связанных параметров (LinkedParameter/SlaveKey)

**Система истории изменений полей:**
- Отслеживание источника данных (ELIS, ручное редактирование, округление ИВК)
- Визуальные индикаторы источников в UI (цветные значки: зелёный/синий/оранжевый)
- История хранится в `FieldHistoryMap` (до 10 записей на поле)
- Автоматическая миграция из старого флага `ElisFilled`
- Раздельная история для value/method/result/document полей
- ⚠️ Требует включенного ELIS в конфигурации (`Elis.Use = true`)

**Configurator Enhancements:**
- Добавлена вкладка Documents - управление конфигурацией типов документов
- Расширены вкладки OPC Connections и ELIS Connections
- Настройки измерительных приборов (СИ)

**UI Theme Improvements:**
- Централизация цветов через CSS переменные в material3.css
- Все hardcoded HEX коды заменены на CSS переменные
- Новые переменные: `--md-primary-active`, `--md-gray-*`, `--md-elis-highlight`, `--md-error-*`
- Единая точка изменения темы оформления

**Journal Report Fix:**
- Исправлена форма печати журнала регистрации СИ (совместимость с DataARM)

**Dependencies:**
- Обновлён docgeneral до версии 1.2.3

### v1.4.3 (October 2024)

**Configurator Basic Version:**
- Веб-интерфейс управления конфигурацией (`/configurator`)
- Вкладки: General, Devices, OPC Connections, ELIS Connections

### v1.4.2 (September 2024)

**Configuration Caching:**
- `IConfigurationCacheService` с LRU eviction (максимум 50 файлов)
- Автоматическая инвалидация кэша при изменении файлов

**Document Module Loading:**
- `IDocModuleLoader` для кэширования загрузки DLL документов
- Оптимизация производительности при создании экземпляров

**In-memory HTML Generation:**
- GetEditDoc теперь работает в памяти (исключено построение HTML из файла)
- Улучшена производительность и устранены race conditions

**Protocol Updates:**
- Обновлён KMH_MI2816 для поддержки ИВК версии 7.12.14.3000

**Removed Components:**
- Удалён проект TN.Tools (устаревшая функциональность)
- Удалён дублирующий шаблон `Act_GOSTR50.2.040(G)_ShiftTime.frx`

### v1.4.1 (August 2024)

**In-memory PDF Generation:**
- `IReportBuffer` для хранения PDF в памяти
- Исключены дисковые I/O операции
- Устранены ошибки "file in use" при параллельных запросах

**Configuration Improvements:**
- Локальные директории пользователей для подписантов
- Отчёты и журналы используют локальные ссылки на пользователей

**LoggingPathService Refactoring:**
- Перенесён в `TN.DocGeneral/Services/` для переиспользования
- Централизованное управление путями логов

## См. также

- [Document Modules Architecture](document-modules.md)
- [Document Editor Architecture](document-editor.md)
- [Configurator Architecture](configurator.md)
- [Passport Editor Logic](passport-editor.md)
- [Field History Feature Documentation](../features/field-history.md)
- [Deployment Guide](../deployment/linux.md)
