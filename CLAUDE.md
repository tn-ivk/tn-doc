# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc is an ASP.NET Core 8.0 web application for generating technical documents and reports from measurement system data (ИВК - Измерительно-вычислительный комплекс). The system generates quality certificates, verification protocols, acceptance acts, and various measurement reports using FastReport templates.

**Version**: 1.4.2
**Target Framework**: .NET 8.0
**SDK Compatibility**: Works with .NET SDK 8.0+ and 9.0+
**Runtime Requirement**: .NET Runtime 8.0.13 or higher
**Main Development Branch**: develop
**Current Branch**: feature/ui-theme-2 (UI theme centralization and Configurator enhancements)
**Note**: Recent work includes UI theme improvements with centralized CSS variables, Configurator Vue application, and status bar cleanup

## Build and Development Commands

### Prerequisites
```bash
# Add required NuGet sources
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text

# Restore NuGet packages
dotnet restore
```

### Building the Solution
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build TN_Doc/TN_Doc.csproj

# Build in Release mode
dotnet build -c Release

# Publish for production (Linux)
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r linux-x64 --self-contained false -o ./publish

# Publish for production (Windows)
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained false -o ./publish

# Clean build
dotnet clean && dotnet build

# Rebuild solution
dotnet clean && dotnet restore && dotnet build
```

### Running the Application
```bash
# Run in development mode (from solution root)
cd TN_Doc
dotnet run

# Run from solution root
dotnet run --project TN_Doc/TN_Doc.csproj

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Run with specific URLs (default ports)
dotnet run --urls="http://localhost:38509;https://localhost:44357"

# Run with verbose output for debugging
dotnet run --verbosity detailed
```

### Building Vue Components (StatusBar & Configurator)
```bash
# Navigate to Client project root (monorepo workspace)
cd TN_Doc/Client

# Install all dependencies (first time only)
npm install

# Development mode - StatusBar with hot reload
npm run dev
# or explicitly
npm run dev --workspace=statusbar

# Development mode - Configurator with hot reload
npm run dev:configurator

# Build StatusBar for production
npm run build

# Build Configurator for production
npm run build:configurator

# Build all Vue apps (StatusBar + Configurator)
npm run build:all

# Type checking across all workspaces
npm run type-check

# Build outputs:
# - StatusBar: TN_Doc/wwwroot/statusbar/
# - Configurator: TN_Doc/wwwroot/configurator/
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests in specific project
dotnet test Tests/Tests.csproj

# Run tests with detailed output
dotnet test --logger:"console;verbosity=detailed"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run tests for specific class
dotnet test --filter "ClassName=AppConfigServiceTests"

# Run tests with code coverage
dotnet test /p:CollectCoverage=true
```

#### Test Structure and Conventions
- **Test Project**: `Tests/Tests.csproj` using NUnit framework with Moq for mocking
- **Test Categories**:
  - Controllers: `Tests/controllers/`
  - Services: `Tests/Services/`
- **Naming Convention**: `MethodName_WhenCondition_ThenExpectedResult`
- **Test Rules**:
  - Write independent tests without hidden environment dependencies
  - Use mocks/fakes for external services and databases
  - Cover negative scenarios and validation errors
  - Follow AAA pattern: Arrange-Act-Assert

### Code Quality and Linting
```bash
# Format code
dotnet format

# Check code style violations
dotnet format --verify-no-changes

# Analyze code with built-in analyzers
dotnet build /p:TreatWarningsAsErrors=true

# Run code analysis (if analyzers are configured)
dotnet build /p:RunAnalyzersDuringBuild=true
```

### Debugging and Diagnostics
```bash
# Check .NET runtime and SDK versions
dotnet --info

# List installed .NET runtimes
dotnet --list-runtimes

# List installed .NET SDKs
dotnet --list-sdks

# Check project dependencies
dotnet list package

# Check for outdated packages
dotnet list package --outdated

# Check for vulnerable packages
dotnet list package --vulnerable

# View detailed build logs
dotnet build -v detailed > build.log 2>&1

# Debug application with verbose logging
ASPNETCORE_ENVIRONMENT=Development dotnet run --verbosity detailed

