# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc — ASP.NET Core 8.0 веб-приложение для генерации технических документов ИВК (Измерительно-вычислительный комплекс) нефтегазовой отрасли. Генерирует паспорта качества, протоколы поверки, акты приёма-сдачи через FastReport.

**Framework**: .NET 8.0 | **Runtime**: 8.0.13+ | **Frontend**: Vue 3 + TypeScript

## Quick Start

```bash
# 1. NuGet sources (обязательно)
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
  --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text

# 2. Build & Run
dotnet restore && dotnet build
cd TN_Doc && dotnet run  # http://localhost:38509

# 3. Vue components (Node.js >=18.0.0)
cd TN_Doc/Client && npm install && npm run build:all
```

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

### Key Services

Сервисы разделены между двумя проектами:

**TN_Doc/Services/** — специфичные для веб-приложения:
| Service | Назначение |
|---------|------------|
| `IStatusProvider` | Мониторинг здоровья системы (многоканальный) |
| `ICircuitBreakerService` | Circuit Breaker для устройств |
| `IConfigurationService` | Управление конфигурацией и документами |
| `PrinterService` | Платформо-зависимая печать |

**tn.docgeneral/TN.DocGeneral/Services/** — общие для всех приложений:
| Service | Назначение |
|---------|------------|
| `IAppConfigService` | Конфигурация + фабрика документов |
| `IConfigurationCacheService` | Кэш JSON-конфигов (LRU, макс. 50) |
| `IReportBuffer` | In-memory PDF хранилище |
| `IDocModuleLoader` | Динамическая загрузка DLL модулей (LRU, макс. 5) |

### Vue Frontend (TN_Doc/Client/)

- **statusbar/** — Vue 3 + PrimeVue + SignalR, real-time мониторинг статуса
- **configurator/** — управление конфигурацией через `/configurator`
- **shared/** — общие утилиты и API клиенты
- **e2e/** — Playwright тесты
- Build output: `wwwroot/statusbar/`, `wwwroot/configurator/`

### StatusBar: Circuit Breaker + Многоканальная диагностика

Диалог диагностики `DeviceDiagnosticsDialog.vue` показывает:
- Статус всех каналов связи (DBConnectionStrings) с latency
- Состояние Circuit Breaker: `Closed` → `Open` → `HalfOpen`
- Категории ошибок: Authentication, MaxRetry, Blocked

Трёхцветная индикация: зелёный (все каналы), жёлтый (частичное), красный (нет связи).

## API Endpoints

### Status API (`/api/status`)
- `GET /api/status` — статус всех устройств и сервисов (кэш 5 сек)
- `POST /api/status/refresh` — принудительное обновление
- `POST /api/status/device/{deviceId}/retry` — сброс Circuit Breaker

### Configurator API (`/api/configurator`)
- `GET/POST /api/configurator/config` — CfgApp.json
- `POST /api/configurator/validate` — валидация конфигурации
- `GET/POST /api/configurator/document-config` — Cfg*.json документов

### Cache API (`/api/config-cache`)
- `GET` — статистика кэша
- `DELETE` — очистить кэш

## Configuration Files

| Файл | Назначение |
|------|------------|
| `TN_Doc/Cfg/CfgApp.json` | Главная конфигурация (устройства, ELIS, OPC) |
| `TN_Doc/Cfg/Cfg{Type}.json` | Настройки шаблона документа |
| `TN_Doc/appsettings.json` | ASP.NET Core настройки |
| `TN_Doc/nlog.config` | Логирование NLog |

## Document Module Interface

Базовый класс `DocGeneral` (tn.docgeneral/TN.DocGeneral/):

```csharp
GetViewDoc(id)                       // JSON данные для отчёта
GetViewDoc(id, protocolNumber)       // Для документов с несколькими протоколами
GetPathTemplateFile()                // Путь к .frx шаблону
```

**Важно**: использовать `Path.Combine()` для кросс-платформенности.

## External Systems

- **ELIS**: интеграция через `TN.ElisConnector`, конфиг в `CfgApp.json → Elis`
- **OPC DA/UA**: теги в `opc.da.tags.json`, данные через `TN_MessagingService`
- **MySQL/MariaDB**: Pomelo.EntityFrameworkCore.MySql, шифрование через `UseSecurityFeatures`

## Platform Notes

| Platform | Logging | Service |
|----------|---------|---------|
| Windows | `TN_Doc/logs/` | `sc create TN_Doc` |
| Linux | `/opt/TN_Doc/logs/` | systemd, требует `libgdiplus` |

## Testing

- **Tests.Unit/** — модульные тесты (NUnit + Moq)
- **Tests.Integration/** — интеграционные тесты (WebApplicationFactory)
- **TN_Doc/Client/e2e/** — E2E тесты (Playwright)
- Naming: `MethodName_WhenCondition_ThenExpectedResult`

## Git Conventions

- **Commit messages**: русский язык, формат `Область: описание`
- **НИКОГДА** не добавлять AI attribution в коммиты
- FastReport `.frx` — бинарные, редактировать через FastReport Designer

## Related Projects

Все проекты разделяют `CfgApp.json`:
- `tn_kmh/` — Контроль метрологических характеристик
- `tn_messagingservice/` — OPC клиент + SignalR
- `tn.elisconnector/` — ELIS интеграция

## Documentation

- `docs/architecture/overview.md` — архитектура
- `docs/deployment/linux.md` — развёртывание
- `docs/development/new-module-tutorial.md` — создание модулей
- `TN_Doc/changes.md` — changelog
