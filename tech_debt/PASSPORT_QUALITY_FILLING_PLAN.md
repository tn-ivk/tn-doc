## План доработки логики заполнения паспорта качества

### 0. Контекст текущей реализации
- `DocPassport.BuildQualityParameters*` (`tn.docgeneral/Passport/DocPassport.cs:862-1500`) уже формирует схему/значения, собирает методы из конфигурации (`CfgEditPassport`) и `LabInfo`, а также подмешивает данные ELIS в measurement/result. Сейчас признак использования ELIS и синхронизация колонок разбросаны по методам (`BuildParameterValues`, `RecalculateResultValue`, `DocUpdate`).
- `DocUpdate`, `AddOrUpdateLabInfo` и `FieldHistoryMap` (`tn.docgeneral/Passport/DocPassport.cs:274-420`, `:600-760`, `:1019+`, `tn.docgeneral/Passport/DataIVKDoc.cs:254-320`) принимают пары `method/result/value/document`, пишут историю источников (Manual/ELIS) и определяют `ElisFilled`. Ручная логика метода сейчас отсутствует: UI отправляет JSON `TN.Doc.Edit.Metod`, а бэкенд просто дописывает его в `LabInfo`.
- `DocumentPassportEditor.vue` + `usePassportEditor.ts` (`TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue`, `.../composables/usePassportEditor.ts`) вычисляют список параметров на лету, синхронизируют measurement/result и при необходимости добавляют отсутствующие методы в `methodOptions`. Все ограничения по UX (история, подсветки, доступность кнопок) реализуются на клиенте.

### 1. Цель и ожидаемый результат
- Разделить алгоритмы заполнения/редактирования паспорта качества в зависимости от включённости ELIS (`CfgApp.json` → `UseElis`).
- Обеспечить корректное поведение балластных и небалластных показателей с возможностью конфигурирования через `CfgEditPassport_GOSTR50.2.040(I).json`.
- Добавить UX-инструменты, позволяющие операторам вручную вносить методы испытаний и результирующие строки без нарушения синхронизации с историей изменений.
- Сохранить обратную совместимость: паспорта без ELIS и старые конфиги должны продолжить работать без изменений контрактов.

### 2. Точки изменений
- Бэкенд: `tn.docgeneral/Passport/DocPassport.cs`, связанные модели (`DataARM`, `LabInfo`, `CorrectionData`, `EditData`, `TN.DocEditor.Passport.*`), конфигурации (`TN_Doc/Cfg/CfgEditPassport_GOSTR50.2.040(I).json`, при необходимости описание `CfgApp.json`) и тесты (`Tests/Services/*`).
- Фронтенд: `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue`, компоненты `PassportQualityTable`, `FormFieldWithHistory`, стора `documentStore`, композиции `usePassportEditor`, `useFieldHistory`, `useElisIntegration`, а также новые модальные компоненты.
- UI-инфраструктура: стили (material3.css), компоненты `PrimeVue`, документация в `docs/ui-design.md` и `docs/architecture/document-editor.md`.

### 3. Разделение сценариев ELIS Off/On
1. **ELIS выключен**  
   - Сохраняем текущую поведенческую модель: measurement/result редактируются напрямую, история полей остаётся локальной.  
   - Логику `IsUsedElis` выносим в стратегии (например, `IPassportQualityStrategy`), чтобы условия не размазывались по всему `DocPassport`.
2. **ELIS включен**  
   - Включаем новую ветку: признак `IsBalast`, синхронизация measurement/result для балластных параметров, модалки ручных методов/результатов, предупреждения об источниках данных.  
   - Истории изменений продолжают детерминировать `ElisFilled`, поэтому все новые действия должны писать `FieldHistory` через существующие механизмы.
3. **Feature-flag подход**  
   - Новое поведение активируется, только если `UseElis = true` и в конфиге есть `IsBalast` (или версия схемы ≥ 2). Это позволяет откатить изменения, не трогая конфигурации старых клиентов.

