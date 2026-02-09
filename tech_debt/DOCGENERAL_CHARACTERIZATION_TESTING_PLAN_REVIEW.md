# Замечания к плану характеризационного тестирования `tn.docgeneral` (v2)

Ревью документа `DOCGENERAL_CHARACTERIZATION_TESTING_PLAN.md` (обновлённая версия).

---

## Статус замечаний первого раунда

| # | Суть | Статус |
|---|------|--------|
| 1 | Неточная формулировка про BaseDocumentTest\<T\> | **Исправлено** (строка 39) |
| 2 | TestDataFixture пропущен | **Исправлено** (раздел 1.5) |
| 3 | 41 → 44 модуля | **Частично** (см. замечание R2-1) |
| 4 | Модули пропущены в матрице | **Исправлено** (разделы 5.2, 5.3) |
| 5 | Нет стратегии SaveDoc rollback | **Исправлено** (раздел 3.6) |
| 6 | Сложность HTML-нормализации | **Исправлено** (раздел 3.2) |
| 7 | CI-фильтры | **Исправлено** (разделы 1.3, 6.1) |
| 8 | LRU-кэш не учтён | **Исправлено** (раздел 7.5) |
| 9 | Нет критерия snapshot vs semantic | **Исправлено** (раздел 3.5, таблица) |
| 10 | Кросс-платформенная нормализация | **Исправлено** (раздел 3.4) |
| 11 | Roadmap: CI-fix до пилота | **Исправлено** (раздел 8.2) |
| 12 | Нет задачи аудита инфраструктуры | **Исправлено** (TDOC-CHAR-000) |

---

## Сводка новых замечаний

| # | Критичность | Суть |
|---|-------------|------|
| R2-1 | **Высокая** | InMemory БД непригодна для characterization — нужна стратегия подключения к MySQL |
| R2-2 | **Высокая** | Статические свойства `DocGeneral` — блокер параллельного запуска тестов |
| R2-3 | **Высокая** | `Report.LoadDataArm()` использует ADO.NET в обход EF — стратегия 3.6 неполна |
| R2-4 | **Средняя** | `TestDataFixture` — минимальные заглушки, а не реальные данные модулей |
| R2-5 | **Средняя** | `TestHelpers.GetAllDocumentTypes()` покрывает 19 из 40 модулей |
| R2-6 | **Средняя** | Количество модулей: 43 → фактически 40 компилируемых .csproj |
| R2-7 | **Средняя** | `protocolNumber` — 6 модулей перегружают `GetViewDoc`, но матрица не учитывает |
| R2-8 | **Низкая** | Не рассмотрены существующие approval-testing библиотеки для .NET |
| R2-9 | **Низкая** | `ActProducer`/`ActRoute` — формулировка «нет в дереве» некорректна |
| R2-10 | **Низкая** | Нет оценки трудозатрат на настройку MySQL для CI |

---

## R2-1. InMemory БД непригодна для characterization-тестов

**Критичность: Высокая**

**Раздел 2.1** рекомендует использовать `Tests.Integration` для characterization-сценариев, а **раздел 3.1** описывает снятие golden-master с реальной БД.

**Факт:** `IntegrationTestBase` (`Tests/Tests.Integration/IntegrationTestBase.cs:10`) настраивает `InMemoryDatabase`:

```csharp
services.AddDbContext<DocGeneral>(options =>
    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
);
```

Но документные модули `tn.docgeneral` **критически зависят от MySQL**:
- `General.cs:74` — `optionsBuilder.UseMySql(activeChannel, MySqlServerVersion.LatestSupportedServerVersion)`
- `General.cs:499` — SQL-команда `DESCRIBE` для интроспекции таблиц (не поддерживается InMemory)
- `Report/DocReport.cs:382` — `Database.ExecuteSqlRaw("UPDATE ...")` (InMemory не поддерживает raw SQL)
- `Report/DocReport.cs:396-408` — ADO.NET через `Database.GetDbConnection()` (InMemory вернёт InMemoryConnection, не MySqlConnection)

**Влияние:** Characterization-тесты через `IntegrationTestBase` с InMemory БД не смогут воспроизвести реальное поведение модулей. `GetViewDoc()` и `GetEditDoc()` либо упадут с ошибкой, либо вернут пустые данные.

**Предложение:** Добавить в раздел 2 подраздел «2.5 Стратегия подключения к БД для characterization-тестов»:

1. Создать `CharacterizationTestBase`, расширяющий `IntegrationTestBase`, с возможностью переключения на реальную MySQL.
2. Использовать Docker-контейнер `mysql:8` с pre-seeded схемой и тестовыми данными (для CI).
3. Для локального запуска — переменная окружения/конфиг `TEST_DB_CONNECTION_STRING`.
4. Разделить тесты: `characterization-offline` (InMemory, ограниченные проверки) vs `characterization-livedb` (MySQL, полноценные golden-master).

Без этого пункт «снять данные из реальной БД» (раздел 3.1) повисает в воздухе — не описано **как** тесты получат доступ к MySQL.

---

## R2-2. Статические свойства `DocGeneral` — блокер параллельного запуска

**Критичность: Высокая**

**Факт:** Базовый класс `DocGeneral` (`General.cs:140-155`) содержит статические свойства с публичными сеттерами:

```csharp
public static Root CfgGeneral { get; set; }           // строка 140
public static Dictionarys DictionarysDoc { get; set; } // строка 145
public static Device CurrentCfgDevice { get; set; }    // строка 150
public static int CurrentIdDevice { get; set; }        // строка 155
```

Эти свойства устанавливаются в конструкторе `DocGeneral` при каждом создании экземпляра.

**Влияние:** Если два characterization-теста запущены параллельно (или NUnit запускает тесты в нескольких потоках), то:
- Тест для `Passport` (IdDevice=1) устанавливает `CurrentIdDevice = 1`
- Одновременно тест для `Report` (IdDevice=2) устанавливает `CurrentIdDevice = 2`
- `Passport` читает `CurrentIdDevice` и получает `2` — тест даёт ложный результат или падает

**Предложение:** Добавить в раздел 7 новый пункт «7.6 Риск: статическое состояние `DocGeneral`»:

Меры:
1. **Обязательно**: запускать characterization-тесты **последовательно** (NUnit: `[NonParallelizable]` на уровне assembly или namespace `Tests.Integration.Characterization`).
2. **В идеале**: вынести статические свойства в `IAppConfigService` (рефакторинг), но это выходит за рамки плана тестирования.
3. В задаче `TDOC-CHAR-000` (аудит) — задокументировать это ограничение и его влияние на design тестового контура.

---

## R2-3. `Report.LoadDataArm()` — ADO.NET в обход EF-транзакций

**Критичность: Высокая**

**Раздел 3.6** описывает стратегию изоляции `SaveDoc` и упоминает, что `ExecuteSqlRaw` может не участвовать в EF-транзакции.

**Факт:** Проблема глубже. `DocReport` использует **два** механизма прямого доступа к БД:

1. `ExecuteSqlRaw` (строка 382) — запись DataARM. Этот вызов **может** участвовать в EF-транзакции, если транзакция создана через `Database.BeginTransaction()`.
2. **ADO.NET** (строки 396-408) — чтение DataARM через `Database.GetDbConnection()` + `cmd.ExecuteScalar()`. Это **полностью вне** EF-контекста и не будет участвовать в EF-транзакции.

```csharp
// Report/DocReport.cs:396-408
using var conn = Database.GetDbConnection();
if (conn.State != ConnectionState.Open) conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT DataARM FROM TableReport WHERE id = @id LIMIT 1";
```

**Влияние:** Вариант изоляции 1 (транзакция + Rollback) из раздела 3.6 не защитит `LoadDataArm()`. Если тест вызывает `SaveDoc` (пишет через `ExecuteSqlRaw`), данные могут быть видны через `LoadDataArm` (ADO.NET) даже внутри незакоммиченной транзакции, а после Rollback — исчезнут. Поведение зависит от уровня изоляции MySQL.

**Предложение:** В разделе 3.6 уточнить:

- Для модулей с ADO.NET (Report) вариант 1 (транзакция + Rollback) **ненадёжен** — рекомендуется вариант 2 (выделенные тестовые записи) или вариант 3 (snapshot/restore).
- Добавить в задачу `TDOC-CHAR-006` проверку: какие именно модули используют ADO.NET напрямую (кроме Report), чтобы заранее определить стратегию для каждого.

---

## R2-4. `TestDataFixture` содержит минимальные заглушки, не реальные данные

**Критичность: Средняя**

**Раздел 1.5** описывает `TestDataFixture` как инфраструктуру, которая «уже покрывает генерацию базового JSON/HTML для ключевых доменов» и рекомендует «расширить, а не заменить».

