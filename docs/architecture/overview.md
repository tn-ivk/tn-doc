# Архитектура TN_Doc

## Обзор

TN_Doc построен на основе многослойной архитектуры с четким разделением ответственности между компонентами.

## Общая архитектура системы

```mermaid
graph TB
    subgraph "Клиент"
        Browser[Web Browser]
        StatusBar[StatusBar Vue.js]
        Configurator[Configurator Vue.js]
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
    Configurator --> Controllers
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
- Vue.js StatusBar и Configurator (SPA)
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
- `IAppConfigService` - управление конфигурацией + фабрика документов
- `IDocModuleLoader` - динамическая загрузка модулей документов (LRU, макс. 5)
- `IConfigurationCacheService` - кэш JSON-конфигов (LRU, макс. 50)
- `IConfigurationService` - управление конфигурацией и документами (веб)
- `IDeviceConnectionDiagnosticService` - диагностика подключений устройств
- `StatusMonitoringService` - BackgroundService: периодическая проверка + SignalR push
- `IStatusProvider` - мониторинг здоровья системы (многоканальный)
- `PrinterService` - платформо-зависимая печать
- `IReportBuffer` - in-memory PDF хранилище
- `LoggingPathService` - кросс-платформенное определение путей логирования

**Ответственность:**
- Бизнес-правила генерации документов
- Управление конфигурацией
- Создание экземпляров модулей документов
- Мониторинг здоровья системы
- Диагностика подключений к устройствам ИВК
- Кэширование конфигурационных файлов

```mermaid
classDiagram
    class IAppConfigService {
        <<interface>>
        +GetAppCfg() CfgApp
        +GetDeviceCfg(idDevice) Device
        +GetPathToDocDll(idDevice, idDoc) string
        +GetPathTemplateFile(idDevice, idDoc) string
    }

    class IDocModuleLoader {
        <<interface>>
        +LoadDocsModule(options, idDevice, idDoc, baseDirectory) DocGeneral
    }

    class AppConfigService
    class DocModuleLoader

    class DocGeneral {
        +GetViewDoc(id) object
        +GetViewDoc(id, protocolNumber) object
        +GetPathTemplateFile() string
        +GetEditDoc(id) string
    }

    IAppConfigService <|.. AppConfigService
    IDocModuleLoader <|.. DocModuleLoader
    DocModuleLoader ..> DocGeneral : creates
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
    participant DocLoader
    participant AppConfig
    participant DocModule
    participant FastReport
    participant ReportBuffer

    User->>Controller: Запрос документа (deviceId, docId, recordId)
    Controller->>DocLoader: LoadDocsModule(options, deviceId, docId, baseDir)
    DocLoader->>AppConfig: GetPathToDocDll(deviceId, docId)
    DocLoader->>DocModule: Создать экземпляр модуля
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
        DocLoader[CachedDocModuleLoader]
        ConfigCache[ConfigurationCacheService]
        LogPath[LoggingPathService]
        DiagSvc[DeviceConnectionDiagnosticService]
        DbSchema[DbSchemaCache]
        ClientTracker[AppClientTracker]
    end

    subgraph "Scoped Services"
        StatusProv[StatusProvider]
        ConfigSvc[ConfigurationService]
    end

    subgraph "Hosted Services"
        Monitor[StatusMonitoringService]
    end

    subgraph "Transient Services"
        Printer[PrinterService]
    end

    SC --> AppConfig
    SC --> ReportBuffer
    SC --> DocLoader
    SC --> ConfigCache
    SC --> LogPath
    SC --> DiagSvc
    SC --> DbSchema
    SC --> ClientTracker
    SC --> StatusProv
    SC --> ConfigSvc
    SC --> Monitor
    SC --> Printer
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
   - Настройки устройств ИВК (Devices, UsedSI)
   - Строки подключения к БД (DBConnectionStrings)
   - ELIS интеграция
   - OPC серверы (ARM и per-device)
   - Флаги безопасности (UseSecurityFeatures)
   - Диагностика подключений (DeviceConnectionDiagnostic)

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

    Note over Monitor: Background Service (60s interval)

    Vue->>SignalR: Connect to /statusHub
    SignalR->>Hub: Establish connection

    loop Every 60 seconds
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
    subgraph "Module Loading"
        AppConfig[AppConfigService]
        Loader[DocModuleLoader]
        Dll[Doc DLL]
        Activator[Activator.CreateInstance]
    end

    AppConfig --> Loader
    Loader --> Dll
    Loader --> Activator
    Activator --> Instance[DocGeneral Instance]
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

## См. также

- [Document Modules Architecture](document-modules.md)
- [StatusBar Architecture](statusbar.md)
- [API Endpoints](../api/endpoints.md)
- [Deployment Guide](../deployment/linux.md)
