# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc is an ASP.NET Core 8.0 web application for generating technical documents and reports from measurement system data (ИВК - Измерительно-вычислительный комплекс). The system generates quality certificates, verification protocols, acceptance acts, and various measurement reports using FastReport templates.

**Version**: 1.4.3 (.NET 8.0)
**Main Development Branch**: develop
**Runtime Requirement**: .NET Runtime 8.0.13+ (SDK 9.0+ для разработки)
**Node.js Requirement**: Node.js 18.0+ и npm 8.0+ (для Vue компонентов)

**Critical Rules:**
- ⚠️ **НИКОГДА** не упоминать AI, генерацию кода, автоматизацию или "Claude" в коммитах
- ⚠️ **НИКОГДА** не добавлять "Generated with Claude Code" или подобные атрибуции
- ⚠️ **НИКОГДА** не добавлять "Co-Authored-By: Claude" или любые AI co-author теги
- ⚠️ **ВСЕГДА** использовать русский язык для коммит-сообщений

## Essential Commands

```bash
# Build and run
dotnet build                                    # Build entire solution (or open TN_Doc.sln in Visual Studio/Rider)
cd TN_Doc && dotnet run                         # Run app (http://localhost:38509)

# Vue components (from TN_Doc/Client/ - uses npm workspaces)
npm run build:all                               # Build all Vue apps (statusbar, configurator, document-editor)
npm run dev                                     # StatusBar dev server (port 5173)
npm run dev:configurator                        # Configurator dev server (port 5174)
npm run dev:editor                              # Document Editor dev server (port 5175)
npm run clean                                   # Clean all build artifacts (requires Git Bash/PowerShell on Windows)

# Testing
dotnet test                                     # Run all tests
dotnet test --filter "ClassName=YourTestClass"  # Run specific test class

# Code quality
dotnet format                                   # Format code (requires .NET SDK)
```

**Note**: Commands use forward slashes for cross-platform compatibility. On Windows, these work in PowerShell and Git Bash.

**Дополнительные правила**: См. [AGENTS.md](AGENTS.md) для детальных правил по:
- Организации структуры проекта и модулей
- Командам сборки, тестирования и разработки
- Стилю кодирования и соглашениям об именовании
- Правилам тестирования
- Правилам коммитов и Pull Request
- Заметкам о конфигурации и безопасности

## Quick Start for New Developers

1. **Setup NuGet sources** (required for FastReport and internal packages):
   ```bash
   dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
   dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
     --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text
   ```

2. **Install dependencies and build**:
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Build Vue components** (npm workspaces):
   ```bash
   cd TN_Doc/Client
   npm install                # Устанавливает зависимости для всех workspaces
   npm run build:all          # Собирает statusbar, configurator, document-editor
   cd ../..
   ```

4. **Run the application**:
   ```bash
   cd TN_Doc
   dotnet run
   # Visit http://localhost:38509
   ```

5. **Verify everything works**:
   ```bash
   dotnet test
   ```

## Architecture Overview

### Solution Structure
```
tn_doc/
├── TN_Doc/                    # Main ASP.NET Core web application
│   ├── Program.cs/Startup.cs  # Entry points, DI configuration
│   ├── Controllers/           # API endpoints
│   ├── Models/Services/       # Business logic and services
│   ├── Cfg/                   # Document and app configuration files
│   ├── Doc/                   # FastReport templates (*.frx)
│   └── Client/                # Frontend Vue 3 applications (npm workspaces)
│       ├── statusbar/         # Real-time status monitoring (Vue 3 + PrimeVue) - PRODUCTION
│       ├── configurator/      # Configuration management UI (v1.4.2+) - PRODUCTION
│       ├── document-editor/   # In-browser document editing interface - IN DEVELOPMENT
│       └── shared/            # Shared TypeScript utilities and types
├── TN.DocGeneral/             # Core business logic and shared utilities
├── Ivk.DataBase/              # Database library for IVK data access
├── tn.docgeneral/             # Document module libraries (git submodule, 45 libraries)
│   ├── Act, Passport, Report, Jornal (4 core documents)
│   ├── ActProducer, ActRoute (2 additional act types)
│   ├── Poverka* (20 verification documents)
│   ├── KMH*/KMX* (17 quality control documents)
│   └── Common* (2 shared libraries)
├── tn_toolsfastreport/        # FastReport utilities (git submodule)
├── winprutil/                 # Windows printing utility (git submodule)
└── Tests/                     # NUnit tests with Moq
```

