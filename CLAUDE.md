# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc — ASP.NET Core 8.0 веб-приложение для генерации технических документов ИВК (Измерительно-вычислительный комплекс): паспорта качества, протоколы поверки, акты, отчёты. Использует FastReport для шаблонов `.frx`.

**Version**: 1.3.8
**Branches**: `master` (релизы/PR), `developWork` (разработка)
**Runtime**: .NET 8.0.13+

**Critical Rules:**
- ⚠️ **НИКОГДА** не упоминать AI, Claude в коммитах
- ⚠️ **НИКОГДА** добавлять "Co-Authored-By: Claude" или подобное
- ⚠️ **ВСЕГДА** русский язык для коммит-сообщений
- ⚠️ **Формат коммитов**: `Область: описание` (например: `Passport: исправлена валидация`)

## Commands

```bash
# Git submodules (⚠️ обязательно после клонирования)
git submodule update --init --recursive

# Build & Run
dotnet restore                            # Восстановить зависимости
dotnet build                              # Сборка решения
dotnet run --project TN_Doc               # Запуск (http://localhost:5000)

# Release-сборка
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained false

# Testing (NUnit)
dotnet test                                      # Все тесты
dotnet test --filter "ClassName=TestClass"       # По классу
dotnet test --filter "Namespace~KMH"             # По namespace (КМХ тесты работают)
dotnet test --filter "FullyQualifiedName~Test"   # По полному имени
dotnet test --logger "console;verbosity=detailed"  # Подробный вывод
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
│   ├── Models/Services/       # Бизнес-логика
│   ├── Views/                 # Razor представления
│   ├── wwwroot/               # Статика (JS/CSS/HTML)
│   │   ├── js/                # Common.js, DirEditorComponentScript.js
│   │   └── HTML/              # HTML-шаблоны редактирования документов
│   ├── Cfg/                   # Конфигурации документов (JSON)
│   └── Doc/                   # FastReport шаблоны (*.frx)
├── tn.docgeneral/             # ⚠️ Git submodule: ~48 модулей документов
├── tn_toolsfastreport/        # ⚠️ Git submodule: утилиты FastReport
├── winprutil/                 # ⚠️ Git submodule: Windows печать
├── Tests/                     # NUnit тесты
└── docs/                      # Документация проекта
```

**UI Stack:** Razor views + jQuery/Bootstrap/DataTables. Редактирование документов — через HTML-формы в iframe.

### Document Generation Flow

```
User Request → HomeController → DocGeneral DLL (Reflection) → FastReport → PDF
```

1. `HomeController` загружает DLL модуля документа через Reflection
2. Вызывает `GetViewDoc(id)` → JSON данные
3. Получает путь к шаблону `.frx` через `GetPathTemplateFile()`
4. FastReport генерирует PDF в `wwwroot/PDF/PDF.pdf`

### Document Module Pattern

Каждый тип документа — отдельная DLL в `tn.docgeneral/`. Файлы документа:
- `Cfg/Cfg{DocumentType}.json` — настройки шаблона
- `Cfg/CfgEdit{DocumentType}.json` — конфигурация формы редактирования
- `Doc/{Number}_{DocumentType}.frx` — FastReport шаблон

### Key Controllers

| Controller | Назначение |
|------------|------------|
| `HomeController` | Главная: просмотр/печать/редактирование документов |
| `DirEditorController` | Справочники и конфигурация паспортов качества |
| `PrintController` | Печать последнего PDF |
| `ElisController` | Логирование сообщений ELIS |

### Services (DI)

- **AppConfigService** — загрузка/кэш конфигурации (`CfgApp.json`, `Cfg*.json`)
- **PrinterService** — печать PDF
- **LoggingPathService** — путь логов по ОС

## Key Paths

| Назначение | Путь |
|------------|------|
| Главная конфигурация | `TN_Doc/Cfg/CfgApp.json` |
| Конфиги паспортов | `TN_Doc/Cfg/Passport/CfgEditPassport*.json` |
| FastReport шаблоны | `TN_Doc/Doc/*.frx` |
| UI скрипты | `TN_Doc/wwwroot/js/Common.js` |
| HTML редакторы | `TN_Doc/wwwroot/HTML/` |
| Логи (Windows) | `{basedir}/TN_Doc/logs/` |
| Логи (Linux) | `/opt/TN_Doc/logs/` |
| DI конфигурация | `TN_Doc/Startup.cs` |
| Конфигурация логов | `TN_Doc/nlog.config` |

## Testing Status (v1.3.8)

- **~315 работающих тестов (~48%)**, ~335 отключенных тестов (~52%)
- **КМХ тесты полностью актуализированы** (7 файлов, ~168 тестов)
- **Common тесты работают** (CommonPoverka1974, CommonSikn425)
- Core тесты (ActDocument, JornalDocument, PassportDocument, ReportDocument) — требуют правки конструкторов

## Known Issues (v1.3.8)

- `ConfigurationCacheService`, `DbSchemaCache` — не реализованы
- `ClientLogController`, `PdfController` — не реализованы
- `PrintControllerTests` — требует интерфейс `IPrinterService`

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

| Проблема | Решение |
|----------|---------|
| Ошибки сборки | Проверить NuGet sources (ortpr, FastReport) |
| Submodules пустые | `git submodule update --init --recursive` |
| Изменения конфига не применяются | Перезапустить приложение (кэш конфигурации) |
| libgdiplus (Linux) | `sudo apt-get install libgdiplus` |

## Documentation

Подробная документация в папке `docs/`:
- [Архитектура](docs/architecture/overview.md)
- [Модули документов](docs/architecture/document-modules.md)
- [Редактор справочников](docs/architecture/configurator.md)
- [Конфигурация паспорта](docs/configs/passport.md)
- [Интеграция ELIS](docs/integration/elis.md)
- [Развёртывание на Linux](docs/deployment/linux.md)
- [Управление логами](docs/operations/logging.md)
- [CHANGELOG](CHANGELOG.md)

## Related Projects

Все проекты используют общий `TN_Doc/Cfg/CfgApp.json`:
- **TN_KMH** — Контроль метрологических характеристик
- **TN_MessagingService** — OPC клиент (DA/UA)
- **TN.ElisConnector** — Интеграция с ELIS/LabHub
