# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc — ASP.NET Core 8.0 веб-приложение для генерации технических документов ИВК (Измерительно-вычислительный комплекс): паспорта качества, протоколы поверки, акты, отчёты. Использует FastReport для шаблонов `.frx`.

**Version**: 1.3.8
**Runtime**: .NET 8.0
**Branches**: `master` (релизы/PR), `developWork` (разработка)

**Critical Rules:**
- **НИКОГДА** не упоминать AI, Claude в коммитах
- **НИКОГДА** добавлять "Co-Authored-By: Claude" или подобное
- **ВСЕГДА** русский язык для коммит-сообщений
- **Формат коммитов**: `Область: описание` (например: `Passport: исправлена валидация`)

## Commands

```bash
# Git submodules (обязательно после клонирования)
git submodule update --init --recursive

# Build & Run
dotnet restore
dotnet build
dotnet run --project TN_Doc                       # http://localhost:38509

# Release
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained false

# Vue Client (если используется)
cd TN_Doc/Client && npm install && npm run build:all

# Testing (NUnit)
dotnet test                                       # Все тесты
dotnet test Tests/Tests.Unit                      # Unit-тесты
dotnet test Tests/Tests.Integration               # Интеграционные
dotnet test --filter "FullyQualifiedName~TestClass.TestMethod"  # Конкретный тест
dotnet test --filter "Namespace~KMH"              # По namespace

# E2E тесты (Playwright)
dotnet test Tests/Tests.E2E
set HEADED=1 && dotnet test Tests/Tests.E2E       # В видимом режиме (Windows)
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
├── TN_Doc/                    # ASP.NET Core MVC приложение
│   ├── Controllers/           # API endpoints
│   ├── Models/Services/       # Бизнес-логика (PrinterService, DirectoryService)
│   ├── Extensions/            # DI расширения (ServiceCollectionExtensions)
│   ├── Views/                 # Razor представления
│   ├── wwwroot/               # Статика (JS/CSS/HTML)
│   ├── Cfg/                   # Конфигурации документов (JSON)
│   └── Doc/                   # FastReport шаблоны (*.frx)
├── tn.docgeneral/             # Git submodule: ~48 модулей документов
├── tn_toolsfastreport/        # Git submodule: утилиты FastReport
└── Tests/                     # NUnit тесты
    ├── Tests.Unit/            # Модульные тесты
    ├── Tests.Integration/     # Интеграционные (КМХ ~168 тестов)
    ├── Tests.E2E/             # End-to-end (Playwright)
    └── Tests.Shared/          # Общие fixtures и helpers
```

### Document Generation Flow

```
User Request → HomeController → DLL загрузка (Reflection) → FastReport → PDF
```

1. `HomeController.LoadDocsModule()` загружает DLL модуля документа через `Assembly.LoadFrom()`
2. Вызывает `GetViewDoc(id)` → JSON данные
3. Получает путь к шаблону `.frx` через `GetPathTemplateFile()`
4. FastReport генерирует PDF в `wwwroot/PDF/PDF.pdf`

### Document Module Pattern

Каждый тип документа — отдельная DLL в `tn.docgeneral/` с конфигурацией:
- `Cfg/Cfg{DocumentType}.json` — настройки шаблона
- `Cfg/CfgEdit{DocumentType}.json` — конфигурация формы редактирования
- `Doc/{Number}_{DocumentType}.frx` — FastReport шаблон

### Controllers

| Controller | Назначение |
|------------|------------|
| `HomeController` | Главная: просмотр/печать/редактирование документов, загрузка модулей |
| `DirEditorController` | Справочники и конфигурация паспортов качества |
| `PrintController` | Печать последнего PDF |
| `ExportController` | Форматы экспорта |
| `ElisController` | Логирование сообщений ELIS |

### Services & DI

Регистрация в `Startup.cs` через extension-методы из `TN_Doc/Extensions/`:
- `services.AddPrinterService()` — `PrinterService` (печать PDF)
- `services.AddPrinters()` — `WindowsPrinter` / `LinuxPrinter` (платформо-зависимые)
- `services.AddAppInfoProvider()` — информация о приложении
- `AppConfigService` (Singleton) — загрузка/кэш конфигурации (`CfgApp.json`, `Cfg*.json`)

## Key Files

| Назначение | Путь |
|------------|------|
| Главная конфигурация | `TN_Doc/Cfg/CfgApp.json` |
| Конфиги паспортов | `TN_Doc/Cfg/Passport/CfgEditPassport*.json` |
| FastReport шаблоны | `TN_Doc/Doc/*.frx` |
| UI скрипты | `TN_Doc/wwwroot/js/Common.js` |
| HTML редакторы | `TN_Doc/wwwroot/HTML/` |
| DI конфигурация | `TN_Doc/Startup.cs` |
| Точка входа | `TN_Doc/Program.cs` |

## Development Patterns

- `PascalCase` для типов/методов, `_camelCase` для private readonly
- Early returns, избегать глубокой вложенности
- Бизнес-логика в `Services/`, не в контроллерах
- Логи через NLog, не подавлять исключения
- **Test Naming**: `MethodName_WhenCondition_ThenExpectedResult`
- **Mocking**: Moq для внешних сервисов

## Submodule Workflow

```bash
cd tn.docgeneral
git add . && git commit -m "Описание"
git push
cd ..
git add tn.docgeneral
git commit -m "Обновлён субмодуль tn.docgeneral"
```

## Common Issues

| Проблема | Решение |
|----------|---------|
| Ошибки сборки | Проверить NuGet sources (ortpr, FastReport) |
| Submodules пустые | `git submodule update --init --recursive` |
| Изменения конфига не применяются | Перезапустить приложение (кэш) |
| libgdiplus (Linux) | `sudo apt-get install libgdiplus` |

**Known Issues (v1.3.8):**
- `ConfigurationCacheService`, `DbSchemaCache` — не реализованы
- `ClientLogController`, `PdfController` — не реализованы
- `PrintControllerTests` — требует интерфейс `IPrinterService`

## Documentation

Подробная документация в папке `docs/`:
- [Архитектура](docs/architecture/overview.md)
- [Модули документов](docs/architecture/document-modules.md)
- [Редактор справочников](docs/architecture/configurator.md)
- [Конфигурация паспорта](docs/configs/passport.md)
- [Интеграция ELIS](docs/integration/elis.md)
- [Тестирование](docs/development/testing.md)
- [Развёртывание на Linux](docs/deployment/linux.md)

## Related Projects

Все проекты используют общий `TN_Doc/Cfg/CfgApp.json`:
- **TN_KMH** — Контроль метрологических характеристик
- **TN_MessagingService** — OPC клиент (DA/UA)
- **TN.ElisConnector** — Интеграция с ELIS/LabHub
