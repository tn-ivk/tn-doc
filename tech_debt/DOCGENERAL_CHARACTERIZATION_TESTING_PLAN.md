# План: Characterization + интеграционные тесты для `tn.docgeneral`

## Краткое резюме

- В проекте уже есть рабочая NUnit-основа: `Tests.Unit`, `Tests.Integration`, `Tests.Shared` (`TN_Doc.sln:106`, `TN_Doc.sln:108`, `TN_Doc.sln:110`).
- Покрытие сильное по контроллерам/сервисам, но почти нет characterization по документным DLL-модулям `tn.docgeneral`.
- Основная рекомендация: эволюционно расширять существующие `Tests.Integration` и `Tests.Shared`, без построения «параллельного мира».
- В `Tests.Shared` уже есть полезная база для reuse: `BaseDocumentTest<T>`, `TestDataFixture`, `MockConfigHelper` (`Tests/Tests.Shared/BaseTestClass.cs:239`, `Tests/Tests.Shared/Fixtures/TestDataFixture.cs:10`, `Tests/Tests.Shared/Fixtures/MockConfigHelper.cs:10`).
- Golden-master строить на реальных данных БД: `GetList -> GetViewDoc/GetEditDoc`, с обязательной нормализацией нестабильных полей.
- Для high-risk модулей добавить семантические проверки поверх snapshot-сравнения, чтобы не «заморозить баг».
- Отдельный NUnit-проект нужен только при достижении порогов по времени/секретам/изолированию тяжёлых DB-тестов.
- Перед пилотом исправить CI-фильтры тестов (особенно integration), затем разделить быстрые/медленные/DB-зависимые наборы и ввести прозрачную политику обновления baseline.
- Базовый roadmap: 2 недели (пилот + high ядро), расширенный: 4 недели (масштабирование на большую часть модулей).

---

## 1. Текущее состояние тестовой архитектуры

### 1.1 Тестовые проекты и зависимости

- `Tests/Tests.Unit/Tests.Unit.csproj` (NUnit + Moq + InMemory + Shared refs) (`Tests/Tests.Unit/Tests.Unit.csproj:15`).
- `Tests/Tests.Integration/Tests.Integration.csproj` (NUnit + WebApplicationFactory + InMemory) (`Tests/Tests.Integration/Tests.Integration.csproj:13`).
- `Tests/Tests.Shared/Tests.Shared.csproj` (общие хелперы/фикстуры) (`Tests/Tests.Shared/Tests.Shared.csproj:1`).

Оба тестовых проекта уже ссылаются на:

- `TN_Doc/TN_Doc.csproj` (`Tests/Tests.Unit/Tests.Unit.csproj:36`, `Tests/Tests.Integration/Tests.Integration.csproj:31`)
- `tn.docgeneral/TN.DocGeneral/TN.DocGeneral.csproj` (`Tests/Tests.Unit/Tests.Unit.csproj:37`, `Tests/Tests.Integration/Tests.Integration.csproj:32`)
- `tn.docgeneral/tn.utils/TN.Utils/TN.Utils.csproj` (`Tests/Tests.Unit/Tests.Unit.csproj:38`, `Tests/Tests.Integration/Tests.Integration.csproj:33`)

### 1.2 Что уже покрыто

- Unit-фокус в основном на контроллерах и сервисах (`Tests/Tests.Unit/controllers/*`, `Tests/Tests.Unit/Services/*`).
- В `Tests.Unit` уже есть полезные characterization-элементы для базовых методов `DocGeneral` (`Tests/Tests.Unit/Services/DocGeneralTests.cs:7`).
- Интеграционные тесты сейчас преимущественно smoke/DI/endpoint (`Tests/Tests.Integration/Controllers/*`, `Tests/Tests.Integration/Data/DatabaseIntegrationTests.cs:9`).

### 1.3 Слабые места

