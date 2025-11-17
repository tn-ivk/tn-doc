## План доработки логики заполнения паспорта качества

### 1. Цель и ожидаемый результат
- Разделить алгоритмы заполнения/редактирования паспорта качества в зависимости от включённости ELIS (`CfgApp.json` → `UseElis` для конкретного устройства).
- Обеспечить корректное поведение балластных и небалластных показателей с возможностью конфигурирования через `CfgEditPassport_GOSTR50.2.040(I).json`.
- Добавить UX-инструменты, позволяющие операторам вручную вносить методы испытаний и результирующие строки без нарушения синхронизации с историей изменений.

### 2. Точки изменений
- Бэкенд: `tn.docgeneral/Passport/DocPassport.cs`, связанные модели (`DataARM`, `LabInfo`, `CorrectionData`, `EditData`, `TN.DocEditor.Passport.*`), конфигурации (`TN_Doc/Cfg/CfgEditPassport_GOSTR50.2.040(I).json`, при необходимости `CfgApp.json` описание).
- Фронтенд: `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue`, компоненты `PassportQualityTable`, `FormFieldWithHistory`, стора `documentStore`, композиции `usePassportEditor`, `useFieldHistory`, `useElisIntegration`.
- UI-инфраструктура: модальные окна и кнопки действий (новые компоненты под модалки редактирования метода и результата).

### 3. Разделение сценариев ELIS Off/On
1. **ELIS выключен**  
   - Сохраняем текущую поведенческую модель: measurement/result редактируются как сейчас, история полей ведётся только локально, методы выбираются из справочника без дополнительной логики.
   - Кодовую базу упорядочить так, чтобы все проверки `IsUsedElis` концентрировались в отдельных методах/стратегиях (удобнее покрывать тестами).
2. **ELIS включен**  
   - Активируем новую ветку логики: обработка `IsBalast`, жёсткая синхронизация measurement/result для балластных, новый UX для небалластных, работа с ручными методами и предупреждениями.

### 4. Обновление конфигураций
1. **Расширение схемы параметров качества**
   - В `CfgEditPassport_GOSTR50.2.040(I).json` добавить поле `IsBalast` для каждого элемента `Parameters[]`.
   - Проверить поддержку значения по умолчанию (false) для старых конфигов, при отсутствии ― считать небалластным.
   - Балластные показатели: `TempCorrection`, `PressCorrection`, `DensCorrection`, `Dens20Correction`, `Dens15Correction`, `MassWaterFracCorrection`, `Chloride_Salts.Concentration`, `Chloride_Salts.MassFraction`, `Impurity`.
   - Небалластные показатели: `SulfurCorrection`, `DNP.kPa`, `DNP.mercury_mm`, `Yield_fraction_200`, `Yield_fraction_300`, `Yield_fraction_350`, `Mass_fraction_of_paraffin`, `Mass_fraction_of_hydrogen_sulfide`, `Mass_fraction_of_methyl_and_ethyl_mercaptan`, `Mass_fraction_of_organic_chlorides`.
2. **Модели / DTO / JSON**  
   - Расширить `TN.DocEditor.Passport.QualityParameter`, `QualityParameterSchema`, `Edit.Parameter`, а также связанные DTO `PassportQualityParameterSchema` и `PassportQualityParameter` (ts-тип из `TN_Doc/Client/document-editor/src/types/passport.types.ts`) полем `IsBalast`.  
   - Обновить `DocPassport.BuildQualityParameters` и `DocPassport.BuildQualityParametersSchema`, чтобы новое поле прокидывалось во все уровни сериализации (DTO → JSON → `PassportEditConfig`).  
   - Убедиться, что `PassportEditConfig` и данные, отдаваемые `usePassportEditor`, содержат `isBalast`, иначе фронтенд не сможет включить балластную логику.
- Добавить булев признак `IsFromConfig` во все DTO/JSON структуры, описывающие методы (`TN.Doc.Edit.Metod`, `PassportQualityParameterSchema.MethodOptions`, `PassportQualityParameter.method`, `passport.types.ts → MethodOption`). Значение `true` ставится для элементов, пришедших из конфигурации/`LabInfo`, `false` — для ручных значений, в том числе восстановленных из истории. `DocPassport` сериализует флаг обратно в `PassportEditConfig`, чтобы фронтенд различал источники.

### 5. Бэкенд: переработка DocPassport
1. **Формирование `QualityParameterSchema`**
   - При загрузке схемы заполнять `IsBalast` из конфигурации.
   - Переписать `BuildQualityParameters` и `BuildQualityParametersSchema`, чтобы поле попадало в итоговый JSON `PassportQualityParameterSchema`/`PassportEditConfig`, который читает `DocumentPassportEditor` → `usePassportEditor`.
   - Для ELIS-on фронтенд самостоятельно определяет блокировки и вспомогательные состояния (`ResultReadOnly`, `ResultRequiresModal`, предупреждения по методам) на основе полученной схемы и значений.
