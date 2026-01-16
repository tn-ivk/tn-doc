# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc — ASP.NET Core 8.0 веб-приложение для генерации технических документов и отчётов ИВК (Измерительно-вычислительный комплекс): паспорта качества, протоколы поверки, акты, отчёты по измерениям. Использует FastReport для шаблонов `.frx`.

**Version**: 1.4.3 (.NET 8.0)
**Branches**: `master` (релизы/PR), `developWork` (разработка)
**Runtime**: .NET 8.0.13+, Node.js 18+

**Critical Rules:**
- ⚠️ **НИКОГДА** не упоминать AI, Claude, автоматизацию в коммитах
- ⚠️ **НИКОГДА** добавлять "Co-Authored-By: Claude" или подобные атрибуции
- ⚠️ **ВСЕГДА** русский язык для коммит-сообщений

## Essential Commands

```bash
# Build and run
dotnet build                                    # Build solution
cd TN_Doc && dotnet run                         # Run app (http://localhost:38509)

# Git submodules (⚠️ required after clone)
git submodule update --init --recursive

# Vue components (from TN_Doc/Client/)
npm install && npm run build:all               # Build all Vue apps
npm run dev:editor                             # Document Editor dev (port 5175)

# Testing
dotnet test                                     # Run all tests
dotnet test --filter "ClassName=TestClass"      # Specific test class
dotnet test --filter "Namespace~KMH"            # Filter by namespace
```

## Quick Start

```bash
# 1. Setup NuGet sources
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
  --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text

# 2. Build
git submodule update --init --recursive
dotnet restore && dotnet build
cd TN_Doc/Client && npm install && npm run build:all
cd ../.. && dotnet run --project TN_Doc
```

## Architecture

```
tn_doc/
├── TN_Doc/                    # ASP.NET Core web app
│   ├── Controllers/           # API endpoints
│   ├── Services/              # Business logic
│   ├── Cfg/                   # Document configs
│   ├── Doc/                   # FastReport templates (*.frx)
│   └── Client/                # Vue 3 apps (npm workspaces)
│       ├── statusbar/         # Status monitoring (port 5173)
│       ├── configurator/      # Config UI (port 5174)
│       ├── document-editor/   # Document editing (port 5175)
│       └── shared/            # Shared utilities
├── tn.docgeneral/             # ⚠️ Git submodule: ~48 document libraries
├── tn_toolsfastreport/        # ⚠️ Git submodule: FastReport utilities
├── winprutil/                 # ⚠️ Git submodule: Windows printing
└── Tests/                     # NUnit tests
```

### Document Module Pattern

Каждый тип документа реализует **IDocumentEditor** (`tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs`):

```csharp
public interface IDocumentEditor
{
    DocumentEditConfig GetEditConfig(int id);  // Config for Vue editor
    bool SaveDocument(int id, Dictionary<string, object> values);  // Save from Vue
}
```

**Файлы конфигурации документа:**
- `TN_Doc/Cfg/Cfg{DocumentType}.json` — настройки шаблона
- `TN_Doc/Cfg/CfgEdit{DocumentType}.json` — конфигурация формы редактирования
- `TN_Doc/Doc/{Number}_{DocumentType}.frx` — FastReport шаблон

### Key Controllers

| Controller | Purpose |
|------------|---------|
| `HomeController` | Главная, просмотр/печать документов |
| `DocumentEditController` | API редактирования (Vue Document Editor) |
| `ConfiguratorController` | API конфигуратора |
| `ElisController` | Интеграция с ELIS |

### Key Services (DI)

**Singleton:** `IAppConfigService`, `IReportBuffer`, `IConfigurationCacheService`
**Scoped:** `IStatusProvider`, `IConfigurationService`
**Background:** `StatusMonitoringService`

## Key File Paths

| Purpose | Path |
|---------|------|
| Main config | `TN_Doc/Cfg/CfgApp.json` |
| Passport edit configs | `TN_Doc/Cfg/Passport/CfgEditPassport*.json` |
| FastReport templates | `TN_Doc/Doc/*.frx` |
| Vue Document Editor | `TN_Doc/Client/document-editor/` |
| IDocumentEditor interface | `tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs` |
| DI configuration | `TN_Doc/Startup.cs` |
| Logging config | `TN_Doc/nlog.config` |

## Development Patterns

### Adding API Endpoint
1. Создать action в контроллере `TN_Doc/Controllers/`
2. Inject сервисы через конструктор
3. Бизнес-логика в `TN_Doc/Services/`, не в контроллере
4. Логи через NLog, не подавлять исключения

### C# Conventions
- `PascalCase` для типов/методов, `_camelCase` для private readonly
- Early returns, избегать глубокой вложенности
- 4-space indentation, braces на отдельных строках

### Test Conventions
- **Naming**: `MethodName_WhenCondition_ThenExpectedResult`
- **Pattern**: AAA (Arrange-Act-Assert)
- **Mocking**: Moq для внешних сервисов

### Vue Guidelines
- **UI Library**: PrimeVue
- **Overlays**: `appendTo="body"` для dropdown
- **DateTime**: Локальное время, не UTC
- **State**: Pinia для stores, composables для логики

## Passport Configuration (v1.4.4)

**Key fields in CfgEditPassport*.json:**

| Field | Purpose |
|-------|---------|
| `SlaveKey` | Master-slave связь (slave скрыт, Legal=0) |
| `LinkedParameter` | Общий метод для пары параметров |
| `IsBallast` | Result = Measurement автоматически |
| `Edit` | Разрешить редактирование |
| `RequiredFill` | Обязательное поле |

**Field History System:** Требует `IsUsedElis = true` в конфигурации устройства.

## Common Issues

| Issue | Solution |
|-------|----------|
| Build errors | Проверить NuGet sources (ortpr, FastReport) |
| Submodules empty | `git submodule update --init --recursive` |
| Vue build fails | `npm install` в `TN_Doc/Client/` |
| Config changes ignored | Restart app (кэш конфигурации) |
| PrimeVue overlay clipped | `appendTo="body"` + global styles |
| Datetime timezone shift | Local time, не UTC |
| Field history not showing | `IsUsedElis = true` в CfgApp.json |

## Git Workflow

**Submodule changes:**
```bash
cd tn.docgeneral
git add . && git commit -m "Описание"
git push
cd ..
git add tn.docgeneral
git commit -m "Обновлён субмодуль tn.docgeneral"
```

**Commit message format:** `Область: описание` (например: `Passport: исправлена валидация`)

## Related Projects

- **TN_KMH**: Контроль метрологических характеристик
- **TN_MessagingService**: OPC клиент
- **TN.ElisConnector**: ELIS интеграция

Все проекты используют общий `CfgApp.json`.

## Additional Resources

- [CHANGELOG.md](CHANGELOG.md) — История версий
- [docs/](docs/) — Полная документация
- [docs/configs/passport.md](docs/configs/passport.md) — Конфигурация паспорта
- [docs/features/field-history.md](docs/features/field-history.md) — Система истории полей