**Git Submodules:**
The project uses git submodules for document libraries and utilities. After cloning:
```bash
git submodule update --init --recursive
```

Submodules:
- `tn.docgeneral` - Document module libraries (v1.2.3)
- `tn_toolsfastreport` - FastReport helper utilities
- `winprutil` - Windows printing utility (winprutil.exe)

### Document Module Pattern

Each document type follows a consistent 4-method interface:

1. **`GetViewDoc(id)`**: Returns JSON data for FastReport template
2. **`GetPathTemplateFile()`**: Returns path to `.frx` template file
3. **`GetEditDoc(id)`**: Generates HTML editing form **in-memory** (v1.4.2+)
   - ⚠️ **IMPORTANT**: Always use `Path.Combine()` for cross-platform compatibility
   - ⚠️ **IMPORTANT**: Add trace logging:
     ```csharp
     _logger.Trace($"HTML форма документа {IdDoc} (id={id}) сгенерирована, размер: {html.Length} символов");
     ```
4. **`SetDocFromJson(json)`**: Updates document data from JSON

**Configuration files per document:**
- `TN_Doc/Cfg/Cfg{DocumentType}.json` - template and report settings
- `TN_Doc/Cfg/CfgEdit{DocumentType}.json` - edit form configuration
- `TN_Doc/Doc/{Number}_{DocumentType}.frx` - FastReport template

### Key Services (Dependency Injection)

Registered in `Startup.cs:ConfigureServices()`:

**Singleton Services:**
- `IAppConfigService` - Configuration management and document class factory
- `IReportBuffer` - In-memory PDF storage (eliminates disk I/O, v1.4.1+)
- `IConfigurationCacheService` - Configuration file caching with LRU eviction (max 50, v1.4.2+)
- `IDocModuleLoader` - Document DLL loading cache (v1.4.2+)
- `IDbSchemaCache` - Database schema information caching

**Scoped Services:**
- `IStatusProvider` - System health monitoring
- `IConfigurationService` - Configuration management

**Background Services:**
- `StatusMonitoringService` - Health checks every 60s

### Multi-platform Support

- **Platform detection**: `Program.cs` configures Windows Service or systemd
- **Windows**: Runs as `NT AUTHORITY\NETWORK SERVICE`, uses `winprutil.exe` for printing
- **Linux**: Runs as `alphadaemon` user, uses CUPS printing, requires `libgdiplus`
- **Logs**:
  - Windows: `TN_Doc/logs/`
  - Linux: `/opt/TN_Doc/logs/`
  - Development mode: `TN_Doc/bin/Debug/net8.0/logs/`
- **Cross-platform paths**: Always use `Path.Combine()`, never hardcode path separators

### Memory Management (v1.4.1+)

- **IReportBuffer**: Stores generated PDF bytes in-memory
- **Custom Middleware**: Intercepts `/PDF/PDF.pdf` requests and serves from buffer
- **Benefits**: Eliminates "file in use" errors and concurrent access issues

## Key Development Patterns

### Coding Conventions (C#)

From `.cursor/rules/coding-conventions-csharp.mdc`:
- **Naming**: `PascalCase` for types/methods, `camelCase` for private fields, `_camelCase` for private readonly
- **API Design**: Explicitly annotate public APIs, avoid `dynamic`/`object` without justification
- **Control Flow**: Prefer early returns, avoid deep nesting
- **Error Handling**: Never suppress exceptions, log and return meaningful errors
- **Dependencies**: Register in `Startup.cs`, bind settings via `IOptions<T>`

### ASP.NET Core Patterns

From `.cursor/rules/aspnet-tn_doc.mdc`:
- **New Endpoints**: Create controller actions, inject services via DI, place logic in `Models/Services`
- **Validation**: Validate input data, return `IActionResult`/`ActionResult<T>`
- **Logging**: Use NLog (`ILogger<T>`), detailed logs in Development, minimal in Production
- **Configuration**: All parameters in `appsettings.*.json`, bind through `IOptions<T>`

### Configuration System

Layered configuration (loaded in order):
1. `appsettings.json` - ASP.NET Core settings
2. `CfgApp.json` - Main app configuration (devices, ELIS, OPC)
3. `Cfg{DocumentType}.json` - Document-specific settings
4. `CfgEdit{DocumentType}.json` - Edit form configurations
5. `*.Development.json` - Auto-loaded in Debug builds only

