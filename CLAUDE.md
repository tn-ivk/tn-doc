# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc is an ASP.NET Core 8.0 web application for generating technical documents and reports from measurement system data (ИВК - Измерительно-вычислительный комплекс). The system generates quality certificates, verification protocols, acceptance acts, and various measurement reports using FastReport templates.

**Version**: 1.4.4-dev (.NET 8.0)
**Main Development Branch**: develop
**Runtime**: .NET Runtime 8.0.13+ (SDK 9.0+ для разработки)
**Node.js**: 18.0+ и npm 8.0+ (для Vue компонентов)

**Critical Rules:**
- ⚠️ **НИКОГДА** не упоминать AI, генерацию кода, автоматизацию или "Claude" в коммитах
- ⚠️ **НИКОГДА** не добавлять "Generated with Claude Code" или подобные атрибуции
- ⚠️ **НИКОГДА** не добавлять "Co-Authored-By: Claude" или любые AI co-author теги
- ⚠️ **ВСЕГДА** использовать русский язык для коммит-сообщений

## Essential Commands

```bash
# Build and run
dotnet build                                    # Build entire solution
cd TN_Doc && dotnet run                         # Run app (http://localhost:38509)

# Vue components (from TN_Doc/Client/)
npm install                                     # Install dependencies for all workspaces
npm run build:all                               # Build all Vue apps
npm run dev                                     # StatusBar dev (port 5173)
npm run dev:configurator                        # Configurator dev (port 5174)
npm run dev:editor                              # Document Editor dev (port 5175)
npm run type-check                              # TypeScript type checking for all workspaces

# Testing
dotnet test                                     # Run all tests
dotnet test --filter "ClassName=YourTestClass"  # Run specific test class
dotnet test --filter "FullyQualifiedName~KMH"   # Test document type libraries
```

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
│   ├── Models/Services/       # Business logic
│   ├── Cfg/                   # Document and app configuration
│   ├── Doc/                   # FastReport templates (*.frx)
│   └── Client/                # Vue 3 applications (npm workspaces)
│       ├── statusbar/         # Real-time status monitoring - PRODUCTION
│       ├── configurator/      # Configuration UI - PRODUCTION
│       ├── document-editor/   # Document editing - IN DEVELOPMENT
│       └── shared/            # Shared TypeScript utilities
├── TN.DocGeneral/             # Core business logic and shared utilities
├── Ivk.DataBase/              # Database library for IVK data access
├── tn.docgeneral/             # Document module libraries (git submodule, ~41 libraries)
├── tn_toolsfastreport/        # FastReport utilities (git submodule)
├── winprutil/                 # Windows printing utility (git submodule)
└── Tests/                     # NUnit tests with Moq
```

### Document Module Pattern

Each document type implements **IDocumentEditor** interface (defined in `TN.DocGeneral/IDocumentEditor.cs`):

1. **`GetViewDoc(id)`**: Returns JSON data for FastReport template
2. **`GetPathTemplateFile()`**: Returns path to `.frx` template file
3. **`GetEditConfig(id)`**: Returns configuration for Vue Document Editor
4. **`SaveDocument(id, values)`**: Saves document from Vue Editor (⚠️ all libraries migrated to this API in v1.4.4)

**Configuration files per document:**
- `TN_Doc/Cfg/Cfg{DocumentType}.json` - template and report settings
- `TN_Doc/Cfg/CfgEdit{DocumentType}.json` - edit form configuration (supports `SlaveKey` for master-slave parameters)
- `TN_Doc/Doc/{Number}_{DocumentType}.frx` - FastReport template

**Field History System (v1.4.4+):**
- Tracks data source for each field (ELIS, manual edit, IVK rounding)
- History stored in `__fieldHistory` object with field key prefix
- Visual indicators in Document Editor (colored badges for data source)
- Requires `IsUsedElis = true` in configuration

### Key Services (Dependency Injection)

Registered in `Startup.cs:ConfigureServices()`:

**Singleton:**
- `IAppConfigService` - Configuration management and document class factory
- `IReportBuffer` - In-memory PDF storage (eliminates disk I/O)
- `IConfigurationCacheService` - Configuration file caching with LRU eviction
- `IDocModuleLoader` - Document DLL loading cache

**Scoped:**
- `IStatusProvider` - System health monitoring
- `IConfigurationService` - Configuration management

**Background:**
- `StatusMonitoringService` - Health checks every 60s

### Configuration System

Layered configuration (loaded in order):
1. `appsettings.json` - ASP.NET Core settings
2. `CfgApp.json` - Main app configuration (devices, ELIS, OPC)
3. `Cfg{DocumentType}.json` - Document-specific settings
4. `CfgEdit{DocumentType}.json` - Edit form configurations
5. `*.Development.json` - Auto-loaded in Debug builds only

⚠️ Configuration files are cached - changes may require cache invalidation or app restart.

## Key Development Patterns

### Coding Conventions (C#)
- **Naming**: `PascalCase` for types/methods, `camelCase` for private fields, `_camelCase` for private readonly
- **Control Flow**: Prefer early returns, avoid deep nesting
- **Error Handling**: Never suppress exceptions, log and return meaningful errors
- **Dependencies**: Register in `Startup.cs`, bind settings via `IOptions<T>`

### Test Guidelines
- **Naming**: `MethodName_WhenCondition_ThenExpectedResult`
- **Pattern**: AAA (Arrange-Act-Assert)
- **Mocking**: Use mocks/fakes for external services and databases

### UI Theme (v1.4.3+)
All colors centralized in `/TN_Doc/wwwroot/css/material3.css`. Use CSS variables, never hardcode HEX colors.

### Vue Component Guidelines
- Use PrimeVue component library for UI elements
- For dropdown overlays, use `appendTo="body"` to avoid clipping issues
- For datetime-local fields, use local time (not UTC) to prevent timezone shifts
- Panel classes for PrimeVue overlays require global styles (not scoped)

## Key Dependencies and External Systems

### ELIS Integration
- Requires TN.ElisConnector module
- Configuration in CfgApp.json under "Elis" section
- SSL certificates in `Cert/` folder

### OPC Communication
- **OPC DA**: Tags in `opc.da.tags.json`
- **OPC UA**: Configuration in `opc.ua.tags.json`
- Settings in CfgApp.json under "OpcConnectionSettings"

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
| Vue build fails | Run `npm install` in `TN_Doc/Client/` first |
| PrimeVue overlay clipped | Use `appendTo="body"` and global styles |
| Datetime timezone shift | Use local time conversion, not UTC |
| Database connection | Verify credentials in CfgApp.json |
| OPC DA tag errors | Pre-register tags in `opc.da.tags.json` |
| Linux permission issues | Ensure `alphadaemon` user has `/opt/TN_Doc/` access |

## Multi-platform Support

- **Windows**: Runs as `NT AUTHORITY\NETWORK SERVICE`, uses `winprutil.exe` for printing
- **Linux**: Runs as `alphadaemon` user, uses CUPS printing, requires `libgdiplus`
- **Cross-platform**: Always use `Path.Combine()`, never hardcode path separators

## Related Projects

Deploy at the same level (share TN_Doc configuration):
- **TN_KMH**: Контроль метрологических характеристик
- **TN_MessagingService**: OPC клиент (samara_build for OPC DA, master for OPC UA)
- **TN.ElisConnector**: ELIS integration

## Additional Resources

- See [AGENTS.md](AGENTS.md) for detailed coding style, testing, and commit guidelines
- See [CHANGELOG.md](CHANGELOG.md) for version history and recent changes
- See `/docs/` for API documentation and deployment guides
