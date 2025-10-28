# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc is an ASP.NET Core 8.0 web application for generating technical documents and reports from measurement system data (ИВК - Измерительно-вычислительный комплекс). The system generates quality certificates, verification protocols, acceptance acts, and various measurement reports using FastReport templates.

**Version**: 1.4.3
**Target Framework**: .NET 8.0
**SDK Compatibility**: Works with .NET SDK 8.0+ and 9.0+
**Runtime Requirement**: .NET Runtime 8.0.13 or higher
**Main Development Branch**: develop

**Important Notes:**
- NEVER mention AI, code generation, or "Claude" in commit messages
- Use Russian language for all commit messages
- Current active branch: feature/additional-info-table (работа над функционалом дополнительной информации)

## Essential Commands

```bash
# Build and run
dotnet build                                    # Build entire solution
cd TN_Doc && dotnet run                         # Run app (http://localhost:38509)

# Vue components (from TN_Doc/Client/)
npm run build:all                               # Build all Vue apps
npm run dev                                     # StatusBar dev server
npm run dev:configurator                        # Configurator dev server
npm run dev:editor                              # Document Editor dev server (experimental POC)

# Testing
dotnet test                                     # Run all tests
dotnet test --filter "ClassName=YourTestClass"  # Run specific test class

# Code quality
dotnet format                                   # Format code
```

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

