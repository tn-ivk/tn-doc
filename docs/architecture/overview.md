# Архитектура TN_Doc

## Обзор

TN_Doc построен на основе многослойной архитектуры с четким разделением ответственности между компонентами.

## Общая архитектура системы

```mermaid
graph TB
    subgraph "Клиент"
        Browser[Web Browser]
        StatusBar[StatusBar Vue.js]
    end

    subgraph "Web Layer - ASP.NET Core"
        Controllers[Controllers]
        Middleware[Middleware]
        SignalR[SignalR Hubs]
    end

    subgraph "Business Logic Layer"
        AppConfig[AppConfigService]
        DocFactory[Document Factory]
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
    StatusBar --> SignalR
    Controllers --> Services
    Services --> AppConfig
    Services --> DocFactory
    DocFactory --> Passport
    DocFactory --> Poverka
    DocFactory --> KMH
    DocFactory --> Other
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
- Vue.js StatusBar (SPA)
- SignalR Hubs для real-time обновлений

**Ответственность:**
- Обработка HTTP запросов
- Рендеринг пользовательского интерфейса
- Real-time обновления статусов
- Валидация входных данных

```mermaid
graph LR
    A[HTTP Request] --> B{Router}
    B --> C[Controller Action]
    C --> D[Service Layer]
    D --> E[Response]
    E --> F[View/JSON]

    G[WebSocket] --> H[SignalR Hub]
    H --> D
    D --> I[Broadcast]
    I --> G
```

### 2. Business Logic Layer (Бизнес-логика)

**Компоненты:**
- `IAppConfigService` - управление конфигурацией и фабрика документов
- `PrinterService` - управление печатью
- `DirectoryService` - работа с файловой системой
- `StatusProvider` - мониторинг статусов
- `IReportBuffer` - буфер для PDF в памяти

**Ответственность:**
- Бизнес-правила генерации документов
- Управление конфигурацией
- Создание экземпляров модулей документов
- Мониторинг здоровья системы

```mermaid
classDiagram
    class IAppConfigService {
        <<interface>>
        +GetDocumentClass(idDevice, idDoc) IDocClass
        +GetConfig() CfgApp
        +GetDeviceConfig(idDevice) DeviceConfig
    }

    class AppConfigService {
        -CfgApp _config
        -Dictionary~string, Type~ _documentTypes
        +GetDocumentClass(idDevice, idDoc) IDocClass
        +LoadConfiguration() void
    }

    class IDocClass {
        <<interface>>
        +GetViewDoc(id) string
        +GetPathTemplateFile() string
        +GetEditDoc(id) string
        +SetDocFromJson(json) void
    }

    IAppConfigService <|.. AppConfigService
    AppConfigService ..> IDocClass : creates
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

**Архитектура генерации документов:**

```mermaid
sequenceDiagram
    participant User
    participant Controller
    participant AppConfig
    participant DocModule
    participant FastReport
    participant ReportBuffer

    User->>Controller: Запрос документа (deviceId, docId, recordId)
    Controller->>AppConfig: GetDocumentClass(deviceId, docId)
    AppConfig->>DocModule: Создать экземпляр модуля
    DocModule-->>Controller: Instance

    Controller->>DocModule: GetViewDoc(recordId)
    DocModule->>DocModule: Загрузить данные из БД
    DocModule->>DocModule: Подготовить JSON
    DocModule-->>Controller: JSON данные

    Controller->>DocModule: GetPathTemplateFile()
    DocModule-->>Controller: Путь к .frx шаблону

    Controller->>FastReport: LoadTemplate(path)
    Controller->>FastReport: SetDataSource(json)
    Controller->>FastReport: PrepareReport()
    Controller->>FastReport: ExportToPDF(MemoryStream)

    FastReport-->>ReportBuffer: PDF bytes
    ReportBuffer-->>User: PDF документ
```

## Dependency Injection Architecture

```mermaid
graph TB
    subgraph "Startup.cs / Program.cs"
        SC[Service Collection]
    end

    subgraph "Singleton Services"
        AppConfig[AppConfigService]
        ReportBuffer[ReportBuffer]
        DocLoader[DocModuleLoader]
    end

    subgraph "Scoped Services"
        DbCtx[DbContext]
        StatusProv[StatusProvider]
        SchemaCache[DbSchemaCache]
    end

    subgraph "Transient Services"
        Printer[PrinterService]
        Directory[DirectoryService]
    end

    SC --> AppConfig
    SC --> ReportBuffer
    SC --> DocLoader
    SC --> DbCtx
    SC --> StatusProv
    SC --> SchemaCache
    SC --> Printer
    SC --> Directory
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

## StatusBar Real-time Architecture

```mermaid
sequenceDiagram
    participant Vue as Vue StatusBar
    participant SignalR as SignalR Client
    participant Hub as StatusHub
    participant Monitor as StatusMonitoringService
    participant Provider as StatusProvider
    participant External as External Systems

    Note over Monitor: Background Service (30s interval)

    Vue->>SignalR: Connect to /statusHub
    SignalR->>Hub: Establish connection

    loop Every 30 seconds
        Monitor->>Provider: CheckAllStatuses()
        Provider->>External: Check DB connections
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

## Module Loading Architecture

```mermaid
graph TB
    subgraph "Module Discovery"
        Scanner[Assembly Scanner]
        Dll[Dll/ Directory]
        Compiled[Compiled Assemblies]
    end

    subgraph "Module Registry"
        Registry[Document Type Registry]
        Cache[Type Cache]
    end

    subgraph "Module Instantiation"
        Factory[Document Factory]
        Activator[Activator.CreateInstance]
    end

    Scanner --> Dll
    Scanner --> Compiled
    Dll --> Registry
    Compiled --> Registry
    Registry --> Cache

    Factory --> Cache
    Factory --> Activator
    Activator --> Instance[IDocClass Instance]
```

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

```mermaid
graph TB
    subgraph "Frontend - Vue Components"
        FieldIndicator[FieldHistoryIndicator.vue]
        HistoryPopup[FieldHistoryPopup.vue]
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
    FieldWrapper --> HistoryPopup
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
    Vue->>Vue: Показать FieldHistoryPopup
    Vue-->>User: История изменений (до 10 записей)
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
- Триггер popup: hover на индикаторе

**FieldHistoryPopup (max 400px height):**
- PrimeVue OverlayPanel с прокруткой
- История изменений: последнее изменение сверху
- Формат: Источник + Дата/время + Старое → Новое значение
- Закрытие: клик вне области, ESC

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

## См. также

- [Document Modules Architecture](document-modules.md)
- [StatusBar Architecture](statusbar.md)
- [API Endpoints](../api/endpoints.md)
- [Deployment Guide](../deployment/linux.md)
- [Field History Feature Documentation](../features/field-history.md)