### 4. Обновление конфигураций и контрактов
1. **Расширение схемы параметров качества**  
   - В `CfgEditPassport_GOSTR50.2.040(I).json` добавить поле `IsBallast` для каждого элемента `Parameters[]`. Отсутствие поля трактуем как `false`.  
   - Балластные показатели: `TempCorrection`, `PressCorrection`, `DensCorrection`, `Dens20Correction`, `Dens15Correction`, `MassWaterFracCorrection`, `Chloride_Salts.Concentration`, `Chloride_Salts.MassFraction`, `Impurity`.  
   - Небалластные показатели: `SulfurCorrection`, `DNP.kPa`, `DNP.mercury_mm`, `Yield_fraction_200`, `Yield_fraction_300`, `Yield_fraction_350`, `Mass_fraction_of_paraffin`, `Mass_fraction_of_hydrogen_sulfide`, `Mass_fraction_of_methyl_and_ethyl_mercaptan`, `Mass_fraction_of_organic_chlorides`.
2. **DTO / JSON / типы на стороне клиента**  
   - Расширить `TN.DocEditor.Passport.QualityParameter`, `QualityParameterSchema`, `Edit.Parameter`, DTO `PassportQualityParameterSchema`, JSON `PassportEditConfig` и ts-типы (`TN_Doc/Client/document-editor/src/types/passport.types.ts`) полем `IsBallast`.  
   - Структуры, описывающие методы (`TN.Doc.Edit.Metod`, `PassportQualityParameter.method`, `MethodOptions[]`), оставляем без дополнительных полей: сервер продолжает собирать список методов из конфига и `LabInfo` так же, как сейчас, а клиент отправляет в `DocUpdate` только сериализованный JSON `Metod`.
3. **История и паспортные конфиги**  
   - Проверить, что `PassportEditConfig.InitialValues` содержит `__history`/`__elisFilled` для всех новых полей, чтобы модалки могли восстановить источник значений.  
   - Для обратной совместимости ориентироваться только на наличие поля `IsBallast`: если поле отсутствует, считается `false`, сервер использует старую логику.
4. **Примеры контрактов**  
   ```json
   {
     "docType": "Passport",
     "isElisUsed": true,
     "qualityParametersSchema": [
       {
         "id": 1,
         "key": "TempCorrection",
         "name": "Коррекция температуры",
         "isBalast": true,
         "methodRequiredFill": true,
         "methodOptions": [
           { "id": 10, "name": "ASTM D1298", "limitValueActivate": false },
           { "id": 0, "name": "Ручной метод 05.2024", "limitValueActivate": true, "limitValue": 0.5, "limitValueString": "менее 0,5" }
         ],
         "resultEditMode": "readonly"
       }
     ],
     "initialValues": {
       "value.TempCorrection": "0,45",
       "value.TempCorrection__elisFilled": true,
       "result.TempCorrection": "0,45",
       "method.TempCorrection": "{\"Name\":\"ASTM D1298\",\"LimitValueActivate\":false}",
       "method.TempCorrection__history": [
         { "source": "ELIS", "modifiedBy": "elis@sender", "timestamp": "2024-06-21T08:14:00Z" }
       ]
     }
   }
   ```
   - Для параметра без `IsBalast` поле просто опускаем — клиент трактует как `false`.
5. **Миграция конфигов и обратная совместимость**  
   - Шаг 1: обновить `IsBallast` во всех актуальных `TN_Doc/Cfg/CfgEditPassport_*.json` (по списку параметров) через существующие процессы управления конфигурациями, без отдельного миграционного скрипта.  
   - Шаг 2: в `DocPassport` определять стратегию по факту наличия `IsBallast`. Для старых конфигов (`IsBallast` отсутствует) автоматически используем старую ветку — никакой версии хранить не нужно.  
   - Шаг 3: документировать процесс в `docs/configs/passport.md` (описание новых полей, порядок обновления конфигов и требования к их проверке).  
   - Шаг 4: после обновления конфигов прогнать `dotnet test` + smoke (`dotnet run --project TN_Doc/TN_Doc.csproj -- --device <id>`), чтобы убедиться, что паспорт открывается и данные с ELIS подтягиваются без ошибок сериализации.

### 5. Бэкенд: переработка DocPassport
1. **Стратегия поведения**  
   - В `DocPassport` создать внутренний сервис (`PassportQualityStrategy`), который на основе `UseElis` и `IsBalast` решает, как формировать значения/блокировки. Это позволит покрыть стратегию модульными тестами без запуска всего EF-контекста.
