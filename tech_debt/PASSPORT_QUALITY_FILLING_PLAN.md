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
   - В `CfgEditPassport_GOSTR50.2.040(I).json` добавить поле `IsBalast` для каждого элемента `Parameters[]`. Отсутствие поля трактуем как `false`.  
   - Балластные показатели: `TempCorrection`, `PressCorrection`, `DensCorrection`, `Dens20Correction`, `Dens15Correction`, `MassWaterFracCorrection`, `Chloride_Salts.Concentration`, `Chloride_Salts.MassFraction`, `Impurity`.  
   - Небалластные показатели: `SulfurCorrection`, `DNP.kPa`, `DNP.mercury_mm`, `Yield_fraction_200`, `Yield_fraction_300`, `Yield_fraction_350`, `Mass_fraction_of_paraffin`, `Mass_fraction_of_hydrogen_sulfide`, `Mass_fraction_of_methyl_and_ethyl_mercaptan`, `Mass_fraction_of_organic_chlorides`.
2. **DTO / JSON / типы на стороне клиента**  
   - Расширить `TN.DocEditor.Passport.QualityParameter`, `QualityParameterSchema`, `Edit.Parameter`, DTO `PassportQualityParameterSchema`, JSON `PassportEditConfig` и ts-типы (`TN_Doc/Client/document-editor/src/types/passport.types.ts`) полем `IsBalast`.  
   - Вместо добавления `IsFromConfig` в `TN.Doc.Edit.Metod` вводим отдельное поле `methodSource`/`source` (enum `'config' | 'lab' | 'manual' | 'elis'`) только в ответах `PassportQualityParameterSchema.MethodOptions` и `PassportQualityParameter.method`. Значение вычисляем на бэкенде по происхождению метода:  
     - `config` — пришёл из `CfgEditPassport.Methods`.  
     - `lab` — сохранён в `LabInfo` с источником ELIS.  
     - `manual` — последняя запись `FieldHistory` по `method.<key>` имеет `Source = Manual`.  
     - `elis` — `FieldHistory` показывает загрузку из ELIS для текущего значения, метод не входит в конфиг (нужен для подсказок).  
   - Клиент продолжает отправлять в `DocUpdate` чистый JSON `Metod`, без `source`; метаданные нужны только для визуализации.
3. **История и паспортные конфиги**  
   - Проверить, что `PassportEditConfig.InitialValues` содержит `__history`/`__elisFilled` для всех новых полей, чтобы модалки могли восстановить источник значений.  
   - Для обратной совместимости ориентироваться только на наличие поля `IsBalast`: если поле отсутствует, считается `false`, сервер использует старую логику.
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
           { "id": 10, "name": "ASTM D1298", "source": "config", "limitValueActivate": false },
           { "id": 0, "name": "Ручной метод 05.2024", "source": "manual", "limitValueActivate": true, "limitValue": 0.5, "limitValueString": "менее 0,5" }
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
   - Шаг 1: обновить `IsBalast` во всех актуальных `TN_Doc/Cfg/CfgEditPassport_*.json` (по списку параметров) через существующие процессы управления конфигурациями, без отдельного миграционного скрипта.  
   - Шаг 2: в `DocPassport` определять стратегию по факту наличия `IsBalast`. Для старых конфигов (`IsBalast` отсутствует) автоматически используем старую ветку — никакой версии хранить не нужно.  
   - Шаг 3: документировать процесс в `docs/configs/passport.md` (описание новых полей, порядок обновления конфигов и требования к их проверке).  
   - Шаг 4: после обновления конфигов прогнать `dotnet test` + smoke (`dotnet run --project TN_Doc/TN_Doc.csproj -- --device <id>`), чтобы убедиться, что паспорт открывается и данные с ELIS подтягиваются без ошибок сериализации.

### 5. Бэкенд: переработка DocPassport
1. **Стратегия поведения**  
   - В `DocPassport` создать внутренний сервис (`PassportQualityStrategy`), который на основе `UseElis` и `IsBalast` решает, как формировать значения/блокировки. Это позволит покрыть стратегию модульными тестами без запуска всего EF-контекста.
