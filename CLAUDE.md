# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc is an ASP.NET Core 8.0 web application for generating technical documents and reports from measurement system data (ИВК - Измерительно-вычислительный комплекс). The system generates quality certificates, verification protocols, acceptance acts, and various measurement reports using FastReport templates.

**Version**: 1.4.3 (.NET 8.0)
**Main Development Branch**: develop
**Runtime Requirement**: .NET Runtime 8.0.13+
**Node.js Requirement**: Node.js 18.0+ и npm 8.0+ (для Vue компонентов)

**Critical Rules:**
- NEVER mention AI, code generation, or "Claude" in commit messages
- Use Russian language for all commit messages
- Current active branch: feature/elis-backlight-2

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
dotnet format                                   # Format code
```

**Note**: Commands use forward slashes for cross-platform compatibility. On Windows, these work in PowerShell and Git Bash. For Command Prompt, use backslashes (`cd TN_Doc` works, but `cd TN_Doc\Client` for nested paths).

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
├── tn.docgeneral/             # Document module libraries (42 libraries)
│   ├── Act, Passport, Report, Jornal (4 core documents)
│   ├── Poverka* (21 verification documents)
│   ├── KMH*/KMX* (17 quality control documents)
│   └── Common* (3 shared libraries)
└── Tests/                     # NUnit tests with Moq
```

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
dotnet test --filter "ClassName=AppConfigServiceTests"
```

## Daily Development Workflow

```bash
# 1. Start with fresh environment
git pull origin develop
dotnet restore && dotnet build

# 2. Run application (from solution root)
# Windows PowerShell/Git Bash:
cd TN_Doc
$env:ASPNETCORE_ENVIRONMENT="Development"  # PowerShell
# or: set ASPNETCORE_ENVIRONMENT=Development  # CMD
dotnet run

# Linux/macOS:
cd TN_Doc && ASPNETCORE_ENVIRONMENT=Development dotnet run

# 3. Work on Vue components (parallel terminal)
cd TN_Doc/Client
npm run dev              # StatusBar with hot reload
npm run dev:configurator # Configurator with hot reload

# 4. Run tests frequently
dotnet test --filter "ClassName=YourTestClass"

# 5. Before committing
dotnet format                        # Format code
dotnet build && dotnet test          # Ensure it builds and tests pass
cd TN_Doc/Client && npm run build:all # Build Vue components
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
  - Build output: `TN_Doc/wwwroot/dist/statusbar/`

- **Configurator** (`TN_Doc/Client/configurator/`): Web UI for app configuration (v1.4.2+)
  - Framework: Vue 3 + TypeScript + PrimeVue
  - Dev server: `npm run dev:configurator` (port 5174)
  - Build output: `TN_Doc/wwwroot/dist/configurator/`

- **Document Editor** (`TN_Doc/Client/document-editor/`): In-browser document editing interface (in development)
  - Framework: Vue 3 + TypeScript + PrimeVue
  - Features: Edit Passport quality parameters, ELIS integration, OPC communication
  - Dev server: `npm run dev:editor` (port 5175)
  - Build output: `TN_Doc/wwwroot/dist/document-editor/`
  - Status: Under active development on feature branches

**Development workflow:**
1. Start ASP.NET Core app: `cd TN_Doc && dotnet run`
2. In parallel terminal, start Vue dev server: `cd TN_Doc/Client && npm run dev`
3. Changes to Vue components hot-reload automatically
4. Before committing, build all components: `npm run build:all`

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

- **NEVER** mention AI, automated generation, or "Claude" in commit messages
- Use Russian language for commit messages
- Follow conventional commits style: "Тип: краткое описание"
- Include detailed description in commit body if needed
- Reference issue numbers when applicable

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
- `/docs/` - Additional documentation (architecture, API, deployment, integration)
- `/README.md` - Project overview

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

- **Document Editor Production Release**: Planned promotion from POC to production-ready status
  - Support for editing Passport quality parameters with real-time validation
  - Integration with ELIS (lab data) and OPC (device communication)
  - Advanced auto-fill functionality for dependent parameters
  - Full support for method selection and measurement input
- **Active development branch**: feature/elis-backlight-2

## Previous Major Changes (v1.4.2 - October 2024)

- ⚠️ **Removed TN.Tools project** - obsolete functionality
- **Updated KMH_MI2816** for IVK version 7.12.14.3000 protocol changes
- **Updated docgeneral to version 1.2.2**
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