2. **Формирование схемы и значений**  
   - В `BuildQualityParametersSchema` прокидывать `IsBalast` и актуальный список методов (как сейчас: данные из конфига дополняются сведениями из `LabInfo`).  
   - `BuildParameterValues` и `BuildParameterMethod` должны учитывать `IsBalast`: measurement/result для балластных всегда синхронны, `result` не перезаписывается ELIS `valueString`, если measurement пришёл вручную.  
   - При генерации данных для UI добавлять признак `ResultEditMode` (`auto`, `modal`, `readonly`) — это упростит работу фронтенда.
3. **Докапливание истории и проверка инвариантов в `DocUpdate`**  
   - При сохранении балластного параметра сервер перепроверяет, что `result.*` совпадает с `value.*`; при расхождении — логирует предупреждение и приводит к measurement.  
   - Для небалластных полей сервер принимает значение из `result.*`, но добавляет в историю запись «Manual overrides ELIS», если последнее значение пришло из ELIS. История пишется через уже существующий `dataArm.AddFieldHistoryEntry`.  
   - Результаты, введённые через модалку, передаются как `Values[]` с `Tag = "PrintValue"`, чтобы текущая обработка (`case PrintValue`) обновила `LabInfo.Value` и `History`.
4. **Ручные методы испытаний**  
   - Используем текущий пайплайн `DocUpdate` → `AddOrUpdateLabInfo`. UI формирует `Values[]` с `Tag = "Metod"` и `Key = "method.<parameterKey>"`, `Value` — сериализованный `TN.Doc.Edit.Metod`.  
   - После десериализации метод помещаем в `LabInfo` без дополнительных признаков; источник изменений фиксируется только в истории (`FieldHistory`, как и сейчас). Никаких новых endpoint'ов не требуется.  
   - В `BuildParameterMethod` оставляем текущую схему формирования `MethodOptions`: методы из конфига и `LabInfo` объединяются в один список без метаданных.
5. **Индикация отсутствия метода**  
   - Сценарии, когда метод исчез из конфига, обрабатываем по текущей схеме: UI работает с тем, что пришло из `LabInfo`, а дополняющие предупреждения формируются на основании истории без добавления временных опций или новых флагов.
6. **Логирование и телеметрия**  
   - Добавить подробный лог `PassportQuality` (уровень Trace/Info) с фиксацией стратегий и источников, чтобы можно было анализировать конфликтующие действия ELIS/оператора.  
   - При сбое десериализации модальных данных сервер должен возвращать понятное сообщение (ValidationProblem) вместо silent fallback.

**Статус Фазы 1 (выполнено)**  
- `IPassportQualityStrategy` внедрён в `DocPassport`, фабрика выбора стратегии покрыта `PassportQualityStrategyTests`.  
- DTO/контракты (`PassportQualityParameterSchema`, `resultEditMode`, `isBalast`) синхронизированы c фронтендом.  
- JSON‑снапшоты `PassportEditConfig` для устройств ELIS on/off обновлены, обратная совместимость подтверждена.  
- Типы клиента и обработка конфигов без `IsBalast` остаются валидными; документировано в `docs/configs/passport.md`.

### 6. Фронтенд: DocumentPassportEditor и связанные компоненты
1. **usePassportEditor / store**  
   - Расширить `PassportQualityParameter` объектами `isBalast` и `resultEditMode`; данные о методах остаются в текущем JSON без дополнительных признаков.  
   - Добавить watchers на `formData["value.<key>"]` и `formData["result.<key>"]`:  
     - для балластных `value` сразу зеркалится в `result` через `documentStore.syncBallastParameter`.  
     - для небалластных `result` переводится в режим ручного редактирования (`documentStore.markManualOverride`).  
   - В `documentStore` реализовать методы `syncBallastParameter(key, payload)` и `markManualOverride(key, historyPayload)`; они обновляют `formData`, `__history`, `__elisFilled` пачкой.  
   - `qualityParameters` добавляет warnings (`requiresManualMethod`, `elisOverride`, `manualOverride`) на основании истории (`store.formData["*.history"]`) и текущих значений формы, без дополнительных метаданных.