- `BaseDocumentTest<T>` реализован, но сейчас не наследуется тестами (`Tests/Tests.Shared/BaseTestClass.cs:239`), из-за чего готовые assert-хелперы не дают эффекта в текущем покрытии.
- InMemory-интеграции не покрывают реальные особенности модулей, которые завязаны на MySQL-схему и `DESCRIBE` (`tn.docgeneral/TN.DocGeneral/General.cs:499`).
- Нет централизованного подхода к baseline/snapshot и нормализации JSON/HTML.
- В workflows фильтры namespace не соответствуют реальным namespace тестов:
  - Unit-фильтр: `Tests.Controllers|Tests.Services|Tests.Configs` вместо `Tests.Unit.*` (`.github/workflows/tests-on-push.yml:139`, `.github/workflows/build-and-package.yml:149`).
  - Integration-фильтр: `Tests.Libraries.*`, при том что namespace `Tests.Libraries.*` в репозитории отсутствует (`.github/workflows/tests-on-push.yml:213`, `.github/workflows/build-and-package.yml:219`).
  - Реальные namespace: `Tests.Unit.*`, `Tests.Integration.*` (`Tests/Tests.Unit/Services/DocGeneralTests.cs:5`, `Tests/Tests.Integration/IntegrationTestBase.cs:10`).

### 1.4 Важные архитектурные точки расширения

- Реальная точка исполнения документных модулей: `HomeController.GetDoc/GetDocEdit/SaveDoc/UpdateDoc` (`TN_Doc/Controllers/HomeController.cs:475`, `TN_Doc/Controllers/HomeController.cs:549`, `TN_Doc/Controllers/HomeController.cs:576`, `TN_Doc/Controllers/HomeController.cs:596`).
- Динамическая загрузка DLL-модулей через `CachedDocModuleLoader` (`TN_Doc/Startup.cs:62`, `tn.docgeneral/TN.DocGeneral/Services/CachedDocModuleLoader.cs:19`).
- Базовый контракт всех модулей в `DocGeneral` (`tn.docgeneral/TN.DocGeneral/General.cs:182`, `tn.docgeneral/TN.DocGeneral/General.cs:192`, `tn.docgeneral/TN.DocGeneral/General.cs:196`, `tn.docgeneral/TN.DocGeneral/General.cs:201`).
- В `CfgApp.json` 41 уникальный `IdDoc`, по 41 на каждое устройство `IdDevice=0/1/2` (всего 123 конфигурации `(IdDoc, IdDevice)`), что задаёт базовый объём матрицы (`TN_Doc/Cfg/CfgApp.json`).
- В `tn.docgeneral` сейчас 43 проекта модулей (исключая `TN.DocGeneral` и `tn.utils`), поэтому матрица тестирования должна учитывать и общие библиотеки (`Common*`) при планировании reuse.

### 1.5 Существующая инфраструктура, которую нужно переиспользовать

- `TestDataFixture` уже покрывает генерацию базового JSON/HTML для ключевых доменов (`Passport/Act/KMH/Poverka/Report/Jornal`) и должен быть расширен, а не заменён (`Tests/Tests.Shared/Fixtures/TestDataFixture.cs:15`, `Tests/Tests.Shared/Fixtures/TestDataFixture.cs:42`, `Tests/Tests.Shared/Fixtures/TestDataFixture.cs:73`, `Tests/Tests.Shared/Fixtures/TestDataFixture.cs:103`, `Tests/Tests.Shared/Fixtures/TestDataFixture.cs:137`, `Tests/Tests.Shared/Fixtures/TestDataFixture.cs:169`, `Tests/Tests.Shared/Fixtures/TestDataFixture.cs:197`).
- `MockConfigHelper` уже инкапсулирует стандартную настройку `IAppConfigService` для модульных тестов (`Tests/Tests.Shared/Fixtures/MockConfigHelper.cs:10`).
- `IntegrationTestBase` и текущие интеграционные тесты дают готовую точку расширения для characterization-кейсов в существующем проекте (`Tests/Tests.Integration/IntegrationTestBase.cs:10`).

---

## 2. Целевая архитектура тестов для `tn.docgeneral`

## 2.1 Основной рекомендуемый вариант (эволюционный)

Использовать текущие проекты:

- `Tests.Unit`:
  - быстрые deterministic unit-тесты (без реальной БД),
  - проверка нормализаторов, парсеров, мапперов, инвариантов.
