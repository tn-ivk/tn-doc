# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc is an ASP.NET Core 8.0 web application for generating technical documents and reports from measurement system data (ИВК - Измерительно-вычислительный комплекс). The system generates quality certificates, verification protocols, acceptance acts, and various measurement reports using FastReport templates.

**Version**: 1.4.3 (.NET 8.0)
**Main Branch**: master (для PR и релизов)
**Development Branch**: developWork (текущая разработка)
**Runtime**: .NET Runtime 8.0.13+ (SDK 9.0+ для разработки)
**Node.js**: 18.0+ и npm 8.0+ (для Vue компонентов)

**Critical Rules:**
- ⚠️ **НИКОГДА** не упоминать AI, генерацию кода, автоматизацию или "Claude" в коммитах
- ⚠️ **НИКОГДА** не добавлять "Generated with Claude Code" или подобные атрибуции
- ⚠️ **НИКОГДА** не добавлять "Co-Authored-By: Claude" или любые AI co-author теги
- ⚠️ **ВСЕГДА** использовать русский язык для коммит-сообщений

**Unreleased Changes (v1.4.4):** See [CHANGELOG.md](CHANGELOG.md) for full details.
- ⚠️ **Field History System** - requires `IsUsedElis=true` in device config
- ⚠️ **All 48 libraries migrated** to `IDocumentEditor.SaveDocument`
- ⚠️ **Breaking**: HTML edit forms marked OBSOLETE, removal planned for v2.0.0

## Essential Commands

```bash
# Build and run
dotnet build                                    # Build entire solution
cd TN_Doc && dotnet run                         # Run app (http://localhost:38509)
dotnet publish -c Release -o distrib/out        # Create release build

# Git submodules (⚠️ required after clone)
git submodule update --init --recursive         # Initialize all submodules
git submodule update --remote                   # Update submodules to latest

# Vue components (from TN_Doc/Client/)
npm install                                     # Install dependencies for all workspaces
npm run build:all                               # Build all Vue apps (statusbar, configurator, editor)
npm run dev                                     # StatusBar dev server (port 5173)
npm run dev:configurator                        # Configurator dev server (port 5174)
npm run dev:editor                              # Document Editor dev server (port 5175)
npm run type-check                              # TypeScript type checking for all workspaces
npm run clean                                   # Clean all node_modules

# Working with individual npm workspace
npm run dev --workspace=document-editor         # Dev server for specific workspace
npm run build --workspace=statusbar             # Build only StatusBar
npm run type-check --workspace=shared           # TypeCheck for shared only

# Testing
dotnet test                                     # Run all tests
dotnet test --filter "ClassName=YourTestClass"  # Run specific test class
dotnet test --filter "FullyQualifiedName~KMH"   # Test KMH libraries
dotnet test --filter "Namespace~Poverka"        # Test Poverka libraries
dotnet test --logger:"console;verbosity=detailed" # Verbose test output
dotnet test /p:CollectCoverage=true             # Run tests with coverage
```

## Quick File Paths Reference

| Purpose | Path |
|---------|------|
| Main app config | `TN_Doc/Cfg/CfgApp.json` |
| Passport edit configs | `TN_Doc/Cfg/Passport/CfgEditPassport*.json` |
| FastReport templates | `TN_Doc/Doc/*.frx` |
| Vue Document Editor | `TN_Doc/Client/document-editor/` |
| IDocumentEditor interface | `tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs` |
| DI configuration | `TN_Doc/Startup.cs`, `TN_Doc/Extensions/` |
| Logging config | `TN_Doc/nlog.config` |

## Branching Strategy

- **master**: Stable releases, PR target
- **developWork**: Active development (current default)
- Feature branches: Create from `developWork`, merge back via PR

## Quick Start

1. **Setup NuGet sources**:
   ```bash
   dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
   dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
     --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text
   ```

2. **Build and run**:
   ```bash
   git submodule update --init --recursive
   dotnet restore && dotnet build
   cd TN_Doc/Client && npm install && npm run build:all
   cd ../.. && dotnet run --project TN_Doc
   ```