2. **PassportQualityTable**  
   - Для балластных полей Result read-only; `@input` на measurement вызывает `documentStore.syncBallastParameter`, чтобы `value/result` оставались идентичны и история отражала источник (Manual/ELIS).  
   - Для небалластных полей рядом с результатом рендерим активную кнопку «Редактировать» → модалка; после закрытия вызываем `markManualOverride`.
3. **Модалка редактирования результатов**  
   - Содержит селект (`менее`/`более`), поле ввода значения (валидируем разрядность/локаль) и предпросмотр итоговой строки (`«менее 4,0»`).  
   - После подтверждения для небалластных вызывает `documentStore.markManualOverride` (история `source = Manual`, `payloadType = ResultModal`). Балластные параметры модалку не открывают (result заблокирован).  
   - Валидация: превышение допустимых разрядов блокирует кнопку «Применить», `store.canSave` учитывает наличие ошибок.
4. **Модалка ручного метода**  
   - Кнопка рядом с комбобоксом метода. Поля: название, `limitValueActivate`, `limitValue`, `limitValueString`, `isDefault`, чекбокс «Сделать основным».  
   - После сохранения добавляем метод в локальный список `methodOptions`, записываем JSON в `store.formData["method.<key>"]` и историю (`source = Manual`).  
   - Если имя совпадает с существующим, показываем подтверждение для замены (`method.options` обновляется локально).
5. **Предупреждения и подсветки**  
   - Если последняя запись в `method.<key>__history` имеет `Source = Manual` или значение отсутствует в конфиге, подсвечиваем строку и показываем tooltip «Отсутствует в справочнике / ручной метод».  
   - Если история показывает, что выбранного метода нет в текущем конфиге, отображаем баннер с предложением открыть справочник методов.  
   - Для результатов без метода показываем напоминание о необходимости выбрать/создать метод.
6. **Обработка ELIS**  
   - `useElisIntegration` вызывает `syncBallastParameter` для балластных и прямое обновление `result.*` + историю `source = ELIS` для небалластных, чтобы далее `markManualOverride` мог сравнить источники.  
   - `pendingElisData` сохраняет исходный протокол (`__elisProtocol`), дополнительно пишем историю `source = ELIS`.
7. **UI/стили**  
   - Модалки используют `material3.css`: фон `var(--md-surface)`, границы `var(--md-outline)`, радиус `var(--md-radius)`, типографика из `DESIGN_DOCUMENTATION.md`.  
   - Кнопки действия — `btn-primary` / `btn-outline-primary`, статус `ProgressSpinner` (`--md-primary`).  
   - Общая высота инпутов 35px, warning-цвет `var(--md-warning)` для подсветки методов/результатов.

### 7. API и модель данных
- REST-поток остаётся прежним: `POST /api/documents/{deviceId}/Passport/save` (`DocUpdate`). Payload содержит `CorrectionData.Values` с тегами `Metod`, `Value`, `PrintValue`, `DocNum`, `History`. Ниже пример запроса, который создаёт ручной метод и вручную редактирует результат:
  ```json
  {
    "DocID": 51234,
    "Values": [
      {
        "Tag": "Metod",
        "Key": "method.TempCorrection",
        "Value": "{\"Id\":0,\"Name\":\"Ручной метод 05.2024\",\"LimitValueActivate\":true,\"LimitValue\":0.5,\"LimitValueString\":\"менее 0,5\",\"IsDefault\":false}",
        "ElisFilled": false,
        "History": [
          {
            "Source": "Manual",
            "ModifiedBy": "lab\\operator1",
            "Timestamp": "2024-06-21T08:20:31Z",
            "Value": "Ручной метод 05.2024"
          }
        ]
      },
      {
        "Tag": "Value",
        "Key": "value.TempCorrection",
        "Value": "0,45",
        "ElisFilled": false
      },
      {
        "Tag": "PrintValue",
        "Key": "result.TempCorrection",
        "Value": "менее 0,5",
        "ElisFilled": false,
        "History": [
          {
            "Source": "Manual",
            "ModifiedBy": "lab\\operator1",
            "Timestamp": "2024-06-21T08:21:10Z",
            "Value": "менее 0,5",
            "Payload": { "origin": "ResultModal" }
          }
        ]
      }
    ]
  }
  ```