- `Tests.Integration`:
  - characterization/golden-master сценарии модулей,
  - интеграционные сценарии `GetList/GetViewDoc/GetEditDoc/SaveDoc`.
- `Tests.Shared`:
  - библиотека общих инструментов (fixture loader, normalizer, diff/assert, metadata).

### 2.2 Предлагаемая структура каталогов (внутри существующей системы)

- `Tests/Tests.Integration/Characterization/`
- `Tests/Tests.Integration/DocModules/`
- `Tests/Tests.Integration/DbLive/` (опционально, под nightly)
- `Tests/Tests.Shared/Characterization/`
- `Tests/Fixtures/DocGeneral/<Module>/<Device>/<Case>/...`

### 2.3 Когда оправдан отдельный NUnit-проект

Отдельный проект нужен только если выполняется хотя бы один критерий:

1. Средний runtime PR-набора characterization > 15-20 минут.
2. Требуется отдельный security perimeter (секреты/раннер/сеть) для live DB.
3. Baseline-артефакты начинают резко нагружать основной тестовый контур.
4. Нужен независимый релизный цикл для тестовой платформы characterization.

### 2.4 Trade-offs и рекомендация

- Встроить в текущие `Tests.Integration`/`Tests.Shared`:
  - плюс: минимум накладных расходов, лучший reuse.
  - минус: аккуратная сегментация по категориям обязательна.
- Отдельный проект:
  - плюс: изоляция тяжёлого контура.
  - минус: дополнительная поддержка и дублирование инфраструктуры.

Рекомендация: стартовать встроенно, выносить в отдельный проект только по факту достижения порогов.

---

## 3. Стратегия characterization tests (golden-master)

## 3.1 Как снимать данные из реальной БД

Источник сценария:

1. Получить список документов через `GetList(...)`.
2. Для каждого `id` снять:
   - `GetViewDoc(id)` / `GetViewDoc(id, protocolNumber)` для модулей с протоколом (`TN_Doc/Controllers/HomeController.cs:508`),
   - `GetEditDoc(id)`,
   - при необходимости `SaveDoc(...)` на безопасных данных.

Объект фиксации на кейс:

- `view.raw.json`
- `edit.raw.html`
- `meta.json` (минимум: deviceId, idDoc, recordId, protocolNumber, sourceDb, capturedAtUtc, moduleVersion)

## 3.2 Как преобразовывать в JSON fixtures

Двухступенчатая модель:

1. `raw` (как вернул модуль, для расследований)
2. `normalized` (для сравнения)

Нормализация JSON:

- сортировка ключей объектов,
- унификация числовых форматов,
- приведение дат к согласованному виду,
- удаление/маскирование технических полей.

Нормализация HTML:

- trim/whitespace normalization,
- канонизация line endings (`\r\n` -> `\n`),
- стабилизация динамических `id`/GUID в `GetEditDoc` (placeholder-замены),
- нормализация или вырезание нестабильных inline-handler (`onchange`, `onclick`, `oninput`),
- канонизация пустых атрибутов и self-closing тегов,
- удаление HTML-комментариев и пустых шумовых блоков.

Практический вывод: HTML normalizer нельзя оценивать как «полдня». Для первого production-ready варианта закладывать 1-1.5 дня.

## 3.3 Как хранить baseline/expected

Рекомендуемая структура:

- `Tests/Fixtures/DocGeneral/<Module>/<Device>/<Case>/expected.view.normalized.json`
- `Tests/Fixtures/DocGeneral/<Module>/<Device>/<Case>/expected.edit.normalized.html`
- `Tests/Fixtures/DocGeneral/<Module>/<Device>/<Case>/actual.*` (только в CI artifacts)
- `Tests/Fixtures/DocGeneral/<Module>/<Device>/<Case>/meta.json`

## 3.4 Нормализация нестабильных полей

Обязательные правила:

- даты/время (`strBegin/strEnd`, периоды, timestamp),
- GUID и случайные ID (например, fallback в `GetId`):
  - `tn.docgeneral/TN.DocGeneral/Extensions/AdditionalInfoExtensions.cs:14`
  - `tn.docgeneral/Passport/Extensions/PassportExtensions.cs:31`
