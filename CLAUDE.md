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
dotnet run --project TN_Doc                       # http://localhost:5000 (Kestrel) или :38509 (IIS Express)

# Release
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained false

# Testing (NUnit)
dotnet test                                       # Все тесты
dotnet test Tests/Tests.Unit                      # Unit-тесты
dotnet test Tests/Tests.Integration               # Интеграционные (~168 тестов КМХ)
dotnet test Tests/Tests.E2E                       # E2E (Playwright)
dotnet test --filter "FullyQualifiedName~TestClass.TestMethod"  # Конкретный тест
dotnet test --filter "Namespace~KMH"              # По namespace
dotnet test --filter "Category=Dictionaries"     # По категории

# E2E тесты (Playwright)
set HEADED=1 && dotnet test Tests/Tests.E2E       # В видимом режиме (Windows)
HEADED=1 dotnet test Tests/Tests.E2E              # Linux/macOS
set PWDEBUG=1 && dotnet test Tests/Tests.E2E      # Debug режим
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
│   ├── Controllers/           # API endpoints (Home, DirEditor, Print, Export, Elis)
│   ├── Models/Services/       # Бизнес-логика (PrinterService, DirectoryService)
│   ├── Extensions/            # DI расширения (ServiceCollectionExtensions)
│   ├── Views/                 # Razor представления
│   ├── wwwroot/               # Статика (JS/CSS/HTML)
│   ├── Cfg/                   # Конфигурации документов (JSON) ~85 файлов
│   └── Doc/                   # FastReport шаблоны (*.frx)
├── tn.docgeneral/             # Git submodule: ~48 модулей документов
│   ├── TN.DocGeneral/         # Базовая библиотека (v1.1.1)
│   ├── tn.utils/              # Вложенный submodule: TN.Utils
│   ├── Passport, Act, Jornal, Report  # Базовые документы
│   ├── Poverka*               # 21 модуль протоколов поверки
│   └── KMH*, KMX*             # 18 модулей КМХ
├── tn_toolsfastreport/        # Git submodule: TN_Tools (утилиты FastReport)
└── Tests/                     # NUnit тесты (~650 тестов, ~48% активны)
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
| Логирование | `TN_Doc/nlog.config` |

## Development Patterns

- `PascalCase` для типов/методов, `_camelCase` для private readonly
- Early returns, избегать глубокой вложенности
- Бизнес-логика в `Services/`, не в контроллерах
- Логи через NLog, не подавлять исключения
- **Test Naming**: `MethodName_WhenCondition_ThenExpectedResult`
- **Mocking**: Moq для внешних сервисов
- **In-Memory DB**: для изоляции интеграционных тестов

## Submodule Workflow

```bash
cd tn.docgeneral
git add . && git commit -m "Описание"
git push
cd ..
git add tn.docgeneral
git commit -m "Обновлён субмодуль tn.docgeneral"
```

## Testing Structure

| Проект | Описание | Ключевые тесты |
|--------|----------|----------------|
| `Tests.Unit` | Изолированные тесты компонентов | Controllers, Services, Models, Extensions |
| `Tests.Integration` | Взаимодействие компонентов | КМХ модули (~168), базовые документы, AppConfigService |
| `Tests.E2E` | UI тесты (Playwright) | Справочники: Users, UserGroups, PowersOfAttorney, TestMethods |
| `Tests.Shared` | Общие helpers | `MockConfigHelper`, `BaseDocumentTest`, `DocumentTestHelpers` |

**Playwright конфигурация:** локаль `ru-RU`, viewport 1920x1080, скриншоты в `tests/dictionaries/results/`

## Common Issues

| Проблема | Решение |
|----------|---------|
| Ошибки сборки | Проверить NuGet sources (ortpr, FastReport) |
| Submodules пустые | `git submodule update --init --recursive` |
| Изменения конфига не применяются | Перезапустить приложение (кэш) |
| libgdiplus (Linux) | `sudo apt-get install libgdiplus` |
| Playwright браузеры | `pwsh Tests/Tests.E2E/bin/Debug/net8.0/playwright.ps1 install` |

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
- [Настройка окружения](docs/development/setup.md)
- [Развёртывание на Linux](docs/deployment/linux.md)

## Related Projects

Все проекты используют общий `TN_Doc/Cfg/CfgApp.json`:
- **TN_KMH** — Контроль метрологических характеристик
- **TN_MessagingService** — OPC клиент (DA/UA)
- **TN.ElisConnector** — Интеграция с ELIS/LabHub
