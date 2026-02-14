# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc — ASP.NET Core 8.0 веб-приложение для генерации технических документов ИВК (Измерительно-вычислительный комплекс) в нефтегазовой отрасли: паспорта качества нефти, протоколы поверки, акты приёма-сдачи, отчёты. Генерация PDF через FastReport (шаблоны `.frx`), данные из MySQL.

**Version**: 1.4.3 (.NET 8.0)
**Runtime**: .NET 8.0.13+, Node.js 18+
**URL**: http://localhost:38509

**Критические правила:**
- **НИКОГДА** не упоминать AI/Claude в коммитах, не добавлять "Co-Authored-By"
- **ВСЕГДА** русский язык для коммит-сообщений
- **Формат коммитов**: `Область: описание` (например: `Passport: исправлена валидация`)

## Essential Commands

```bash
# Сборка и запуск
dotnet build TN_Doc.sln
dotnet run --project TN_Doc                     # http://localhost:38509

# Git submodules (обязательно после clone)
git submodule update --init --recursive

# Vue приложения (из TN_Doc/Client/)
npm install && npm run build:all                # Сборка всех Vue workspaces
npm run dev:editor                              # Document Editor (port 5175)
npm run dev:configurator                        # Configurator (port 5174)

# Тестирование (NUnit)
dotnet test TN_Doc.sln
dotnet test --filter "ClassName=AppConfigServiceTests"       # Один класс
dotnet test --filter "FullyQualifiedName~KMH"                # По namespace
dotnet test --filter "MethodName_WhenCondition"              # Один метод
```

## Architecture

```
tn_doc/
├── TN_Doc/                        # ASP.NET Core web app
│   ├── Controllers/               # 12 контроллеров API
│   ├── Services/                  # Бизнес-логика + Validators/
│   ├── Models/                    # DTOs, Status, Printer, Configuration
│   ├── Hubs/StatusHub.cs          # SignalR (real-time статусы)
│   ├── Middleware/                # AppClientTrackingMiddleware
│   ├── Extensions/                # IServiceCollectionExtensions (регистрация DI)
│   ├── Views/                     # Razor views (Home, Shared, ConfiguratorView)
│   ├── Cfg/                       # Конфигурации документов (JSON)
│   │   ├── CfgApp.json            # Главная конфигурация приложения
│   │   ├── Passport/              # Конфиги паспортов (CfgEditPassport*.json)
│   │   └── Act/, Poverka1974/     # Конфиги по типам документов
│   ├── Doc/                       # FastReport шаблоны (*.frx)
│   └── Client/                    # Vue 3 (npm workspaces)
│       ├── statusbar/             # Мониторинг устройств (SignalR, port 5173)
│       ├── configurator/          # Визуальный редактор CfgApp.json (port 5174)
│       ├── document-editor/       # Редактор документов (port 5175)
│       └── shared/                # Общие утилиты/типы
├── tn.docgeneral/                 # Git submodule: ~48 библиотек документов + TN.DocGeneral
├── tn_toolsfastreport/            # Git submodule: утилиты FastReport
├── winprutil/                     # Git submodule: утилиты Windows-печати
├── Tests/                         # NUnit тесты
├── tech_debt/                     # Планы техдолга и архитектурных улучшений
└── docs/                          # Документация проекта
```

### Потоки данных и ключевые связи

```
Browser (Vue 3) ←→ Controllers ←→ Services ←→ MySQL (EF Core: DocGeneral DbContext)
                                      ↓
                              tn.docgeneral/ (48 DLL библиотек документов)
                                      ↓
                              FastReport (.frx шаблоны) → PDF (in-memory ReportBuffer)

StatusBar (Vue) ←SignalR→ StatusHub ← StatusMonitoringService (фоновый опрос)
                                        ├── MessagingService (http://localhost:5010)
                                        └── ELIS connector
```

### DI-регистрация (Startup.cs + Extensions/)

