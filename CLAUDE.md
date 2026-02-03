# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TN_Doc — ASP.NET Core 8.0 веб-приложение для генерации технических документов ИВК (Измерительно-вычислительный комплекс) нефтегазовой отрасли. Генерирует паспорта качества, протоколы поверки, акты приёма-сдачи через FastReport.

**Version**: 1.4.4 | **Framework**: .NET 8.0 | **Runtime**: 8.0.13+ | **Branch**: develop

## Quick Start

```bash
# 1. NuGet sources (обязательно)
dotnet nuget add source "https://nuget.ortpr.ru/v3/index.json" --name ortpr
dotnet nuget add source "https://nuget.fast-report.com/api/v3/index.json" --name fr_nuget \
  --username "<USERNAME>" --password "<PASSWORD>" --store-password-in-clear-text

# 2. Build & Run
dotnet restore && dotnet build
cd TN_Doc && dotnet run  # http://localhost:38509

# 3. Vue components (StatusBar + Configurator)
# Требуется Node.js >=18.0.0, npm >=8.0.0
cd TN_Doc/Client && npm install && npm run build:all
```

## Common Commands

```bash
# Сборка
dotnet build                              # Вся solution
dotnet build TN_Doc/TN_Doc.csproj        # Только основной проект
dotnet build -c Release                   # Release сборка

# Тестирование (NUnit)
dotnet test                                              # Все тесты
dotnet test --filter "ClassName=AppConfigServiceTests"  # Конкретный класс
dotnet test --filter "FullyQualifiedName~TestName"      # По имени метода

# Vue разработка
cd TN_Doc/Client
npm run dev               # StatusBar с hot reload
npm run dev:configurator  # Configurator с hot reload
npm run build:all         # Production сборка обоих

# Форматирование
dotnet format
dotnet format --verify-no-changes
```

## Architecture

### Solution Structure

```
TN_Doc.sln
├── TN_Doc/                    # Основное веб-приложение
│   ├── Controllers/           # API и MVC контроллеры
│   ├── Models/                # Сервисы и бизнес-логика
│   ├── Client/                # Vue 3 monorepo
│   │   ├── statusbar/         # StatusBar компонент
│   │   ├── configurator/      # Configurator компонент
│   │   ├── shared/            # Общие утилиты
│   │   └── e2e/               # E2E тесты (Playwright)
│   ├── Cfg/                   # JSON конфигурации документов
│   └── Doc/                   # FastReport шаблоны (.frx)
├── tn.docgeneral/             # Модули документов (~42 DLL)
│   ├── TN.DocGeneral/         # Общая бизнес-логика
│   ├── tn.utils/              # Общие утилиты
├── Tests/                     # Тестовые проекты
│   ├── Tests.Shared/          # Общая инфраструктура тестов
│   ├── Tests.Unit/            # Модульные тесты (NUnit)
│   └── Tests.Integration/     # Интеграционные тесты (WebApplicationFactory)
└── tn_toolsfastreport/        # FastReport утилиты
```

### Document Generation Flow

```
HTTP Request → HomeController → IDocModuleLoader.LoadDocsModule(options, idDevice, idDoc, baseDir)
    → Document Module (DLL, DocGeneral) → GetViewDoc(id) → JSON данные
    → FastReport Template (.frx) + "JsonDoc" parameter → PDF/Excel/ODS
```

Каждый документ — отдельная DLL с конфигурацией в `Cfg/Cfg{Type}.json`.

### Key Services (DI)

| Service | Scope | Назначение |
|---------|-------|------------|
| `IAppConfigService` | Singleton | Конфигурация + фабрика документов |
| `IConfigurationCacheService` | Singleton | Кэш JSON-конфигов (LRU, макс. 50 записей) |
| `IReportBuffer` | Singleton | In-memory PDF хранилище |
| `IDocModuleLoader` | Singleton | Динамическая загрузка DLL модулей (LRU, макс. 5) |
| `IStatusProvider` | Scoped | Мониторинг здоровья системы (многоканальный) |
| `ICircuitBreakerService` | Singleton | Circuit Breaker для устройств (backoff, retry) |
| `IConfigurationService` | Scoped | Управление конфигурацией приложения и документов |
| `PrinterService` | Singleton | Платформо-зависимая печать |

