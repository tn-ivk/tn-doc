# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc — ASP.NET Core 8.0 веб-приложение для генерации технических документов ИВК (Измерительно-вычислительный комплекс) нефтегазовой отрасли. Генерирует паспорта качества, протоколы поверки, акты приёма-сдачи через FastReport.

**Framework**: .NET 8.0 | **Runtime**: 8.0.13+ | **Frontend**: Vue 3 + TypeScript | **Version**: 1.5.0

**Ключевые зависимости**: FastReport.Web.Skia 2026.1.2, EF Core 7.0.20 + Pomelo 7.0.0 (намеренно понижена с EF 8 для совместимости с MySQL ИВК), NLog 5.4.0, Newtonsoft.Json 13.0.3.

## Quick Start

```bash
# 1. NuGet sources (обязательно)
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
  --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text

# 2. Build & Run
dotnet restore && dotnet build
cd TN_Doc && dotnet run  # http://localhost:5000 (Kestrel), http://localhost:38509 (IIS Express dev)

# 3. Vue components (Node.js >=18.0.0)
cd TN_Doc/Client && npm install && npm run build:all
```

> **CI NuGet**: в CI/CD используется зеркало FastReport — `https://nuget.xn--90aia9aifhdb2cxbdg.xn--p1ai/api/v3/index.json` (Punycode кириллического домена).

## Common Commands

```bash
# Сборка
dotnet build                              # Вся solution
dotnet build TN_Doc/TN_Doc.csproj         # Только основной проект

# Тестирование (NUnit)
dotnet test                                              # Все тесты
dotnet test --filter "ClassName=AppConfigServiceTests"   # Конкретный класс
dotnet test --filter "FullyQualifiedName~TestName"       # По имени метода

# Vue разработка
cd TN_Doc/Client
npm run dev               # StatusBar с hot reload
npm run dev:configurator  # Configurator с hot reload
npm run build:all         # Production сборка обоих

# E2E тесты (Playwright)
npm run test:e2e          # Все E2E тесты
npm run test:e2e:ui       # Playwright UI mode
```

## Architecture

### Document Generation Flow

```
HTTP Request → HomeController → IDocModuleLoader.LoadDocsModule(options, idDevice, idDoc, baseDir)
    → Document Module (DLL) → GetViewDoc(id) → JSON данные
    → FastReport Template (.frx) + "JsonDoc" parameter → PDF/Excel/ODS
```

Каждый документ — отдельная DLL в `tn.docgeneral/` с конфигурацией в `Cfg/Cfg{Type}.json`.

### DI и Middleware Pipeline (Startup.cs)

**Регистрация сервисов** (ключевые):
```
Singleton: IReportBuffer, IAppConfigService, IDbSchemaCache, IConfigurationCacheService, IDocModuleLoader, IDeviceConnectionDiagnosticService, AppClientTracker
Scoped:    IConfigurationService, IStatusProvider
Transient: AbsPrinter (Windows/Linux), IPrinterService
Hosted:    StatusMonitoringService (BackgroundService)
Other:     AddMemoryCache(), AddSignalR(), AppInfoProvider (через extension method)
HttpClient: "MessagingService" (localhost:5010, timeout 2s), "Elis" (timeout 5s)
```

**Middleware pipeline** (порядок важен):
```
DeveloperExceptionPage / ExceptionHandler → PDF Interceptor (custom) →
StaticFiles → Routing → CORS → AppClientTrackingMiddleware → Endpoints (MVC + SignalR)
```

Custom PDF middleware перехватывает `/PDF/PDF.pdf` и отдаёт PDF из `IReportBuffer` без записи на диск.

При старте регистрируется callback для автоинвалидации кэша: `CfgFileRW.RegisterCacheInvalidator(...)`.

### Key Services

Сервисы разделены между двумя проектами:

