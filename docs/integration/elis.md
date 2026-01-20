# Интеграция с ELIS

## Обзор

ELIS (Единая Лабораторная Информационная Система) — источник лабораторных данных для паспортов качества.

**Статус в текущем коде (v1.3.8):**
- ✅ Модели ELIS: `QualityPassport`, `ParameterInfo`, `SignerInfo` в `tn.docgeneral/Passport/Elis/`
- ✅ Интерфейс алиасов: `IElisData` (`tn.docgeneral/Passport/IElisData.cs`)
- ✅ Контроллер логирования: `ElisController` (`TN_Doc/Controllers/ElisController.cs`)
- ⚠️ `Home/GetElisData` — заглушка (возвращает пустую строку)
- ❌ REST‑эндпойнтов загрузки протоколов ELIS нет
- ❌ SPA‑редактора документов нет (используются HTML‑формы)

## Текущая реализация

### Контроллер ELIS

`ElisController` принимает сообщения об ошибках/предупреждениях и пишет их в лог:
- `POST /Elis/ErrorMessage?msg=...`
- `POST /Elis/WarnMessage?msg=...`

Контроллер распознаёт типовые паттерны ошибок (подпись, сертификат, IBM MQ и т.д.).

### Методы HomeController

- `GET /Home/IsUsedElis?IdDevice=...` — флаг использования ELIS
- `GET /Home/GetDataForRegistrationDeviceInELIS?IdDevice=...` — данные для регистрации устройства
- `GET /Home/GetClientToken?IdDevice=...` / `POST /Home/SetClientToken` — работа с токеном
- `GET /Home/GetElisData` — **заглушка**

## Конфигурация

ELIS настраивается в `TN_Doc/Cfg/CfgApp.json`.

Пример актуального блока:
```json
{
  "Elis": {
    "Use": false,
    "OstKey": "ostKey",
    "SiknKey": "siknKey",
    "ClientName": "clientName",
    "ClientToken": ""
  },
  "Devices": [
    {
      "IdDevice": 0,
      "Elis": null
    }
  ]
}
```

> Поле `Devices[].Elis` может переопределять глобальные настройки, но в текущих конфигурациях часто равно `null`.

## UI

В `TN_Doc/wwwroot/HTML/ElisRequestWindow.html` есть модальное окно для запроса данных ELIS, но фактическая загрузка данных не реализована.

## Планируемая интеграция (в коде отсутствует)

Идея будущей интеграции:
- HTTP‑клиент к ELIS/ELIS‑Connector
- эндпойнты получения протоколов
- автоматическое заполнение паспортов
- история изменений полей

Эти пункты пока **не реализованы** и служат лишь ориентиром для будущих задач.