### Vue Frontend

- **StatusBar** (`/TN_Doc/Client/statusbar/`): Vue 3 + PrimeVue + SignalR, real-time мониторинг статуса устройств/OPC/ELIS
- **Configurator** (`/TN_Doc/Client/configurator/`): Управление конфигурацией через `/configurator` endpoint
  - **Вкладка "Общие"**: настройки экспорта, безопасности, локального OPC-клиента (ARM), настройки ELIS
  - **Вкладка "Устройства"**: список устройств с поиском, множественный выбор для пакетного редактирования
  - **Вкладка "Документы"**: редактирование JSON-конфигураций документов (Cfg*.json) с Monaco Editor
  - **Редактор устройства**: включение/выключение, шаблоны документов, подключение к БД, настройки OPC, используемые СИ
- Build output: `wwwroot/statusbar/`, `wwwroot/configurator/`

### StatusBar: Многоканальная проверка статуса (v1.4.4)

Поддержка нескольких строк подключения (DBConnectionStrings) для устройств:

| Поле | Описание |
|------|----------|
| `isConnected` | Хотя бы один канал связи работает |
| `isFullyConnected` | Все каналы связи работают |
| `channels` | Список каналов с детальной информацией (ConnectionChannel) |

Трёхцветная индикация: зелёный (все каналы), жёлтый (частичное подключение), красный (нет связи).

### StatusBar: DeviceDiagnosticsDialog + Circuit Breaker

Диалог диагностики подключения устройств (`DeviceDiagnosticsDialog.vue`):
- Детальный статус всех каналов связи с latency
- Визуализация состояния Circuit Breaker (Closed/Open/HalfOpen)
- Отображение категорий ошибок (Authentication, MaxRetry, Blocked)
- Кнопка ручного retry для проверки подключения

Circuit Breaker состояния:
| Состояние | Описание |
|-----------|----------|
| `Closed` | Нормальная работа |
| `Open` | Подключение заблокировано после N неудач |
| `HalfOpen` | Тестовая попытка подключения |
| `AUTH` | Ошибка аутентификации (требует проверки учётных данных) |
| `MAX RETRY` | Превышено максимальное количество попыток |

### Configurator: Используемые СИ (v1.4.3)

Настройка средств измерения для каждого устройства:

| Параметр | Описание |
|----------|----------|
| `UsedPR` | Задействовать ПР (преобразователь расхода) |
| `UsedPP` | Задействовать ПП (преобразователь плотности) |
| `UsedPVL` | Задействовать ПВл (преобразователь влагосодержания) |
| `UsedPVS` | Задействовать ПВз (преобразователь вязкости) |
| `UsedSecondSI_*` | Задействовать второй экземпляр СИ (PP, PVL, PVS) |

Логика зависимостей: вторичное СИ доступно только при включённом основном.

## API Endpoints

### Status API (`/api/status`)
| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/status` | Статус всех устройств и сервисов (кэш 5 сек) |
| POST | `/api/status/refresh` | Принудительное обновление статусов |
| POST | `/api/status/device/{deviceId}/retry` | Retry устройства (сброс Circuit Breaker) |

### Configurator API (`/api/configurator`)
| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/configurator/config` | Получить CfgApp.json |
| POST | `/api/configurator/config` | Сохранить CfgApp.json |
| POST | `/api/configurator/validate` | Валидация конфигурации |
| GET | `/api/configurator/document-config?configPath=` | Загрузить Cfg*.json документа |
| POST | `/api/configurator/document-config` | Сохранить Cfg*.json документа |