⚠️ **v1.4.2**: Configuration files are cached via `IConfigurationCacheService` - changes may require cache invalidation

### Test Guidelines

From `.cursor/rules/tests-guide.mdc`:
- **Naming**: `MethodName_WhenCondition_ThenExpectedResult`
- **Pattern**: AAA (Arrange-Act-Assert)
- **Independence**: Write tests without hidden environment dependencies
- **Mocking**: Use mocks/fakes for external services and databases
- **Coverage**: Include negative scenarios and validation errors

Run specific tests:
```bash
# Specific test class
dotnet test --filter "ClassName=AppConfigServiceTests"

# Test specific document type libraries
dotnet test --filter "FullyQualifiedName~KMH"        # All KMH document tests
dotnet test --filter "FullyQualifiedName~Passport"   # Passport tests
dotnet test --filter "FullyQualifiedName~Poverka"    # Verification tests
dotnet test --filter "FullyQualifiedName~Act"        # Act tests
```

## Daily Development Workflow

```bash
# 1. Start with fresh environment
git pull origin develop
dotnet restore && dotnet build

# 2. Run application (from solution root)
# Windows PowerShell:
cd TN_Doc
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run

# Linux/macOS:
cd TN_Doc && ASPNETCORE_ENVIRONMENT=Development dotnet run

# 3. Work on Vue components (parallel terminal)
cd TN_Doc/Client
npm run dev              # StatusBar with hot reload (port 5173)
npm run dev:configurator # Configurator with hot reload (port 5174)
npm run dev:editor       # Document Editor with hot reload (port 5175)

# 4. Run tests frequently
dotnet test --filter "ClassName=YourTestClass"

# 5. Before committing
dotnet format                         # Format code
dotnet build && dotnet test           # Ensure it builds and tests pass
cd TN_Doc/Client && npm run build:all # Build Vue components for production
git status                            # Review changes before commit
```

## Common Development Tasks

### Adding a new document type

1. Create new library in `tn.docgeneral/{DocumentType}/`
2. Implement standard 4-method interface (GetViewDoc, GetEditDoc, GetPathTemplateFile, SetDocFromJson)
3. Add logging with `Path.Combine()` in GetEditDoc
4. Create FastReport template in `TN_Doc/Doc/{DocumentType}/`
5. Add configuration files: `Cfg{DocumentType}.json`, `CfgEdit{DocumentType}.json`
6. Register in IAppConfigService factory
7. Write unit tests in `Tests/`

### Modifying a FastReport template

1. ALWAYS make a backup copy first
2. Use FastReport Designer to edit .frx files
   - **Required version**: FastReport 2025.2.8 or compatible
   - Templates may not open correctly in older/newer versions
   - Ensure proper licensing for FastReport Designer
3. Test with real data from multiple devices
4. Validate all export formats (PDF, Excel, ODS)
5. Check logs for any rendering errors

### Working with configurations

- Use Configurator UI (`/configurator`) for runtime changes (базовая версия с v1.4.2)
- Changes to CfgApp.json require app restart
- Configuration caching is enabled - changes may require cache invalidation

### Modifying UI theme and colors (v1.4.3+)

1. **All colors are centralized** in `/TN_Doc/wwwroot/css/material3.css`
2. Edit CSS variables in `:root` selector to change theme colors
3. DO NOT add hardcoded HEX colors in other stylesheets - use CSS variables instead
4. Common CSS variables:
   - `--md-primary`, `--md-primary-active`, `--md-primary-light` - primary colors
   - `--md-gray-*` - grayscale palette (600, 700, 800)
   - `--md-elis-highlight` - ELIS data highlighting
   - `--md-error-*` - error states
   - `--md-border-*` - border colors
5. After changes, rebuild Vue components: `npm run build:all`

### Working with Vue components

**Production components:**
- **StatusBar** (`TN_Doc/Client/statusbar/`): Real-time monitoring interface with SignalR
  - Framework: Vue 3 + TypeScript + PrimeVue
  - Dev server: `npm run dev` (port 5173)
  - Build output: `TN_Doc/wwwroot/statusbar/`

- **Configurator** (`TN_Doc/Client/configurator/`): Web UI for app configuration (v1.4.2+)
  - Framework: Vue 3 + TypeScript + PrimeVue
  - Dev server: `npm run dev:configurator` (port 5174)
  - Build output: `TN_Doc/wwwroot/configurator/`