2. **Обработка измерений и результатов**
   - При ELIS-on и `IsBalast == true`:  
     - В `BuildParameterValues` обеспечивать равенство Measurement/Result (результат берётся из `measurementValue` либо из загрузки ELIS `value`).  
     - Сохранение (`DocUpdate`, `SaveDocument`) принимает значения как есть; синхронизация `value.*` и `result.*` полностью выполняется на фронтенде (DocumentPassportEditor/PassportQualityTable).
   - При ELIS-on и `IsBalast == false`:  
     - Сохранять текущую схему вычисления результата, но фиксировать, что ручные изменения происходят через новое поле (см. UI).  
     - При загрузке из ELIS `valueString` записывается в `result.*`, даже если measurement идёт из `value`.
3. **История изменений**
   - Убедиться, что добавление историй для результатов/методов учитывает новые сценарии (когда результат редактируется модалкой, источник должен быть `Manual`).  
   - При ручном изменении значений, ранее загруженных из ELIS, история фиксирует событие «Manual overrides ELIS» (source `Manual`, previous source `ELIS`), чтобы аудит явно видел факт перезаписи.
4. **Ручное добавление методов испытаний**
   - Используем текущий пайплайн `DocUpdate` → `AddOrUpdateLabInfo` (`tn.docgeneral/Passport/DocPassport.cs:274-333` и `:616+`), который уже принимает `method.*` JSON и создаёт/обновляет `LabInfo`. Никаких новых endpoint'ов не требуется.  
- Модалка должна формировать элемент `Values[]` с `Tag = "Metod"` и `Key = "method.<parameterKey>"`, где `Value` — сериализованный `TN.Doc.Edit.Metod` (Id, Name, LimitValue*, IsDefault, LimitValueString/Activate, `IsFromConfig = false`). История передаётся в `History` с источником `Manual`, чтобы `DocUpdate` корректно отметил автора и `ElisFilled`.  
- После записи значения в `store.formData` сохраняем документ через существующий `DocumentEditController.Save`; `DocUpdate` разберёт JSON, положит метод в `DataARM.LabInfo` и, при необходимости, создаст новую запись.  
- При сериализации `DocPassport` заполняет `MethodOptions` так, чтобы методы из конфигурации/`LabInfo` имели `IsFromConfig = true`, а элементы, сохранённые вручную, — `false`. Мы больше не «обогащаем» справочник молча: UI получает полный список с флагами и сам решает, как отображать.
5. **Индикация отсутствия метода**
   - Проверку наличия метода выполнять на фронте с учётом флага `IsFromConfig`: `PassportQualityTable` получает список `MethodOptions` из `PassportEditConfig`, находит выбранный метод и считывает `isFromConfig`.  
   - Если значение `false` (ручной метод или восстановленное вручную поле), UI подсвечивает поле и показывает предупреждение без дополнительных запросов к серверу; если метод совсем не найден в списке, считается, что он «устарел» и также подсвечивается.

### 6. Фронтенд: DocumentPassportEditor и связанные компоненты
1. **Синхронизация measurement/result**
   - В `usePassportEditor` хранить сведения о `IsBalast`, пришедшие из `PassportQualityParameterSchema`/`PassportEditConfig`.  
   - Для балластных полей:  
     - Disable инпут Result; при изменении Measurement мгновенно обновлять `result.*` и `result.*__history`.  
     - При загрузке ELIS записывать `value` в обе колонки (а не `valueString`).
   - Для небалластных:  
     - Поле Result заблокировано, рядом кнопка «Редактировать».  
     - При ручном вводе Measurement обновлять Result по текущему алгоритму (синхронно).  
     - При автозаполнении ELIS `valueString` всегда заменяет Result.
2. **Кнопка редактирования результатов**
   - Добавить в таблицу компонент/иконку рядом с Result.  
   - Модалка: select (`менее`/`более`), numeric input с валидацией, предпросмотр конечной строки.  
   - Результат записывается в `result.*`, отмечается флаг `__manual`, добавляется запись в историю (source `Manual`), фронт обновляет `store.formData`.  
   - Итоговое значение всегда имеет формат строки `«менее 4,0»` / `«более 0,5»` (локализованные слова + число с запятой), чтобы соответствовать требованиям печати.
3. **Кнопка ручного метода испытания**
   - UI: кнопка рядом с комбобоксом.  
- Модалка: поля (название, `limitValueActivate`, числовое значение или строка, `isDefault`). Дополнительно даём чекбокс «Сделать основным» и подсказку о роли `LimitValueString`.  
- После подтверждения сериализуем ввод в структуру `Metod`, устанавливаем `isFromConfig = false`, записываем её в `store.formData["method.<key>"]` и добавляем запись истории `store.formData["method.<key>__history"]` со `source = Manual`. `usePassportEditor` (`TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts:62-100`) добавляет новую запись в локальные `methodOptions`, но сохраняет флаг `isFromConfig = false`, чтобы таблица могла подсветить ручной метод.
   - Сохранение выполняется тем же действием, что и изменение остальных полей (`DocumentPassportEditor` → `documentStore.saveDocument`), значит, на сеть идёт стандартный `DocUpdate` запрос.