- Для ручных методов модалка формирует `method.<key>` + историю `method.<key>__history` (source `Manual`). `Value` — сериализованный `TN.Doc.Edit.Metod`. Метод попадает в `LabInfo`, а `PassportEditConfig` вернёт его в составе стандартного списка методов.  
- Дополнительные признаки происхождения метода не передаются — UI восстанавливает контекст по истории (`method.<key>__history`).  
- Результаты модалки передаются через `result.<key>` и историю (`result.<key>__history`). Клиент сам дублирует значение в `value.<key>` для балластных параметров (через `syncBallastParameter`), сервер просто сохраняет полученные данные.  
- Для интеграции с ELIS сохраняем полный протокол в `__elisProtocol` (как и сейчас) + историю `Source = ELIS`, чтобы клиент мог подсветить автоматические изменения без дополнительных полей в DTO.

### 8. Тестирование и наблюдаемость
- **Backend**:  
  - Написать юниты на новую стратегию (`PassportQualityStrategyTests`) с кейсами ELIS on/off, балласт/небалласт, ручные методы.  
  - Добавить интеграционные тесты на `DocUpdate` (моки `CorrectionData`) в `Tests/Services/Passport` для сценариев «ручной метод + история», «ELIS → ручное переопределение».  
  - Проверить сериализацию `IsBalast` и корректное восстановление истории методов через `DocPassportTests`.
- **Frontend**:  
  - Vue unit tests на `usePassportEditor` (результат пересчитывается корректно, предупреждения строятся), компонентные тесты модалок (валидаторы, история).  
  - E2E сценарии: импорт ELIS + ручное редактирование, создание нового метода, подсветка устаревших методов.  
- **Observability**:  
  - Протоколировать ключевые действия (создание/редактирование метода, ручное изменение результата). Добавить счётчик mismatches measurement/result в балластных полях.  
  - В отчёт о регрессии включать список параметров, где сработали предупреждения.

### 9. Критерии приёмки и тестовые сценарии
- **Конфигурации (`TN_Doc/Cfg/CfgEditPassport_GOSTR50.2.040(I).json`)**  
  - Каждый объект `Parameters[]` содержит `IsBalast` (или поле отсутствует и тогда считается `false`).  
  - Скрипт миграции оставляет предыдущие поля нетронутыми и формирует отчёт (JSON/CSV) со списком устройств, где `UseElis = false`, чтобы QA проверили обратную совместимость.
- **Бэкенд (`tn.docgeneral/Passport/DocPassport.cs`)**  
  - `BuildQualityParameters`/`BuildQualityParametersSchema` возвращают признак `isBalast`, `resultEditMode` и текущий набор методов в порядке `config → lab → manual` без дополнительных полей.  
  - `DocUpdate` валидирует, что для балластных параметров `value.* == result.*`; при расхождении пишет `Warn`, но не исправляет payload (ошибка отправителя).  
  - Для небалластных параметров сервер принимает ручные данные и, если история отсутствует, добавляет минимальную запись «Manual overrides ELIS» (покрыто тестом `DocUpdateManualOverridesElis`).  
  - `PassportEditConfig` для устройства без ELIS/старой схемы совпадает с текущим контрактом (проверяется снапшот-тестом).
- **Фронтенд (`usePassportEditor.ts`, `PassportQualityTable.vue`, `DocumentPassportEditor.vue`)**  
  - Балластные строки отображают заблокированный Result и синхронизируются при вводе measurement (unit-тест `usePassportEditor.balast.sync.spec.ts`).  
  - Модалка результатов формирует строки «менее/более + число» и записывает историю; сохранение блокируется при неверном формате.  
  - В комбобоксе метода отображаем предупреждение только по данным истории (`method.<key>__history`); кнопка ручного метода добавляет новую опцию через стандартный JSON без дополнительных флагов.  
  - В сценарии «ELIS → ручное редактирование → повторный импорт ELIS» последнее действие остаётся в силе, история отображает оба шага.