## Architecture Overview

### Solution Structure
```
tn_doc/
├── TN_Doc/                    # Main ASP.NET Core web application
│   ├── Program.cs/Startup.cs  # Entry points, DI configuration
│   ├── Controllers/           # API endpoints
│   ├── Services/              # Business logic services
│   ├── Models/                # DTOs, view models, configuration models
│   ├── Cfg/                   # Document and app configuration
│   ├── Doc/                   # FastReport templates (*.frx)
│   └── Client/                # Vue 3 applications (npm workspaces)
│       ├── statusbar/         # Real-time status monitoring
│       ├── configurator/      # Configuration UI
│       ├── document-editor/   # Document editing (active development v1.4.4)
│       └── shared/            # Shared TypeScript utilities
├── TN.DocGeneral/             # Core business logic and shared utilities (part of tn.docgeneral submodule)
├── tn.docgeneral/             # ⚠️ Document module libraries (git submodule, ~48 библиотек)
├── tn_toolsfastreport/        # ⚠️ FastReport utilities (git submodule)
├── winprutil/                 # ⚠️ Windows printing utility (git submodule)
└── Tests/                     # NUnit tests with Moq
```

**⚠️ Git Submodules:**
Three directories are git submodules and must be initialized after cloning:
- `tn.docgeneral/` - All document type implementations (~48 libraries: Act, Passport, Report, Jornal, Poverka* (21), KMH* (18), Common* (3))
- `tn_toolsfastreport/` - FastReport helper utilities
- `winprutil/` - Windows printing service

Run `git submodule update --init --recursive` after initial clone.

### Document Module Pattern

Each document type implements **IDocumentEditor** interface (defined in `tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs`):

1. **`GetViewDoc(id)`**: Returns JSON data for FastReport template
2. **`GetPathTemplateFile()`**: Returns path to `.frx` template file
3. **`GetEditConfig(id)`**: Returns configuration for Vue Document Editor
4. **`SaveDocument(id, values)`**: Saves document from Vue Editor (⚠️ all libraries migrated to this API in v1.4.4)

**Configuration files per document:**
- `TN_Doc/Cfg/Cfg{DocumentType}.json` - template and report settings
- `TN_Doc/Cfg/CfgEdit{DocumentType}.json` - edit form configuration (supports `SlaveKey` for master-slave parameters)
- `TN_Doc/Doc/{Number}_{DocumentType}.frx` - FastReport template

**Master-Slave Parameters (v1.4.4+):**
- `SlaveKey` в конфигурации определяет связь между параметрами
- Мастер-параметр содержит `SlaveKey` со ссылкой на слейв
- Пример: DNP.kPa (мастер) → DNP.mercury_mm (слейв)
- Slave-параметры скрыты в Document Editor и не участвуют в валидации
- Флаг `Legal = 0` устанавливается автоматически для slave-параметров
- Подробнее: `docs/configs/passport.md`

**Linked Parameters (v1.4.4+):**
- `LinkedParameter` объединяет выбор метода испытаний для связанных параметров
- Оба параметра видимы, но используют общий комбобокс метода (rowspan)
- Примеры пар: Chloride_Salts.Concentration ↔ MassFraction, DNP.kPa ↔ mercury_mm
- Используется в конфигурациях MI3532, Export, ForActNP, EAC
- Компонент: `PassportLinkedParameterGroup.vue`
- Подробнее: `docs/configs/passport.md`

**Field History System (v1.4.4+):**
- Tracks data source for each field (ELIS, manual edit, IVK rounding)
- History stored in `__history` payload with up to 10 entries per field (FIFO)
- Visual indicators in Document Editor (colored badges: green ELIS, blue Manual, orange IVK)
- Composable `useFieldHistory.ts` для управления записями истории
- ⚠️ **КРИТИЧЕСКИ ВАЖНО:** Работает ТОЛЬКО при `IsUsedElis = true` в конфигурации
- Подробнее: `docs/features/field-history.md`