**Факт:** Генераторы в `TestDataFixture` создают **минимальные заглушки**, структурно не похожие на реальный output модулей:

- `CreatePassportJson()` возвращает плоский объект с полями `id, idDevice, number, dates, productName, volume, mass, density, parameters[]`.
- Реальный output `DocPassport.GetViewDoc()` — многоуровневый объект с ELIS-интеграцией, вложенными конфигурациями, десятками специфичных полей, условными блоками.

Аналогично для остальных генераторов — они подходят для unit-тестов (проверка парсинга, маппинга), но **не для characterization** (проверка полноты и структуры реального output).

**Влияние:** Если план предполагает расширение `TestDataFixture` до уровня characterization, это потребует значительно больше усилий, чем «расширить snapshot-вариантами» — фактически нужно будет написать совершенно новые генераторы или, лучше, снять реальные данные из БД.

**Предложение:** Уточнить в разделе 1.5, что `TestDataFixture` — это инфраструктура для **unit-тестов** (минимальные модели). Для characterization-тестов данные снимаются из реальной БД (раздел 3.1), а `TestDataFixture` может использоваться только как fallback для offline-режима. В задаче `TDOC-CHAR-000` разграничить роли:
- `TestDataFixture` → unit-тесты (проверка инвариантов, parsing)
- Captured fixtures из БД → characterization (golden-master)

---

## R2-5. `TestHelpers.GetAllDocumentTypes()` покрывает 19 из 40 модулей

**Критичность: Средняя**

**Факт:** `TestHelpers.GetAllDocumentTypes()` (`Tests/Tests.Shared/TestHelpers.cs:192`) содержит только 19 типов документов:
- Core: Act, Passport, Report, Jornal (4)
- KMH: 10 типов (только базовые, без KMH3265/KMH3288/KMH3312)
- Poverka: 5 типов (только 1974/2816/3151/3189/3380)

Не включены:
- KMH3265_PR_PU, KMH3265_UPR_PR, KMH3288_MPR_TPR, KMH3312_PR_PU, KMH3312_UPR_PR
- Poverka1974_04, Poverka1974_89, Poverka1974_95, Poverka3265_PR_PU, Poverka3265_UPR_PR, Poverka3265_UPR_PU, Poverka3266, Poverka3267, Poverka3272, Poverka3287, Poverka3288, Poverka3312_PR_PU, Poverka3312_UPR_PR
- KMX_Sikn425_PR_PU, KMX_Sikn425_PR_PR, PoverkaSikn425_PR_PU, PoverkaSikn425_PR_PR

Кроме того, IdDoc-значения в `TestHelpers` (Act=1, Passport=2) **не совпадают** с реальными значениями в `CfgApp.json` (Report=0, Passport=1, Act=2, Jornal=3).

**Влияние:** При масштабировании characterization-тестов (Фаза 2-3) придётся существенно расширить эти списки. Несовпадение IdDoc может привести к тихим ошибкам.

**Предложение:** Добавить в задачу `TDOC-CHAR-005` (test case loader) подзадачу: привести `TestHelpers.GetAllDocumentTypes()` в соответствие с реальными IdDoc из `CfgApp.json` и включить все 40 модулей. Рассмотреть автогенерацию списка из `CfgApp.json` вместо ручного поддержания.

---

## R2-6. Количество модулей: 43 в плане, 40 фактически

**Критичность: Средняя**

**Раздел 1.4**, строка 53: *«В `tn.docgeneral` сейчас 43 проекта модулей (исключая `TN.DocGeneral` и `tn.utils`)»*

**Факт:** В директории `tn.docgeneral/` находится 45 поддиректорий. Из них:
- 40 содержат `.csproj` (компилируемые модули)
- 2 — служебные (`TN.DocGeneral`, `tn.utils`)
- 1 — тесты (`TN.Utils.Tests`)
- 2 — пустые директории с только `bin/obj` (`ActProducer`, `ActRoute`) — **не содержат `.csproj` и исходного кода**

Итого компилируемых модульных проектов: **40**, а не 43.

**Предложение:** Уточнить строку 53: «40 компилируемых модульных проектов плюс 2 пустые директории (`ActProducer`, `ActRoute`)». Скорректировать матрицу приоритизации: High(14) + Medium(27) + Low(2) = 43, но фактически Low должен содержать только `CommonPoverka1974` и `CommonSikn425` (2 проекта), и сумма High+Medium покрывает 41 модуль, а не 40 (проверить расхождение).

---

