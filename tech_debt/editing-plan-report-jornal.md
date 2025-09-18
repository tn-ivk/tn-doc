# План (каркас): редактирование документов типов Report и Jornal без добавления новых контроллеров

## Цель
Включить базовую возможность редактирования для документов типа `Report` и `Jornal`, переиспользуя имеющиеся подходы (как в `Act`/`Passport`) и существующие точки входа. Нужно дать возможность заполнять ФИО подписантов для этих документов.

## Ограничения
- Без новых контроллеров и маршрутов.
- Встраиваемся в текущие экшены/страницы (расширяем их логику и представления).
- Референс на существующий функционал редактирования в `Act`/`Passport`.

## Пример-эталон (KMH_PP)
- Файл: `tn.docgeneral/KMH_PP/KMH_PP.cs`
- Методы:
  - `GetEditDoc(int id)`: формирует HTML на базе `wwwroot/HTML/DocEdit.html` (HtmlAgilityPack).
  - `SaveDoc(string json)`: парсит JSON и сохраняет данные в соответствующие поля таблицы (через EF/SQL).

## Требования по доработкам

### 1) Report (`tn.docgeneral/Report/DocReport.cs`)
- Доработать `GetEditDoc(int id)`:
  - Вызвать `GetViewDoc(id)`.
  - Загрузить `CfgEdit` для Report (`tn.docgeneral/Report/CfgEditReport.cs`, список `AdditionalInfo`).
  - Загрузить шаблон `PathToRootDirectory + "/wwwroot/HTML/DocEdit.html"`.
  - Сгенерировать строки полей (по `AdditionalInfo.Use == true`) и вставить в `#AdditionalInfo > tbody`.
  - Сохранить итог в `PathToRootDirectory + "/wwwroot/HTML/html.html"`.
  - Вернуть `""` (как сделано в KMH_PP).
- Дополнить `SaveDoc(string json)` (по аналогии с KMH_PP):
  - Десериализовать JSON (использовать общий формат `CorrectionData`).
  - Сохранить JSON в `TableReport.DataARM`.
    - Так как `TableReportData.DataARM` помечено `[NotMapped]`, использовать SQL:
      - `Database.ExecuteSqlRaw("UPDATE TableReport SET DataARM = {0} WHERE id = {1}", json, docId);`
  - Вернуть `true/false` по результату.
- Метод `DocUpdate(json)` НЕ реализовывать.

### 2) Jornal (`tn.docgeneral/Jornal/DocJornal.cs`)
- Доработать `GetEditDoc(int id)`:
  - Вызвать `GetViewDoc(id)`.
  - Загрузить `CfgEdit` для Jornal (добавить `tn.docgeneral/Jornal/CfgEditJornal.cs` по аналогии с Report: `List<DataARM> AdditionalInfo`).
  - Загрузить шаблон `PathToRootDirectory + "/wwwroot/HTML/DocEdit.html"`.
  - Сгенерировать поля и вставить в `#AdditionalInfo > tbody`.
  - Сохранить в `PathToRootDirectory + "/wwwroot/HTML/html.html"`.
  - Вернуть `""`.
- Дополнить `SaveDoc(string json)`:
  - Десериализовать JSON (`CorrectionData`).
  - Сохранить в `TableMeasurementJornalData.DataARM` (поле существует):
    - `var row = new TableMeasurementJornalData { id = docId, DataARM = json };`
    - `Entry(row).Property(x => x.DataARM).IsModified = true;`
    - `SaveChanges();`
  - Вернуть `true`.
- Метод `DocUpdate(json)` НЕ реализовывать.

## Проверка существования колонки DataARM (однократно, с кэшированием)

- **Назначение**: включать режим редактирования только если в БД у целевой таблицы есть колонка `DataARM`.
- **Таблицы**:
  - `Report` → таблица `TableReport`
  - `Jornal` → таблица `TableMeasurementJornal`

### Реализация

