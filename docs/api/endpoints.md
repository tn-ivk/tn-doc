# API Endpoints

## Обзор

TN_Doc предоставляет HTTP API для управления документами, конфигурацией и справочниками через традиционный ASP.NET MVC маршрутинг.

- **Текущая версия приложения**: 1.3.8
- **Аутентификация**: отсутствует (в текущей версии)
- **Маршрутизация**: `{controller=Home}/{action}/{id?}`

## Base URL

```
Development: http://localhost:38509
Production:  http://<server-address>:38509
```

## Справочники (DirEditor)

Базовый путь: `/DirEditor`

Контроллер: `DirEditorController` (API Controller)

### Получить все справочники

```http
GET /DirEditor/GetDir
```

**Ответ:**
```json
{
  "dirJsonRaw": "{ ... JSON со всеми справочниками ... }"
}
```

### Установить справочники

```http
POST /DirEditor/SetDir
Content-Type: application/json

{
  "dirJsonRaw": "{ ... новые справочники в JSON ... }"
}
```

### Получить конфигурацию паспортов качества

```http
GET /DirEditor/GetQpConfigs
```

**Ответ:**
```json
{
  "qpCfgJsonRaw": "{ ... конфигурация паспортов качества ... }"
}
```

### Установить конфигурацию паспортов качества

```http
POST /DirEditor/SetQpConfigs
Content-Type: application/json

{
  "qpCfgJsonRaw": "{ ... новая конфигурация паспортов ... }"
}
```

## ELIS API

Базовый путь: `/Elis`

Контроллер: `ElisController`

### Отправить сообщение об ошибке

```http
POST /Elis/ErrorMessage?msg=сообщение
```

Логирует расширенное сообщение об ошибке от ELIS с дополнительными описаниями известных проблем.

**Распознаваемые паттерны ошибок:**
- "сообщение, подпись, или соподписи модифицированы" — нарушена целостность системы
- "Электронная подпись не соответствует" — подпись не соответствует ожидаемой ТСПД
- "Неверный сертификат" — система не может найти сертификат
- "2035 MQRC_NOT_AUTHORIZED" — ошибка связи с IBM MQ
- "ASN1 coorupted data" — поврежденная подпись
- "CompCode" — сетевые ошибки подключения к IBM MQ

### Отправить предупреждение

```http
POST /Elis/WarnMessage?msg=сообщение
```

Логирует предупреждение от ELIS.

## Печать

Базовый путь: `/Print`

Контроллер: `PrintController` (API Controller)

### Получить список принтеров

```http
GET /Print/GetListPrinters
```

**Ответ:**
```json
["Принтер 1", "Принтер 2", "PDF Printer"]
```

### Печать документа

```http
GET /Print/PrintDoc?printerName=Принтер%201
```

Печатает последний сгенерированный PDF документ на указанном принтере.

**Параметры:**
- `printerName` — имя принтера из списка

## Экспорт документов

Базовый путь: `/Export`

Контроллер: `ExportController`

### Получить список форматов экспорта

```http
GET /Export/GetListFormats
```

**Ответ:**
```json
["pdf", "excel", "ods", "xml"]
```

### Экспорт документа

```http
GET /Export/ExportDoc?IdDevice={id}&IdDoc={type}&id={docId}&format={format}
```

**Примечание:** Метод частично реализован, копирует PDF файл.

## Home API (Основной контроллер)

Базовый путь: `/Home`

Контроллер: `HomeController`

### Главная страница

```http
GET /
GET /Home
GET /Home/Index
```

Отображает главную страницу приложения с встроенным FastReport viewer.

### Управление устройствами

#### Получить список устройств

```http
GET /Home/GetListDevices
```

**Ответ:**
```json
[
  { "id": 1, "name": "Устройство 1" },
  { "id": 2, "name": "Устройство 2" }
]
```

#### Получить имя БД для устройства

```http
GET /Home/GetNameDBForDevice?IdDevice={id}
```

**Ответ:** `"database_name"`

### Управление документами

#### Получить список типов документов

```http
GET /Home/GetListDocs?IdDevice={id}
```

**Ответ:**
```json
[
  { "id": 1, "name": "Паспорт качества" },
  { "id": 2, "name": "Акт приема-передачи" }
]
```

#### Получить список шаблонов документа

```http
GET /Home/GetTemplatesDoc?IdDevice={id}&idDoc={docType}
```

**Ответ:**
```json
[
  { "id": 1, "name": "Шаблон 1" },
  { "id": 2, "name": "Шаблон 2" }
]
```

#### Получить список протоколов

```http
GET /Home/GetListProtocolNumber?IdDevice={id}&idDoc={docType}
```

**Ответ:**
```json
[
  { "id": 1, "name": "Протокол 1" },
  { "id": 2, "name": "Протокол 2" }
]
```

#### Получить/установить ID шаблона

```http
GET /Home/GetIdTemplateDoc?IdDevice={id}&IdDoc={docType}
POST /Home/SetIdTemplateDoc?IdDevice={id}&IdDoc={docType}&idTemplateDoc={templateId}
```

#### Получить путь к шаблону

```http
GET /Home/GetPathTemplateDoc?IdDevice={id}&IdDoc={docType}&IdTemplateDoc={templateId}
```

### Работа с документами

#### Получить список документов

