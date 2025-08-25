### План модульных тестов для контроллеров (NUnit)

#### Общие принципы
- **Фреймворк**: NUnit 4, адаптер NUnit3, Moq для моков, EFCore.InMemory при необходимости.
- **Подход**: Arrange — Act — Assert; изоляция тестов, детерминированные фикстуры; минимум взаимодействия с ФС, кроме мест, где контроллер принудительно зависит от `AppConfigService`.
- **Логирование**: проверка вызовов `ILogger` через перехват `Log(LogLevel, ...)` в Moq.
- **DI/зависимости**: для `HomeController`, `ExportController`, `PdfController`, `PrintController` — мокать `IReportBuffer`, `IDocModuleLoader`, `ILogger<T>`, и передавать валидные `DbContextOptions<DocGeneral>` (InMemory).
- **ФС/конфиги**: для `DirEditorController` и мест, где используется `AppConfigService.GetInstance(IConfiguration)`, применять временные каталоги и `IConfiguration` In-Memory, как в существующих `AppConfigServiceTests`.

---

### ClientLogController
Метод: `LogClientMessage([FromBody] ClientLogMessage log)`
- **Должен вернуть 400** при `log == null`, лог `LogError("...объект log равен null")`.
- **Должен вернуть 400** при `Level` пустом/whitespace, лог `LogError("...уровень логирования")`.
- **Должен вернуть 400** при `Message` пустом/whitespace, лог `LogError("...пустое сообщение")`.
- **Должен вернуть 200** и вызвать соответствующий уровень лога для `trace|debug|info|warn|warning|error|fatal|critical`.
- **Должен вернуть 400** при неизвестном уровне; лог содержит `Unknown log level: <value>`.

---

### DirEditorController
Методы: `GetDirAsync()`, `SetDirAsync(DirEditDTO)`, `GetQpConfigsAsync()`, `SetQpConfigsAsync(QpEditDto)`
- `GetDirAsync`: 200, тело содержит `DirJsonRaw` (не пустое) из тестовой конфигурации.
- `SetDirAsync`: 200; запись нового JSON в тестовые файлы; последующий `GetDirAsync` отражает изменения.
- `GetQpConfigsAsync`: 200, `QpCfgJsonRaw` не пустой (инициализация из тестовых файлов).
- `SetQpConfigsAsync`: 200; последующий `GetQpConfigsAsync` возвращает обновлённые данные.
- Ошибочные сценарии: некорректный JSON (опционально — ожидается логирование/ошибка сервиса; фиксируем фактическое поведение).

---

### ElisController
Методы: `ErrorMessage(string msg)`, `WarnMessage(string msg)`
- `ErrorMessage`: при `null/empty` — лог не вызывается.
- `ErrorMessage`: для каждого известного шаблона сообщений добавляется человекочитаемое описание; итоговый `LogError` содержит это описание.
- `WarnMessage`: вызывает `LogWarning` с переданным текстом.

---

### ExportController
Методы: `GetListFormats()`, `ExportDoc(int IdDevice, IdDoc IdDoc, int id, string format, int protocolNumber)`
- `GetListFormats`: возвращает список ровно из `{"pdf","excel","ods","xml"}`.
- `ExportDoc`: при неподдерживаемом `format` — возвращает пустую строку (через `NotSupportedException` внутри `catch`).
- `ExportDoc`: при `IDocModuleLoader.LoadDocsModule(...) == null` — возвращает пустую строку и логирует ошибку.
- `ExportDoc`: при `GetViewDoc(...) == null` — возвращает пустую строку и логирует ошибку.
- Позитивный сценарий экспорта (pdf/xlsx/ods/xml) — будет покрыт интеграционными тестами отдельно из‑за зависимостей от FastReport и файловой системы.

---

### HomeController
Ключевые методы без UI:
- `GetListDevices()`: когда в конфиге нет `Use==true` устройств — возвращает пустой список и пишет ошибку; когда есть — возвращает список `Id/Name`.
- `GetNameDBForDevice(int IdDevice)`: при отсутствии устройства — пустая строка; при валидном — возвращает `Database`.
- `GetListDocs(int IdDevice)`: при отсутствии устройства/документов — пустой список; при валидном — список `Id/Name`; при `IDocModuleLoader == null` — пустой список и лог.
- `GetTemplatesDoc(int IdDevice, IdDoc idDoc)`: ошибки конфигурации — пустой список; при валидных данных — список шаблонов.
- `GetListProtocolNumber(...)`: возвращает список с двумя элементами по умолчанию.
- `SetIdTemplateDoc(...)`/`GetIdTemplateDoc(...)`: корректная запись/чтение ID последнего шаблона, падение на неинициализированных шаблонах — корректные fallback‑ветви.
- `IsUsedSecurity()`, `IsUsedElis(int)`: отражают конфиг.
- `GetDataForRegistrationDeviceInELIS(int)`: возвращает словарь с ключами при наличии настроек на уровне устройства или приложения; при отсутствии — `null`.
- `GetClientToken(int)`: корректно формирует словарь с `clientToken` (null/значение).
- `SetClientToken(int, string)`: возвращает результат `AppConfigService.SetElisClientToken`.
- `GetDoc(int, IdDoc, int, int)`: `id==0` — `false`; `IDocModuleLoader==null` — `false`; пустой `pathTemplateFile` или отсутствие файла — `false`.
- `GetDocEdit(int, IdDoc, int)`: `id==0` — `string.Empty`; `IDocModuleLoader==null` — `string.Empty`.
- `SaveDoc(int, IdDoc, string)`: при `IDocModuleLoader==null` — без исключений.
- `UpdateDoc(int, IdDoc, string)`: для не‑`Passport` — предупреждение и `return`; для `Passport` с пустыми данными — предупреждение и `return`.
- `GetPeriodDocument(int, IdDoc, int)`: при `IDocModuleLoader==null` — `null`.
- `GetListUsers()`: при валидной настройке возвращает не пустую строку.
- `GetInvalideChars(int)`: сериализует список из конфигурации; при исключениях — пустая строка.
- `GetSaveBtnText(int, IdDoc)`: ELIS выключен — "Сохранить"; включён и `IdDoc` в `{Act, Passport}` — специальный текст.
- `Index(...)`: smoke‑тест — возвращает `ViewResult`, `ViewData["Version"]` задан; (без проверки FastReport рендеринга).
- Вспомогательные методы преобразования строк/байт — тесты на корректное преобразование (минимальные проверки на пустые/валидные данные).

---

### PdfController
Метод: `Get()`
- При буфере `null` или пустом — `NotFound()`.
- При наличии байт — `FileContentResult` с `ContentType = "application/pdf"` и анти‑кеш заголовками.

---

### PrintController
Методы: `GetListPrinters()`, `PrintDoc(string printerName)`
- `GetListPrinters`: успешный сценарий — `Ok(printers[])`, логирование списка при наличии; сценарий исключения из сервиса — `StatusCode(500, ...)`.
- `PrintDoc`: вызывает `_service.PrintDocAsync(printerName)` и возвращает `Ok()`.

---

### Организация тестов и артефактов
- Создать папку `Tests/controllers`.
- Для каждого контроллера создать отдельный файл `*ControllerTests.cs` в `Tests/controllers` с тест‑классом‑контейнером.
- Именование тестов: `MethodName_Condition_ExpectedBehavior`.
- Фикстуры, требующие ФС (конфиг/директории), используют временные каталоги и `IConfiguration` In‑Memory; очистка в `OneTimeTearDown`.