3. **Build Vue components**:
   ```bash
   cd TN_Doc/Client
   npm install
   npm run build:all
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
- **TN_Doc**: Main ASP.NET Core web application
  - Entry point: `Program.cs` and `Startup.cs`
  - Controllers: `TN_Doc/Controllers/`
  - Services: `TN_Doc/Models/` and `TN_Doc/Services/`
  - Configuration: `appsettings.json`, `nlog.config`

- **TN_Doc/Client/**: Frontend applications (npm workspace monorepo)
  - `statusbar/`: Real-time status monitoring Vue 3 app
  - `configurator/`: Configuration management Vue 3 app (базовая версия с v1.4.2)
  - `document-editor/`: Experimental document editing Vue 3 app (POC)
  - `shared/`: Shared TypeScript utilities and API client
  - Stack: Vue 3.4.21 + TypeScript + PrimeVue 4.2+ + Vite

- **TN.DocGeneral**: Core business logic and shared utilities
- **Ivk.DataBase**: Database library for IVK measurement system data access
- **tn.docgeneral/**: Document modules (Passport, Poverka*, KMH*, Act, Report, Jornal)
- **Tests**: Unit tests using NUnit framework with Moq

### Document Module Pattern
Each document type implements a consistent pattern:
- **Class Library**: Contains document-specific business logic
- **Configuration Files**:
  - Document config: `TN_Doc/Cfg/Cfg{DocumentType}.json`
  - Edit form config: `TN_Doc/Cfg/CfgEdit{DocumentType}.json`
- **FastReport Template**: `TN_Doc/Doc/{Number}_{DocumentType}.frx`
- **Standard Interface Methods**:
  - `GetViewDoc(id)`: Returns JSON data for report generation
  - `GetPathTemplateFile()`: Returns path to .frx template
  - `GetEditDoc(id)`: Returns HTML for editing forms (⚠️ v1.4.2: теперь строится в памяти, без дисковых операций)
  - `SetDocFromJson(json)`: Updates document data from JSON

### Key Services and Patterns

**Dependency Injection** (registered in `Startup.cs`):
- **IAppConfigService**: Singleton - configuration management and document class factory
- **IReportBuffer**: Singleton - in-memory PDF storage (no disk I/O since v1.4.1)
- **IConfigurationCacheService**: Singleton - кэширование конфигурационных файлов с LRU eviction (max 50 entries) ⚠️ v1.4.2
- **IDocModuleLoader**: Singleton - кэширование загрузки документов (оптимизация загрузки DLL-библиотек) ⚠️ v1.4.2
- **IDbSchemaCache**: Scoped - caching database schema information
- **IStatusProvider**: Scoped - system health monitoring
- **StatusMonitoringService**: Background hosted service (checks every 60s)
- **LoggingPathService**: Static utility for platform-specific log paths (TN.DocGeneral/Services/)

**Multi-platform Support**:
- Platform detection in `Program.cs` configures Windows Service or systemd
- Windows: Runs as `NT AUTHORITY\NETWORK SERVICE`, uses `prutils/winprutil.exe` for printing
- Linux: Runs as `alphadaemon` user, uses CUPS printing, requires `libgdiplus`
- Log directories are platform-specific (Windows: `TN_Doc/logs/`, Linux: `/opt/TN_Doc/logs/`)

**Memory Management (v1.4.1+)**:
- **IReportBuffer**: Stores generated PDF bytes in memory
- **Middleware Interceptor**: Custom middleware intercepts `/PDF/PDF.pdf` requests and serves from buffer
- **Benefits**: Eliminates "file in use" errors and concurrent access issues

## Key Development Patterns

### Coding Conventions (C#)
From `.cursor/rules/coding-conventions-csharp.mdc`:
- **Naming**: `PascalCase` for types/methods, `camelCase` for private fields, `_camelCase` for private readonly fields
- **API Design**: Explicitly annotate public APIs, avoid `dynamic`/`object` without justification
- **Control Flow**: Prefer early returns, avoid deep nesting
- **Error Handling**: Never suppress exceptions, log and return meaningful errors
- **Dependencies**: Register in `Startup.cs`/`IServiceCollectionExtensions.cs`, bind settings via `IOptions<T>`

### ASP.NET Core Patterns
From `.cursor/rules/aspnet-tn_doc.mdc`:
- **New Endpoints**: Create actions in appropriate controllers, inject services via DI, place logic in `Models/Services`
- **Validation**: Validate input data, return `IActionResult`/`ActionResult<T>`
- **Logging**: Use NLog (`ILogger<T>`), detailed logs in Development, minimal in Production
- **Configuration**: All parameters in `appsettings.*.json`, bind sections through `IOptions<T>`

### Configuration System
Layered configuration approach:
1. **appsettings.json**: ASP.NET Core application settings
2. **CfgApp.json**: Main application configuration (devices, ELIS, OPC)
3. **Cfg{DocumentType}.json**: Document-specific template and report settings
4. **CfgEdit{DocumentType}.json**: Edit form configurations
5. **Development overlays**: `*.Development.json` files auto-loaded in Debug builds

⚠️ **v1.4.2**: Configuration files are now cached via `IConfigurationCacheService` - minimize disk I/O

## Working with Document Modules

### Standard Document Module Interface
All document modules must implement these methods:

1. **GetViewDoc(id)**: Returns JSON data for report generation
   - Fetches document data from database
   - Transforms to format expected by FastReport template
   - Returns JSON string with "JsonDoc" parameter

2. **GetPathTemplateFile()**: Returns path to .frx template
   - Resolves path from document configuration
   - Returns absolute path to FastReport template file

3. **GetEditDoc(id)**: Returns HTML for editing forms
   - ⚠️ **v1.4.2**: Generates HTML in memory only (no file operations)
   - **IMPORTANT**: Always use `Path.Combine()` for cross-platform compatibility
   - **IMPORTANT**: Add trace logging on generation:
     ```csharp
     _logger.Trace($"HTML форма документа {IdDoc} (id={id}) сгенерирована, размер: {html.Length} символов");
     ```
   - Returns HTML as string

4. **SetDocFromJson(json)**: Updates document data from JSON
   - Parses JSON from client
   - Validates input data
   - Updates database records

### Complete Document Libraries List (42 libraries)

**Core Documents (4)**: Act, Passport, Jornal, Report

**Verification Documents - Poverka (21)**: Poverka1974, Poverka1974_04, Poverka1974_89, Poverka1974_95, Poverka2816, Poverka3151, Poverka3189, Poverka3265_PR_PU, Poverka3265_UPR_PR, Poverka3265_UPR_PU, Poverka3266, Poverka3267, Poverka3272, Poverka3287, Poverka3288, Poverka3312_PR_PU, Poverka3312_UPR_PR, Poverka3380, PoverkaSikn425_PR_PR, PoverkaSikn425_PR_PU

**Quality Control - KMH (14)**: KMH3265_PR_PU, KMH3265_UPR_PR, KMH3288_MPR_TPR, KMH3312_PR_PU, KMH3312_UPR_PR, KMH_MI2816, KMH_MPR_MPR, KMH_MPR_PU, KMH_MPR_TPR, KMH_PP, KMH_PP_Areom, KMH_PR_PR, KMH_PR_PU, KMH_PV, KMH_PW, KMX_Sikn425_PR_PR, KMX_Sikn425_PR_PU

**Common Libraries (3)**: CommonPoverka1974, CommonSikn425

## Development Workflow

### Daily Development Cycle
```bash
# 1. Start with fresh environment
git pull origin develop
dotnet restore && dotnet build

