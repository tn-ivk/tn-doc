# Tests - Тестовая документация TN_Doc

> **Статус актуализации:** ✅ Актуализировано для версии 1.3.8
> **Дата актуализации:** 2026-01-19
> **Всего тестовых файлов:** 35

## Обзор

Это директория с unit и интеграционными тестами для проекта TN_Doc. Тесты используют **NUnit**, **Moq** для моков и **EntityFrameworkCore.InMemory** для in-memory базы данных.

## Структура тестов

```
Tests/
├── controllers/          # Тесты контроллеров (7 файлов)
├── Services/             # Тесты сервисов (5 файлов)
├── Libraries/            # Тесты библиотек документов
│   ├── Core/             # Базовые документы (4 файла)
│   ├── Common/           # Общие библиотеки DTOs (2 файла)
│   ├── KMH/              # КМХ документы (7 файлов)
│   └── Integration/      # Интеграционные тесты (1 файл)
├── Configs/              # Тесты конфигураций (2 файла)
├── Fixtures/             # Вспомогательные классы (2 файла)
└── UsersTests.cs         # Тесты формирования ФИО (1 файл)
```

## Статус актуализации по категориям

### ✅ Controllers Tests (2/7 работают)

| Файл | Статус | Тесты |
|------|--------|-------|
| DirEditorControllerTests.cs | ✅ Работает | ~12 |
| ElisControllerTests.cs | ⚠️ Частично (4 теста отключены) | ~12 |
| PrintControllerTests.cs | ⚠️ Требует доработки IPrinterService | ~20 |
| ClientLogControllerTests.cs | ❌ Контроллер не реализован | ~30 |
| PdfControllerTests.cs | ❌ Контроллер не реализован | ~25 |
| ExportControllerTests.cs | ❌ Значительно изменен | ~40 |
| HomeControllerTests.cs | ❌ Требует переработки | ~150 |

**Отключено/требует правки:** ~265 тестов
**Работает:** ~24 теста

### ✅ Services Tests (2/5 работают)

| Файл | Статус | Тесты |
|------|--------|-------|
| AppConfigServiceTests.cs | ✅ Работает | 14 |
| CfgAppSyncTests.cs | ✅ Работает | 1 |
| ConfigurationCacheServiceTests.cs | ❌ Сервис не реализован | ~20 |
| DbSchemaCacheTests.cs | ❌ Сервис не реализован | ~25 |
| DocGeneralTests.cs | ⚠️ Частично (14 тестов отключены) | 20 (6 работают) |

**Отключено:** ~59 тестов
**Работает:** ~21 тест

### ✅ Libraries Tests - Core/Common (2/9 работают)

| Категория | Файл | Статус | Тесты |
|-----------|------|--------|-------|
| **Common** | CommonPoverka1974DocumentTests.cs | ✅ Работает | 14 |
| **Common** | CommonSikn425DocumentTests.cs | ✅ Работает | 15 |
| **Core** | ActDocumentTests.cs | ⚠️ Требует правки | 24 |
| **Core** | JornalDocumentTests.cs | ⚠️ Требует правки | 28 |
| **Core** | PassportDocumentTests.cs | ⚠️ Требует правки | 13 |
| **Core** | ReportDocumentTests.cs | ⚠️ Требует правки | 38 |
| **Helpers** | BaseDocumentTest.cs | ✅ Актуален | - |
| **Helpers** | DocumentTestHelpers.cs | ✅ Актуален | - |
| **Helpers** | InfrastructureTests.cs | ✅ Работает | 21 |

**Проблема Core тестов:** Используют несуществующий параметр `IConfigurationCacheService` в конструкторах.

**Работает:** ~29 тестов
**Требует правки:** ~104 теста

### ✅ Libraries Tests - KMH (7/7 актуализированы)

| Файл | Статус | Библиотеки | Тесты |
|------|--------|------------|-------|
| KmhDensityTests.cs | ✅ Работает | KMH_PP, KMH_PP_Areom | ~24 |
| KmhFlowTests.cs | ✅ Работает | KMH_PR_PR, KMH_PR_PU | ~24 |
| KmhMeasurementTests.cs | ✅ Работает | KMH_MPR_MPR, KMH_MPR_PU, KMH_MPR_TPR | ~24 |
| KmhMi2816Tests.cs | ✅ Работает | KMH_MI2816 | ~24 |
| KmhParameterTests.cs | ✅ Работает | KMH_PV, KMH_PW | ~24 |
| KmhSikn425Tests.cs | ✅ Работает | KMX_Sikn425_PR_PR, KMX_Sikn425_PR_PU | ~24 |
| KmhStandardTests.cs | ✅ Работает | KMH3265_*, KMH3288_*, KMH3312_* | ~24 |