| Lifetime | Сервис | Назначение |
|----------|--------|------------|
| Singleton | `IAppConfigService` | Доступ к CfgApp.json (**статический** через `AppConfigService.GetInstance()`) |
| Singleton | `IReportBuffer` | Буфер сгенерированного PDF в памяти |
| Singleton | `IConfigurationCacheService` | Кэш конфигурационных файлов документов |
| Singleton | `IDbSchemaCache` | Кэш схемы MySQL |
| Singleton | `IDocModuleLoader` | Загрузка DLL библиотек документов (`CachedDocModuleLoader`) |
| Singleton | `AppClientTracker` | Отслеживание подключённых клиентов |
| Scoped | `IConfigurationService` | Работа с конфигурациями документов |
| Scoped | `IStatusProvider` | Проверка статусов устройств/сервисов |
| Scoped | `DocGeneral` | EF Core DbContext для MySQL |
| Hosted | `StatusMonitoringService` | Фоновый опрос статусов, пуш через SignalR |

**Middleware pipeline**: ExceptionHandler → PDF interceptor (`/PDF/PDF.pdf` → in-memory) → StaticFiles → Routing → CORS → AppClientTracking → Endpoints (MVC + SignalR `/statusHub` + SPA fallback `/document-editor/*`)

### Контроллеры

| Контроллер | Назначение |
|------------|------------|
| `HomeController` | Главная, просмотр/печать документов, проверка ELIS |
| `DocumentEditController` | API для Vue Document Editor (GetEditConfig/SaveDocument) |
| `ConfiguratorController` | API конфигуратора (GetConfig/SaveConfig/ValidateConfig) |
| `DirEditorController` | Справочники (методы испытаний, пользователи, QP) |
| `ElisController` | Интеграция с ELIS (лабораторная система) |
| `StatusController` | Статусы устройств/сервисов |
| `ExportController` | Экспорт документов в PDF/HTML |
| `PrintController` | Печать документов (GetListPrinters/PrintDoc) |
| `PdfController` | Отдача PDF из in-memory буфера |
| `ConfigCacheController` | Статистика/очистка кэша конфигураций |
| `ClientLogController` | Логирование ошибок с клиента |
| `ConfiguratorViewController` | Рендеринг Configurator Razor view |

### Система документов (IDocumentEditor)

Каждая из ~48 библиотек документов в `tn.docgeneral/` наследуется от `General` и реализует `IDocumentEditor` (`TN_DocGeneral.Interfaces`):

```csharp
public interface IDocumentEditor
{
    DocumentEditConfig GetEditConfig(int id);                          // Конфиг формы для Vue
    bool SaveDocument(int id, Dictionary<string, object> values);     // Сохранение из Vue
}
```

**DocumentEditConfig** содержит: `DocId`, `DocType` (enum `IdDoc`), `DeviceId`, `Fields` (список `FormField`), `InitialValues`, `Dictionaries`.

**FormField** — описание поля: `Key`, `Label`, `Type` (select/text/number/date/datetime-local), `Required`, `Editable`, `RoundValue`, `Options`, `ElisAlias` (fallback-массив алиасов ELIS), `AllowManualInput`, `Tag`.

**Файлы конфигурации документа:**
- `TN_Doc/Cfg/Cfg{Type}.json` — настройки шаблона
- `TN_Doc/Cfg/CfgEdit{Type}.json` — конфигурация полей формы редактирования
- `TN_Doc/Doc/{Number}_{Type}.frx` — FastReport шаблон

Библиотеки загружаются динамически через `CachedDocModuleLoader`. При сохранении конфига `CfgFileRW` автоматически инвалидирует кэш через callback в `IConfigurationCacheService`.

### Vue Frontend

**Стек**: Vue 3.4 + TypeScript 5.4 + PrimeVue 4.2 + Pinia 2.1 + Vite 5.2

**NPM Workspaces** (`TN_Doc/Client/`):

| Workspace | Назначение | Порт |
|-----------|------------|------|
| `statusbar` | Мониторинг устройств через SignalR | 5173 |
| `configurator` | Визуальный редактор CfgApp.json и конфигов документов | 5174 |
| `document-editor` | Редактор документов (паспорта, акты) — Vue Router + Axios | 5175 |
| `shared` | Общие утилиты и TypeScript типы | — |