# Check application logs (platform-specific)
# Linux: tail -f /opt/TN_Doc/logs/*.log
# Windows: Get-Content TN_Doc\logs\*.log -Tail 50 -Wait
```

### Related Projects Setup
The following projects must be deployed at the same level (all share TN_Doc configuration):
- TN_KMH: `git clone http://192.168.100.100/orpovy/ivk/tn_kmh.git`
- TN_MessagingService: `git clone http://192.168.100.100/orpovy/ivk/tn_messagingservice.git`
  - Use "samara_build" branch for OPC DA support (older IVK systems)
  - Master branch for standard OPC UA communication
- TN.ElisConnector (for ELIS integration): `git clone http://192.168.100.100/orpovy/ivk/tn.elisconnector.git`
  - Includes TSPD.Mock.Hub for testing ELIS integration

## Architecture Overview

### Solution Structure
- **TN_Doc**: Main ASP.NET Core web application
  - Entry point: `TN_Doc/Program.cs` and `TN_Doc/Startup.cs`
  - Controllers: `TN_Doc/Controllers/`
  - Models/Services: `TN_Doc/Models/`
  - Views: `TN_Doc/Views/`
  - Static files: `TN_Doc/wwwroot/`
  - Configuration: `TN_Doc/appsettings.json`, `TN_Doc/nlog.config`