# 2. Run application (from solution root)
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

### Common Development Tasks

**Adding a new document type:**
1. Create new library in `tn.docgeneral/{DocumentType}/`
2. Implement standard interface (GetViewDoc, GetEditDoc, GetPathTemplateFile, SetDocFromJson)
3. Add logging with `Path.Combine()` in GetEditDoc
4. Create FastReport template in `TN_Doc/Doc/{DocumentType}/`
5. Add configuration files: `Cfg{DocumentType}.json`, `CfgEdit{DocumentType}.json`
6. Register in IAppConfigService factory
7. Write unit tests in `Tests/`

**Modifying a FastReport template:**
1. ALWAYS make a backup copy first
2. Use FastReport Designer to edit .frx files
3. Test with real data from multiple devices
4. Validate all export formats (PDF, Excel, ODS)
5. Check logs for any rendering errors

**Working with configurations:**
- Use Configurator UI (`/configurator`) for runtime changes (базовая версия доступна с v1.4.2)
- Changes to CfgApp.json require app restart
- ⚠️ Configuration caching is enabled - changes may require cache invalidation

### Test Guidelines
From `.cursor/rules/tests-guide.mdc`:
- **Naming Convention**: `MethodName_WhenCondition_ThenExpectedResult`
- **AAA Pattern**: Arrange-Act-Assert
- **Test Rules**:
  - Write independent tests without hidden environment dependencies
  - Use mocks/fakes for external services and databases
  - Cover negative scenarios and validation errors
- Run specific tests: `dotnet test --filter "ClassName=AppConfigServiceTests"`

## Git Workflow and Commit Conventions

