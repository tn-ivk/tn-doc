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

- `Devices[]` — список устройств.
- `DBConnectionStrings[]` — подключения к БД (Server, Userid, Password, Database, ConnectionTimeout).
- `OpcConnectionSettings` — настройки OPC:
  - `Type`: `0` — OPC DA, `1` — OPC UA
  - `DaSettings`, `UaSettings` — параметры подключения
- `Elis` — параметры интеграции ELIS (Use, OstKey, SiknKey, ClientName, ClientToken).

## Редактирование

Рекомендуемый способ — **Configurator** (`/configurator`), т.к. он валидирует данные и сохраняет `CfgApp.json` безопасно. Ручное редактирование возможно, но делайте резервную копию.

## Проверка после изменений

- Перезапустите приложение/службу.
- Проверьте `/api/status` и логи NLog.

## См. также

- [Развертывание на Linux](linux.md)
- [API Reference](../api/endpoints.md)