- **TN_Doc/Client/**: Frontend applications (npm workspace monorepo)
  - `statusbar/`: Real-time status monitoring Vue 3 app
  - `configurator/`: Configuration management Vue 3 app
  - `shared/`: Shared TypeScript utilities and API client
  - `package.json`: Workspace root with unified scripts
  - `vite.config.base.ts`: Shared Vite configuration
- **TN.DocGeneral**: Core business logic and shared utilities
- **Ivk.DataBase**: Database library
- **tn.docgeneral/** (Document modules organized by type):
  - Individual document implementations (Passport, Poverka*, KMH*, Act, Report, Jornal)
  - Sikn425 modules in separate subfolder
  - Common modules for shared document logic
- **Tests**: Unit tests using NUnit framework with Moq for mocking
  - Controller tests: `Tests/controllers/`
  - Service tests: `Tests/Services/`
- **tn_toolsfastreport/TN_Tools**: FastReport utilities
- **winprutil**: Go-based Windows printing utility
- **Dll/**: Pre-compiled document module assemblies
- **prutils/**: Contains winprutil.exe for Windows PDF printing
- **Template/Config packages**: `TN_Doc/Doc/`, `TN_Doc/Cfg/`, `TN_Doc/DLL/`

### Document Module Pattern
Each document type module implements a consistent pattern:
- **Class Library**: Contains document-specific business logic
- **Configuration Files**:
  - Document config: `TN_Doc/Cfg/Cfg{DocumentType}.json`
  - Edit form config: `TN_Doc/Cfg/CfgEdit{DocumentType}.json`
- **FastReport Template**: `TN_Doc/Doc/{Number}_{DocumentType}.frx`
- **Standard Interface Methods**:
  - `GetViewDoc(id)`: Returns JSON data for report generation
  - `GetPathTemplateFile()`: Returns path to .frx template
  - `GetEditDoc(id)`: Returns HTML for editing forms
  - `SetDocFromJson(json)`: Updates document data from JSON

### FastReport Integration
- Templates are .frx files with embedded C# scripting
- Data passed via JSON parameter "JsonDoc"
- Templates can reference project DLLs for complex processing
- Export formats: PDF, Excel, ODS, XML, HTML
- Script security disabled for template flexibility
- Platform-specific rendering libraries (SkiaSharp, HarfBuzz)

### Configuration System
- **CfgApp.json**: Main application settings
  - Device definitions with database connections
  - ELIS integration settings
  - OPC communication parameters
  - Security features toggle
  - PVL and PVS usage enabled by default
- **Document Configs** (`Cfg*.json`):
  - Template paths and report settings
  - Edit form configurations
  - Field mappings and validation rules
- **Environment-Specific**:
  - Development configs: `*.Development.json`
  - Only copied to output in Debug builds
  - Removed before deployment to avoid conflicts
- **Configuration Management**:
  - Web-based configurator UI at `/configurator` endpoint
  - ConfigurationService provides thread-safe config updates
  - Validators ensure OPC and DB settings correctness before save
  - Change logging with diff calculation for audit trails

## Key Dependencies and External Systems

### ELIS Integration
ELIS (Единая Лабораторная Информационная Система) for quality passport data:
- Requires TN.ElisConnector module from separate repository
- Communicates with LabHub for laboratory data
- Configuration in CfgApp.json under "Elis" section
- Supports global or per-device ELIS configuration
- SSL certificate support for secure communication
- **SSL Certificate Setup**:
  - Place certificate files in `Cert/` folder
  - Install certificates on server
  - Configure certificate path in ELIS settings

### OPC Communication
Real-time data acquisition from measurement systems:
- **OPC DA**: Legacy protocol (requires "samara_build" branch of TN_MessagingService)
  - Tags must be pre-defined in `opc.da.tags.json`
  - All tags must be registered before first use
- **OPC UA**: Modern protocol with better security
  - Configuration in `opc.ua.tags.json`
- Settings in CfgApp.json under "OpcConnectionSettings"

### Database Connectivity
- **MySQL/MariaDB** via Pomelo.EntityFrameworkCore.MySql (v7.0.0)
- Per-device connection strings in CfgApp.json
- Password encryption supported via EncryptionLibrary
- Entity Framework Core for data access

### Key Package Dependencies
- **FastReport.Web.Skia** (2025.2.8): Report generation engine
- **NLog** (5.4.0): Structured logging
- **Newtonsoft.Json** (13.0.3): JSON processing
- **Platform Libraries**:
  - SkiaSharp.NativeAssets.Linux
  - HarfBuzzSharp.NativeAssets.Linux
  - System.Drawing.Common
  - libgdiplus (Linux system dependency)
- **Hosting Extensions**:
  - Microsoft.Extensions.Hosting.Systemd (Linux)
  - Microsoft.Extensions.Hosting.WindowsServices (Windows)

## Important File Locations

### Configuration
- `/TN_Doc/Cfg/CfgApp.json` - Main application configuration
- `/TN_Doc/Cfg/Cfg{DocumentType}.json` - Document template configurations
- `/TN_Doc/Cfg/CfgEdit{DocumentType}.json` - Edit form configurations
- `/TN_Doc/appsettings.json` - ASP.NET Core settings
- `/TN_Doc/nlog.config` - Logging configuration
- `/TN_Doc/opc.da.tags.json` - OPC DA tag definitions
- `/TN_Doc/opc.ua.tags.json` - OPC UA tag definitions

### Templates and Documents
- `/TN_Doc/Doc/**/*.frx` - FastReport templates
- `/TN_Doc/Doc/GOSTR8.1011-2022/` - GOST standard templates
- `/TN_Doc/Doc/Act/` - Acceptance act templates
- `/TN_Doc/Doc/Passport/` - Quality passport templates
- `/TN_Doc/wwwroot/HTML/` - HTML editing forms

### Output Directories
- `/TN_Doc/wwwroot/PDF/` - Generated PDF documents
- **Log Directories** (Platform-specific as of v1.4.1):
  - Windows: `TN_Doc/logs/`
  - Linux: `/opt/TN_Doc/logs/`
- `/TN_Doc/UserPreference/` - User preferences (last used templates)

## Development Notes

### Service Architecture
The application uses dependency injection with the following key services:

**TN_Doc Services:**
- **IAppConfigService**: Singleton configuration management and document class factory
- **PrinterService**: Platform-specific printing (Windows/Linux)
- **DirectoryService**: File system operations
- **DbContext**: Entity Framework database context
- **AppInfoProvider**: Application version information
- **IDbSchemaCache**: Scoped service for caching database schema information
- **IDocModuleLoader**: Singleton for dynamic loading of document modules
- **IStatusProvider**: Scoped service for system health monitoring
- **StatusMonitoringService**: Background hosted service for continuous status monitoring
- **ConnectionTracker**: Tracks active SignalR connections to optimize background checks

**TN.DocGeneral Shared Services:**
- **IReportBuffer**: Singleton for in-memory PDF storage (no external dependencies)
- **LoggingPathService**: Static utility for platform-specific log paths (accepts `applicationName` parameter)

Services are registered in `Startup.cs` and `Extensions/IServiceCollectionExtensions.cs`.

### Status Bar Architecture
The application includes a real-time status bar built with **Vue 3 + PrimeVue** that monitors system health:
- **Frontend Stack**:
  - Vue 3.4.21 with TypeScript
  - **PrimeVue 4.2+** - Enterprise UI component library
  - Pinia for state management
  - SignalR client for real-time updates
  - Vite as build tool
  - Source: `/TN_Doc/Client/statusbar/`
- **Device Indicators**: Database connectivity for each configured ИВК device
- **OPC Indicators**: OPC DA/UA server connection status
- **Service Indicators**: SignalR Hub and ELIS laboratory system status
- **Real-time Updates**: SignalR-based push notifications for status changes via `/statusHub` endpoint
- **Manual Refresh**: Click individual indicators or global refresh button
- **Configuration-Driven**: Dynamically creates indicators based on `CfgApp.json` device/OPC settings
- **Backend Components**:
  - `StatusHub`: SignalR hub at `/statusHub` for broadcasting status updates
  - `StatusMonitoringService`: Background service that periodically checks system health (every 60s)
  - `StatusProvider`: Service that queries database, OPC, MessagingService, and ELIS endpoints
  - `StatusController`: REST API endpoint `/api/status` with 5-second caching
  - HTTP clients configured with timeouts (2s for MessagingService, 5s for ELIS)
- **ELIS Indicator Logic**:
  - Shown only when `CfgApp.json → Elis.Use = true`
  - Current implementation: checks if ELIS configuration is enabled (not real connection test)
  - TODO: Implement real health check via TN.ElisConnector endpoint
  - Status determined by `CheckElisServiceAsync()` in StatusProvider
- **PrimeVue Components Used**:
  - Badge: Status indicators with color coding
  - Button: Refresh action with loading state
  - Tag: SignalR connection indicator
  - Message: Error notifications
  - Tooltip: Contextual help

### Configurator Architecture
The application includes a web-based configuration interface built with **Vue 3 + PrimeVue**:
- **Frontend Stack**:
  - Vue 3.4.21 with TypeScript
  - **PrimeVue 4.2+** - Enterprise UI component library
  - Pinia for state management
  - Axios HTTP client
  - Vite as build tool
  - Source: `/TN_Doc/Client/configurator/`
- **Features**:
  - General settings tab: export paths, security features, local OPC client config
  - Devices tab: device list with search, multi-select for batch editing, mixed-state indicators
  - Per-device configuration: enabled state, document templates, DB connections, OPC settings
  - Real-time validation with backend validators (OpcConfigValidator, DbConfigValidator)
  - Unsaved changes protection (browser beforeunload warning)
  - Change logging with diff calculation
- **Backend Components**:
  - `ConfiguratorController`: REST API at `/api/configurator/`
  - `ConfigurationService`: Business logic and config file management
  - `OpcConfigValidator`, `DbConfigValidator`: Server-side validation
- **PrimeVue Components Used**:
  - TabView/TabPanel: Main navigation
  - DataTable: Device list with search/selection
  - InputSwitch: Toggle controls
  - MultiSelect: Document template selection
  - InputText/Password: Form inputs
  - Button: Actions with loading states
  - Message: Validation errors and warnings
  - Tag: Document template badges

### Document Generation Architecture
The system uses a factory pattern with dynamic module loading:
- **IAppConfigService**: Singleton that manages configuration and provides `GetDocumentClass(idDevice, idDoc)` factory method
- **Document Classes**: Each document type implements standard interface with methods:
  - `GetViewDoc(id)`: Returns JSON data for report generation
  - `GetPathTemplateFile()`: Returns path to .frx template file
  - `GetEditDoc(id)`: Returns HTML for editing forms
  - `SetDocFromJson(json)`: Updates document data from JSON
- **Dynamic Loading**: Document modules are loaded at runtime from `Dll/` directory or compiled assemblies
- **Template Resolution**: FastReport templates (.frx files) paths resolved through document configuration files

### Memory Management and PDF Generation
The application uses in-memory PDF generation (implemented in v1.4.1):
- **IReportBuffer**: Singleton service that stores generated PDF bytes in memory
- **Middleware Interceptor**: Custom middleware in `Startup.cs` intercepts `/PDF/PDF.pdf` requests and serves from memory buffer
- **Cache Control**: Responses include `no-store, no-cache, must-revalidate` headers to prevent browser caching
- **FastReport Integration**: Reports exported to `MemoryStream`, then stored in `IReportBuffer`
- **Printer Support**: Temporary files still needed for physical printing (winprutil.exe/CUPS)
- **Benefits**: Eliminates "file in use" errors and concurrent access issues

### Multi-platform Support
- **Windows**:
  - Deploy as Windows Service using `sc create`
  - Run as `NT AUTHORITY\NETWORK SERVICE` (must have appropriate permissions)
  - Uses `prutils/winprutil.exe` for PDF printing
- **Linux**:
  - Deploy with systemd service
  - Run as user `alphadaemon` (created during package installation)
  - Native CUPS printing support
  - Requires `libgdiplus` package
- Platform detection in `Program.cs` configures appropriate hosting

### Document Generation Workflow
1. User selects document type and record ID from web interface
2. Controller instantiates appropriate document module
3. Module retrieves data from database using ID
4. Configuration loaded from `Cfg{DocumentType}.json`
5. FastReport template (`.frx`) loaded with path from config
6. JSON data injected into template via "JsonDoc" parameter
7. Report rendered to requested format (PDF/Excel/etc.)
8. Result streamed to browser or saved to `/wwwroot/PDF/`

### Security Considerations
- **UseSecurityFeatures** flag in CfgApp.json enables:
  - Database password encryption
  - Enhanced input validation
  - Restricted file access
- FastReport script security disabled for flexibility
- Sensitive configs excluded from release builds
- CORS configured with permissive policy (review for production)

### Deployment Process

#### Linux Deployment (via GitLab CI/CD)
1. Tag push triggers pipeline (e.g., `v1.3.5`)
2. Docker builds with .NET SDK 8.0
3. Creates `.deb` package with:
   - Application files in `/opt/TN_Doc/`
   - Systemd service as `tn-doc.service` (runs with `ASPNETCORE_ENVIRONMENT=Production`)
   - Log directory at `/var/log/TN_Doc/`
4. Package validates .NET Runtime 8.0.13+
5. Pre/post-install scripts create `alphadaemon` user and set permissions

#### Windows Deployment
1. Publish as self-contained or framework-dependent
2. Create service: `sc create TN_Doc binPath="path\to\TN_Doc.exe"`
3. Configure service identity as `NT AUTHORITY\NETWORK SERVICE`
4. Ensure `prutils/winprutil.exe` is accessible
5. Grant necessary permissions to service account

### Development Workflow Tips
- Use `ASPNETCORE_ENVIRONMENT=Development` for local debugging
- Development configs auto-loaded in Debug builds
- Check logs for detailed error information (platform-specific paths)
- Test document generation with various IDs before deployment
- Validate FastReport templates with test data
- Remove `appsettings.Development.json` before production deployment
- When working with OPC DA, ensure all tags are pre-registered
- **Windows Service Deployment**: Use `NT AUTHORITY\NETWORK SERVICE` account (no password)
- **Deployment from develop branch**: All three projects should be deployed from develop folder

### Common Issues and Solutions
- **Build errors**: Ensure NuGet sources are configured (ortpr and FastReport)
- **Missing templates**: Check that .frx files exist and paths in Cfg*.json are correct
- **Database connection**: Verify MySQL/MariaDB connectivity and credentials in CfgApp.json
- **Document generation failures**: Check logs and validate JSON data structure
- **Platform-specific printing issues**: Ensure winprutil.exe (Windows) or CUPS (Linux) are available
- **OPC DA tag errors**: All tags must be pre-registered in `opc.da.tags.json` before use (unlike OPC UA)
- **ELIS integration issues**: Check SSL certificates in `Cert/` folder and verify LabHub connectivity
- **FastReport license errors**: Ensure FastReport NuGet source is configured with valid credentials
- **Runtime version mismatch**: Verify .NET Runtime 8.0.13+ is installed (`dotnet --info`)
- **Permission issues on Linux**: Ensure `alphadaemon` user has access to `/opt/TN_Doc/` and `/var/log/TN_Doc/`

### Working with Document Modules
When working on specific document types:
- Document-specific templates are in `/TN_Doc/Doc/{DocumentType}/`
- Configuration files follow pattern `Cfg{DocumentType}.json` and `CfgEdit{DocumentType}.json`
- Each document module implements standard interface methods in corresponding class library
- Test document generation thoroughly before modifying templates
- FastReport templates (.frx files) are binary - use FastReport Designer for editing
- Pre-compiled modules available in `Dll/` directory

#### Complete Document Libraries List (43 libraries)
The system contains the following document libraries that require test coverage:

**Core Documents (4)**:
- Act - Acceptance acts
- Passport - Quality passports
- Jornal - Journals
- Report - Reports

**Verification Documents - Poverka (21)**:
- Poverka1974, Poverka1974_04, Poverka1974_89, Poverka1974_95 - GOST R 8.1011-2022 standard
- Poverka2816 - MI 2816 verification
- Poverka3151 - GOST 3151 verification
- Poverka3189 - GOST 3189 verification
- Poverka3265_PR_PU, Poverka3265_UPR_PR, Poverka3265_UPR_PU - GOST 3265 verification
- Poverka3266, Poverka3267, Poverka3272 - GOST 3266/3267/3272 verification
- Poverka3287, Poverka3288 - GOST 3287/3288 verification
- Poverka3312_PR_PU, Poverka3312_UPR_PR - GOST 3312 verification
- Poverka3380 - GOST 3380 verification
- PoverkaSikn425_PR_PR, PoverkaSikn425_PR_PU - SIKN-425 verification

**Quality Control - KMH (14)**:
- KMH3265_PR_PU, KMH3265_UPR_PR - GOST 3265 quality control
- KMH3288_MPR_TPR - GOST 3288 mass/temperature quality control
- KMH3312_PR_PU, KMH3312_UPR_PR - GOST 3312 quality control
- KMH_MI2816 - MI 2816 quality control
- KMH_MPR_MPR, KMH_MPR_PU, KMH_MPR_TPR - Mass/pressure/temperature control
- KMH_PP, KMH_PP_Areom - Density control
- KMH_PR_PR, KMH_PR_PU - Pressure control
- KMH_PV - Volume control, KMH_PW - Mass control
- KMX_Sikn425_PR_PR, KMX_Sikn425_PR_PU - SIKN-425 quality control

**Common Libraries (3)**:
- CommonPoverka1974 - Shared functionality for GOST R 8.1011-2022 documents
- CommonSikn425 - Common SIKN-425 functionality

### Version Management
- Version is centrally managed in `TN_Doc.csproj`
- Changes are documented in `/TN_Doc/changes.md`
- Current version: 1.4.2 (.NET 8.0)

### Git Workflow Notes
- **Main branches**: `master` (production), `develop` (active development)
- Large number of configuration files are tracked - be careful with bulk changes
- Many binary .frx templates are in the repository - use FastReport Designer for editing
- Current working directory is on `develop` branch with many modified files

### Key Development Patterns

#### Coding Conventions (C#)
- **Naming**: `PascalCase` for types/methods, `camelCase` for private fields, `_camelCase` for private readonly fields
- **API Design**: Explicitly annotate public APIs, avoid `dynamic`/`object` without justification
- **Control Flow**: Prefer early returns, avoid deep nesting
- **Error Handling**: Never suppress exceptions, log and return meaningful errors
- **Dependencies**: Register in `Startup.cs`/`IServiceCollectionExtensions.cs`, bind settings via `IOptions<T>`
- **Project Files**: Fix target frameworks and exact package versions in `*.csproj`

#### ASP.NET Core Patterns
- **New Endpoints**: Create actions in appropriate controllers, inject services via DI, place logic in `Models/Services`
- **Validation**: Validate input data, return `IActionResult`/`ActionResult<T>`
- **Logging**: Use NLog (`ILogger<T>`), detailed logs in Development, minimal in Production
- **Configuration**: All parameters in `appsettings.*.json`, bind sections through `IOptions<T>`
- **Static Files**: Place resources in `wwwroot/`, consider browser caching

#### Dependency Injection Setup
- **Singleton Services**: AppConfigService (thread-safe singleton pattern)
- **Platform-Specific Services**: Printers registered based on OS detection in `IServiceCollectionExtensions.cs`
- **Release-Only Services**: Directory configuration only applied in Release builds (`#if RELEASE`)