- незначимый порядок коллекций,
- технические id/временные метки,
- динамически формируемые export file name (`FileNameForExportDoc`) во множестве модулей,
- нормализация путей (`\\` -> `/`) и line endings для кросс-платформенного сравнения (Windows/Linux),
- фиксация инвариантной культуры форматирования чисел/дат при сравнении snapshot.

Рекомендация по платформе baseline: снимать canonical baseline в том же окружении, где исполняется основной CI (Linux), а локально приводить вывод к этому формату.

## 3.5 Предохранитель от фиксации багов

На каждый high-risk модуль добавить semantic checks поверх snapshot:

- обязательные поля/структура,
- инварианты диапазонов,
- проверка непротиворечивости связанных полей.

Это снижает риск «golden-master закрепил неправильное поведение».

Критерий выбора snapshot/semantic:

| Тип сценария | Snapshot | Semantic |
|---|---:|---:|
| Стабильный модуль с детерминированным JSON | Да | Опционально |
| Модуль с ELIS/условным рендерингом | Да | Да |
| Модуль с `SaveDoc`/SQL side effects | Да | Да |
| Сильно шумный output (много случайных ID/GUID) | Ограниченно | Да |
| Новый или активно меняющийся модуль | Ограниченно | Да |

## 3.6 Стратегия безопасного тестирования `SaveDoc`

Критично для модулей с записью в БД (`Passport`, `Report`):

- `Passport` обновляет сущности и вызывает `SaveChanges()` (`tn.docgeneral/Passport/DocPassport.cs:730`, `tn.docgeneral/Passport/DocPassport.cs:893`).
- `Report` выполняет `Database.ExecuteSqlRaw(...)` (`tn.docgeneral/Report/DocReport.cs:382`).

Варианты изоляции:

1. Транзакция на тест с гарантированным `Rollback` в `TearDown`.
2. Выделенный тестовый диапазон записей (`id > N`) + идемпотентный cleanup.
3. Snapshot/restore схемы или таблиц на уровень suite (дороже, но надёжно для nightly).

Рекомендуемый порядок:

1. На пилоте использовать вариант 2 (наименее рискованный организационно).
2. Параллельно проверить вариант 1 для конкретных модулей и драйвера MySQL (чтобы убедиться, что все операции, включая `ExecuteSqlRaw`, остаются в общей транзакции теста).
3. Для nightly livedb, если тестовые данные «плывут», перейти на вариант 3 для ограниченного набора таблиц.

---

## 4. Пошаговый план внедрения по фазам

## 4.1 Фаза 0: Подготовка (3-4 дня)

Цель: зафиксировать правила и подготовить основу.

Задачи:

1. Провести аудит и каталогизацию существующей test-инфраструктуры (`BaseDocumentTest<T>`, `TestDataFixture`, `MockConfigHelper`, `IntegrationTestBase`) и зафиксировать, что переиспользуем без переписывания.
2. Исправить CI-фильтрацию тестов до старта пилота (в первую очередь integration).
3. Утвердить формат fixtures/baseline и naming.
4. Утвердить список нормализуемых полей (JSON/HTML/кросс-платформенные).
5. Определить перечень БД/устройств для capture.
6. Зафиксировать политику baseline update и review.
7. Утвердить политику изоляции `SaveDoc`-тестов (rollback/test-record/snapshot).

Критерий выхода:

- CI действительно запускает целевые integration-тесты,
- есть утвержденный шаблон кейса и чек-лист обновления baseline,
- принято решение по безопасному контуру `SaveDoc`.

## 4.2 Фаза 1: Пилот (2-3 библиотеки, 5-7 дней)

Рекомендуемый пилот:

1. `Passport` (сложная бизнес-логика + ELIS признаки)
2. `Report` (DataARM + SQL update/read path)
3. `KMH_PP_Areom` или `Poverka3380` (характерный спецмодуль)

Задачи:

1. Снять 10-20 кейсов на модуль через существующую интеграционную инфраструктуру.
2. Внедрить golden-тесты `GetViewDoc`/`GetEditDoc`.
3. Добавить semantic-assert минимум на критические поля.
4. Расширить `TestDataFixture`/`MockConfigHelper` под snapshot-процессы, без дублирования существующих генераторов.
5. Собрать статистику flaky/diff.