- **Смоук проверки**  
  - Сценарий 1: устройство с `UseElis = false` открывает паспорт, редактирует measurement/result как раньше, сохранение проходит (ручной тест).  
  - Сценарий 2: устройство с `UseElis = true`, балластный параметр получает значение из ELIS, оператор вручную меняет measurement и метод — результат блокирован, история фиксирует Manual.  
  - Сценарий 3: небалластный параметр → оператор открывает модалку, вводит «менее 0,5», сохраняет, запускает печать — строка выводится в формате FastReport.  
  - Сценарий 4: добавление нового метода через модалку, повторная загрузка документа → метод отображается с пометкой «ручной», предупреждений нет.

### 10. Риски и вопросы
- Ручные методы хранятся в `LabInfo` и в справочник попадают только через модалку — важно документировать и протестировать, чтобы не появлялись «висящие» методы.  
- Строковый формат результата («менее/более + число с запятой») должен совпадать у бэкенда и фронтенда, иначе печать даст расхождения.  
- Макеты модалок необходимо согласовать с заказчиком до реализации (см. п.6.7).  
- История изменений из модалок сохраняется стандартно (`__history`), но нужно явно описать это в документации, чтобы избежать ложных ожиданий про логи ELIS.  
- Приоритет последнего действия сохраняется: ручное изменение перекрывает данные ELIS, `FieldHistory` фиксирует источник.  
- Нужны ли дополнительные предупреждения оператору при перезаписи ELIS-значений? Пока UI просто подсвечивает источник, но заказчик может потребовать подтверждение.

### 11. Шаги внедрения и чек-лист задач
1. **Фаза 0 — подготовка конфигов и feature‑flag**  
   - **0.1. Инвентаризация конфигов**  
     - Собрать список всех файлов `TN_Doc/Cfg/CfgEditPassport_*.json` (в т.ч. копий под разные ГОСТ/устройства).  
     - Для каждого устройства проверить `CfgApp.json` на наличие/значение `UseElis`, зафиксировать матрицу «устройство ↔ UseElis».  
   - **0.2. Расширение схемы параметров**  
     - В эталонном `CfgEditPassport_GOSTR50.2.040(I).json` добавить поле `IsBalast` в элементы `Parameters[]` согласно списку из раздела 4.1 (балласт/небалласт).  
     - Убедиться, что отсутствие `IsBalast` в JSON корректно трактуется как `false` парсером (на уровне кода и тестов).  
   - **0.3. Ручное обновление конфигов**  
     - Для актуальных конфигов `CfgEditPassport_*.json` проставить `IsBalast` по согласованному списку параметров, используя существующие механизмы правки конфигов (MR/PR, ручная правка, без отдельного миграционного скрипта).  
     - Для каждого изменённого файла сохранить diff и согласовать его с эксплуатацией/ответственными за конфигурации.  
   - **0.4. Документация и rollback**  
     - В `docs/configs/passport.md` описать поле `IsBalast`, ожидания по поведению и порядок обновления конфигов (какие файлы менять, кто утверждает изменения).  
     - Описать сценарий отката (восстановление конфигов из VCS/резервных копий) без привязки к отдельному скрипту или отчёту миграции.  
   - **Проверка после Фазы 0**  
     - На выборке конфигов (минимум 2–3 файла) проверить валидность JSON и соответствие структуре эталонного `CfgEditPassport_GOSTR50.2.040(I).json`.  
     - Прогнать unit‑тесты на парсер конфигов (`Tests/Configs/CfgEditPassportTests.cs`) и убедиться, что старые конфиги без `IsBalast` не ломают десериализацию.  
     - На одном устройстве с `UseElis = true` и одном с `UseElis = false` вручную открыть паспорт в UI (текущая версия `DocumentPassportEditor.vue`), проверить отсутствие ошибок загрузки/валидации.  
  
2. **Фаза 1 — стратегия и контракты (бэкенд)** — **выполнено**  
   - Стратегии `PassportQualityStrategy*`, новые DTO (`isBalast`, `resultEditMode`) и снапшоты `PassportEditConfig` внедрены; тесты `PassportQualityStrategyTests` и проверки обратной совместимости пройдены.  
   - Дальнейшие этапы используют готовые API/типы, возврат к этому блоку не требуется.
  