- **Сервис-кэш**: `IDbSchemaCache` (Singleton) в `TN_Doc/Models/Services`
  - Ключ кэша: `(deviceId, tableName)`
  - Значение: `bool hasDataArm`
  - Публичный метод:
    - `bool HasDataArm(int deviceId, IdDoc idDoc)`
      - Определяет `tableName` по `idDoc`
      - Если нет в кэше → выполнить проверку схемы → положить в кэш → вернуть
- **Проверка схемы** (MySQL):
  - Переиспользовать `DBtService`/`GetTableInfo` из `TN.DocGeneral` (уже есть в `General.cs`):
    - `var fields = new DBtService(CurrentCfgDevice.DBConnectionStrings.Where(x=>x.Use).ToList()).GetTableInfo(tableName);`
    - `var hasDataArm = fields.Any(f => string.Equals(f.Field, "DataARM", StringComparison.OrdinalIgnoreCase));`
  - Альтернатива (при необходимости): запрос к `INFORMATION_SCHEMA.COLUMNS`
- **Где вызывать**:
  - На стороне веб-приложения — при формировании UI-флагов (один раз на устройство):
    - В месте, где готовим модель/флаги для страницы (например, в `HomeController` при выборе `IdDevice/IdDoc`):
      - `var canEdit = dbSchemaCache.HasDataArm(IdDevice, IdDoc);`
      - Передать `canEdit` в представление для показа/скрытия переключателя “Просмотр/Редактирование”
- **Связь с модулями**:
  - Модули `Report`/`Jornal` не делают проверку каждый раз.
  - Редактор и `SaveDoc` вызываются только если `canEdit == true`.

### Обработка ошибок и деградация

- Если доступ к БД временно недоступен:
  - Считать `hasDataArm = false`, логировать предупреждение, режим редактирования скрыть.
- Кэш очищать по рестарту приложения; дополнительный TTL не обязателен.

### Псевдокод сервиса

- `IDbSchemaCache`:
  - `bool HasDataArm(int deviceId, IdDoc idDoc)`
- `DbSchemaCache`:
  - `ConcurrentDictionary<(int,string), bool> _cache`
  - `bool HasDataArm(...) { if (!_cache.TryGetValue(key, out var v)) { v = CheckWithDBtService(...); _cache[key] = v; } return v; }`


## Проверка наличия DataARM и UI
- Редактирование возможно только если в БД есть колонка `DataARM`.
  - Для Jornal: колонка есть (`TableMeasurementJornalData.DataARM`).
  - Для Report: колонка может быть в БД, но свойство в модели `[NotMapped]`; запись — через SQL.
- Проверку наличия `DataARM` выполнять единожды на устройство (кэшировать результат), чтобы показывать/скрывать переключатель “Просмотр/Редактирование”.
- Без новых контроллеров: использовать существующие `HomeController.GetDocEdit` и `HomeController.SaveDoc`.

## Пошаговая реализация
1) Report:
   - Заполнить `GetEditDoc(id)` согласно описанию.
   - Реализовать `SaveDoc(json)` — запись JSON в `TableReport.DataARM` через `ExecuteSqlRaw`.
2) Jornal:
   - Добавить `Jornal/CfgEditJornal.cs` с `List<DataARM> AdditionalInfo`.
   - Заполнить `GetEditDoc(id)` как для Report.
   - Реализовать `SaveDoc(json)` — EF-обновление `DataARM`.
3) UI:
   - Переиспользовать `wwwroot/HTML/DocEdit.html` и текущий переключатель “Просмотр/Редактирование”.
   - Форма сохранения дергает уже существующий `SaveDoc(...)`.
4) Проверка схемы:
   - Одноразовая проверка наличия `DataARM` и кэширование результата для показа/скрытия режима редактирования.

## Критерии приемки
- Для `Report` и `Jornal`:
  - Переключатель “Просмотр/Редактирование” показывается только при наличии `DataARM`.
  - `GetEditDoc(id)` возвращает подготовленный редактор (HTML в `wwwroot/HTML/html.html`).
  - `SaveDoc(json)` сохраняет введенные данные подписантов в `DataARM`.
  - Печать/просмотр не нарушены.