2. **Формирование схемы и значений**  
   - В `BuildQualityParametersSchema` прокидывать `IsBalast`, `methodSource`, список методов (`config` + `LabInfo` + `manual`).  
   - `BuildParameterValues` и `BuildParameterMethod` должны учитывать `IsBalast`: measurement/result для балластных всегда синхронны, `result` не перезаписывается ELIS `valueString`, если measurement пришёл вручную.  
   - При генерации данных для UI добавлять признак `ResultEditMode` (`auto`, `modal`, `readonly`) — это упростит работу фронтенда.
3. **Докапливание истории и проверка инвариантов в `DocUpdate`**  
   - При сохранении балластного параметра сервер перепроверяет, что `result.*` совпадает с `value.*`; при расхождении — логирует предупреждение и приводит к measurement.  
   - Для небалластных полей сервер принимает значение из `result.*`, но добавляет в историю запись «Manual overrides ELIS», если последнее значение пришло из ELIS. История пишется через уже существующий `dataArm.AddFieldHistoryEntry`.  
   - Результаты, введённые через модалку, передаются как `Values[]` с `Tag = "PrintValue"`, чтобы текущая обработка (`case PrintValue`) обновила `LabInfo.Value` и `History`.
4. **Ручные методы испытаний**  
   - Используем текущий пайплайн `DocUpdate` → `AddOrUpdateLabInfo`. UI формирует `Values[]` с `Tag = "Metod"` и `Key = "method.<parameterKey>"`, `Value` — сериализованный `TN.Doc.Edit.Metod`.  
   - После десериализации метод помещаем в `LabInfo`. `MethodSource` вычисляем по истории (`Manual` → `manual`, `ELIS` → `elis`). Никаких новых endpoint'ов не требуется.  
   - В `BuildParameterMethod` дополняем `MethodOptions` вручную добавленными методами (которые есть в `LabInfo`, но отсутствуют в конфиге) с `source = manual`.
5. **Индикация отсутствия метода**  
   - Если выбранный метод отсутствует в `MethodOptions`, но значение сохранено в `LabInfo`, сервер добавляет временную опцию с `source = 'legacy'` и явным флагом `requiresManualAction = true`, чтобы UI отобразил предупреждение.  
   - Дополнительно в `PassportEditConfig` возвращаем список `methodWarnings[]` для параметров, где метод исчез из конфигурации.
6. **Логирование и телеметрия**  
   - Добавить подробный лог `PassportQuality` (уровень Trace/Info) с фиксацией стратегий и источников, чтобы можно было анализировать конфликтующие действия ELIS/оператора.  
   - При сбое десериализации модальных данных сервер должен возвращать понятное сообщение (ValidationProblem) вместо silent fallback.

### 6. Фронтенд: DocumentPassportEditor и связанные компоненты
1. **usePassportEditor / store**  
   - Расширить `PassportQualityParameter` объектами `isBalast`, `resultEditMode`, `method.source`.  
   - Избавиться от дублирования логики пересчёта: `recalculateResult` должен принимать `isBalast` и `methodSource`, понимать когда `result` запираем.  
   - `qualityParameters` добавляет warnings (`requiresManualMethod`, `elisOverride`, `manualOverride`) на основании истории (`store.formData["*.history"]`) и `method.source`.
2. **PassportQualityTable**  
   - Для балластных полей блокируем колонку Result и показываем иконку синхронизации. Изменение measurement через инпут автоматически пишет `result.*` и историю (`store.bulkUpdateFields`).  
   - Для небалластных полей рядом с результатом рендерим кнопку «Редактировать» → модалка. Default value отображается в read-only input.