**TN_Doc/Services/** — специфичные для веб-приложения:
| Service | Назначение |
|---------|------------|
| `IStatusProvider` | Мониторинг здоровья системы (многоканальный) |
| `IDeviceConnectionDiagnosticService` | Circuit breaker для подключений (защита MySQL от max_connect_errors) |
| `IConfigurationService` | CRUD для CfgApp.json и Cfg*.json документов |
| `StatusMonitoringService` | BackgroundService: периодическая проверка + SignalR push |
| `PrinterService` | Платформо-зависимая печать (Windows/Linux) |
| `ISystemJournalService` | Запись в системный журнал ОС (Event Log / syslog) |
| `IDbSchemaCache` | Кэш схемы БД (проверка наличия колонки DataARM) |
| `DirectoryService` | Работа со справочниками (DirEditor) |
| `AppClientTracker` | Отслеживание подключённых клиентов |
| `Validators/` | `DbConfigValidator`, `OpcConfigValidator` — валидация конфигураций |

**tn.docgeneral/TN.DocGeneral/Services/** — общие для всех приложений:
| Service | Назначение |
|---------|------------|
| `IAppConfigService` | Конфигурация + фабрика документов (partial: Devices, Documents, Dictionaries, Elis, LastUsedTemplate) |
| `IConfigurationCacheService` | LRU-кэш JSON-конфигов (макс. 50), кэширует raw JSON |
| `IReportBuffer` | In-memory PDF хранилище (последний PDF) |
| `IDocModuleLoader` | Динамическая загрузка DLL модулей (LRU метаданных, макс. 5) |
| `LoggingPathService` | Кросс-платформенное определение путей логирования |

### Controllers

| Controller | Route | Назначение |
|------------|-------|------------|
| `HomeController` | `/` | Генерация документов через FastReport |
| `StatusController` | `/api/status` | Мониторинг статуса устройств (кэш 5 сек) |
| `ConfiguratorController` | `/api/configurator` | CRUD конфигурации |
| `ConfigCacheController` | `/api/config-cache` | Управление кэшем |
| `ConfiguratorViewController` | `/configurator` | SPA страница конфигуратора |
| `PrintController` | `/Print` | Печать документов |
| `PdfController` | `/PDF/PDF.pdf` | PDF из IReportBuffer |
| `ExportController` | `/Export` | Экспорт в PDF/Excel/ODS/XML |
| `ElisController` | `/Elis` | Обработка ошибок ELIS |
| `DirEditorController` | `/DirEditor` | Редактирование справочников |
| `ClientLogController` | `/api/ClientLog` | Проксирование логов из Vue в NLog |

### Vue Frontend (TN_Doc/Client/)

npm workspaces монорепозиторий:
- **statusbar/** — Vue 3 + PrimeVue 4 + SignalR, real-time мониторинг статуса
- **configurator/** — управление конфигурацией через `/configurator`
- **shared/** — общие утилиты и API клиенты (зависимость для statusbar/configurator)
- **e2e/** — Playwright тесты

Build output: `wwwroot/statusbar/`, `wwwroot/configurator/`

**Vite config** (`vite.config.base.ts`): alias `@` → `src/`, `@shared` → `../shared/src`. Proxy `/api` и `/statusHub` на `localhost:38509`.

**SignalR Hub**: `StatusHub` (`/statusHub`) — при подключении клиента отправляет текущий статус, `StatusMonitoringService` пушит обновления через `statusUpdated`.

### StatusBar: Диагностика подключений

Диалог диагностики `DeviceDiagnosticsDialog.vue` показывает:
- Статус всех каналов связи (DBConnectionStrings) с latency
- Состояние подключения: `Closed` → `Open` → `HalfOpen`
- Категории ошибок: Authentication, MaxRetry, Blocked

Трёхцветная индикация: зелёный (все каналы), жёлтый (частичное), красный (нет связи).

### Configurator: Настройки диагностики

Параметры диагностики настраиваются в Configurator → вкладка "Общие" → панель "Диагностика связи с ИВК":
- `InitialPollSeconds` (1-3600) — начальный интервал опроса
- `MaxPollSeconds` (60-86400) — максимальный интервал
- `PollMultiplier` (1.1-10) — множитель увеличения интервала
- `NetworkFailureThreshold` (1-100) — порог сетевых ошибок
- `MaxRetryCount` (1-1000) — попытки до перехода в HalfOpen

## API Endpoints

### Status API (`/api/status`)
- `GET /api/status` — статус всех устройств и сервисов (кэш 5 сек)
- `POST /api/status/refresh` — принудительное обновление
- `POST /api/status/device/{deviceId}/retry` — сброс диагностики подключения

### Configurator API (`/api/configurator`)
- `GET/POST /api/configurator/config` — CfgApp.json
- `POST /api/configurator/validate` — валидация конфигурации
- `GET/POST /api/configurator/document-config` — Cfg*.json документов

### Cache API (`/api/config-cache`)
- `GET /api/config-cache/statistics` — статистика кэша (hits, misses, hit rate)
- `POST /api/config-cache/clear` — очистить кэш
- `GET /api/config-cache/health` — проверка работоспособности сервиса

## Configuration Files

| Файл | Назначение |
|------|------------|
| `TN_Doc/Cfg/CfgApp.json` | Главная конфигурация (устройства, ELIS, OPC, DeviceConnectionDiagnostic) |
| `TN_Doc/Cfg/Cfg{Type}.json` | Настройки шаблона документа |
| `TN_Doc/Cfg/CfgEdit{Type}.json` | Конфигурация формы редактирования документа |
| `TN_Doc/appsettings.json` | ASP.NET Core настройки (Kestrel порт: 5000, пути конфигов) |
| `TN_Doc/Cfg/CfgApp.Development.json` | Dev-конфиг: альтернативный адрес БД (копируется только в Debug) |
| `TN_Doc/Cfg/Cfg.Development.json` | Dev-конфиг документа (копируется только в Debug) |
| `TN_Doc/appsettings.Development.json` | Dev-конфиги (Trace logging, `CfgApp.Development.json`) |
| `TN_Doc/nlog.config` | Логирование NLog (File + Console в Debug) |

Dev-файлы (`CfgApp.Development.json`, `Cfg.Development.json`) копируются в output только при `Configuration=Debug` и **не публикуются**.

## Document Module Interface

Базовый класс `DocGeneral` (tn.docgeneral/TN.DocGeneral/General.cs) — наследует `DbContext`:

```csharp
// Обязательные методы для реализации в модуле
GetList() / GetList(UTBegin, UTEnd)  // Список документов
GetViewDoc(id)                       // JSON данные для отчёта
GetViewDoc(id, protocolNumber)       // Для документов с несколькими протоколами
GetEditDoc(id)                       // HTML форма редактирования
SaveDoc(jsonData)                    // Сохранение документа
GetPathTemplateFile()                // Путь к .frx шаблону
```

**Доступные в наследниках**: `_appConfig`, `_configCache`, `_deviceId`, `CfgGeneral`, `CurrentCfgDevice`, `LoadCfg<T>()`.

**Типичная структура модуля** (пример: Passport/):
```
Passport/
├── Passport.csproj          # ProjectReference → TN.DocGeneral
├── DocPassport.cs            # Наследует DocGeneral, реализует все методы
├── Models/                   # DTO модели документа
├── Elis/                     # ELIS интеграция (если нужна)
└── html.html                 # HTML шаблон формы редактирования
```

**Важно**: использовать `Path.Combine()` для кросс-платформенности.

## Git Submodules

```
tn.docgeneral/          → git.tncpa.ru/orpovy/ivk/tn.docgeneral.git
  └── tn.utils/         → (вложенный субмодуль TN.Utils)
tn_toolsfastreport/     → git.tncpa.ru/orpovy/ivk/tn_toolsfastreport.git
winprutil/              → git.tncpa.ru/orpovy/ivk/winprutil.git
```

В CI URL перезаписываются на GitHub-зеркала (`tn-ivk/*`) с `GH_SUBMODULES_TOKEN`.

## CI/CD

**Dual CI**: GitHub Actions + GitLab CI (оба активны).

### GitHub Actions

| Workflow | Триггер | Назначение |
|----------|---------|------------|
| `tests-on-push.yml` | push в `develop*` | build → unit-test + integration-test → test-summary |
| `build-and-package.yml` | push tag | build → test → package (.deb + .msi) → notify-telegram → create-release |

**Retention артефактов**:
- `tests-on-push.yml`: build-output — 1 день, test-results — 7 дней
- `build-and-package.yml`: build/test — 3 дня, packages — 7 дней

**Фильтры тестов в CI**:
```bash
# Unit
--filter "Namespace~Tests.Controllers|Namespace~Tests.Services|Namespace~Tests.Configs"
# Integration
--filter "Namespace~Tests.Libraries.Integration|Namespace~Tests.Libraries.KMH|Namespace~Tests.Libraries.Common|Namespace~Tests.Libraries.Core"
```

**Секреты CI**:
| Секрет | Назначение |
|--------|------------|
| `GH_SUBMODULES_TOKEN` | Доступ к приватным субмодулям |
| `FR_NUGET_USERNAME/PASSWORD` | FastReport NuGet feed |
| `TELEGRAM_BOT_TOKEN/CHAT_ID` | Уведомления в Telegram |
| `PROJECT_API_TOKEN` | GitLab API (build number) |

### Версионирование и packaging

Формат: `{VERSION}-b{BUILD_NUMBER}-{SHORT_SHA}` (пример: `1.5.0-b42-a1b2c3d4`)

**Linux (.deb):** `tn.doc-{FULL_VERSION}_amd64.deb`
- Требует `dotnet-runtime-8.0 >= 8.0.13`
- Устанавливается в `/opt/TN_Doc`, логи в `/var/log/TN_Doc`
- Backup перед обновлением в `/var/backups/TN_Doc/`
- Systemd unit: `TN_Doc.service`, пользователь `alphadaemon`

**Windows (.msi):** `tn.doc-full-{FULL_VERSION}_win-x64.msi` (self-contained), `tn.doc-{FULL_VERSION}_win-x64.msi` (minimal)
- WiX v6, Scope perMachine, установка в `C:\ProjectVU\DotNetComponents\TN_Doc` (настраиваемо)
- Интерфейс на русском языке (Cultures=ru-RU)
- UI: Приветствие → Выбор пути → Имя службы → Подтверждение → Установка → Завершение
- Windows Service с настраиваемым именем (по умолчанию `tn.doc`)
- Очистка директории перед установкой (util:RemoveFolderEx)
- Автоматический бэкап в `C:\ProgramData\TN_Doc\backups\` перед установкой (если директория не пуста, исключая logs/)
- Поддержка тихой установки через `msiexec /quiet`

## External Systems

- **ELIS**: интеграция через `TN.ElisConnector`, конфиг в `CfgApp.json → Elis`
- **OPC DA/UA**: данные через `TN_MessagingService` (localhost:5010), API: `/api/Values/` (чтение/запись тегов), `/api/OPCClientCache/` (кэш)
- **MySQL/MariaDB**: Pomelo.EntityFrameworkCore.MySql 7.0.0 (EF Core 7 — понижена с EF 8 для совместимости с БД ИВК), multi-channel подключения с автоматическим failover

## Platform Notes

| Platform | Logging | Service | Installer |
|----------|---------|---------|-----------|
| Windows | `TN_Doc/logs/` | Windows Service (MSI) | `installer/windows/` (WiX v6) |
| Linux | `/opt/TN_Doc/logs/` | systemd, требует `libgdiplus` | `.deb` пакет (CI inline) |

Определение платформы в `Program.cs`: `UseWindowsService()` / `UseSystemd()`.

### Windows MSI Installer

Проект WiX v6 в `installer/windows/`:
```
installer/windows/
├── TN_Doc.Installer.wixproj   # WiX SDK-style проект (Heat + HarvestDirectory)
├── Package.wxs                 # Пакет, MajorUpgrade, Features, UI (WixUI_InstallDir + ServiceNameDlg), ru-RU
├── Directories.wxs             # Структура директорий (ProgramFiles64Folder)
├── ServiceConfig.wxs           # Windows Service + бэкап + очистка директории
├── ExcludeMainExe.xslt         # XSLT: исключает TN_Doc.exe из harvest (определён в ServiceConfig)
└── Scripts/Backup.ps1          # PowerShell бэкап перед установкой (исключает logs/)
```

> **Важно**: UI-элементы (WixUI, диалоги, Publish) должны быть внутри `<Package>` в Package.wxs, а не в отдельных Fragment-файлах — иначе WiX линкер отбрасывает нелинкованные фрагменты.

**Локальная сборка MSI:**
```bash
# 1. Publish
dotnet publish TN_Doc/TN_Doc.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64-full

# 2. Build MSI (Heat harvesting + WiX compilation integrated via MSBuild)
dotnet build installer/windows/TN_Doc.Installer.wixproj -c Release -p:ProductVersion=1.5.0 -p:HarvestPath=../../publish/win-x64-full
```

**Тихая установка:**
```cmd
msiexec /i TN_Doc.msi /quiet INSTALLFOLDER="C:\ProjectVU\DotNetComponents\TN_Doc" SERVICENAME="tn.doc"
```

## Testing

```
Tests/
├── Tests.Unit/         # Модульные тесты (NUnit 4 + Moq)
├── Tests.Integration/  # Интеграционные тесты (WebApplicationFactory)
└── Tests.Shared/       # Общая тестовая инфраструктура
TN_Doc/Client/e2e/      # E2E тесты (Playwright)
```

```bash
# Все тесты
dotnet test

# Unit-тесты с покрытием
dotnet test Tests/Tests.Unit --collect:"XPlat Code Coverage"

# E2E (требует запущенный сервер на localhost:38509)
cd TN_Doc/Client && npm run test:e2e
```

**Naming**: `MethodName_WhenCondition_ThenExpectedResult`

**Playwright**: baseURL `localhost:38509`, браузеры: Chromium, Firefox, WebKit, Mobile Chrome. Retries: 2 в CI.

**InternalsVisibleTo**: `TN_Doc.csproj` → `Tests.Unit` (доступ к internal классам).

## Git Conventions

- **Commit messages**: русский язык, формат `Область: описание`
- **НИКОГДА** не добавлять AI attribution в коммиты
- FastReport `.frx` — бинарные (в `.gitattributes`), редактировать через FastReport Designer
- `.gitattributes`: LF для кода, CRLF для .bat/.cmd/.ps1

## Changelog Format

**`TN_Doc/changes.md`** — внутренний changelog (отличается от CHANGELOG.md):

```markdown
## Версия X.Y.Z:
    - Обычное изменение
    -⚠️Важное изменение или новая функциональность
    -🐞Исправление бага
```

**`CHANGELOG.md`** — формат [Keep a Changelog](https://keepachangelog.com/) с секциями Added/Changed/Fixed.

## Related Projects

Все проекты разделяют `CfgApp.json`:
- `tn_kmh/` — Контроль метрологических характеристик
- `tn_messagingservice/` — OPC клиент + SignalR
- `tn.elisconnector/` — ELIS интеграция

## Documentation

- `docs/architecture/overview.md` — архитектура
- `docs/architecture/document-modules.md` — модули документов
- `docs/architecture/statusbar.md` — StatusBar архитектура
- `docs/deployment/linux.md` — развёртывание
- `docs/deployment/configuration.md` — конфигурация
- `docs/development/setup.md` — настройка окружения разработчика
- `docs/development/building.md` — сборка проекта
- `docs/development/new-module-tutorial.md` — создание модулей
- `docs/development/fastreport-templates.md` — шаблоны FastReport
- `docs/api/endpoints.md` — API документация
- `docs/integration/elis.md` — интеграция ELIS
- `docs/ui-design.md` — UI Design гайд
- `tech_debt/` — планы по техническому долгу (16 файлов)
- `TN_Doc/changes.md` — changelog