**Decimal Normalization (v1.4.4+):**
- Автоматическая нормализация количества десятичных знаков при загрузке из БД
- Применяется к полям Measurement и Result для паспорта качества
- Использует `RoundValue` из конфигурации параметра для определения количества знаков
- Предотвращает ложные срабатывания watcher'ов при сравнении значений
- Пример: если БД содержит "1.6", а `RoundValue=4`, нормализуется до "1.6000"

**IDocumentEditor Quick Reference:**
```csharp
public interface IDocumentEditor
{
    DocumentEditConfig GetEditConfig(int id);  // Returns form config for Vue editor
    bool SaveDocument(int id, Dictionary<string, object> values);  // Saves from Vue editor
}
```
Key types: `DocumentEditConfig`, `FormField`, `SelectOption` (see `tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs`)

### API Controllers

Ключевые контроллеры в `TN_Doc/Controllers/`:

| Controller | Назначение |
|------------|------------|
| `HomeController` | Главная страница, просмотр/печать документов |
| `DocumentEditController` | API редактирования документов (Vue Document Editor) |
| `ConfiguratorController` | API конфигуратора устройств и настроек |
| `StatusController` | API статуса системы (SignalR) |
| `ElisController` | Интеграция с ELIS (лабораторные данные) |
| `DictionaryController` | Справочники (методы испытаний, подписанты) |

**Key Document Edit Endpoints (v1.4.4):**
- `GET /api/documents/{deviceId}/{docType}/edit/{id}` - loads config with field history in `initialValues.__history`
- `POST /api/documents/{deviceId}/{docType}/save/{id}` - saves with `__history` payload
- `POST /api/elis/load-protocol` - loads ELIS data, creates history entries with Source=ELIS
- `GET /api/dictionaries/methods/{parameterKey}` - returns method options for parameter
- `POST /api/dictionaries/methods` - adds new method to dictionary

**History payload structure:**
```json
{
  "value.Density": "850.5",
  "__history": {
    "value.Density": [{
      "source": "ELIS",
      "modifiedAt": "2025-01-14T10:00:00Z",
      "modifiedBy": "ELIS",
      "value": "850.5",
      "comment": "Загружено из протокола ПР-2024-12345"
    }]
  }
}
```

### Key Services (Dependency Injection)

Registered in `Startup.cs:ConfigureServices()` and `Extensions/IServiceCollectionExtensions.cs`:

**Singleton:**
- `IAppConfigService` - Configuration management and document class factory
- `IReportBuffer` - In-memory PDF/HTML storage (исключает дисковые операции, возврат через StringWriter)
- `IConfigurationCacheService` - Configuration file caching with LRU eviction
- `IDocModuleLoader` - Document DLL loading cache

**Scoped:**
- `IStatusProvider` - System health monitoring
- `IConfigurationService` - Configuration management

**Background:**
- `StatusMonitoringService` - Health checks every 60s

**Adding new services:**
- Register in `Startup.cs` or extension methods in `Extensions/IServiceCollectionExtensions.cs`
- Bind configuration settings via `IOptions<T>` pattern
- Inject through constructor (prefer constructor injection over property injection)

### Configuration System

Layered configuration (loaded in order):
1. `appsettings.json` - ASP.NET Core settings
2. `CfgApp.json` - Main app configuration (devices, ELIS, OPC)
3. `Cfg{DocumentType}.json` - Document-specific settings
4. `CfgEdit{DocumentType}.json` - Edit form configurations
5. `*.Development.json` - Auto-loaded in Debug builds only

⚠️ Configuration files are cached - changes may require cache invalidation or app restart.

## Key Development Patterns

### Adding New API Endpoints
1. **Create action** in appropriate controller under `TN_Doc/Controllers/`
2. **Inject services** via constructor (registered in `Startup.cs`)
3. **Place business logic** in `TN_Doc/Services/`, not in controller
4. **Validate input** and return `IActionResult` or `ActionResult<T>`
5. **Log errors** via NLog (`ILogger<T>`) - never suppress exceptions
6. **Return meaningful errors** - use appropriate HTTP status codes