- **Document Editor** (`TN_Doc/Client/document-editor/`): In-browser document editing interface (in development)
  - Framework: Vue 3 + TypeScript + PrimeVue
  - Features: Edit Passport quality parameters, ELIS integration, OPC communication
  - Dev server: `npm run dev:editor` (port 5175)
  - Build output: `TN_Doc/wwwroot/document-editor/`
  - Status: Under active development on feature branches

**Development workflow:**
1. Start ASP.NET Core app: `cd TN_Doc && dotnet run`
2. In parallel terminal, start Vue dev server: `cd TN_Doc/Client && npm run dev`
3. Changes to Vue components hot-reload automatically
4. Before committing:
   ```bash
   cd TN_Doc/Client
   npm run build:all                    # Build all Vue apps for production

   # Verify TypeScript types (no unit tests configured yet)
   cd statusbar && npm run type-check   # Check StatusBar types
   cd ../configurator && npm run type-check  # Check Configurator types
   cd ../document-editor && npm run type-check  # Check Editor types
   ```

**Important notes:**
- All Vue components use npm workspaces configured in `TN_Doc/Client/package.json`
- Shared TypeScript types and utilities are in `TN_Doc/Client/shared/`
- PrimeVue theme customization is in Material Design 3 style via CSS variables
- **No unit tests configured** for Vue components - use `type-check` to catch type errors

## Git Workflow and Commit Conventions

### Branch Strategy