### Cache API (`/api/config-cache`)
| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/config-cache` | Статистика кэша конфигураций |
| DELETE | `/api/config-cache` | Очистить кэш |

## Configuration

| Файл | Назначение |
|------|------------|
| `TN_Doc/Cfg/CfgApp.json` | Главная конфигурация (устройства, ELIS, OPC) |
| `TN_Doc/Cfg/Cfg{DocumentType}.json` | Настройки шаблона документа |
| `TN_Doc/Cfg/CfgEdit{DocumentType}.json` | Конфигурация формы редактирования |
| `TN_Doc/appsettings.json` | ASP.NET Core настройки |
| `TN_Doc/nlog.config` | Логирование NLog |
| `TN_Doc/opc.da.tags.json` | OPC DA теги (должны быть предрегистрированы) |

## Document Module Interface

Базовый класс `DocGeneral` и переопределяемые методы:

```csharp
GetViewDoc(id)                       // JSON данные для отчёта
GetViewDoc(id, protocolNumber)       // Для документов с несколькими протоколами
GetPathTemplateFile()                // Путь к .frx шаблону
GetEditDoc(id)                       // HTML форма редактирования (если используется)
```

**Важно** в `GetEditDoc`: использовать `Path.Combine()` для кросс-платформенности.

## External Systems

### ELIS
- Интеграция через `TN.ElisConnector` (отдельный репозиторий)
- Конфигурация: `CfgApp.json → Elis`
- SSL сертификаты в `Cert/`

### OPC
- **OPC DA**: теги в `opc.da.tags.json`, требуют предрегистрации
- **OPC UA**: теги в `opc.ua.tags.json`
- Данные через `TN_MessagingService` (отдельный репозиторий)

### Database
- MySQL/MariaDB через Pomelo.EntityFrameworkCore.MySql
- Connection strings в `CfgApp.json` per device
- Шифрование паролей через `UseSecurityFeatures` flag

## Platform Notes

| Platform | Logging | Printing | Service |
|----------|---------|----------|---------|
| Windows | `TN_Doc/logs/` | `prutils/winprutil.exe` | `sc create TN_Doc` |
| Linux | `/opt/TN_Doc/logs/` | CUPS | systemd `tn-doc.service` |

Linux требует `libgdiplus`.

## Testing

### Структура тестовых проектов

```
Tests/
├── Tests.Shared/              # Общая инфраструктура для тестов
│   ├── BaseTestClass.cs       # Базовые классы для всех тестов
│   ├── TestHelpers.cs         # Статические helper методы
│   └── Fixtures/              # Генераторы тестовых данных
│       ├── MockConfigHelper.cs
│       └── TestDataFixture.cs
│
├── Tests.Unit/                # Модульные тесты
│   ├── Controllers/           # Тесты контроллеров
│   ├── Services/              # Тесты сервисов
│   └── UsersTests.cs
│
├── Tests.Integration/         # Интеграционные тесты (WebApplicationFactory)
│   ├── Controllers/           # Интеграционные тесты API
│   ├── Services/              # Тесты взаимодействия сервисов
│   └── Data/                  # Тесты работы с БД (InMemory)
│
TN_Doc/Client/e2e/             # E2E тесты (Playwright + TypeScript)
├── pages/                     # Page Object Model
├── tests/                     # Тестовые сценарии
└── playwright.config.ts
```

### Команды тестирования

```bash
# Модульные тесты
dotnet test Tests/Tests.Unit/Tests.Unit.csproj
dotnet test Tests/Tests.Unit/Tests.Unit.csproj --filter "ClassName=AppConfigServiceTests"

# Интеграционные тесты
dotnet test Tests/Tests.Integration/Tests.Integration.csproj

# E2E тесты (Playwright)
cd TN_Doc/Client
npm run test:e2e           # Запуск всех E2E тестов
npm run test:e2e:headed    # С видимым браузером
npm run test:e2e:ui        # Playwright UI mode
```

### Naming convention
- Методы: `MethodName_WhenCondition_ThenExpectedResult`
- Framework: NUnit + Moq (backend), Playwright (frontend)

## Git Conventions

- **Commit messages**: русский язык, формат `Область: описание`
- **НИКОГДА** не добавлять AI attribution в коммиты
- **Branches**: `develop` → feature branches → merge back
- FastReport `.frx` файлы — бинарные, редактировать через FastReport Designer

## Related Projects

Все проекты должны быть на одном уровне и разделяют `CfgApp.json`:
- `tn_kmh/` — Контроль метрологических характеристик
- `tn_messagingservice/` — OPC клиент + SignalR
- `tn.elisconnector/` — ELIS интеграция

## Detailed Documentation

- Architecture: `docs/architecture/overview.md`
- API: `docs/api/endpoints.md`
- Deployment: `docs/deployment/linux.md`
- UI Design: `docs/ui-design.md`
- Changelog: `CHANGELOG.md`