3. **Модалка редактирования результатов**  
   - Содержит селект (`менее`/`более`), поле ввода значения (валидируем разрядность/локаль) и предпросмотр итоговой строки (`«менее 4,0»`).  
   - После подтверждения вызывает `handleResultUpdate` с уже собранной строкой. Параллельно добавляет запись в историю (`__history`) с `source = Manual` и пометкой `payloadType = ResultModal`.  
   - Валидация: превышение допустимых разрядов блокирует кнопку «Применить», `store.canSave` учитывает наличие ошибок.
4. **Модалка ручного метода**  
   - Кнопка рядом с комбобоксом метода. Поля: название, `limitValueActivate`, `limitValue`, `limitValueString`, `isDefault`, чекбокс «Сделать основным».  
   - После сохранения добавляем метод в локальный список `methodOptions` (`source = manual`), записываем JSON в `store.formData["method.<key>"]` и историю (`source = Manual`).  
   - Если имя совпадает с существующим, показываем подтверждение для замены (`method.options` обновляется локально).
5. **Предупреждения и подсветки**  
   - `method.source !== 'config'` → жёлтая подсветка, tooltip «Отсутствует в справочнике / ручной метод».  
   - Если `methodWarnings` от сервера содержит параметр, отображаем баннер с предложением открыть справочник методов.  
   - Для результатов без метода показываем напоминание о необходимости выбрать/создать метод.
6. **Обработка ELIS**  
   - `useElisIntegration` при автозаполнении учитывает `isBalast`: балластные параметры заполняются только `value.*`, `result.*` копируется из measurement.  
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
- Для ручных методов модалка формирует `method.<key>` + историю `method.<key>__history` (source `Manual`). `Value` — сериализованный `TN.Doc.Edit.Metod`. Метод попадает в `LabInfo`, а `PassportEditConfig` вернёт его с `methodSource = "manual"`.  
- `methodSource` присутствует только в ответе `PassportEditConfig` и в типах клиента (`PassportQualityParameterSchema`, `MethodOption` и т.д.). Клиент не отправляет это поле в `DocUpdate`.  
- Результаты модалки передаются через `result.<key>` и историю (`result.<key>__history`). `DocPassport` сам решает, нужно ли продублировать значение в measurement (балластные параметры) или оставить раздельным (небалластные).  
- Для интеграции с ELIS сохраняем полный протокол в `__elisProtocol` (как и сейчас) + историю `Source = ELIS`, чтобы `methodSource = "elis"` можно было вычислить на бэкенде без дополнительных полей в DTO.

### 8. Тестирование и наблюдаемость
- **Backend**:  
  - Написать юниты на новую стратегию (`PassportQualityStrategyTests`) с кейсами ELIS on/off, балласт/небалласт, ручные методы.  
  - Добавить интеграционные тесты на `DocUpdate` (моки `CorrectionData`) в `Tests/Services/Passport` для сценариев «ручной метод + история», «ELIS → ручное переопределение».  
  - Проверить сериализацию `methodSource` и `IsBalast` через `DocPassportTests`.
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
  - `BuildQualityParameters`/`BuildQualityParametersSchema` возвращают признак `isBalast`, `resultEditMode`, `methodSource` и отдают методы в порядке `config → lab → manual`.  
  - `DocUpdate` гарантирует, что при сохранении балластного параметра `value.* == result.*`; в лог попадает предупреждение при попытке сохранить расхождение.  
  - Для небалластных параметров ручное редактирование результата создаёт запись истории `Manual overrides ELIS`, если предыдущий источник был `ELIS` (проверяется в тесте `DocUpdateManualOverridesElis`).  
  - `PassportEditConfig` для устройства без ELIS/старой схемы совпадает с текущим контрактом (проверяется снапшот-тестом).
- **Фронтенд (`usePassportEditor.ts`, `PassportQualityTable.vue`, `DocumentPassportEditor.vue`)**  
  - Балластные строки отображают заблокированный Result и синхронизируются при вводе measurement (unit-тест `usePassportEditor.balast.sync.spec.ts`).  
  - Модалка результатов формирует строки «менее/более + число» и записывает историю; сохранение блокируется при неверном формате.  
  - В комбобоксе метода отображается источник (tooltip + подсветка), а кнопка ручного метода создаёт новую опцию с `source = manual`.  
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
  
