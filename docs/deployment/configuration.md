# Конфигурация

## Где лежат конфиги

- **Разработка**: `TN_Doc/Cfg/`
- **Linux-пакет**: `/opt/TN_Doc/Cfg/`

Основные пути задаются относительно корня приложения.

## Основные файлы

- `CfgApp.json` — основная конфигурация (устройства, документы, БД, OPC, ELIS).
- `CfgApp.Development.json` — локальные/дев-изменения (опционально).
- `Cfg*.json` — конфигурации конкретных документов.
- `CfgEdit*.json` — конфигурации форм редактирования (если используются).
- `appsettings.json` — настройки ASP.NET Core.
- `nlog.config` — логирование.

## Ключевые разделы `CfgApp.json`

- `Devices[]` — список устройств ИВК.
  - `DBConnectionStrings[]` — подключения к БД (Server, Userid, Password, Database, ConnectionTimeout).
  - `OpcConnectionSettings` — OPC настройки per-device.
  - `UsedSI` — используемые средства измерения (UsedPR, UsedPP, UsedPVL, UsedPVS, SecondSI).
  - `Docs[]` — документы устройства с шаблонами.
- `ArmOpcConnectionSettings` — глобальные настройки OPC для ARM:
  - `Type`: `DA` или `UA`
  - `DaSettings` — параметры OPC DA (Host, ProgId, StartPrefix, UpdateRate)
  - `UaSettings` — параметры OPC UA (ConfigFilename, UpdateRate)
- `Elis` — параметры интеграции ELIS (Use, OstKey, SiknKey, ClientName, ClientToken).
- `ExportDoc` — настройки экспорта документов (Path).
- `UseSecurityFeatures` — флаг включения функций безопасности (шифрование паролей).
- `DeviceConnectionDiagnostic` — настройки диагностики подключений:
  - `InitialPollSeconds` (1–3600) — начальный интервал опроса
  - `MaxPollSeconds` (60–86400) — максимальный интервал
  - `PollMultiplier` (1.1–10) — множитель увеличения интервала
  - `NetworkFailureThreshold` (1–100) — порог сетевых ошибок
  - `MaxRetryCount` (1–1000) — попытки до перехода в HalfOpen

## Редактирование

Рекомендуемый способ — **Configurator** (`/configurator`), т.к. он валидирует данные и сохраняет `CfgApp.json` безопасно. Ручное редактирование возможно, но делайте резервную копию.

## Проверка после изменений

- Перезапустите приложение/службу.
- Проверьте `/api/status` и логи NLog.

## См. также

- [Развертывание на Linux](linux.md)
- [API Reference](../api/endpoints.md)