## R2-7. `protocolNumber` — 6 модулей с отдельным контрактом, не учтённых в матрице

**Критичность: Средняя**

**Раздел 3.1**, строка 116 упоминает `GetViewDoc(id, protocolNumber)`, а **раздел 10**, вопрос 6 спрашивает владельца о комбинациях `protocolNumber`.

**Факт:** 6 модулей перегружают `GetViewDoc(int id, int protocolNumber)`:
1. `KMH_PP_Areom`
2. `KMH_PW`
3. `KMH_MI2816`
4. `KMH_PV`
5. `Poverka2816`
6. `Passport`

Во всех модулях `protocolNumber` влияет на output: он устанавливает `doc.Doc.Settings.General.ProtocolNumber`, что может менять структуру JSON (количество колонок, поля, форматирование).

**Влияние:** Для этих 6 модулей нужно снимать baseline для **нескольких** значений `protocolNumber`, а не только для одного. Это увеличивает объём fixture-файлов и количество тестовых кейсов, но план не учитывает это в оценке трудозатрат (раздел 8).

**Предложение:**
1. В разделе 3.1 указать явно: для 6 модулей с `protocolNumber` снимать минимум 2-3 кейса с разными `protocolNumber` (например, 1 и максимальный).
2. В разделе 8.1 скорректировать оценку Фазы 1: пилот по `Passport` потребует дополнительных кейсов на `protocolNumber`.
3. В задаче `TDOC-CHAR-101` (Pilot: Passport) явно указать: включить варианты с `protocolNumber = 1, 2, N`.

---

## R2-8. Не рассмотрены approval-testing библиотеки для .NET

**Критичность: Низкая**

**Раздел 3** описывает custom-подход к snapshot-тестированию: JSON normalizer, HTML normalizer, snapshot comparator, diff report.

**Факт:** В экосистеме .NET существуют зрелые библиотеки для approval/snapshot testing:
- **Verify** (github.com/VerifyTests/Verify) — NUnit-интеграция, автоматическое управление `.verified`/`.received` файлами, встроенные scrubbers для GUID/DateTime, extensible.
- **ApprovalTests** — аналог, более старая библиотека.

Эти библиотеки решают задачи `TDOC-CHAR-002`–`TDOC-CHAR-004` из коробки (нормализация, сравнение, diff report).

**Предложение:** Добавить в задачу `TDOC-CHAR-000` (аудит) подпункт: «Оценить `Verify` для NUnit как альтернативу custom snapshot comparator. Если подходит — сократить scope `TDOC-CHAR-002/003/004`». Это может сэкономить 1-2 дня на Фазе 0.

---

## R2-9. `ActProducer`/`ActRoute` — формулировка «нет в дереве» некорректна

**Критичность: Низкая**

**Раздел 5.3**, строка 328: *«модулей `ActProducer` и `ActRoute` в текущем дереве `tn.docgeneral` нет»*

**Факт:** Директории `ActProducer/` и `ActRoute/` **физически присутствуют** в `tn.docgeneral/`, но содержат только `bin/` и `obj/` (артефакты сборки) — без `.csproj` и исходного кода.

**Предложение:** Уточнить формулировку: «Директории `ActProducer` и `ActRoute` присутствуют в `tn.docgeneral`, но не содержат проектных файлов и исходного кода (только артефакты `bin/obj`). Необходимо уточнить у владельца: это удалённые модули (директории можно очистить) или модули, вынесенные в отдельный репозиторий.»

---

## R2-10. Нет оценки трудозатрат на настройку MySQL для CI

**Критичность: Низкая**

**Раздел 8** оценивает трудозатрат по фазам, но нигде не учтена настройка тестовой среды БД.

**Факт:** Для полноценных characterization-тестов (раздел 3.1: «снять данные из реальной БД») нужна MySQL-инстанция в CI. Это включает:
- Docker-образ MySQL 8 в GitHub Actions (`services:` или `docker-compose`)
- Скрипт инициализации схемы и тестовых данных (миграции или SQL dump)
- Настройка секретов для connection string
- Интеграция с nightly pipeline

Это нетривиальная задача, особенно если текущий CI не имеет доступа к MySQL.

**Предложение:** Добавить в Эпик D (CI и governance) задачу `TDOC-CHAR-305`: «Настроить тестовый MySQL-контейнер в CI для characterization-livedb набора». Оценка: 1-2 дня. Без этой задачи nightly livedb pipeline (раздел 6.2) не может быть реализован.