2. **Фаза 1 — стратегия и контракты (бэкенд)**  
   - **1.1. Введение стратегии качества паспорта**  
     - В `tn.docgeneral/Passport/DocPassport.cs` выделить интерфейс `IPassportQualityStrategy` (ELIS On/Off + балласт/небалласт):  
       - Определить методы для формирования схемы (`BuildQualityParametersSchema`), значений (`BuildParameterValues`), методов (`BuildParameterMethod`) и `ResultEditMode`.  
       - Реализовать минимум две реализации: `PassportQualityStrategy` (текущее/нормальное поведение, флаг `IsBalast` присутствует в контракте, но не влияет на расчёт) и `PassportQualityStrategyElis` (новое поведение, использующее `IsBalast` для разделения балластных/небалластных параметров).  
     - Добавить фабрику/решатель стратегии (_внутри_ `DocPassport`, без DI наружу) на основе `UseElis`.  
   - **1.2. Расширение DTO и контрактов**  
     - В проектах `TN.DocEditor.Passport.*` и `TN.DocEditor.Passport.Dto/*.cs` расширить модели `QualityParameterSchema`, `PassportQualityParameterSchema` полями `IsBalast`, `ResultEditMode`, `MethodSource`.  
     - Обновить маппинг в `DocPassport.BuildQualityParameters*`, чтобы новые поля заполнялись из стратегии.  
     - Синхронизировать типы на фронтенде: обновить `TN_Doc/Client/document-editor/src/types/passport.types.ts` (интерфейсы `PassportEditConfig`, `QualityParameterSchema`, `MethodOption`).  
   - **1.3. Обратная совместимость**  
     - Убедиться, что при отсутствии `IsBalast` и `ResultEditMode` сериализатор не меняет JSON (старые клиенты/снапшоты остаются валидными).  
     - В реализации `PassportQualityStrategy` возвращать те же данные, что и до рефакторинга (можно опираться на снапшот‑тесты `DocPassportTests`); при этом `IsBalast` может присутствовать в схеме, но логика расчёта его игнорирует.  
   - **1.4. Тесты стратегии**  
     - Добавить `PassportQualityStrategyTests` с кейсами:  
       - ELIS выключен — используется `PassportQualityStrategy`, поведение совпадает с текущим, а наличие/отсутствие `IsBalast` в конфиге не влияет на расчёт.  
       - ELIS включен и конфиг содержит `IsBalast` — используется `PassportQualityStrategyElis`, `IsBalast` и `ResultEditMode` заполнены и влияют на поведение.  
       - Порядок методов: `config → lab → manual`.  
   - **Проверка после Фазы 1**  
     - Запустить `dotnet test Tests/Services --filter PassportQuality` и убедиться, что новые тесты стратегии зелёные.  
     - Снять JSON‑снапшот `PassportEditConfig` для пары устройств (ELIS on/off) до и после изменений, сравнить дифф: новые поля либо отсутствуют, либо равны ожидаемым значениям.  
     - В тестовом стенде открыть паспорт в UI и убедиться, что интерфейс не изменил поведение (result/measurement редактируются как раньше).  
  