### Coding Conventions (C#)
- **Naming**: `PascalCase` for types/methods, `camelCase` for private fields, `_camelCase` for private readonly
- **Control Flow**: Prefer early returns, avoid deep nesting
- **Error Handling**: Never suppress exceptions, log and return meaningful errors
- **Dependencies**: Register in `Startup.cs`, bind settings via `IOptions<T>`
- **Formatting**: 4-space indentation, braces on separate lines (follow ReSharper profile in `TN_Doc.sln.DotSettings`)
- **Async**: Suffix async methods with `Async`

**Project-Specific Patterns:**

**Document Library Pattern (tn.docgeneral submodule):**
```csharp
// All document classes must implement IDocumentEditor
public class DocPassport : IDocumentEditor
{
    // Load configuration for Vue editor
    public DocumentEditConfig GetEditConfig(int id)
    {
        // 1. Load DataARM from database
        // 2. Resolve slave/linked parameters using ResolveLinkedParametersRoles()
        // 3. Build schema with valueHistory/methodHistory/resultHistory
        // 4. Return config with initialValues containing field data + __history suffix
    }

    // Save from Vue editor (v1.4.4+)
    public bool SaveDocument(int id, Dictionary<string, object> values)
    {
        // 1. Extract __history payload from values
        // 2. Parse correctionData for value.*, method.*, result.*
        // 3. Save field history to DataARM.FieldHistoryMap (if IsUsedElis=true)
        // 4. Set Legal=0 for slave parameters and non-editable parameters
        // 5. Update DataARM and persist to database
    }
}
```

**Configuration Access Pattern:**
```csharp
// Use IAppConfigService for device configuration
var isElisUsed = _appConfigService.IsUsedElis(deviceId);

// Use IConfigurationCacheService for document configs
var config = _configCacheService.GetDocumentConfig<PassportConfig>("Passport", deviceId);

// Cache invalidation on save
_configCacheService.InvalidateCache(configPath);
```

**Field History Pattern (v1.4.4+):**
```csharp
// Backend: Add history entry
dataArm.AddFieldHistoryEntry("value.Density", new FieldHistoryEntry
{
    Source = DataSource.Manual,
    ModifiedAt = DateTime.UtcNow,
    ModifiedBy = "Пользователь",
    Value = "850.5",
    PreviousValue = "850.4"
});

// Frontend: Track changes
trackManualChange('value.Density', '850.5', '850.4');
trackElisLoad('value.Density', '850.5', 'ПР-2024-12345');
```

**Decimal Normalization (v1.4.4+):**
```csharp
// Normalize measurement/result on load from DB
var normalized = NormalizeDecimalPlaces(rawValue, parameter.RoundValue);
// Example: "1.6" with RoundValue=4 → "1.6000"
```

### Test Guidelines
- **Naming**: `MethodName_WhenCondition_ThenExpectedResult`
  - Examples: `GetEditConfig_WhenDocumentNotFound_ThrowsException`, `SaveDocument_WhenValidData_ReturnsTrue`
- **Pattern**: AAA (Arrange-Act-Assert)
- **Mocking**: Use Moq for external services and databases - never test infrastructure
- **Coverage**: Run `dotnet test /p:CollectCoverage=true` before risky refactors
- **Independent**: Tests must not depend on environment, file system, or execution order
- **Negative scenarios**: Always cover validation errors and edge cases

### Test Structure
```
Tests/
├── controllers/           # Controller unit tests
├── Services/              # Service layer tests
├── Configs/               # Configuration validation tests
│   ├── CfgEditPassportTests.cs      # Validation of passport configs (IsBallast, SlaveKey, etc.)
│   └── CfgEditPassportLinkedParametersTests.cs  # LinkedParameter validation
└── Libraries/             # Document library tests (for tn.docgeneral submodule)
    ├── Core/              # Act, Passport, Report, Jornal
    ├── KMH/               # KMH library tests
    ├── Poverka/           # Poverka library tests
    ├── Common/            # Shared document tests
    └── Integration/       # Cross-library compliance tests
```