- **master** - Production branch (only stable releases)
- **develop** - Main development branch ⭐ (use this for new features)
- **feature/** - Feature branches (create from develop)
- **bugs/** - Bug fix branches
- **projects/** - Project-specific branches

**Guidelines:**
- Always create new branches from `develop`
- Merge back to `develop` after testing
- Use descriptive names (e.g., `feature/status-bar-improvements`)
- Delete feature branches after merge

### Commit Message Rules

⚠️ **КРИТИЧЕСКИ ВАЖНО - НИКОГДА НЕ НАРУШАТЬ:**
- **НИКОГДА** не упоминать AI, генерацию кода, автоматизацию или "Claude" в коммит-сообщениях
- **НИКОГДА** не добавлять "Generated with Claude Code" или подобные атрибуции
- **НИКОГДА** не добавлять "Co-Authored-By: Claude" или любые AI co-author теги
- **ВСЕГДА** использовать русский язык для коммит-сообщений
- Коммиты должны выглядеть как написанные человеком-разработчиком

**Стандартные правила:**
- Следовать стилю conventional commits: "Тип: краткое описание"
- Включать детальное описание в body коммита при необходимости
- Ссылаться на номера задач когда применимо
- Примеры хороших коммитов из истории:
  - "Рефакторинг архитектуры DocPassport"
  - "Document Editor: увеличена ширина колонок таблицы паспорта качества до 150px"
  - "Централизация управления цветами: все цвета в CSS переменных"

**Important Warnings:**
- Large number of configuration files are tracked - be careful with bulk changes
- Many binary .frx templates are in repository - use FastReport Designer for editing
- Do NOT commit Development config files to production branches
- Always test document generation before committing template changes

## Key Dependencies and External Systems

### ELIS Integration
ELIS (Единая Лабораторная Информационная Система) for quality passport data:
- Requires TN.ElisConnector module from separate repository
- Configuration in CfgApp.json under "Elis" section
- SSL certificate support (place certificates in `Cert/` folder)

### OPC Communication
Real-time data acquisition from measurement systems:
- **OPC DA**: Legacy protocol - tags must be pre-defined in `opc.da.tags.json`
- **OPC UA**: Modern protocol - configuration in `opc.ua.tags.json`
- Settings in CfgApp.json under "OpcConnectionSettings"

### Database Connectivity
- **MySQL/MariaDB** via Pomelo.EntityFrameworkCore.MySql (v7.0.0)
- Per-device connection strings in CfgApp.json
- Password encryption supported via EncryptionLibrary

### Key Package Dependencies
- **FastReport.Web.Skia** (2025.2.8) - Report generation engine
- **NLog** (5.4.0) - Structured logging
- **Newtonsoft.Json** (13.0.3) - JSON processing
- **SkiaSharp, HarfBuzzSharp** - Graphics rendering
- **System.Drawing.Common, libgdiplus** - Platform-specific graphics

## Important File Locations

### Configuration
- `/TN_Doc/Cfg/CfgApp.json` - Main application configuration
- `/TN_Doc/Cfg/Cfg{DocumentType}.json` - Document template configurations
- `/TN_Doc/Cfg/CfgEdit{DocumentType}.json` - Edit form configurations
- `/TN_Doc/appsettings.json` - ASP.NET Core settings
- `/TN_Doc/nlog.config` - Logging configuration

### Templates and Output
- `/TN_Doc/Doc/**/*.frx` - FastReport templates
- `/TN_Doc/wwwroot/PDF/` - Generated PDF documents (deprecated - now in-memory)
- `/TN_Doc/wwwroot/HTML/` - HTML editing forms (deprecated since v1.4.2)
- **Logs**:
  - Windows: `TN_Doc/logs/`
  - Linux: `/opt/TN_Doc/logs/`
  - Development mode: `TN_Doc/bin/Debug/net8.0/logs/`

### Documentation
- `/CHANGELOG.md` - Version history
- `/TN_Doc/changes.md` - Detailed change log
- `/docs/` - Additional documentation (architecture, API, deployment, integration, operations)
- `/docs/operations/logging.md` - ⭐ **Руководство по управлению логами** (просмотр, копирование, архивирование)
- `/README.md` - Project overview
- `/AGENTS.md` - Repository guidelines and development rules

### Planning and Technical Debt
- `/tech_debt/` - Планы улучшения архитектуры и отслеживание технического долга
  - `ARCHITECTURE_IMPROVEMENTS.md` - Предложения по улучшению архитектуры системы
  - `PASSPORT_QUALITY_FILLING_PLAN.md` - Детальное планирование функций паспорта качества
  - `PASSPORT_QUALITY_FILLING_PROMPTS.md` - Промпты для реализации паспорта качества
  - `FASTREPORT_OPTIMIZATION_PLAN.md` - План оптимизации генерации отчетов
  - `SECURITY_HARDENING_PLAN.md` - Дорожная карта повышения безопасности
  - `WINDOWS_INSTALLER_IMPLEMENTATION_PLAN.md` - Руководство по реализации установщика Windows
  - `ELIS_CONFIG_EXAMPLES.md` - Примеры конфигурации интеграции с ELIS
  - `DOCUMENT_LIBRARIES_LIST.md` - Список всех библиотек документов

## Common Issues and Solutions

**Build and Dependencies:**
- **Build errors**: Ensure NuGet sources are configured (ortpr and FastReport)
- **Runtime version mismatch**: Verify .NET Runtime 8.0.13+ (`dotnet --info`)

**Vue Component Issues:**
- **Build fails**: Run `npm install` in `TN_Doc/Client/` first
- **Hot reload not working**: Check Vite dev server is running on correct port
- **Stale build output**: Run `npm run clean` in `TN_Doc/Client/` and rebuild

**Document Generation:**
- **Missing templates**: Check .frx files exist and paths in Cfg*.json are correct
- **"File in use" errors**: Should not occur in v1.4.1+ due to in-memory PDF generation

**Database and Integration:**
- **Database connection**: Verify MySQL/MariaDB connectivity and credentials in CfgApp.json
- **OPC DA tag errors**: All tags must be pre-registered in `opc.da.tags.json` before use
- **ELIS integration issues**: Check SSL certificates in `Cert/` folder

**Platform-specific:**
- **Permission issues on Linux**: Ensure `alphadaemon` user has access to `/opt/TN_Doc/`
- **libgdiplus errors on Linux**: Install via `sudo apt install libgdiplus` (Debian/Ubuntu)

## Recent Changes (v1.4.3)

- **Configurator Enhancements**: Added settings for measurement instruments (СИ), ELIS connections, and OPC connections
- **Journal Report Fix**: Fixed printing form for measurement instrument registration journal (DataARM compatibility)
- **UI Theme Improvements**: Centralized color management via CSS variables
  - All colors now defined in `TN_Doc/wwwroot/css/material3.css`
  - Replaced hardcoded HEX colors with CSS variables across all stylesheets
  - New CSS variables: `--md-primary-active`, `--md-gray-*`, `--md-elis-highlight`, `--md-error-*`, etc.
- **Updated docgeneral to version 1.2.3**

## In Development (Upcoming v1.4.4)

- **Система истории изменений полей паспорта качества**:
  - Отслеживание источника данных (ELIS, ручное редактирование, округление ИВК)
  - Визуальные индикаторы источников в UI (цветные значки)
  - Детальная история изменений в popup окне (до 10 записей на поле)
  - Автоматическая миграция из старого флага `ElisFilled`
  - Раздельная история для value/method/result/document полей
  - ⚠️ Требует включенного ELIS в конфигурации (`IsUsedElis = true`)
- **Document Editor Production Release**: Planned promotion from POC to production-ready status
  - Support for editing Passport quality parameters with real-time validation
  - Integration with ELIS (lab data) and OPC (device communication)
  - Advanced auto-fill functionality for dependent parameters
  - Full support for method selection and measurement input
  - ✅ **Fixed signer fields display order**: Signer fields now appear in correct position (between СИКН № and sampling date), preserving original order from CfgEditPassport.json
- **Улучшенная UX редактирования паспорта качества** (Ноябрь 2025):
  - ✅ **Визуальная индикация методов вне справочника**: желтая рамка (#f5c24c) и предупреждение для методов, не зарегистрированных в локальной конфигурации
  - ✅ **Отключена автоматическая подстановка методов**: убраны флаги `IsDefault` для параметров с Id: 11, 13, 15, 17, 19, 21, 23, 33, 35, 37 в `CfgEditPassport_GOSTR50.2.040(I).json`
  - ✅ **Явная индикация незаполненных методов**: placeholder "Метод не выбран" вместо автовыбора первого метода
  - ✅ Обновлен подмодуль `tn.docgeneral` с улучшенной логикой выбора методов
  - Компонент: `PassportMethodSelect.vue` с CSS классом `.unknown-method`
  - Новое поле: `isInDictionary` в интерфейсе `ParameterMethod`
  - ✅ **Исправлена обработка datetime-local полей**:
    - Исправлена конвертация Date в локальное время (было: UTC через `toISOString()`, стало: локальное время через компоненты даты)
    - Добавлена специализированная функция `handleDateTimeChange()` для обработки событий PrimeVue DatePicker
    - Реализована защита от дублирования событий при изменении даты/времени
    - Теперь корректно создаются записи истории для datetime-local полей
    - Индикаторы источников данных (ELIS/Manual) корректно отображаются для datetime-local полей
    - Устранены искажения времени при сохранении (проблема сдвига часового пояса)

## Previous Major Changes (v1.4.2 - October 2024)

- ⚠️ **Removed TN.Tools project** - obsolete functionality
- **Updated KMH_MI2816** for IVK version 7.12.14.3000 protocol changes
- **Updated docgeneral to version 1.2.3**
- ⚠️ **Removed duplicate Act template**: Removed `Act_GOSTR50.2.040(G)_ShiftTime.frx` (duplicated functionality)
- **UI Refactoring**: Major application interface improvements
- **Status bar improvements**: 4 indicator states (online, offline, ndv, warning)
  - Improved alignment and sizing of device/service indicators
  - Removed redundant refresh button and SignalR indicator
- **LoggingPathService refactoring**: Moved to `TN.DocGeneral/Services/` for reusability

## Previous Major Changes (v1.4.1)

- ⚠️ **Базовая версия конфигуратора**: веб-интерфейс для управления конфигурацией (`/configurator`)
- ⚠️ **Базовая версия строки состояния**: диагностические индикаторы связи
- ⚠️ **Кэширование конфигурационных файлов**: `IConfigurationCacheService` с LRU eviction
- ⚠️ **Кэширование загрузки документов**: `IDocModuleLoader` оптимизация загрузки DLL
- ⚠️ **Исключено построение HTML из файла**: GetEditDoc теперь работает в памяти
- ⚠️ **In-memory document generation**: `IReportBuffer` eliminates disk I/O for PDF generation
- **Local user directories for signatories**: Reports and journals now use local user references

## Related Projects

The following projects must be deployed at the same level (all share TN_Doc configuration):
- **TN_KMH**: Контроль метрологических характеристик
  ```bash
  git clone http://192.168.100.100/orpovy/ivk/tn_kmh.git
  ```
- **TN_MessagingService**: OPC клиент
  ```bash
  git clone http://192.168.100.100/orpovy/ivk/tn_messagingservice.git
  ```
  - Use "samara_build" branch for OPC DA support
  - Master branch for OPC UA communication
- **TN.ElisConnector**: ELIS integration
  ```bash
  git clone http://192.168.100.100/orpovy/ivk/tn.elisconnector.git
  ```

## Additional Notes

- **Development logs location**: `./TN_Doc/bin/Debug/net8.0/logs/`
- **Production logs location**:
  - Windows: `TN_Doc/logs/`
  - Linux: `/opt/TN_Doc/logs/`