3. **Фаза 2 — сохранение и история (бэкенд)**  
   - **2.1. Расширение DocUpdate / CorrectionData**  
     - В `DocPassport.DocUpdate(int id, Dictionary<string, object> values)` и `BuildCorrectionData` убедиться, что:  
       - История `__history` корректно маппится в `Dictionary<string, List<FieldHistoryEntry>>`.  
       - При ELIS Off история игнорируется, логируется `Trace`‑сообщение (как уже заложено).  
     - Добавить в `CorrectionData.Values` обработку новых тегов, если нужны для модалок (`PrintValue`, `Metod`, `History`).  
   - **2.2. Обработка балластных/небалластных параметров при сохранении**  
     - В `DocUpdate(string jsonData, QualityPassport? elisProtocol)` после десериализации `CorrectionData` внедрить вызов стратегии/помощника, который:  
       - Для балластных параметров проверяет инвариант `value.* == result.*`; при расхождении логирует `Warn` и приводит `result.*` к `value.*`.  
       - Для небалластных параметров принимает `result.*` как основное, дописывая историю «Manual overrides ELIS», если предыдущее значение было из ELIS.  
     - В `AddOrUpdateLabInfo` учитывать новые источники (`Manual`, `ELIS`) и правильно проставлять `ElisFilled`.  
   - **2.3. Вычисление methodSource и история методов**  
     - Реализовать функцию/класс, вычисляющий `methodSource` по данным `LabInfo` и `FieldHistoryEntry`:  
       - `config` — метод из конфига, история пустая/служебная.  
       - `lab`/`elis` — последнее изменение с `Source = ELIS`.  
       - `manual` — последнее изменение с `Source = Manual`.  
     - В `BuildParameterMethod` дополнять `MethodOptions` ручными методами (из `LabInfo`), проставлять `source` и флаги `requiresManualAction`, если метод не найден в конфиге.  
   - **2.4. Логирование и диагностика**  
     - Добавить категорию логов `PassportQuality`:  
       - Логировать выбор стратегии (`PassportQualityStrategy`/`PassportQualityStrategyElis`), устройство, признак `UseElis`.  
       - Логировать случаи конфликтов measurement/result (балластные) и сценарии `Manual overrides ELIS`.  
   - **2.5. Тесты сохранения**  
     - Добавить `Tests/Services/Passport/DocUpdateTests.cs` с интеграционными сценариями:  
       - Импорт ELIS → ручное изменение результата небалластного параметра → повторный импорт (значение пользователя сохраняется).  
       - Балластный параметр: попытка сохранить расхождение value/result приводит к выравниванию и записи `Warn`‑лога.  
       - Создание ручного метода через `Tag = "Metod"` + история, `methodSource = manual`.  
   - **Проверка после Фазы 2**  
     - Запустить `dotnet test Tests/Services/Passport/DocUpdateTests.cs` и убедиться в прохождении всех сценариев.  
     - На тестовом стенде повторить сценарии из раздела 9 (Manual overrides, ELIS import) через UI и проверить консоль/файлы логов на наличие ожидаемых записей.  
     - Проверить, что при `UseElis = false` сохранение паспорта не пишет историю и продолжает работать по старой модели.  
  
