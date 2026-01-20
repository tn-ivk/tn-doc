# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc — ASP.NET Core 8.0 веб-приложение для генерации технических документов ИВК (Измерительно-вычислительный комплекс): паспорта качества, протоколы поверки, акты, отчёты. Использует FastReport для шаблонов `.frx`.

**Version**: 1.3.8
**Branches**: `master` (релизы/PR), `developWork` (разработка)
**Runtime**: .NET 8.0.13+, Node.js 18+

**Critical Rules:**
- ⚠️ **НИКОГДА** не упоминать AI, Claude в коммитах
- ⚠️ **НИКОГДА** добавлять "Co-Authored-By: Claude" или подобное
- ⚠️ **ВСЕГДА** русский язык для коммит-сообщений
- ⚠️ **Формат коммитов**: `Область: описание` (например: `Passport: исправлена валидация`)

## Commands

```bash
# Build & Run
dotnet build                              # Build solution
dotnet run --project TN_Doc               # Run (http://localhost:38509)

# Git submodules (⚠️ required after clone)
git submodule update --init --recursive

# Vue components (from TN_Doc/Client/)
npm install && npm run build:all          # Build all Vue apps
npm run dev:editor                        # Document Editor dev (port 5175)

# Testing (NUnit)
dotnet test                               # All tests
dotnet test --filter "ClassName=TestClass"    # Specific class
dotnet test --filter "Namespace~KMH"          # By namespace
dotnet test --logger "console;verbosity=detailed"  # Verbose
```

**NuGet Sources (required):**
```bash
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
  --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text
```

## Architecture

```
tn_doc/
├── TN_Doc/                    # ASP.NET Core MVC app
│   ├── Controllers/           # API endpoints
│   ├── Models/Services/       # Business logic
│   ├── Views/                 # Razor views
│   ├── wwwroot/               # Static files (JS/CSS)
│   ├── Cfg/                   # Document configs (JSON)
│   ├── Doc/                   # FastReport templates (*.frx)
│   └── Client/                # Vue 3 apps (npm workspaces)
├── tn.docgeneral/             # ⚠️ Git submodule: ~48 document libraries
├── tn_toolsfastreport/        # ⚠️ Git submodule: FastReport utilities
├── winprutil/                 # ⚠️ Git submodule: Windows printing
└── Tests/                     # NUnit tests (~315 working, ~335 disabled)
```

**UI Stack:** Razor views + jQuery/Bootstrap/DataTables. Vue 3 компоненты (statusbar, configurator, document-editor) находятся в `TN_Doc/Client/`.

### Document Generation Flow

```
User Request → HomeController → DocGeneral DLL (Reflection) → FastReport → PDF
```

1. `HomeController` загружает DLL модуля документа через Reflection
2. Вызывает `GetViewDoc(id)` → JSON данные
3. Получает путь к шаблону `.frx`
4. FastReport генерирует PDF

### Document Module Pattern

Каждый тип документа реализует **IDocumentEditor** (`tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs`):

```csharp
public interface IDocumentEditor
{
    DocumentEditConfig GetEditConfig(int id);  // Config for Vue editor
    bool SaveDocument(int id, Dictionary<string, object> values);
}
```

**Файлы документа:**
- `Cfg/Cfg{DocumentType}.json` — настройки шаблона
- `Cfg/CfgEdit{DocumentType}.json` — конфигурация формы редактирования
- `Doc/{Number}_{DocumentType}.frx` — FastReport шаблон

### Key Controllers

| Controller | Purpose |
|------------|---------|
| `HomeController` | Главная, просмотр/печать/редактирование документов |
| `DirEditorController` | Справочники и конфигурация паспортов |
| `DocumentEditController` | API редактирования (Vue Document Editor) |
| `ElisController` | Интеграция с ELIS |

### Services (DI)

**Singleton:** `IAppConfigService`, `IReportBuffer`
**Scoped:** `IStatusProvider`, `IConfigurationService`
**Background:** `StatusMonitoringService`

## Key Paths

| Purpose | Path |
|---------|------|
| Main config | `TN_Doc/Cfg/CfgApp.json` |
| Passport configs | `TN_Doc/Cfg/Passport/CfgEditPassport*.json` |
| FastReport templates | `TN_Doc/Doc/*.frx` |
| Vue apps | `TN_Doc/Client/` |
| Logs (Windows) | `{basedir}/TN_Doc/logs/` |
| Logs (Linux) | `/opt/TN_Doc/logs/` |
| DI config | `TN_Doc/Startup.cs` |
| Logging config | `TN_Doc/nlog.config` |

## Known Issues (v1.3.8)

- `ConfigurationCacheService`, `DbSchemaCache` — не реализованы
- `ClientLogController`, `PdfController` — не реализованы
- `PrintControllerTests` — требует интерфейс `IPrinterService`
- Core тесты (ActDocument, JornalDocument, PassportDocument, ReportDocument) — требуют правки конструкторов

## Development Patterns

### C# Conventions
- `PascalCase` для типов/методов, `_camelCase` для private readonly
- Early returns, избегать глубокой вложенности
- Бизнес-логика в `Services/`, не в контроллерах
- Логи через NLog, не подавлять исключения

### Test Conventions
- **Naming**: `MethodName_WhenCondition_ThenExpectedResult`
- **Pattern**: AAA (Arrange-Act-Assert)
- **Mocking**: Moq для внешних сервисов

### Vue Guidelines
- **UI Library**: PrimeVue
- **Overlays**: `appendTo="body"` для dropdown
- **DateTime**: Локальное время, не UTC
- **State**: Pinia для stores

## Submodule Workflow

```bash
# Изменения в submodule
cd tn.docgeneral
git add . && git commit -m "Описание"
git push
cd ..
git add tn.docgeneral
git commit -m "Обновлён субмодуль tn.docgeneral"
```

## Common Issues

| Issue | Solution |
|-------|----------|
| Build errors | Проверить NuGet sources (ortpr, FastReport) |
| Submodules empty | `git submodule update --init --recursive` |
| Vue build fails | `npm install` в `TN_Doc/Client/` |
| Config changes ignored | Restart app (кэш конфигурации) |
| PrimeVue overlay clipped | `appendTo="body"` + global styles |
| Field history not showing | `IsUsedElis = true` в CfgApp.json |

## Documentation

- [Architecture](docs/architecture/overview.md)
- [Passport Config](docs/configs/passport.md)
- [Field History](docs/features/field-history.md)
- [ELIS Integration](docs/integration/elis.md)
- [Deployment](docs/deployment/linux.md)
- [CHANGELOG](CHANGELOG.md)

## Related Projects

- **TN_KMH** — Контроль метрологических характеристик
- **TN_MessagingService** — OPC клиент
- **TN.ElisConnector** — ELIS интеграция

Все проекты используют общий `CfgApp.json`.