```http
POST /Home/GetList
Content-Type: application/json

{
  "idDevice": 1,
  "idDoc": "Passport",
  "dtBegin": "2024-01-01",
  "dtEnd": "2024-12-31"
}
```

**Ответ:** массив объектов `RequestListDocs`

#### Сгенерировать PDF документа

```http
GET /Home/GetDoc?IdDevice={id}&IdDoc={docType}&id={docId}&protocolNumber={protocolNum}
```

Генерирует PDF документа и сохраняет его в `wwwroot/PDF/PDF.pdf`.

**Возвращает:** `true` при успехе, `false` при ошибке.

**Доступ к PDF:**
```http
GET /PDF/PDF.pdf
```

#### Получить форму редактирования (Legacy)

```http
GET /Home/GetDocEdit?IdDevice={id}&IdDoc={docType}&id={docId}
```

**Устарело:** Возвращает URL для редактирования. В текущей версии используется устаревший механизм.

#### Экспортировать документ

```http
GET /Home/ExportDoc?IdDevice={id}&IdDoc={docType}&id={docId}&format={format}&protocolNumber={num}
```

**Параметры:**
- `format` — `pdf`, `excel`, `ods`, `xml`

**Возвращает:** путь к экспортированному файлу

Файл сохраняется в: `{ExportDoc.Path}/{DocumentName}/{filename}.{ext}`

#### Сохранить документ (Legacy)

```http
POST /Home/SaveDoc?IdDevice={id}&IdDoc={docType}&data={jsonData}
```

**Устарело:** Сохраняет документ через механизм `DocGeneral.SaveDoc()`.

#### Обновить документ после подтверждения

```http
POST /Home/UpdateDoc?IdDevice={id}&IdDoc={docType}&data={jsonData}
```

Поддерживается только для `Passport`. Использует `IDocUpdater.DocUpdate()`.

#### Получить период документа

```http
GET /Home/GetPeriodDocument?IdDevice={id}&IdDoc={docType}&id={docId}
```

**Ответ:**
```json
{
  "dateBegin": "2024-01-01T00:00:00",
  "dateEnd": "2024-01-31T23:59:59"
}
```

### ELIS интеграция

#### Проверить использование ELIS

```http
GET /Home/IsUsedElis?IdDevice={id}
```

**Ответ:** `true` или `false`

#### Получить данные регистрации в ELIS

```http
GET /Home/GetDataForRegistrationDeviceInELIS?IdDevice={id}
```

**Ответ:**
```json
{
  "ostKey": "...",
  "siknKey": "...",
  "clientName": "..."
}
```

#### Получить/установить токен клиента

```http
GET /Home/GetClientToken?IdDevice={id}
POST /Home/SetClientToken?IdDevice={id}&clientToken={token}
```

#### Получить данные ELIS

```http
GET /Home/GetElisData
```

**Возвращает:** пустую строку (метод-заглушка)

### Справочники и конфигурация

#### Получить список пользователей

```http
GET /Home/GetListUsers
```

**Ответ:** JSON со словарями, включая пользователей

#### Получить недопустимые символы

```http
GET /Home/GetInvalideChars?IdDevice={id}
```

**Ответ:**
```json
["символ1", "символ2", ...]
```

#### Получить текст кнопки сохранения

```http
GET /Home/GetSaveBtnText?IdDevice={id}&IdDoc={docType}
```

**Ответ:**
- "Сохранить" — если ELIS выключен
- "Завершить редактирование и отправить" — для Passport/Act с ELIS
- "Сохранить" — для остальных типов документов

### Безопасность

#### Проверить использование безопасности

```http
GET /Home/IsUsedSecurity
```

**Ответ:** `true` или `false`

### Обработка ошибок

```http
GET /Home/Error
```

Отображает страницу ошибки.

## Статические файлы

### PDF файлы

```http
GET /PDF/PDF.pdf
```

Последний сгенерированный PDF документ (создается через `/Home/GetDoc`).

### Другие статические ресурсы

- `/css/...` — стили
- `/js/...` — скрипты
- `/lib/...` — библиотеки
- `/Pictures/...` — изображения
- `/HTML/...` — HTML файлы
- `/Data/...` — данные

## Ошибки и коды ответа

### Успешные ответы

- `200 OK` — успешная операция
- `200 OK` с `true`/`false` — булевы методы

### Ошибки

- `500 Internal Server Error` — исключение в контроллере
- Методы возвращают пустые коллекции/строки при ошибках (логи в NLog)

### Типичные ошибки

```json
{
  "error": "Не удалось загрузить DLL для документа Passport"
}
```

## Примечания по использованию

### Маршрутизация

Все контроллеры используют стандартную ASP.NET MVC маршрутизацию:
- `DirEditorController`, `PrintController` — помечены `[ApiController]`
- `HomeController`, `ElisController`, `ExportController` — обычные MVC контроллеры

### CORS

Приложение настроено на разрешение всех источников:
```csharp
AllowAnyHeader()
AllowAnyMethod()
SetIsOriginAllowed((host) => true)
AllowCredentials()
```

### Безопасность

- FastReport Security отключена: `EnableScriptSecurity = false`
- Аутентификация отсутствует
- Используйте безопасность на уровне сети

## См. также

- [Architecture Overview](../architecture/overview.md)
- [IDocumentEditor Interface](../../tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs)
- [Конфигурация приложения](../configs/app-config.md)