Критерий выхода:

- 3 стабильных прогона подряд,
- flaky < 5%,
- процессы recapture/update baseline прозрачны.

## 4.3 Фаза 2: Масштабирование на high-priority (7-10 дней)

Подключаем:

- `Act`, `Jornal`, `KMH_PV`, `KMH_PW`, `KMH_MI2816`, `Poverka2816`, `Poverka3380`, SIKN425-модули.

Задачи:

1. Параметризовать повторяющиеся тестовые шаблоны.
2. Добавить safe-сценарии для `SaveDoc` по утверждённой стратегии из раздела 3.6.
3. Укрепить диагностику diff отчётов.
4. Ввести технику сброса/изоляции кэша модулей в сценариях через `CachedDocModuleLoader` (`ClearCache()`), где тесты идут через `HomeController`.

Критерий выхода:

- покрыто > 80% high-пула,
- время PR-прогона в пределах SLA.

## 4.4 Фаза 3: Расширение на medium/long-tail (5-8 дней)

Подключаем остальные KMH/Poverka модули по единому шаблону.

Задачи:

1. Массовое добавление кейсов через список `IdDoc`.
2. Nightly live-DB drift report.
3. Финальная стабилизация категорий и пайплайнов.

Критерий выхода:

- flaky < 2%,
- baseline update governance работает без ручного хаоса.

---

## 5. Матрица приоритизации библиотек `tn.docgeneral`

## 5.1 High

Библиотеки:

- Core: `Passport`, `Act`, `Report`, `Jornal`
- KMH: `KMH_PP_Areom`, `KMH_PV`, `KMH_PW`, `KMH_MI2816`
- Poverka: `Poverka2816`, `Poverka3380`
- SIKN425: `KMX_Sikn425_PR_PU`, `KMX_Sikn425_PR_PR`, `PoverkaSikn425_PR_PU`, `PoverkaSikn425_PR_PR`

Риск регрессий: высокий.

Зависимость от внешних ресурсов: высокая (MySQL schema, конфиги, шаблоны, DLL loader, иногда прямой SQL/DataARM).

## 5.2 Medium

Библиотеки:

- KMH: `KMH_PR_PU`, `KMH_PR_PR`, `KMH_PP`, `KMH_MPR_MPR`, `KMH_MPR_PU`, `KMH_MPR_TPR`, `KMH3265_PR_PU`, `KMH3265_UPR_PR`, `KMH3288_MPR_TPR`, `KMH3312_PR_PU`, `KMH3312_UPR_PR`
- Poverka: `Poverka1974`, `Poverka1974_04`, `Poverka1974_89`, `Poverka1974_95`, `Poverka3151`, `Poverka3189`, `Poverka3265_PR_PU`, `Poverka3265_UPR_PR`, `Poverka3265_UPR_PU`, `Poverka3266`, `Poverka3267`, `Poverka3272`, `Poverka3287`, `Poverka3288`, `Poverka3312_PR_PU`, `Poverka3312_UPR_PR`

Риск регрессий: средний.

Зависимость от внешних ресурсов: средняя-высокая (БД + конфиги).

## 5.3 Low

Библиотеки:

- `CommonPoverka1974`, `CommonSikn425`, частично `TN.Utils` (в контексте doc-characterization).

Примечание: модулей `ActProducer` и `ActRoute` в текущем дереве `tn.docgeneral` нет; если они существуют во внешних репозиториях/ветках, их нужно добавить отдельным пунктом после подтверждения владельца.

Риск регрессий: низкий-средний.

Зависимость: низкая-средняя.

---

## 6. Интеграция в CI

## 6.1 Предусловие: сначала починить фильтры

До запуска пилота нужно исправить фильтры в workflow-файлах:

1. `.github/workflows/tests-on-push.yml:213` и `.github/workflows/build-and-package.yml:219`:
   - убрать `Tests.Libraries.*`, заменить на актуальные namespace/category для `Tests.Integration.*`.