Run specific library tests:
```bash
dotnet test --filter "Namespace~Libraries.KMH"      # All KMH tests
dotnet test --filter "Namespace~Libraries.Core"     # Core document tests
dotnet test --filter "Namespace~Libraries.Poverka"  # Poverka tests
dotnet test --filter "ClassName~CfgEditPassport"    # Configuration tests
```

**Important test categories:**
- **IDocumentEditor compliance** - all libraries must implement GetEditConfig, SaveDocument
- **Configuration validation** - CfgEdit*.json files must be valid and parseable
- **SlaveKey validation** - slave parameters must have Legal=0, not participate in validation
- **LinkedParameter validation** - linked pairs must share method selection
- **Field history** - history entries must follow FIFO (max 10), serialize/deserialize correctly

### UI Theme and Static Files
- **Colors**: All colors centralized in `/TN_Doc/wwwroot/css/material3.css`. Use CSS variables, never hardcode HEX colors.
- **Static files**: Place in `/TN_Doc/wwwroot/` directory
- **Caching**: Browser caching applies - consider versioning for CSS/JS files or use cache busting
- **Development vs Production**: Use detailed logs in Development, minimal in Production (controlled via `appsettings.{Environment}.json`)

### FastReport Templates
- **Templates**: Located in `TN_Doc/Doc/` directory (*.frx files)
- **Naming**: `{Number}_{DocumentType}.frx` (e.g., `01_Passport.frx`)
- **Configuration**: `TN_Doc/Cfg/Cfg{DocumentType}.json` specifies template path
- **Data source**: `GetViewDoc(id)` method returns JSON data for template
- **Best practices**:
  - Always backup template before modifications
  - Maintain backwards compatibility with existing data structure
  - Test template with real data before deployment

### Vue Component Guidelines
- **UI Library**: PrimeVue для всех UI компонентов
- **Overlays**: Используйте `appendTo="body"` для dropdown чтобы избежать обрезания
- **DateTime**: Используйте локальное время (не UTC) для предотвращения сдвига часового пояса
- **Styles**: PrimeVue overlay panels требуют global styles (не scoped)
- **Naming**: PascalCase для файлов компонентов, kebab-case для директорий
- **Workspaces**: Проект использует npm workspaces (statusbar, configurator, document-editor, shared)
- **State**: Pinia для управления состоянием, composables для переиспользуемой логики

**Key Document Editor Components (v1.4.4):**
- `PassportQualityTable.vue` - таблица показателей качества с поддержкой истории изменений
- `PassportLinkedParameterGroup.vue` - группа связанных параметров с объединённым методом
- `PassportParameterRow.vue` - строка параметра с редактируемыми полями
- `PassportMethodSelect.vue` - выбор метода испытаний с индикацией "не в справочнике"
- `FieldHistoryIndicator.vue` - индикатор источника данных (ELIS/Manual/IVK)
- `FormFieldWithHistory.vue` - обёртка для полей с поддержкой истории
- `DateRangeFieldGroup.vue` - горизонтальное отображение пары datetime полей

**Key Composables:**
- `useFieldHistory.ts` - управление историей изменений полей
- `usePassportEditor.ts` - логика редактирования паспорта (валидация, автозаполнение)
- `usePassportSave.ts` - логика сохранения паспорта с историей
- `useDocumentEditor.ts` - общая логика редактирования документов
- `useVisualEditor.ts` - определение типа визуального редактора конфигурации

## Vue Dev Server Proxy

При разработке Vue компонентов dev-серверы проксируют API на бэкенд:
- StatusBar (5173) → `http://localhost:38509`
- Configurator (5174) → `http://localhost:38509`
- Document Editor (5175) → `http://localhost:38509`

