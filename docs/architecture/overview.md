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

## См. также

- [Document Modules Architecture](document-modules.md)
- [StatusBar Architecture](statusbar.md)
- [API Endpoints](../api/endpoints.md)
- [Deployment Guide](../deployment/linux.md)