2. `.github/workflows/tests-on-push.yml:139` и `.github/workflows/build-and-package.yml:149`:
   - синхронизировать unit-фильтрацию с реальными namespace `Tests.Unit.*` или перейти на category-based запуск.

Без этого новые characterization-тесты не дадут надёжного CI gate.

## 6.2 Наборы для запуска

PR:

1. `Tests.Unit` (fast)
2. `Tests.Integration` smoke (без live DB)
3. `characterization-offline` только high-risk или changed modules

Merge/Main:

1. полный `Tests.Unit`
2. полный `Tests.Integration`
3. полный `characterization-offline`

Nightly:

1. `characterization-livedb` полный
2. drift report + flaky report + baseline mismatch report

## 6.3 Разделение быстрых/медленных/DB

Ввести категории NUnit:

- `fast`
- `characterization`
- `db`
- `slow`

И запускать через фильтры category/property, а не namespace-строки.

## 6.4 Политика обновления baseline

1. Baseline меняется только отдельным PR `baseline-update`.
2. Обязателен machine-readable diff + human-readable summary.
3. Обязательное ревью владельца доменного модуля.
4. Запрет на незаметное обновление baseline в feature PR.

---

## 7. Риски и меры снижения

## 7.1 Риск: «зафиксировали баг как эталон»

Меры:

1. semantic-assert над snapshot,
2. отдельный review baseline,
3. реестр known-bug baselines с ссылками на тикеты.

## 7.2 Риск: дрейф данных БД

Меры:

1. фиксированный набор recordId для core кейсов,
2. nightly recapture candidate и сравнение с baseline,
3. version metadata в каждом кейсе.

## 7.3 Риск: нестабильные тесты

Меры:

1. строгая нормализация дат/GUID/порядка,
2. изоляция live-DB тестов от PR-гейта,
3. отдельный flaky dashboard и auto-rerun policy только для nightly.

## 7.4 Риск: закрытые NuGet и инфраструктура

Меры:

1. использовать существующий подход в workflows (приватные source + secrets):
   - `.github/workflows/tests-on-push.yml:120`
   - `.github/workflows/tests-on-push.yml:194`
2. DB-зависимые наборы запускать в контролируемом окружении с секретами.

## 7.5 Риск: LRU-кэш загрузчика модулей (`CachedDocModuleLoader`)

Риск: при прогоне через `HomeController` тесты проходят через кэш модулей с ограничением `MAX_CACHED_MODULES = 5`, что может влиять на порядок/состояние сценариев (`tn.docgeneral/TN.DocGeneral/Services/CachedDocModuleLoader.cs:22`).

Меры:

1. в suite-level setup/teardown очищать кэш (`ClearCache()`) перед пачками characterization,
2. группировать тесты по модулям, чтобы снизить лишние eviction-циклы,
3. для части тестов вызывать модули напрямую (минуя loader), если нужен полностью детерминированный контур без влияния LRU.

---

## 8. Оценка трудозатрат и roadmap на 2-4 недели

## 8.1 Оценка по фазам

1. Фаза 0: 3-4 дня
2. Фаза 1 (пилот): 5-7 дней
3. Фаза 2 (high): 7-10 дней
4. Фаза 3 (масштаб): 5-8 дней

Суммарно:

- минимальный результат: 2 недели (Ф0 + Ф1 + часть Ф2),
- расширенный результат: 4 недели (Ф0-Ф3).

## 8.2 Минимальный roadmap (2 недели)

Неделя 1:

1. аудит текущей инфраструктуры + решение по reuse (`BaseDocumentTest<T>`, `TestDataFixture`, `MockConfigHelper`, `IntegrationTestBase`)
2. исправить CI-фильтры до запуска пилота
3. утвердить baseline/normalization policy и стратегию `SaveDoc`-изоляции
4. запустить пилот 2-3 модуля

Неделя 2:

1. стабилизировать pilot и diff-отчеты
2. добавить high-risk core пул
3. разделить PR/merge/nightly наборы
4. formalize baseline governance

## 8.3 Расширенный roadmap (4 недели)

Неделя 3:

1. массовое подключение remaining high + часть medium
2. safe `SaveDoc` integration patterns