**Всего тестов:** ~168
**Все актуализированы** - убран параметр `IConfigurationCacheService` из конструкторов

### ✅ Configs/Fixtures/Integration (4/6 работают)

| Файл | Статус | Тесты |
|------|--------|-------|
| CfgEditPassportLinkedParametersTests.cs | ❌ Функционал не реализован | 10 |
| CfgEditPassportTests.cs | ⚠️ 2 теста отключены (IsBallast) | 2 |
| DocumentTestDataFixture.cs | ✅ Актуален | - |
| MockConfigHelper.cs | ✅ Актуален | - |
| DocumentInterfaceComplianceTests.cs | ✅ Обновлен (2 типа) | ~30 |
| UsersTests.cs | ✅ Работает | 19 |

**Отключено:** 12 тестов
**Работает:** ~49 тестов

## Общая статистика

| Метрика | Значение |
|---------|----------|
| **Всего тестовых файлов** | 35 |
| **Полностью работают** | 15 (~43%) |
| **Частично работают** | 6 (~17%) |
| **Не применимы/отключены** | 14 (~40%) |
| | |
| **Всего тестов** | ~650+ |
| **Работающих тестов** | ~315 (~48%) |
| **Отключенных тестов** | ~335 (~52%) |

## Быстрый старт

### Предварительные требования

```bash
# 1. Инициализировать git submodules (ОБЯЗАТЕЛЬНО)
cd C:\dev\dotnet\ivk\docs\docs\tn_doc
git submodule update --init --recursive

# 2. Собрать весь solution
dotnet restore
dotnet build tn_doc.sln
```

### Запуск тестов

```bash
# Все тесты
dotnet test Tests/Tests.csproj

# Только работающие тесты контроллеров
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~DirEditorControllerTests"
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~ElisControllerTests"

# Только работающие тесты сервисов
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~AppConfigServiceTests"
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~CfgAppSyncTests"

# Тесты библиотек Common (DTOs)
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~CommonPoverka1974DocumentTests"
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~CommonSikn425DocumentTests"

# Все КМХ тесты (после исправления Core тестов)
dotnet test Tests/Tests.csproj --filter "Namespace~Tests.Libraries.KMH"

# Users тесты
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~UsersTests"

# Подробный вывод
dotnet test Tests/Tests.csproj --logger "console;verbosity=detailed"

# Исключить отключенные тесты (с [Ignore])
dotnet test Tests/Tests.csproj --filter "Category!=Ignore"
```

## Известные проблемы

### 🔧 Критические (блокируют тесты)

1. **Core документы требуют правки** (ActDocumentTests, JornalDocumentTests, PassportDocumentTests, ReportDocumentTests)
   - **Проблема:** Используется несуществующий параметр `IConfigurationCacheService` в конструкторах
   - **Решение:** Удалить параметр из всех вызовов конструкторов (см. раздел "Исправление Core тестов")

2. **HomeController требует переработки**
   - **Проблема:** Конструктор изменен, удалены зависимости `IReportBuffer`, `IDocModuleLoader`, `IDbSchemaCache`
   - **Решение:** Переписать тесты под новую архитектуру (использование Reflection для загрузки модулей)

### ⚠️ Важные (функционал не реализован)

3. **ConfigurationCacheService не реализован**
   - Централизованное кэширование JSON конфигураций
   - LRU eviction (max 50 элементов)
   - Статистика cache hits/misses

4. **DbSchemaCache не реализован**
   - Кэширование проверок схемы БД (наличие колонок DataARM)

5. **ClientLogController не реализован**
   - Контроллер для логирования ошибок клиента

6. **PdfController не реализован**
   - Отдельный контроллер для генерации PDF

7. **Расширенная конфигурация паспорта**
   - Поля `LinkedParameter`, `SlaveKey`, `IsBallast` отсутствуют в конфигурациях

### ℹ️ Информационные

8. **PrintController использует конкретный класс**
   - Есть `PrinterService`, но нет интерфейса `IPrinterService`
   - Моки в тестах могут работать некорректно

9. **ExportController значительно упрощен**
   - Большая часть функциональности перенесена в `HomeController`

## Исправление Core тестов

Для работы тестов `ActDocumentTests.cs`, `JornalDocumentTests.cs`, `PassportDocumentTests.cs`, `ReportDocumentTests.cs` нужно удалить параметр `IConfigurationCacheService`:

**Было (6 параметров):**
```csharp
new DocAct(
    dbOptions,
    _mockAppConfig.Object,
    _mockConfigCache.Object,  // ❌ УДАЛИТЬ
    idDevice: 1,
    idDoc: IdDoc.Act,
    path: _testBasePath
)
```

**Стало (5 параметров):**
```csharp
new DocAct(
    dbOptions,
    _mockAppConfig.Object,
    idDevice: 1,
    idDoc: IdDoc.Act,
    path: _testBasePath
)
```

**Файлы для исправления:**
- `Tests/Libraries/Core/ActDocumentTests.cs` (4 места)
- `Tests/Libraries/Core/JornalDocumentTests.cs` (4 места)
- `Tests/Libraries/Core/PassportDocumentTests.cs` (3 места, удалить `null` параметр)
- `Tests/Libraries/Core/ReportDocumentTests.cs` (5 мест + класс TestableDocReport)

## Roadmap восстановления функциональности

### Фаза 1: Исправление существующих тестов (приоритет: ВЫСОКИЙ)

- [ ] Исправить Core тесты (удалить `IConfigurationCacheService`)
- [ ] Создать интерфейс `IPrinterService` для PrintController
- [ ] Переработать HomeControllerTests под новую архитектуру

### Фаза 2: Восстановление сервисов (приоритет: СРЕДНИЙ)

- [ ] Реализовать `ConfigurationCacheService` (LRU кэширование конфигураций)
- [ ] Реализовать `DbSchemaCache` (кэширование проверок схемы БД)
- [ ] Реализовать `ISystemJournalService` (для ElisController)
- [ ] Реализовать методы `DocGeneral.NormalizeDecimalString` и `MapPropertiesByName`

### Фаза 3: Восстановление контроллеров (приоритет: НИЗКИЙ)

- [ ] Решить, нужны ли `ClientLogController` и `PdfController`
- [ ] Переработать `ExportController` или удалить лишние тесты
- [ ] Реализовать недостающие методы контроллеров

### Фаза 4: Расширенная конфигурация паспорта (приоритет: ПЛАНИРУЕТСЯ)

- [ ] Добавить поля `LinkedParameter`, `SlaveKey`, `IsBallast` в конфигурации
- [ ] Реализовать логику работы с этими полями
- [ ] Включить тесты `CfgEditPassportLinkedParametersTests.cs` и части `CfgEditPassportTests.cs`

## Документация зависимостей

### NuGet пакеты

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="NUnit" Version="4.3.2" />
<PackageReference Include="NUnit3TestAdapter" Version="5.0.0" />
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="7.0.20" />
<PackageReference Include="HtmlAgilityPack" Version="1.12.1" />
```

### Ссылки на проекты

**Основные:**
- `TN_Doc.csproj` - главное приложение
- `TN.DocGeneral` - интерфейсы и базовые классы
- `TN.Utils` - утилиты

**Библиотеки документов (Core):**
- Act, Passport, Jornal, Report
- CommonPoverka1974, CommonSikn425

**КМХ библиотеки (18 штук):**
- KMH_PP, KMH_PP_Areom (плотность)
- KMH_PR_PR, KMH_PR_PU (расход)
- KMH_MPR_MPR, KMH_MPR_PU, KMH_MPR_TPR (массовый расход)
- KMH_PV, KMH_PW (параметры)
- KMH_MI2816 (РМГ 117-2021)
- KMH3265_*, KMH3288_*, KMH3312_* (ГОСТ стандарты)
- KMX_Sikn425_* (СИКН-425)

**Poverka библиотеки (закомментированы - фаза 3)**

## Соглашения по тестам

### Naming Convention

```csharp
[Test]
public void MethodName_WhenCondition_ThenExpectedResult()
{
    // Arrange
    var input = ...;

    // Act
    var result = ...;

    // Assert
    Assert.That(result, Is.EqualTo(expected));
}
```

### Использование Moq

```csharp
var mock = new Mock<IAppConfigService>();
mock.Setup(x => x.GetCfg()).Returns(new Root { ... });
```

### In-Memory Database

```csharp
var options = new DbContextOptionsBuilder<TN_DocContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;
```

## Полезные ссылки

- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4)
- [EF Core In-Memory Provider](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)

## История изменений

### 2026-01-19 - Актуализация для версии 1.3.8

- Скопирована директория Tests/ из docs-elis/tn_doc
- Актуализированы 35 тестовых файлов по 5 категориям
- Исправлен Tests.csproj (добавлены все зависимости)
- Помечены тесты для нереализованного функционала `[Ignore]`
- Обновлены конструкторы (удален `IConfigurationCacheService` из КМХ тестов)
- Создана документация README.md