Запуск полной среды разработки:
```bash
# Терминал 1: бэкенд
cd TN_Doc && dotnet run

# Терминал 2: Vue dev server (выбрать один)
cd TN_Doc/Client && npm run dev:editor
```

## Document Editor

Vue 3 компонент для редактирования документов в браузере:
- Расположение: `TN_Doc/Client/document-editor/`
- Ключевые файлы:
  - `src/components/passport/PassportQualityTable.vue` - таблица показателей качества паспорта
  - `src/stores/documentStore.ts` - Pinia store для состояния документа
  - `src/composables/usePassportEditor.ts` - логика редактирования паспорта
  - `src/composables/usePassportSave.ts` - логика сохранения паспорта

**Ключевые фичи v1.4.4:**
- Все ~48 библиотек используют `IDocumentEditor.SaveDocument`
- Система истории изменений полей (Field History)
- Механизм связанных параметров (master-slave) для паспорта
- Механизм объединения методов для LinkedParameters
- Нормализация десятичных разрядов при загрузке из БД
- Индикация методов "вне справочника"

## Working with Passport Configurations

**Configuration files location:**
- Main configs: `TN_Doc/Cfg/Passport/CfgEditPassport*.json`
- Device-specific overrides: `TN_Doc/Cfg/CfgEditPassport*.json` (if needed)

**Key configuration fields:**

| Field | Type | Purpose | Example |
|-------|------|---------|---------|
| `Use` | bool | Show parameter in UI | `true` |
| `Edit` | bool | Allow manual editing | `true` (Legal=1), `false` (Legal=0) |
| `IsBallast` | bool | Result auto-syncs with Measurement | `true` for TempCorrection |
| `SlaveKey` | string | Master-slave relationship | `"DNP.mercury_mm"` |
| `LinkedParameter` | string | Shared method selection | `"Chloride_Salts.MassFraction"` |
| `RequiredFill` | bool | Validation required | `true` |
| `RoundValue` | int | Decimal places for normalization | `4` |
| `IsDefault` | bool | Auto-select method | `false` (v1.4.4+) |

**Configuration update workflow:**
1. Edit reference config: `CfgEditPassport_GOSTR50.2.040(I).json`
2. Validate JSON: `jq '.' <config-file>`
3. Propagate changes to other variants (MI3532, Export, EAC, etc.)
4. Run tests: `dotnet test --filter CfgEditPassport`
5. Test in browser: open Document Editor, verify no console errors
6. Update documentation: `docs/configs/passport.md`

**Common configuration patterns:**

```json
// Master-slave pair (slave hidden in UI)
{
  "Key": "DNP.kPa",
  "SlaveKey": "DNP.mercury_mm",  // slave will have Legal=0
  "Edit": true
}

// Linked parameters (both visible, shared method)
{
  "Key": "Chloride_Salts.Concentration",
  "LinkedParameter": "Chloride_Salts.MassFraction",
  "Edit": true
}

// Ballast parameter (result = measurement)
{
  "Key": "TempCorrection",
  "IsBallast": true,
  "Edit": true
}

// Non-editable IVK value (Legal=0)
{
  "Key": "IVK_CalculatedField",
  "Edit": false,
  "Use": true
}
```

**Configuration cache invalidation:**
- Configurator saves to `AppContext.BaseDirectory` (not `ContentRootPath`)
- Cache key matches save path for correct invalidation
- Restart app if manual config changes don't apply

## Key Dependencies and External Systems

### ELIS Integration
- Requires TN.ElisConnector module
- Configuration in CfgApp.json under "Elis" section
- SSL certificates configured in ELIS settings

### OPC Communication
- Configuration in CfgApp.json under "OpcConnectionSettings" section
- Supports both OPC DA (ProgId-based) and OPC UA protocols
- Connection settings include ProgId, server address, timeout, retry settings

### Database
- MySQL/MariaDB via Pomelo.EntityFrameworkCore.MySql
- Per-device connection strings in CfgApp.json