Неделя 4:

1. долгий хвост medium-модулей
2. nightly drift pipeline
3. метрики стабильности и финальная корректировка SLA

---

## 9. Backlog-ready задачи (практический список)

### Эпик A: Базовая инфраструктура characterization

1. `TDOC-CHAR-000` Аудит и каталогизация существующей тестовой инфраструктуры (`BaseDocumentTest<T>`, `TestDataFixture`, `MockConfigHelper`, `IntegrationTestBase`) с решением по reuse/расширению.
2. `TDOC-CHAR-001` Описать и утвердить формат fixture/baseline + meta schema.
3. `TDOC-CHAR-002` Реализовать JSON normalizer в `Tests.Shared`.
4. `TDOC-CHAR-003` Реализовать HTML normalizer в `Tests.Shared` (оценка: 1-1.5 дня, с GUID/id/js-канонизацией).
5. `TDOC-CHAR-004` Реализовать snapshot comparator + diff report.
6. `TDOC-CHAR-005` Добавить шаблон test case loader для `IdDoc/device/record`.
7. `TDOC-CHAR-006` Оформить и внедрить стратегию безопасного тестирования `SaveDoc` (rollback/test-record/snapshot).

### Эпик B: Пилот

1. `TDOC-CHAR-101` Pilot characterization: `Passport`.
2. `TDOC-CHAR-102` Pilot characterization: `Report`.
3. `TDOC-CHAR-103` Pilot characterization: `KMH_PP_Areom` (или `Poverka3380`).
4. `TDOC-CHAR-104` Добавить semantic assertions для pilot-модулей.
5. `TDOC-CHAR-105` Провести 3 прогоновую стабилизацию и отчёт.

### Эпик C: Масштабирование high-risk

1. `TDOC-CHAR-201` Добавить `Act` и `Jornal`.
2. `TDOC-CHAR-202` Добавить `KMH_PV`, `KMH_PW`, `KMH_MI2816`.
3. `TDOC-CHAR-203` Добавить `Poverka2816`, `Poverka3380`.
4. `TDOC-CHAR-204` Добавить SIKN425 модульный пул.
5. `TDOC-CHAR-205` Добавить стабильный паттерн изоляции `CachedDocModuleLoader` (очистка кэша/группировка suite) для anti-flaky.

### Эпик D: CI и governance

1. `TDOC-CHAR-302` Исправить CI-фильтрацию тестов под реальные namespace/category (блокер перед пилотом).
2. `TDOC-CHAR-301` Ввести категории NUnit (`fast`, `characterization`, `db`, `slow`).
3. `TDOC-CHAR-303` Настроить nightly livedb characterization job.
4. `TDOC-CHAR-304` Утвердить baseline update policy и review checklist.

---

## 10. Критичные открытые вопросы владельцу проекта

1. Какая среда БД допустима для nightly (staging/реплика production)?
2. Разрешены ли записи в БД для `SaveDoc`-тестов, или только read-only режим?
3. Какой SLA на длительность тестов в PR и merge?
4. Кто утверждает baseline updates по каждому домену (`Passport`, `KMH`, `Poverka`)?
5. Нужно ли покрытие всех устройств `IdDevice=0/1/2` или эталонного поднабора?
6. Какие комбинации `protocolNumber` обязательны для регрессионной матрицы?
7. Храним большие baseline в Git или в внешнем artifact storage?
8. Ночной drift должен блокировать релиз или только создавать инцидент/тикет?
9. Есть ли в периметре продукта внешние модули `ActProducer`/`ActRoute`, которых нет в текущем репозитории `tn.docgeneral`, но которые нужно включить в матрицу?

---

## Примечание по актуализации контекста

Документ `tech_debt/TEST_COVERAGE_PLAN.md` содержит устаревшие допущения (например, ссылки на `Tests/Libraries/*`, которых сейчас нет в репозитории). Использовать его как исторический материал, но не как источник текущей структуры (`tech_debt/TEST_COVERAGE_PLAN.md:19`, `tech_debt/TEST_COVERAGE_PLAN.md:31`, `tech_debt/TEST_COVERAGE_PLAN.md:108`).