**Branch Strategy:**
- **master**: Production branch (only stable releases)
- **develop**: Main development branch ⭐ (use this for new features)
- **feature/***: Feature branches (create from develop)
- **bugs/***: Bug fix branches
- **projects/***: Project-specific branches (e.g., projects/sikn-531-gazprom)

**Branch Guidelines:**
- Always create new branches from `develop`
- Merge back to `develop` after testing
- Use descriptive names (e.g., `feature/status-bar-improvements`)
- Delete feature branches after merge

**Commit Message Rules:**
- NEVER mention AI, automated generation, or "Claude" in commit messages
- Use Russian language for commit messages
- Follow conventional commits style: "Тип: краткое описание"
- Include detailed description in commit body if needed
- Reference issue numbers when applicable

**Important Warnings:**
- Large number of configuration files are tracked - be careful with bulk changes
- Many binary .frx templates are in the repository - use FastReport Designer for editing
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
- **FastReport.Web.Skia** (2025.2.8): Report generation engine
- **NLog** (5.4.0): Structured logging
- **Newtonsoft.Json** (13.0.3): JSON processing
- **Platform Libraries**: SkiaSharp, HarfBuzzSharp, System.Drawing.Common, libgdiplus (Linux)

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
- `/TN_Doc/wwwroot/HTML/` - HTML editing forms (deprecated since v1.4.2 - now generated in-memory)
- **Logs**: Windows: `TN_Doc/logs/`, Linux: `/opt/TN_Doc/logs/`

### Documentation
- `/CHANGELOG.md` - Version history and detailed change log
- `/docs/` - Additional documentation (architecture, API, deployment, integration guides)
- `/README.md` - Project overview and quick start guide

## Common Issues and Solutions

**Build and Dependencies:**
- **Build errors**: Ensure NuGet sources are configured (ortpr and FastReport)
- **Runtime version mismatch**: Verify .NET Runtime 8.0.13+ is installed (`dotnet --info`)

**Vue Component Issues:**
- **Build fails**: Run `npm install` in `TN_Doc/Client/` first
- **Hot reload not working**: Check Vite dev server is running on correct port
- **Stale build output**: Run `npm run clean` in `TN_Doc/Client/` and rebuild

**Document Generation:**
- **Missing templates**: Check that .frx files exist and paths in Cfg*.json are correct
- **"File in use" errors**: Should not occur in v1.4.1+ due to in-memory PDF generation

**Database and Integration:**
- **Database connection**: Verify MySQL/MariaDB connectivity and credentials in CfgApp.json
- **OPC DA tag errors**: All tags must be pre-registered in `opc.da.tags.json` before use (unlike OPC UA)
- **ELIS integration issues**: Check SSL certificates in `Cert/` folder

**Platform-specific:**
- **Permission issues on Linux**: Ensure `alphadaemon` user has access to `/opt/TN_Doc/` and `/opt/TN_Doc/logs/`
- **libgdiplus errors on Linux**: Install via `sudo apt install libgdiplus` (Debian/Ubuntu)

## Recent Changes (v1.4.3)

Key improvements (see `/TN_Doc/changes.md` for full history):
- Removed TN.Tools project (obsolete functionality)
- Updated KMH_MI2816 for IVK version 7.12.14.3000 protocol changes
- Updated docgeneral to version 1.2.2
- **UI Theme Improvements**: Centralized color management via CSS variables in `material3.css`
- **Status bar improvements**: 4 indicator states (online, offline, ndv, warning), improved layout
- **LoggingPathService refactoring**: Moved to `TN.DocGeneral/Services/` for cross-project reusability

## Previous Major Changes (v1.4.2)

- ⚠️ **Базовая версия конфигуратора**: веб-интерфейс для управления конфигурацией (`/configurator`)
- ⚠️ **Базовая версия строки состояния**: диагностические индикаторы связи с устройствами и сервисами
- ⚠️ **Кэширование конфигурационных файлов**: `IConfigurationCacheService` с LRU eviction
- ⚠️ **Кэширование загрузки документов**: `IDocModuleLoader` оптимизация загрузки DLL
- ⚠️ **Исключено построение HTML из файла**: GetEditDoc теперь работает в памяти
- Рефакторинг UI приложения
- Упрощение определения путей до файлов

## Related Projects

The following projects must be deployed at the same level (all share TN_Doc configuration):
- **TN_KMH**: Контроль метрологических характеристик - `git clone http://192.168.100.100/orpovy/ivk/tn_kmh.git`
- **TN_MessagingService**: OPC клиент - `git clone http://192.168.100.100/orpovy/ivk/tn_messagingservice.git`
  - Use "samara_build" branch for OPC DA support
  - Master branch for OPC UA communication
- **TN.ElisConnector**: ELIS integration - `git clone http://192.168.100.100/orpovy/ivk/tn.elisconnector.git`

## Version Management

- Version is centrally managed in `TN_Doc.csproj`
- Changes are documented in `/TN_Doc/changes.md`
- Current version: 1.4.3 (.NET 8.0)