4. **Предупреждение об отсутствии метода**
- После загрузки параметра UI проверяет `selectedMethod?.isFromConfig`: если флаг `false`, подсвечиваем селект в жёлтый и показываем текст `отсутствует в справочнике`. Для совместимости со старыми данными оставляем fallback — при полном отсутствии записи в `methodOptions` подсветка срабатывает автоматически.
   - Дать ссылку/кнопку на модалку ручного добавления для быстрого исправления.
5. **Валидация точности Measurement**
   - Нормализация и сравнение значений уже реализованы в `usePassportEditor.normalizeValue` и `useFieldHistory.trackManualChange`; при расширении логики точности изменяем эти util-методы и переиспользуем их в местах вызова, не дублируя проверки внутри `FormFieldWithHistory`.  
   - Дополнить unit-/компонентными тестами для `usePassportEditor` и `useFieldHistory`, а также е2е сценарием: превышение допустимых разрядов блокирует сохранение (`store.canSave = false`) и подсвечивает поле.
6. **Хранение полного протокола ELIS**
   - Уже реализовано через `__elisProtocol`, но проверить, что новые модальные изменения не затирают связанные флаги (`__history`, `__elisFilled`).
7. **Модальные окна (UX)**
   - Использовать стили из `material3.css`: фон `var(--md-surface)`, границы `var(--md-outline)`, радиус `var(--md-radius)`, типографику из `DESIGN_DOCUMENTATION.md`.  
   - Хедер: цвет текста `var(--md-text)`, вспомогательный текст `var(--md-text-secondary)`, кнопки действия — `btn-primary`/`btn-outline-primary`.  
   - Элементы формы: PrimeVue inputs/dropdowns с токенами из `docs/ui-design.md` (высота 35px, фокус-стиль `var(--md-primary)`), подсветка предупреждений `var(--md-warning)`.  
   - Для кнопок «Сохранить метод»/«Применить результат» отображать статус через `ProgressSpinner` в цветах `--md-primary`.

### 7. API и модель данных
- Остаётся единый REST-поток `POST /api/documents/{deviceId}/Passport/save` (или текущий `save`), который принимает `CorrectionData.Values`. Для ручных методов добавляем описание того, как модалка формирует пары `method.<parameterKey>` + `method.<parameterKey>__history`.  
- Формат `Value` — JSON `TN.Doc.Edit.Metod`: при создании достаточно `Id = 0`, `Name`, `LimitValueActivate`, `LimitValue` **или** `LimitValueString`, `IsDefault`. Атрибуты `Use`/`IdParameter` задаём из UI при необходимости (по конфигу).  
- Валидация дублей выполняется на клиенте до сохранения: если имя уже существует в `method.options`, модалка требует подтверждения, но после подтверждения всё равно отправляет через `DocUpdate`, чтобы `AddOrUpdateLabInfo` синхронизировал `LabInfo` и историю. Дополнительных миграций/кэшей не требуется — реестр методов по-прежнему живёт в `LabInfo`.

### 8. Риски и вопросы
- Новые методы всегда фиксируются в `LabInfo`, а в справочник заносятся только через ручную модалку — важно описать и протестировать этот поток, чтобы не появлялись «висящие» методы.  
- Формат строки результата закреплён («менее/более + число с запятой») — нужно убедиться, что локализация чисел и печать поддерживают одинаковое представление.  
- UX модалок определён (см. п.6.7), но требуется макет/прототип для согласования с заказчиком до реализации.  
- История изменений, полученная из модалок, сохраняется стандартно в `__history`; дополнительных интеграций с ELIS-отчётами не требуется, однако стоит задокументировать это, чтобы избежать двусмысленных ожиданий.  
- Приоритет имеет последнее действие: если после импорта ELIS оператор редактирует поле вручную, именно ручное значение уходит в сохранение, а история фиксирует факт перезаписи ELIS записи.  
- Дополнительные предупреждения оператору при перезаписи ELIS значений пока не требуются (UI просто подсветит источник в истории).

### 9. Шаги внедрения
1. Подготовить расширения конфигов и моделей: добавить `IsBalast` в `CfgEditPassport_GOSTR50.2.040(I).json`, в `DocPassport.BuildQualityParameters*`, DTO (`QualityParameter`, `QualityParameterSchema`, `PassportQualityParameterSchema`) и фронтовые типы (`passport.types.ts`, `PassportEditConfig`), чтобы JSON для `usePassportEditor` сразу содержал признак.  
2. Реализовать новую ветку логики в `DocPassport` (поддержка `IsBalast`, переработка `BuildParameterValues`, `DocUpdate`).  
3. Реализовать поток ручных методов через существующий `DocUpdate`: формировать `method.*` payload + историю и проверить, что `AddOrUpdateLabInfo` отражает изменения в `LabInfo`.  
4. Обновить фронтенд-компоненты (таблица, модалки, подсветки, валидации).  
5. Обновить документацию (`docs/features/field-history.md`, `docs/elis-summary.md`, новый раздел в `docs/architecture/document-editor.md`).  
6. Выполнить сборку (`dotnet build`, `npm run build` для UI) и регрессию с включённым/выключенным ELIS.