#### Configuration Loading Pattern
Configuration follows a layered approach:
1. **appsettings.json**: ASP.NET Core application settings
2. **CfgApp.json**: Main application configuration (devices, ELIS, OPC)
3. **Cfg{DocumentType}.json**: Document-specific template and report settings
4. **CfgEdit{DocumentType}.json**: Edit form configurations
5. **Development overlays**: `*.Development.json` files auto-loaded in Debug builds

#### Error Handling and Logging
- **NLog**: Structured logging with platform-specific log directories
  - Configuration: `/TN_Doc/nlog.config`
  - Client-side logging via `ClientLogController` (v1.4.1+)
  - Document operation logging with library events
- **Exception Boundaries**: Controllers catch exceptions and return appropriate HTTP status codes
- **Resource Cleanup**: Explicit disposal of FastReport objects and streams in finally blocks

### Recent Changes (v1.4.2)
- Removed TN.Tools project (obsolete functionality)
- Simplified file path determination logic
- Updated KMH_MI2816 for IVK version 7.12.14.3000 protocol changes
- Updated docgeneral to version 1.2.2
- ⚠️ **UI Theme Improvements** (feature/ui-theme-2 branch):
  - **Centralized color management**: All colors moved to CSS variables in `material3.css`
  - Replaced hardcoded HEX colors with CSS variables across all stylesheets:
    * `elisRequestWindow.css` - 10 replacements
    * `errorDialogWindow.css` - 6 replacements
    * `LeftPanel.css` - 4 replacements
    * `menu-dropdown.css` - 6 replacements
    * `site.css` - 6 replacements including tab selector styling
    * `newstyle.css` - 9 replacements
    * `elisEditForm.css` - 2 replacements
    * `commonEditForm.css` - 1 replacement
  - **Centralized control sizes**: Heights defined via CSS variables (standard height: 35px)
  - **Configurator UI enhancements**:
    * Apply/Cancel buttons with improved styling
    * Device editor optimization and vertical alignment
    * Template tag styling improvements
  - **Top panel improvements**: Precise vertical alignment of controls and comboboxes
  - **Dictionary buttons**: Enhanced styling and icon sizing
  - **Document template selector**: Optimized combobox width
