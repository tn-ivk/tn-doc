# История изменений полей документов

**Версия:** Планируется для будущих релизов
**Статус:** ⚠️ НЕ РЕАЛИЗОВАНО
**Область применения:** Паспорт качества (Passport)

> **ВАЖНО:** Функционал истории изменений полей описан в документации как планируемый функционал, но **не реализован** в текущей версии кода. Классы `FieldHistoryEntry`, `DataSource`, интерфейсы и клиентские composables (`useFieldHistory.ts`, `useElisIntegration.ts`) отсутствуют в кодовой базе.

## Планируемая архитектура

### Критическое требование

История изменений будет работать **только при включенной интеграции с ELIS** (когда она будет реализована):

```json
{
  "Elis": { "Use": true }
}
```

Флаг может быть задан глобально или на уровне устройства (`Devices[].IsUsedElis`). Если ELIS выключен, история не будет сохраняться и не будет возвращаться на фронтенд.

## Что хранится

История фиксирует источник и значения изменений для каждого поля:

- источник (ELIS, Manual, IVK и т.д.)
- время изменения
- автор ("Пользователь", "ELIS", "IVK", "Система")
- текущее и предыдущее значение
- комментарий

Максимум 10 записей на поле (FIFO).

## Источники данных

`DataSource` задаётся в `tn.docgeneral/Passport/Models/DataSource.cs`:

- `Unknown` — неизвестный источник (старые данные)
- `ELIS` — данные из протокола ELIS
- `Manual` — ручное изменение
- `IVK` — округление ИВК
- `ElisMissing` — ожидалось из ELIS, но не пришло
- `Auto` — автозаполнение (без индикатора)
- `ReturnToELIS` — возврат к исходному значению ELIS
- `DefaultMethod` — метод по умолчанию из конфигурации

## Структура данных

```csharp
public class FieldHistoryEntry
{
    public DataSource Source { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
    public string Value { get; set; }
    public string? PreviousValue { get; set; }
    public string? Comment { get; set; }

    public const int MaxHistoryEntries = 10;
}
```

История хранится в `DataARM.FieldHistoryMap` (JSON поле БД).

## Ключи истории

**AdditionalInfo поля** (простые ключи):
- `ExportPermit`, `Sample`, `Laboratory_IOF`, `Delive_IOF`, `Receive_IOF`, и др.

**Параметры качества**:
- `value.{ParameterKey}` — измерение
- `result.{ParameterKey}` — результат
- `method.{ParameterKey}` — метод испытаний
- `document.{ParameterKey}` — ELIS-документ

## Передача истории через API

### GET edit config

`GET /api/documents/{deviceId}/{docType}/edit/{id}`

История возвращается в `initialValues` с суффиксом `__history`:

```json
{
  "initialValues": {
    "ExportPermit": "АБВ123",
    "ExportPermit__history": [
      {
        "source": "Manual",
        "modifiedAt": "2025-01-14T09:00:00Z",
        "modifiedBy": "Пользователь",
        "value": "АБВ123"
      }
    ],
    "value.Density": "850.5",
    "value.Density__history": [
      {
        "source": "ELIS",
        "modifiedAt": "2025-01-14T10:00:00Z",
        "modifiedBy": "ELIS",
        "value": "850.5",
        "comment": "Загружено из протокола ПР-2024-12345"
      }
    ]
  }
}
```

### POST save

`POST /api/documents/{deviceId}/{docType}/save/{id}`

История отправляется в блоке `__history`:

```json
{
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

## UI индикация

Компонент: `TN_Doc/Client/document-editor/src/components/history/FieldHistoryIndicator.vue`.

- Индикатор скрывается для `Unknown` и `Auto`.
- Tooltip берётся из `SOURCE_DISPLAY_CONFIG`.
- История отображается только визуально (popup-окна нет).

## Frontend API

`useFieldHistory.ts` предоставляет функции:

- `trackManualChange`
- `trackElisLoad`
- `trackIVKRounding`
- `trackElisMissing`
- `trackAutoFill`
- `trackReturnToElis`
- `trackDefaultMethod`

## Особые случаи

- **DefaultMethod** создаётся при загрузке нового паспорта, если метод выбран из конфигурации автоматически.
- **Auto** используется для балластных параметров, чтобы не показывать лишний индикатор.

## Troubleshooting

**Индикаторы не отображаются**
- Проверьте `Elis.Use = true` (глобально или для устройства) в `TN_Doc/Cfg/CfgApp.json`.
- Перезапустите приложение после изменения конфигурации.

**История не сохраняется**
- Проверьте, что фронтенд отправляет `__history`.
- Убедитесь, что значения действительно изменились (с учётом нормализации).

## Текущая реализация ELIS

В текущей версии присутствует базовая интеграция с ELIS:
- Класс `QualityPassport` (`tn.docgeneral/Passport/Elis/QualityPassport.cs`) для хранения данных протокола испытаний
- Класс `ParameterInfo` (`tn.docgeneral/Passport/Elis/ParameterInfo.cs`) для хранения информации о параметрах
- Интерфейс `IElisData` для поддержки алиасов ELIS ключей
- Контроллер `ElisController` (`TN_Doc/Controllers/ElisController.cs`) для обработки ошибок ELIS

Система истории полей будет реализована в будущих версиях на основе этой базовой интеграции.

## См. также

- [Document Editor Architecture](../architecture/document-editor.md)
- [Passport Configuration](../configs/passport.md)
- [ELIS Integration](../integration/elis.md) - текущая реализация ELIS интеграции
