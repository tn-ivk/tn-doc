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
   - Добавить версионирование схемы (например, `PassportEditConfig.schemaVersion = 2`) для будущих миграций.

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
- REST-поток остаётся прежним: `POST /api/documents/{deviceId}/Passport/save` (`DocUpdate`). Payload содержит `CorrectionData.Values` с тегами `Metod`, `Value`, `PrintValue`, `DocNum`, `History`.  
- Для ручных методов модалка формирует `method.<key>` + историю `method.<key>__history` (source `Manual`). `Value` — JSON `TN.Doc.Edit.Metod`.  
- Новый `methodSource` присутствует только в ответе `PassportEditConfig` и в типе `PassportQualityParameter`. Его не нужно отправлять на бэкенд.  
- Результаты модалки передаются через `result.<key>` и историю (`result.<key>__history`). `DocPassport` сам решит, нужно ли продублировать значение в measurement (балластные) или оставить отдельным.

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

### 9. Риски и вопросы
- Ручные методы хранятся в `LabInfo` и в справочник попадают только через модалку — важно документировать и протестировать, чтобы не появлялись «висящие» методы.  
- Строковый формат результата («менее/более + число с запятой») должен совпадать у бэкенда и фронтенда, иначе печать даст расхождения.  
- Макеты модалок необходимо согласовать с заказчиком до реализации (см. п.6.7).  
- История изменений из модалок сохраняется стандартно (`__history`), но нужно явно описать это в документации, чтобы избежать ложных ожиданий про логи ELIS.  
- Приоритет последнего действия сохраняется: ручное изменение перекрывает данные ELIS, `FieldHistory` фиксирует источник.  
- Нужны ли дополнительные предупреждения оператору при перезаписи ELIS-значений? Пока UI просто подсвечивает источник, но заказчик может потребовать подтверждение.

### 10. Шаги внедрения
1. **Фаза 0 — подготовка конфигов**: добавить `IsBalast`, поднять `schemaVersion`, обновить документацию по конфигам и договориться о миграции с эксплуатацией.  
2. **Фаза 1 — стратегия и контракты**: внедрить `PassportQualityStrategy`, прокинуть `IsBalast`/`methodSource` во все DTO и JSON, добавить логи.  
3. **Фаза 2 — сохранение и история**: обновить `DocUpdate`, чтобы он синхронизировал балластные результаты, корректно писал историю методов/результатов, вычислял `methodSource`.  
4. **Фаза 3 — UI и UX**: реализовать модалки, подсветки, предупреждения, обновить `usePassportEditor`, `PassportQualityTable`, интеграцию с ELIS.  
5. **Фаза 4 — тесты и документация**: покрыть новую логику тестами (`dotnet test`, `npm run test`, e2e), обновить `docs/features/field-history.md`, `docs/elis-summary.md`, `docs/architecture/document-editor.md`.  
6. **Фаза 5 — регресс и релиз**: собрать решение (`dotnet build`, `npm run build`), прогнать регрессию с включённым/выключенным ELIS, подготовить release notes и скриншоты UI.