### Key Packages
- **FastReport.Web.Skia** (2025.2.8) - Report generation
- **NLog** (5.4.0) - Structured logging
- **SignalR** - Real-time communication

## Important File Locations

### Configuration
- `/TN_Doc/Cfg/CfgApp.json` - Main application configuration
- `/TN_Doc/Cfg/Cfg{DocumentType}.json` - Document template configurations
- `/TN_Doc/appsettings.json` - ASP.NET Core settings
- `/TN_Doc/nlog.config` - Logging configuration

### Logs
- Windows: `TN_Doc/logs/`
- Linux: `/opt/TN_Doc/logs/`
- Development: `TN_Doc/bin/Debug/net8.0/logs/`
- Configuration: Controlled by `nlog.config` (see Configuration section)

### Documentation
- `/CHANGELOG.md` - Version history (⚠️ check for recent changes before major work)
- `/docs/` - Architecture, API, deployment guides
- `/docs/features/field-history.md` - Field history system documentation
- `/docs/configs/passport.md` - Passport configuration and SlaveKey mechanism
- `/tech_debt/` - Architecture improvement plans

## Common Issues

| Issue | Solution |
|-------|----------|
| Build errors | Ensure NuGet sources configured (ortpr, FastReport) |
| Submodules empty | Run `git submodule update --init --recursive` |
| Vue build fails | Run `npm install` in `TN_Doc/Client/` first |
| Config changes ignored | Restart app - configuration files are cached (IConfigurationCacheService) |
| PrimeVue overlay clipped | Use `appendTo="body"` and global styles (not scoped) |
| Datetime timezone shift | Use local time conversion, not UTC |
| Database connection | Verify credentials in CfgApp.json |
| OPC connection errors | Check OpcConnectionSettings in CfgApp.json (ProgId, server address, timeout) |
| Linux permission issues | Ensure `alphadaemon` user has `/opt/TN_Doc/` access |
| Logs not appearing | Check `nlog.config` and verify write permissions to logs directory |
| Template not found | Verify `GetPathTemplateFile()` path and check `TN_Doc/Doc/` directory |
| Field history not showing | Ensure `IsUsedElis = true` in device configuration (CfgApp.json), restart app |
| Slave parameters visible | Check `SlaveKey` configuration in CfgEditPassport.json |
| Method not in dictionary warning | Add method to `MethodOptions` in CfgEditPassport.json or update via Configurator |
| Field not editable | Check `Edit = true` in config, verify not a slave parameter |
| ELIS integration not working | Verify ELIS connector running, check SSL certs in CfgApp.json, review ELIS logs |

## Debugging and Troubleshooting

### Backend Debugging

**Check application logs:**
```bash
# Windows
type TN_Doc\logs\tn_doc.log | findstr /C:"ERROR"

# Linux
tail -f /opt/TN_Doc/logs/tn_doc.log | grep ERROR
```

**Verify configuration loading:**
```bash
# Check if CfgApp.json is valid
jq '.' TN_Doc/Cfg/CfgApp.json

# Check specific device configuration
jq '.Devices[] | select(.Id == 1)' TN_Doc/Cfg/CfgApp.json
```

**Test database connection:**
```bash
# From MySQL client
mysql -h <host> -u <user> -p<password> <database> -e "SELECT COUNT(*) FROM DataARM;"
```

**Verify document library loading:**
- Check logs for "Загрузка библиотеки документов" messages
- Errors like "DLL not found" indicate missing files in `tn.docgeneral/` submodule

### Frontend Debugging

**Vue DevTools:**
- Install Vue DevTools browser extension
- Inspect Pinia stores: `documentStore`, `configStore`, `statusStore`
- Check component state and props in real-time

**Browser console checks:**
```javascript
// Check if Document Editor loaded correctly
const store = useDocumentStore();
console.log('Config:', store.config);
console.log('FormData:', store.formData);
console.log('Field History:', store.formHistory);

// Check ELIS status
console.log('ELIS enabled:', store.config?.isElisUsed);

// Verify API responses
// Open Network tab → filter by 'documents' → inspect payloads
```