3. **Фаза 2 — сохранение и история (бэкенд, валидация поверх фронтовых инвариантов)** — **выполнено**  
   - **Что сделано:**  
     - `DocPassport.DocUpdate(int id, Dictionary<string, object> values)` теперь поднимает `__history` в `Dictionary<string, List<FieldHistoryEntry>>`, при `UseElis = false` пишет `Trace` и игнорирует историю.  
     - В `DocUpdate(string jsonData, QualityPassport? elisProtocol)` формируется карта параметров: для балластных `ValidateBallastPayload` пишет `Warn` (payload не меняем), для небалластных `EnsureManualOverrideHistory` автоматически добавляет запись `Manual overrides ELIS`, если последнее изменение пришло из ELIS.  
     - `AddOrUpdateLabInfo` принимает признак ELIS и ключ истории, вычисляет `ElisFilled` по последнему источнику (`method.<key>__history`). При ELIS Off логируем `Trace`, что история пропущена.  
     - Для диагностики задействован отдельный логгер категории `PassportQuality`: фиксируются выбранная стратегия (`PassportQualityStrategy*`), предупреждения по балластным параметрам и все сценарии `Manual overrides ELIS`.  
   - **Тесты:**  
     - Добавлен файл `Tests/Services/Passport/DocUpdateTests.cs` (три интеграционных сценария: manual override небалластного параметра, конфликт балластного value/result, создание ручного метода).  
     - Запуск: `SolutionDir=/home/snafu/projects/ivk/tn_doc dotnet test Tests/Tests.csproj --filter FullyQualifiedName~Tests.Services.Passport.DocUpdateTests`.  
   - **Примеры логов `PassportQuality`:**  
     - `Info|DocUpdate strategy resolved: docId=51234, deviceId=101, strategy=PassportQualityStrategyElis, isElisUsed=True`  
     - `Warn|Балластный параметр: обнаружено расхождение measurement/result (docId=60001, parameter=TempCorrection, measurement='0,45', result='0,40'). Payload не изменён.`  
     - `Info|Manual overrides ELIS: docId=51234, parameter=SulfurCorrection, value='менее 0,5'`  
     - `Trace|История method.TempCorrection проигнорирована: ELIS выключен (docId=unknown)`
  
4. **Фаза 3 — UI и UX (DocumentPassportEditor + PassportQualityTable, реализация инвариантов)**  
  - **3.1. Расширение usePassportEditor и стора**  
    - В `usePassportEditor.ts` добавить поддержку `isBalast`, `resultEditMode` и watchers на `formData["value/result"]`.  
     - Реализовать методы `documentStore.syncBallastParameter` и `documentStore.markManualOverride`, которые одним вызовом обновляют `formData`, `__history`, `__elisFilled` и служебные флаги для отправки в `DocUpdate`.  
    - `qualityParameters` дополнять вычисляемыми предупреждениями (`requiresManualMethod`, `elisOverride`, `manualOverride`) на основании истории (`value/result/method.__history`).
   - **3.2. PassportQualityTable: поведение колонок**  
     - Для балластных параметров Result read-only, а инпут measurement триггерит `syncBallastParameter`, чтобы `value/result` оставались равными и сразу писалась история (Manual/ELIS).  
     - Для небалластных параметров рядом с Result отображать кнопку «Редактировать» (активную), открывающую модалку; после подтверждения вызывать `markManualOverride`. Default value остаётся read-only превью печатной строки.  
   - **3.3. Модалка редактирования результатов**  
     - Создать компонент `ResultEditDialog.vue` (оператор `<`/`>`, поле значения, предпросмотр).  
     - Добавить валидацию локали и формата; при ошибках блокировать «Применить».  
     - При подтверждении вызывать `markManualOverride` с `payloadType = ResultModal`; для балластных модалка не открывается.  
   - **3.4. Модалка ручного метода**  
     - Создать компонент `ManualMethodDialog.vue`: поля имя метода, `limitValueActivate`, `limitValue`, `limitValueString`, чекбокс «Сделать основным».  
     - Интегрировать с `PassportQualityTable`: кнопка рядом с комбобоксом метода открывает модалку.  
     - После сохранения:  
      - Добавлять метод в локальный список `methodOptions`.  
       - Обновлять `store.formData["method.<key>"]` (JSON) и `method.<key>__history` (Source = Manual).  
   - **3.5. Подсветки и предупреждения**  
     - В `useFieldHistory.ts` и компонентах добавить:  
      - Жёлтую подсветку комбобокса и текст «метод отсутствует в словаре», если текущее выбранное наименование метода испытаний отсутствует в справочнике (`Methods` из `CfgEditPassport_GOSTR50.2.040(I).json`).   
   - **3.6. UI/стили и локализация**  
     - Оформить модалки в стиле `material3.css` (см. раздел 6.7): радиусы, цвета, размеры инпутов.  
     - Добавить строки в `TN_Doc/Client/document-editor/src/locales/ru.json` для всех новых подписей, подсказок и предупреждений.  
   - **Проверка после Фазы 3**  
    - В Storybook/тестовом стенде визуально проверить: балластные строки заблокированы по Result, модалки открываются и корректно сохраняют данные.  
    - Пройти руками сценарии 2–4 из раздела 9 (ELIS → ручное редактирование, создание метода, печать) и зафиксировать скриншоты для заказчика.  
  