Собранные Vue-приложения деплоятся в `TN_Doc/wwwroot/{app-name}/`. Vite-манифесты обрабатываются `ViteManifestService`.

## Passport Configuration

Паспорта качества — основной тип документа. Конфиги в `TN_Doc/Cfg/Passport/CfgEditPassport*.json`.

**Ключевые поля конфигурации:**

| Поле | Назначение |
|------|------------|
| `SlaveKey` | Master-slave связь параметров (slave скрыт, Legal=0) |
| `LinkedParameter` | Общий метод испытаний для пары параметров |
| `IsBallast` | Result = Measurement автоматически |
| `Edit` | Разрешить редактирование поля |
| `RequiredFill` | Обязательное поле |

**Field History System**: требует `IsUsedElis = true` в конфигурации устройства (`CfgApp.json`). Показывает историю изменений: ELIS/ручное/округление ИВК.

## Development Patterns

### C#
- `PascalCase` для типов/методов, `_camelCase` для private readonly
- Early returns, избегать глубокой вложенности
- Бизнес-логика в `Services/`, не в контроллерах
- Логи через NLog (`ILogger<T>`), не подавлять исключения
- DI — регистрация в `Startup.cs` и `Extensions/IServiceCollectionExtensions.cs`
- Платформо-зависимый код: `AbsPrinter` → `WindowsPrinter`/`LinuxPrinter`, `ISystemJournalService` → `Windows`/`Linux`

### Vue
- **UI**: PrimeVue (Material Design 3 стили в `wwwroot/css/`)
- **Overlays**: `appendTo="body"` для dropdown/dialog (иначе clip)
- **DateTime**: локальное время, **не** UTC
- **State**: Pinia stores, composables для логики
- **Маршрутизация**: Vue Router (document-editor), SPA fallback настроен в Startup.cs

### Тесты (NUnit)
- **Naming**: `MethodName_WhenCondition_ThenExpectedResult`
- **Pattern**: AAA (Arrange-Act-Assert)
- **Mocking**: Moq для внешних сервисов
- **In-memory DB**: `Microsoft.EntityFrameworkCore.InMemory` для тестов EF Core
- **Project Aliases**: Tests.csproj использует ProjectReference Aliases (CommonPoverka1974Lib, ActLib) для избежания конфликтов имён между библиотеками

## Git Workflow

**Submodule changes — двойной коммит:**
```bash
cd tn.docgeneral
git add . && git commit -m "Область: описание"
git push
cd ..
git add tn.docgeneral
git commit -m "Обновлён субмодуль tn.docgeneral"
```

3 submodule: `tn.docgeneral` (48 библиотек + TN.DocGeneral + TN.Utils), `tn_toolsfastreport`, `winprutil`.

## NuGet Sources

```bash
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
  --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text
```

## Common Issues

| Проблема | Решение |
|----------|---------|
| Ошибки сборки | Проверить NuGet sources (ortpr, FastReport) |
| Пустые submodules | `git submodule update --init --recursive` |
| Vue build fails | `npm install` в `TN_Doc/Client/` |
| Изменения конфигов не видны | Перезапуск приложения (кэш конфигураций) |
| PrimeVue overlay обрезается | `appendTo="body"` + global styles |
| Сдвиг datetime | Использовать локальное время, не UTC |
| Field history не отображается | `IsUsedElis = true` в CfgApp.json |

## Related Projects

Все используют общий `CfgApp.json` из `TN_Doc/Cfg/`:
- **TN_KMH** (http://localhost:5002) — контроль метрологических характеристик
- **TN_MessagingService** (http://localhost:5010) — OPC DA/UA клиент + SignalR
- **TN.ElisConnector** (http://127.0.0.1:5050) — прокси ТСПД/ELIS

## Resources

- [CHANGELOG.md](CHANGELOG.md) — история версий
- [docs/](docs/) — документация (configs, features, API)
- [tech_debt/](tech_debt/) — планы архитектурных улучшений