- ⚠️ Status bar improvements: Removed time display and project version info from status bar
- Cleaned up status bar JavaScript to remove unused time update functionality
- **LoggingPathService refactoring**: Moved to `TN.DocGeneral/Services/LoggingPathService.cs` for reusability across projects (TN_Doc, TN_KMH, etc.). Now accepts `applicationName` parameter for dynamic log paths
- **ReportBuffer cleanup**: Removed unused `_preparedReport` field and FastReport dependency. Class now only stores PDF bytes without external dependencies
- **GetEditDoc logging enhancement**: Added logging to HTML form save operations in all 37 document libraries
  - Replaced string concatenation with `Path.Combine()` for cross-platform compatibility
  - Added trace logging with full file path on successful save: `_logger.Trace($"HTML форма документа {IdDoc} (id={id}) сохранена: {htmlPath}")`
  - Affects: Act, all Poverka libraries (18), all KMH libraries (18)

### Previous Changes (v1.4.1)
- 🐞 Fixed incorrect population of Act shifts when filling "reverse" passports
- Sorting of Acts by date and documents in KMH and verifications by date
- Display of values in locked cells for quality passport editing forms
- Corrected log messages to properly display device names
- Updated docgeneral to version 1.2.1
- Manual viscosity correction for Poverka 1974 variants (2004, 1995, 1989)
- Client-side logging moved to separate controller (`ClientLogController`)
- Enhanced document operation logging (library events)
- Corrected ELIS form population logging

### Previous Major Changes
- **v1.4.0**: Modal dictionary window closing after saving, validation improvements
- **v1.3.7**: Manual viscosity correction for Poverka 3380, export fixes
- **v1.3.6**: Template corrections and passport improvements
- **v1.3.5**: Fixed document loading for IVK-2/IVK-3 devices
- **v1.3.4**: Platform-specific log paths, PVL/PVS enabled by default