5. **Фаза 4 — интеграционные проверки, документация и ручные сценарии**  
  - **4.1. Документация**  
    - Обновить `docs/features/field-history.md`, описав, как история (`Source = Manual/ELIS/System`) используется для восстановления происхождения значений без дополнительных полей.  
     - Обновить `docs/elis-summary.md` с описанием того, как ELIS взаимодействует с ручными изменениями (приоритет последнего действия).  
     - В `docs/architecture/document-editor.md` добавить подпункт про `DocumentPassportEditor`, модалки и флаги `isBalast`/`resultEditMode`.  
   - **4.2. Интеграционные и ручные сценарии**  
     - Для ключевых сценариев из раздела 9 (импорт ELIS, Manual overrides, создание ручного метода, печать) убедиться, что:  
       - есть покрытие unit/integration‑тестами на уровне бэкенда (`DocPassport`, `PassportQualityStrategy`, `DocUpdate`),  
       - остальная часть цепочки (UI, взаимодействие пользователя) проверена вручную по чек‑листу.  
     - Подготовить чек‑лист ручного тестирования для QA/эксплуатации с привязкой к конкретным сценариям и устройствам (ELIS on/off).  
   - **4.3. Интеграционная матрица тестов**  
     - Составить таблицу сценариев из раздела 9 и разделить их на: unit, integration и manual.  
     - Убедиться, что для каждого сценария есть либо автоматизированный тест (unit/integration), либо явный ручной сценарий в чек‑листе (кроме чисто UX‑визуальных).  
   - **Проверка после Фазы 4**  
     - Запустить `dotnet test` (все проекты), сохранить отчёты в артефакты/документацию.  
     - Пройти чек‑лист ручного тестирования для стендов ELIS on/off и задокументировать результаты.  
     - Подготовить краткий отчёт по результатам (авто‑ и ручных проверок) и приложить к MR/PR.  
  
6. **Фаза 5 — регресс и релиз**  
   - **5.1. Полный регрессионный прогон**  
     - Выполнить `dotnet build TN_Doc.sln` и `dotnet test /p:CollectCoverage=true`, убедиться в отсутствии новых падений и в приемлемом уровне покрытия для изменённых областей.  
     - Собрать фронтенд: `npm run build` в `TN_Doc/Client/statusbar` и `TN_Doc/Client/document-editor`.  
   - **5.2. Смоук‑тесты на стендах**  
     - Провести smoke‑проверки на стендах с `UseElis = false` и `UseElis = true`:  
       - Создание/редактирование паспорта, сохранение, печать.  
       - Импорт ELIS, ручное редактирование, повторный импорт.  
     - Зафиксировать список устройств/стендов и результаты проверок (таблица в `docs/release/passport_<version>.md`).  
   - **5.3. Release notes и коммуникация**  
     - Подготовить release notes: описание фич (балласт/небалласт, модалки, история), влияние на конфиги, инструкции по миграции и откату.  
     - Приложить скриншоты новых экранов/модалок, указать, нужны ли действия эксплуатаций (запуск миграции конфигов).  
   - **Проверка после Фазы 5**  
     - Убедиться, что все устройства из целевого периметра прошли smoke‑тесты без блокирующих багов.  
     - Получить подтверждение от заказчика по UX (протокол встречи/подпись в системе задач).  
     - Зафиксировать в `docs/release/passport_<version>.md` итоговый статус релиза и известные ограничения/риски.