4. **Фаза 3 — UI и UX (DocumentPassportEditor + PassportQualityTable)**  
   - **3.1. Расширение usePassportEditor и стора**  
     - В `usePassportEditor.ts` добавить поддержку полей `isBalast`, `resultEditMode`, `method.source` в `qualityParameters`.  
     - Вынести общий пересчёт `recalculateResult` с учётом `isBalast` и `methodSource` (когда блокируем/разрешаем редактирование).  
     - В `documentStore` добавить удобные методы для пакетного обновления `__history` при действиях модалок.  
   - **3.2. PassportQualityTable: поведение колонок**  
     - Для балластных параметров:  
       - Заблокировать колонку Result (read‑only инпут).  
       - При изменении measurement через инпут автоматически обновлять `result.*` и историю (`bulkUpdateFields`).  
       - Отрисовывать иконку синхронизации и tooltip «Балластный параметр — результат синхронизируется с измерением».  
     - Для небалластных параметров:  
       - Добавить кнопку «Редактировать» в ячейку Result → открытие модалки редактирования результата.  
       - Отображать фактическое значение в формате, пригодном для печати.  
   - **3.3. Модалка редактирования результатов**  
     - Создать компонент `ResultEditDialog.vue` (или аналог) с полями: оператор (`<`, `>`), числовое значение, предпросмотр строки (`"менее 4,0"`).  
     - Реализовать валидацию локали (запятая как разделитель), диапазона и разрядности; при ошибках блокировать кнопку «Применить».  
     - При подтверждении:  
       - Обновлять `result.<key>` и при необходимости `value.<key>` (для балластных).  
       - Добавлять запись в историю (`result.<key>__history`) с `source = Manual` и `payloadType = ResultModal`.  
   - **3.4. Модалка ручного метода**  
     - Создать компонент `ManualMethodDialog.vue`: поля имя метода, `limitValueActivate`, `limitValue`, `limitValueString`, чекбокс «Сделать основным».  
     - Интегрировать с `PassportQualityTable`: кнопка рядом с комбобоксом метода открывает модалку.  
     - После сохранения:  
       - Добавлять метод в локальный список `methodOptions` с `source = manual`.  
       - Обновлять `store.formData["method.<key>"]` (JSON) и `method.<key>__history` (Source = Manual).  
   - **3.5. Подсветки и предупреждения**  
     - В `useFieldHistory.ts` и компонентах добавить:  
       - Жёлтую подсветку для методов с `method.source !== 'config'` и tooltip «Ручной/ELIS метод».  
       - Баннер/иконку предупреждения, если от бэкенда пришёл `methodWarnings[]` для параметра.  
       - Напоминание, если результат заполнен, а метод отсутствует.  
   - **3.6. UI/стили и локализация**  
     - Оформить модалки в стиле `material3.css` (см. раздел 6.7): радиусы, цвета, размеры инпутов.  
     - Добавить строки в `TN_Doc/Client/document-editor/src/locales/ru.json` для всех новых подписей, подсказок и предупреждений.  
   - **3.7. Тесты UI**  
     - Написать unit‑тесты на `usePassportEditor` (балласт/небалласт, пересчёт result, флаги предупреждений).  
     - Написать компонентные тесты для модалок (валидация, запись истории, поведение кнопки «Применить»).  
   - **Проверка после Фазы 3**  
     - Запустить `npm run test -- usePassportEditor` и тесты компонентов модалок, убедиться в зелёном прогоне.  
     - В Storybook/тестовом стенде визуально проверить: балластные строки заблокированы по Result, модалки открываются и корректно сохраняют данные.  
     - Пройти руками сценарии 2–4 из раздела 9 (ELIS → ручное редактирование, создание метода, печать) и зафиксировать скриншоты для заказчика.  
  
5. **Фаза 4 — end‑to‑end проверки и документация**  
   - **4.1. Документация**  
     - Обновить `docs/features/field-history.md` с описанием новых источников (`config/lab/manual/elis`) и поведения истории для result/методов.  
     - Обновить `docs/elis-summary.md` с описанием того, как ELIS взаимодействует с ручными изменениями (приоритет последнего действия).  
     - В `docs/architecture/document-editor.md` добавить подпункт про `DocumentPassportEditor`, модалки и флаги `isBalast`/`resultEditMode`.  
   - **4.2. E2E‑сценарии**  
     - Добавить/обновить e2e тесты (Cypress/Playwright):  
       - Импорт ELIS → авто‑заполнение полей + протокол в `__elisProtocol`.  
       - Ручное изменение результата небалластного параметра через модалку → повторный импорт ELIS (результат сохраняется).  
       - Создание ручного метода → повторная загрузка паспорта (метод отображается с пометкой `manual`).  
     - Настроить фикстуры данных (mock‑ответы ELIS, конфиги с `IsBalast`) для стабильного прогона e2e.  
   - **4.3. Интеграционная матрица тестов**  
     - Составить таблицу сценариев из раздела 9 и разделить их на: unit, integration, e2e.  
     - Убедиться, что для каждого сценария есть как минимум один автоматизированный тест (кроме чисто UX‑визуальных).  
   - **Проверка после Фазы 4**  
     - Запустить `dotnet test` (все проекты) и `npm run test:e2e` в тестовом окружении, сохранить отчёты в артефакты.  
     - Проверить, что e2e‑сценарии для ELIS on/off проходят, а UI‑регрессия не выявлена.  
     - Подготовить краткий отчёт по результатам прогонов и приложить к MR/PR.  
  
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
