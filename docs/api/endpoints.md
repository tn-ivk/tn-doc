# API Endpoints

## Обзор

TN_Doc предоставляет HTTP API для редактирования документов через Vue SPA, управления конфигурацией и мониторинга состояния системы.

- **Текущая версия приложения**: 1.4.3
- **Аутентификация**: отсутствует (в текущей версии)

## Base URL

```
Development: http://localhost:38509
Production:  http://<server-address>:38509
```

## Document Editor API

Базовый путь: `/api/documents`

### Health check

```http
GET /api/documents/health
```

Возвращает статус доступности Document Editor API.

### Получить конфигурацию формы редактирования

```http
GET /api/documents/{deviceId}/{docType}/edit/{id}
```

- `deviceId` — ID устройства (int)
- `docType` — тип документа (Passport, Act, Report и т.д.), парсится в `IdDoc`
- `id` — ID записи документа

**Ответ:** объект `DocumentEditConfig` (см. `tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs`).

**История изменений:**
- Возвращается только если ELIS включён (`Elis.Use = true` глобально или для устройства).
- Передаётся в `initialValues` с суффиксом `__history`, например `value.Density__history`.
- Также передаются флаги `__elisFilled` для восстановления источника.

### Сохранить документ

```http
POST /api/documents/{deviceId}/{docType}/save/{id}
```

Тело запроса — плоский JSON с полями документа. Для истории используется специальный блок `__history`:

```json
{
  "ExportPermit": "АБВ123",
  "value.Density": "850.567",
  "__history": {
    "value.Density": [
      {
        "source": "Manual",
        "modifiedAt": "2025-01-14T10:32:00Z",
        "modifiedBy": "Пользователь",
        "value": "850.567",
        "previousValue": "850.5",
        "comment": "Отредактировано вручную"
      }
    ]
  }
}
```

**Примечания:**
- `__history` обрабатывается только при включенном ELIS.
- Все документы должны реализовать `IDocumentEditor.SaveDocument`.

### Обновить документ после подтверждения от ИВК

```http
POST /api/documents/{deviceId}/{docType}/update/{id}
```

- Поддерживается только для `Passport`.
- Использует `IDocUpdater.DocUpdate()` внутри документной библиотеки.

## Configurator API

Базовый путь: `/api/Configurator`

- `GET /api/Configurator/config` — получить текущую конфигурацию (`CfgApp`).
- `POST /api/Configurator/config` — сохранить конфигурацию.
- `POST /api/Configurator/validate` — валидировать конфигурацию.
- `GET /api/Configurator/document-config?path=Cfg/CfgPassport.json` — загрузить конфиг документа.
- `POST /api/Configurator/document-config` — сохранить конфиг документа.

## Status API

Базовый путь: `/api/Status`

- `GET /api/Status` — получить статус устройств и сервисов.
- `POST /api/Status/refresh` — принудительно обновить кэш статуса.

## Config Cache API

Базовый путь: `/api/config-cache`

- `GET /api/config-cache/statistics` — статистика кэша конфигураций.
- `POST /api/config-cache/clear` — очистка кэша.
- `GET /api/config-cache/health` — проверка работоспособности.

## Client Log API

Базовый путь: `/api/ClientLog`

```http
POST /api/ClientLog/logging
```

```json
{
  "level": "info",
  "message": "Документ сохранён"
}
```

## Справочники (DirEditor)

Базовый путь: `/DirEditor`

- `GET /DirEditor/GetDir` — получить все справочники.
- `POST /DirEditor/SetDir` — заменить справочники (JSON).
- `GET /DirEditor/GetQpConfigs` — получить конфигурацию паспортов качества.
- `POST /DirEditor/SetQpConfigs` — сохранить конфигурацию паспортов качества.
- `POST /DirEditor/AddMethod` — добавить метод испытаний в конфиг.

## Печать и PDF

- `GET /Print/GetListPrinters` — список принтеров.
- `GET /Print/PrintDoc?printerName=...` — печать последнего документа.
- `GET /PDF/PDF.pdf` — получить последний сгенерированный PDF из памяти.

## Legacy MVC Endpoints

В контроллере `HomeController` есть устаревшие действия:

- `GetDoc` — генерация PDF (устаревший путь, всё ещё используется для просмотра).
- `GetDocEdit` — возвращает URL Vue Document Editor (помечен как obsolete).

Используйте REST API `/api/documents/...` для редактирования через SPA.

## Ошибки

Типовые ответы:

```json
{ "error": "Unknown document type: InvalidType" }
```

```json
{ "error": "Internal server error", "message": "..." }
```

## См. также

- [Architecture Overview](../architecture/overview.md)
- [Document Editor Architecture](../architecture/document-editor.md)
- [Field History](../features/field-history.md)
- [Configurator Architecture](../architecture/configurator.md)