**Common Vue errors:**

| Error | Причина | Решение |
|-------|---------|---------|
| `Cannot read property 'xxx' of undefined` | Данные не загружены | Добавить `v-if` проверку перед рендерингом |
| `Uncaught (in promise) TypeError` | API вернул неожиданный формат | Проверить response в Network tab |
| `Maximum call stack size exceeded` | Бесконечная рекурсия в watcher | Добавить условие выхода, проверить computed свойства |
| `[Vue warn]: Invalid prop` | Неверный тип prop | Проверить PropType определения |

### Performance Debugging

**Slow page load:**
1. Check FastReport template complexity (large images, complex bands)
2. Verify database query performance (add indexes if needed)
3. Check network latency to OPC servers
4. Review configuration cache settings

**Memory leaks:**
```bash
# Monitor process memory on Linux
watch -n 5 'ps aux | grep TN_Doc'

# Windows PowerShell
while($true) { Get-Process TN_Doc | Select-Object WS; Start-Sleep 5 }
```

**Vue performance profiling:**
- Use Vue DevTools Performance tab
- Check for unnecessary re-renders in table components
- Verify v-for keys are unique and stable

## Multi-platform Support

- **Windows**: Runs as `NT AUTHORITY\NETWORK SERVICE`, uses `winprutil.exe` for printing
- **Linux**: Runs as `alphadaemon` user, uses CUPS printing, requires `libgdiplus`
- **Cross-platform**: Always use `Path.Combine()`, never hardcode path separators

## Related Projects

Deploy at the same level (share TN_Doc configuration):
- **TN_KMH**: Контроль метрологических характеристик
- **TN_MessagingService**: OPC клиент (samara_build for OPC DA, master for OPC UA)
- **TN.ElisConnector**: ELIS integration

## Commit Guidelines

- **Language**: Всегда использовать русский язык для коммит-сообщений
- **Format**: Одно предложение с заглавной буквы, область изменения в начале (например: `Poverka: исправление расчёта`)
- **Submodules**: При изменении файлов в `tn.docgeneral/` делать коммит сначала в submodule, затем в основном репозитории
- **No AI Attribution**: НИКОГДА не упоминать AI, Claude, автоматическую генерацию в коммитах

**Работа с submodule tn.docgeneral:**
```bash
# После изменений в tn.docgeneral/
cd tn.docgeneral
git add . && git commit -m "Описание изменений"
git push
cd ..
git add tn.docgeneral
git commit -m "Обновлён субмодуль tn.docgeneral"
```

**⚠️ ВАЖНО при работе с submodule:**
- Всегда делать коммит сначала в submodule, затем обновлять ссылку в основном репозитории
- После `git pull` в основном репозитории выполнять `git submodule update` для синхронизации
- Проверять статус submodule: `git submodule status` (нет `+` или `-` перед хешем)
- При изменении интерфейса `IDocumentEditor` обновлять все ~48 библиотек в submodule
- Запускать тесты библиотек после изменений: `dotnet test --filter "Namespace~Libraries"`

**Примеры хороших коммитов:**
```
Document Editor: исправлена валидация datetime-local полей
Passport: добавлен механизм master-slave параметров
StatusBar: убрана ложная кликабельность индикаторов
Обновлён субмодуль tn.docgeneral: рефакторинг логики Legal
```

## Additional Resources

- [AGENTS.md](AGENTS.md) - Подробные правила стиля кода, тестирования и коммитов
- [CHANGELOG.md](CHANGELOG.md) - История версий (⚠️ проверить перед большими изменениями)
- [README.md](README.md) - Общее описание проекта и быстрый старт
- `/docs/` - Документация по архитектуре, API, развёртыванию
- `/docs/features/field-history.md` - Документация системы истории полей
- `/docs/configs/passport.md` - Конфигурация паспорта и SlaveKey
- `.cursor/rules/` - Дополнительные правила для Cursor IDE