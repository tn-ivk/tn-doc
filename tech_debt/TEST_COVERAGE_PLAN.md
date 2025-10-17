# План покрытия тестами библиотек TN_Doc

> **⚠️ ВАЖНО**: Данный план был проанализирован и оптимизирован. См. [TEST_COVERAGE_PLAN_ANALYSIS.md](./TEST_COVERAGE_PLAN_ANALYSIS.md) для:
> - Критического анализа текущего плана
> - Оптимизированной версии с параметризованными тестами
> - Уменьшения дублирования кода на 80%
> - Сокращения времени реализации на 27% (11 недель вместо 15)
> - Добавления интеграционных тестов (90+)
> - Учета специфики v1.4.2 (GetEditDoc logging)

## 📋 Текущее состояние проекта тестов

### ✅ Статус реализации (Обновлено: январь 2025)

**ВАЖНО**: Базовая тестовая инфраструктура полностью реализована и функционирует! 🎉

#### 🏗️ Реализованная инфраструктура:

**✅ Базовый класс `BaseDocumentTest<T>`** (`Tests/Libraries/BaseDocumentTest.cs`):
- Централизованная настройка моков и зависимостей
- Автоматическое управление временными директориями
- Вспомогательные методы для валидации JSON, HTML, путей к файлам
- Поддержка паттерна AAA (Arrange-Act-Assert)
- Изоляция тестов через in-memory БД (уникальная для каждого теста)
- Полная интеграция с NUnit lifecycle (OneTimeSetUp, SetUp, TearDown, OneTimeTearDown)

**✅ Fixtures и вспомогательные классы**:
- `DocumentTestDataFixture` - генераторы тестовых JSON данных для всех типов документов
- `DocumentTestHelpers` - вспомогательные методы для проверки HTML, JSON, шаблонов

**✅ Реализованные тесты документных библиотек** (7 файлов, 100+ тестов):
- **Core Documents** (`Tests/Libraries/Core/`):
  - ✅ `ActDocumentTests.cs` - акты приема-сдачи
  - ✅ `PassportDocumentTests.cs` - паспорта качества (включая ELIS интеграцию)
  - ✅ `JornalDocumentTests.cs` - журналы
  - ✅ `ReportDocumentTests.cs` - отчеты
- **Common Libraries** (`Tests/Libraries/Common/`):
  - ✅ `CommonPoverka1974DocumentTests.cs` - базовые классы данных для Poverka1974 (используется в 4 вариантах)
  - ✅ `CommonSikn425DocumentTests.cs` - базовые классы данных для SIKN-425 (используется в 4 модулях)
- **Integration Tests** (`Tests/Libraries/Integration/`):
  - ✅ `DocumentInterfaceComplianceTests.cs` - параметризованные тесты соответствия интерфейсу для всех библиотек

**✅ Статус сборки**:
- Проект успешно компилируется (0 ошибок, 1 warning о version conflict)
- Все тесты готовы к запуску
- Внешние алиасы (extern alias) настроены для избежания конфликтов имен

### 🎯 Существующая структура `Tests/`

#### ✅ Настроенные зависимости:
- **NUnit** (4.3.2) - тестовый фреймворк
- **Moq** (4.20.72) - мокирование
- **Microsoft.EntityFrameworkCore.InMemory** (7.0.20) - in-memory БД для тестов
- **HtmlAgilityPack** (1.12.1) - парсинг HTML
- **coverlet.collector** (6.0.4) - покрытие кода
- **Newtonsoft.Json** (13.0.3) - JSON сериализация в тестах

#### ✅ Существующие тесты (20+ файлов):
- **Controllers/**: `HomeController`, `PdfController`, `PrintController`, `ExportController`, `DirEditorController`, `ElisController`, `ClientLogController`
- **Services/**: `AppConfigService`, `DocGeneral`, `CfgAppSync`, `DbSchemaCache`
- **Libraries/Core/**: `ActDocumentTests`, `PassportDocumentTests`, `JornalDocumentTests`, `ReportDocumentTests`
- **Libraries/Common/**: `CommonPoverka1974DocumentTests`, `CommonSikn425DocumentTests`
- **Libraries/Integration/**: `DocumentInterfaceComplianceTests`
- **Fixtures/**: `DocumentTestDataFixture`
- **Users/**: `UsersTests`

#### ✅ Project References (уже добавлены):
- `TN_Doc.csproj` (основное веб-приложение)
- `TN.DocGeneral.csproj` (общая библиотека)
- `Act.csproj`, `Passport.csproj`, `Jornal.csproj`, `Report.csproj` ✅ **РАБОТАЮТ**
- `CommonPoverka1974.csproj`, `CommonSikn425.csproj` ✅ **РАБОТАЮТ**
- `TN.Utils.csproj` (утилиты)

#### 📊 Актуальное количество библиотек: **42** (без ActProducer, ActRoute, ReportIncomplete)
- Core Documents: 4 (Act, Passport, Jornal, Report) ✅ **ТЕСТЫ ГОТОВЫ**
- KMH модули: 17 (включая KMX_Sikn425) ⏳ **В ПЛАНЕ**
- Poverka модули: 19 ⏳ **В ПЛАНЕ**
- Common модули: 2 (CommonPoverka1974, CommonSikn425) ✅ **ТЕСТЫ ГОТОВЫ**

### 🎯 Namespace Convention
- Существующие тесты: `Tests.Controllers`, `Tests.Services`
- Новые тесты библиотек: `Tests.Libraries`, `Tests.Libraries.Core`, `Tests.Libraries.Common`, `Tests.Libraries.Integration`
- Fixtures: `Tests.Fixtures`

### ⚠️ Важные особенности архитектуры (выявленные при реализации):

1. **Защищенные методы**: `GetPathConfigFile()` и `GetPathEditConfigFile()` являются `protected` методами базового класса `DocGeneral` и не могут быть протестированы напрямую. Они тестируются косвенно через публичные методы.

2. **IAppConfigService**: Интерфейс НЕ содержит методов `GetBasePath()`, `GetWwwrootPath()`, `GetConfigPath()`. Пути предоставляются через `TestBasePath` и `TestWwwrootPath` из `BaseDocumentTest`.

3. **Конструкторы документных классов**: Различаются между библиотеками. PassportClass требует параметр `path:` и опциональный `configCache`.

4. **SetDocFromJson()**: Метод существует не во всех документных классах (например, отсутствует в DocPassport).

5. **GetViewDoc() возвращаемый тип**: В DocPassport возвращает `object` вместо `string`, требуется конвертация через `.ToString()`.

6. **Базовые классы для DTOs**: Header, Data, Footer, Dictionarys находятся в namespace `TN_DocReport` и используются через extern alias в Common библиотеках.

## ⚠️ ВАЖНЫЕ ТРЕБОВАНИЯ К РЕАЛИЗАЦИИ

### 🔒 Принцип неизменности кода
- **НЕ ВНОСИТЬ ИЗМЕНЕНИЯ** в существующий код проекта
- **НЕ МОДИФИЦИРОВАТЬ** классы библиотек для тестирования
- **НЕ ДОБАВЛЯТЬ** тестовые методы или свойства в продуктовые классы
- **ТОЛЬКО ЧТЕНИЕ И ТЕСТИРОВАНИЕ** существующей функциональности через публичный интерфейс

### 📁 Дополнение существующей структуры
- **Использовать существующий проект** `Tests/Tests.csproj`
- **Следовать существующему namespace pattern**: `Tests.Libraries`
- **Один файл = Один класс = Одна библиотека**
- **Новое расположение**: `Tests/Libraries/` (новая папка)

### 🔄 Интеграция с существующим кодом
- **Использовать существующие моки и хелперы** из других тестов
- **Следовать существующим паттернам** Setup/TearDown
- **Переиспользовать конфигурацию** тестовых данных

---

## Общая архитектура тестирования

### Базовые принципы
- **Тестовая структура**: AAA (Arrange-Act-Assert)
- **Framework**: NUnit + Moq для мокирования
- **Naming Convention**: `MethodName_WhenCondition_ThenExpectedResult`
- **Покрытие**: Unit тесты + интеграционные тесты
- **Мокирование**: DbContext, IAppConfigService, внешние зависимости

### Шаблон тестового класса (соответствует существующему стилю):
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TN.Doc;
using TN.DocData;
using TN_DocGeneral.Services;

namespace Tests.Libraries;

/// <summary>
/// Набор тестов для {LibraryName}
/// </summary>
[TestFixture]
public class {LibraryName}DocumentTests
{
    private DbContextOptions<DocGeneral> _dbOptions;
    private Mock<IAppConfigService> _mockAppConfig;
    private Mock<IConfiguration> _mockConfiguration;
    private {LibraryName} _documentLibrary;
    private string _testBasePath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "{LibraryName}DocumentTests");
        Directory.CreateDirectory(_testBasePath);
        // Подготовка тестовых данных и конфигураций
    }

    [SetUp]
    public void SetUp()
    {
        // Настройка in-memory базы данных
        _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Мокирование зависимостей
        _mockAppConfig = new Mock<IAppConfigService>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Инициализация тестируемой библиотеки
        _documentLibrary = new {LibraryName}(_dbOptions, _mockAppConfig.Object, 1, IdDoc.{DocType}, _testBasePath);
    }

    [TearDown]
    public void TearDown()
    {
        _documentLibrary?.Dispose();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (Directory.Exists(_testBasePath))
            Directory.Delete(_testBasePath, true);
    }
}
```

### 🔧 Необходимые изменения в Tests.csproj:
Добавить project references для всех документных библиотек:

```xml
<!-- Добавить в существующий Tests.csproj -->
<ItemGroup>
  <!-- KMH модули -->
  <ProjectReference Include="..\tn.docgeneral\KMH_MPR_MPR\KMH_MPR_MPR.csproj" />
  <ProjectReference Include="..\tn.docgeneral\KMH_PR_PR\KMH_PR_PR.csproj" />
  <ProjectReference Include="..\tn.docgeneral\KMH_PP\KMH_PP.csproj" />
  <!-- ... остальные KMH модули ... -->

  <!-- Poverka модули -->
  <ProjectReference Include="..\tn.docgeneral\Poverka1974\Poverka1974.csproj" />
  <ProjectReference Include="..\tn.docgeneral\Poverka3380\Poverka3380.csproj" />
  <!-- ... остальные Poverka модули ... -->

  <!-- Другие модули -->
  <ProjectReference Include="..\tn.docgeneral\Jornal\Jornal.csproj" />
  <ProjectReference Include="..\tn.docgeneral\Report\Report.csproj" />
  <ProjectReference Include="..\tn.docgeneral\CommonPoverka1974\CommonPoverka1974.csproj" />
  <ProjectReference Include="..\tn.docgeneral\CommonSikn425\CommonSikn425.csproj" />
</ItemGroup>
```

### Общие тестовые сценарии для всех библиотек
1. **Конструкторы и инициализация**
2. **Стандартные методы интерфейса**:
   - `GetViewDoc(id)` - тестирование JSON-генерации
   - `GetPathTemplateFile()` - корректность путей к шаблонам
   - `GetEditDoc(id)` - генерация HTML форм
   - `SetDocFromJson(json)` - обновление данных из JSON

---

## 1. **Act (Акты приема-сдачи) - 3 варианта**

### 1.1 Файл: `ActDocumentTests.cs`
### Класс: `ActDocumentTests`
### Приоритет: ВЫСОКИЙ

#### Тестовые методы:
- `Constructor_WithValidParameters_InitializesCorrectly`
- `Constructor_WithNullParameters_ThrowsArgumentException`
- `GetViewDoc_WithValidId_ReturnsValidJsonString`
- `GetViewDoc_WithInvalidId_ReturnsEmptyOrNull`
- `GetViewDoc_WithNonExistentId_HandlesGracefully`
- `GetPathTemplateFile_ReturnsExistingFilePath`
- `GetPathConfigFile_ReturnsExistingConfigPath`
- `GetPathEditConfigFile_ReturnsExistingEditConfigPath`
- `GetEditDoc_WithValidId_ReturnsValidHtmlString`
- `GetEditDoc_UsesPathCombine_ForCrossPlatformCompatibility` (v1.4.2)
- `GetEditDoc_AddsTraceLogging_OnSuccessfulSave` (v1.4.2)
- `GetEditDoc_WithInvalidId_ReturnsDefaultOrError`
- `SetDocFromJson_WithValidJson_DoesNotThrowException`
- `SetDocFromJson_WithInvalidJson_ThrowsException`
- `SetDocFromJson_WithEmptyJson_ThrowsException`

#### Интеграционные тесты:
- Полный цикл: создание → редактирование → сохранение → генерация отчета

### 1.2 Файл: `ActProducerDocumentTests.cs`
### Класс: `ActProducerDocumentTests`
### Приоритет: СРЕДНИЙ

#### Тестовые методы:
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithProducerData_ReturnsValidJsonString`
- `ValidateProducerSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtmlString`
- `GetEditDoc_UsesPathCombine_ForCrossPlatformCompatibility` (v1.4.2)

### 1.3 Файл: `ActRouteDocumentTests.cs`
### Класс: `ActRouteDocumentTests`
### Приоритет: СРЕДНИЙ

#### Тестовые методы:
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithRouteData_ReturnsValidJsonString`
- `ValidateRouteSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtmlString`
- `GetEditDoc_UsesPathCombine_ForCrossPlatformCompatibility` (v1.4.2)

---

## 2. **KMH документы (18 отдельных классов)**

### 2.1 Файл: `KmhMprMprDocumentTests.cs`
### Класс: `KmhMprMprDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithMprData_ReturnsCorrectMeasurementJson`
- `ValidateDataStructure_ForMprMpr_ContainsRequiredFields`
- `GetTemplateFile_ForMprMpr_ReturnsCorrectTemplate`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithMprJson_UpdatesCorrectly`

### 2.2 Файл: `KmhMprPuDocumentTests.cs`
### Класс: `KmhMprPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPuData_ReturnsCorrectJson`
- `ValidateDataStructure_ForMprPu_ContainsRequiredFields`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithPuJson_UpdatesCorrectly`

### 2.3 Файл: `KmhMprTprDocumentTests.cs`
### Класс: `KmhMprTprDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithTprData_ReturnsCorrectJson`
- `ValidateDataStructure_ForMprTpr_ContainsRequiredFields`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.4 Файл: `KmhPpDocumentTests.cs`
### Класс: `KmhPpDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithDensityData_ReturnsCorrectJson`
- `ValidateDensityMeasurements_InJson_ContainsCorrectStructure`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithDensityJson_UpdatesCorrectly`

### 2.5 Файл: `KmhPpAreomDocumentTests.cs`
### Класс: `KmhPpAreomDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithAreometerData_ReturnsCorrectJson`
- `ValidateAreometerSpecificFields_InJson_ArePresent`
- `ValidateSampleDivision_ForAreometer_IsHandledCorrectly`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.6 Файл: `KmhPrPrDocumentTests.cs`
### Класс: `KmhPrPrDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithFlowData_ReturnsCorrectJson`
- `ValidateFlowMeasurements_InJson_ContainsCorrectData`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithFlowJson_UpdatesCorrectly`

### 2.7 Файл: `KmhPrPuDocumentTests.cs`
### Класс: `KmhPrPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPuFlowData_ReturnsCorrectJson`
- `ValidateFlowMeasurements_InJson_ContainsCorrectData`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.8 Файл: `KmhPvDocumentTests.cs`
### Класс: `KmhPvDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithViscosityData_ReturnsCorrectJson`
- `ValidateViscosityMeasurements_InJson_ContainsCorrectData`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithViscosityJson_UpdatesCorrectly`

### 2.9 Файл: `KmhPwDocumentTests.cs`
### Класс: `KmhPwDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithWaterData_ReturnsCorrectJson`
- `ValidateWaterContentMeasurements_InJson_ContainsCorrectData`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.10 Файл: `KmhMi2816DocumentTests.cs`
### Класс: `KmhMi2816DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithMi2816Data_ReturnsCorrectJson`
- `ValidateMi2816SpecificFields_InJson_ArePresent`
- `ValidateEnvironmentalConditions_CanBeManuallyEdited`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.11 Файл: `Kmh3265PrPuDocumentTests.cs`
### Класс: `Kmh3265PrPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_With3265PrPuData_ReturnsCorrectJson`
- `Validate3265SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.12 Файл: `Kmh3265UprPrDocumentTests.cs`
### Класс: `Kmh3265UprPrDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_With3265UprPrData_ReturnsCorrectJson`
- `Validate3265UprPrSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.13 Файл: `Kmh3288MprTprDocumentTests.cs`
### Класс: `Kmh3288MprTprDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_With3288Data_ReturnsCorrectJson`
- `Validate3288SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.14 Файл: `Kmh3312PrPuDocumentTests.cs`
### Класс: `Kmh3312PrPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_With3312PrPuData_ReturnsCorrectJson`
- `Validate3312SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.15 Файл: `Kmh3312UprPrDocumentTests.cs`
### Класс: `Kmh3312UprPrDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_With3312UprPrData_ReturnsCorrectJson`
- `Validate3312UprPrSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.16 Файл: `KmxSikn425PrPrDocumentTests.cs`
### Класс: `KmxSikn425PrPrDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithSikn425PrPrData_ReturnsCorrectJson`
- `ValidateSikn425PrPrSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 2.17 Файл: `KmxSikn425PrPuDocumentTests.cs`
### Класс: `KmxSikn425PrPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithSikn425PrPuData_ReturnsCorrectJson`
- `ValidateSikn425PrPuSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

---

## 3. **Poverka документы (22 отдельных класса)**

### 3.1 Файл: `CommonPoverka1974DocumentTests.cs`
### Класс: `CommonPoverka1974DocumentTests`
### Приоритет: ВЫСОКИЙ
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka1974Data_ReturnsCorrectJson`
- `ValidateViscosityCorrection_InJson_IsAppliedCorrectly`
- `ValidateViscosityCorrection_ForFirstMeasurement_IsApplied`
- `ValidateViscosityCorrection_ForLastMeasurement_IsApplied`
- `GetTemplateFile_ForPoverka1974_ReturnsCorrectPath`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithPoverkaJson_UpdatesCorrectly`

### 3.2 Файл: `CommonSikn425DocumentTests.cs`
### Класс: `CommonSikn425DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithCommonSikn425Data_ReturnsCorrectJson`
- `ValidateSikn425CommonFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithSikn425Json_UpdatesCorrectly`

### 3.3 Файл: `Poverka1974DocumentTests.cs`
### Класс: `Poverka1974DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithBasePoverka1974_ReturnsCorrectJson`
- `ValidateBasePoverka1974Fields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.4 Файл: `Poverka1974_04DocumentTests.cs`
### Класс: `Poverka1974_04DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka1974_04_ReturnsCorrectVariant`
- `ValidateViscosityCorrection_For04Variant_IsApplied`
- `ValidateVariantSpecificFields_For04_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.5 Файл: `Poverka1974_89DocumentTests.cs`
### Класс: `Poverka1974_89DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka1974_89_ReturnsCorrectVariant`
- `ValidateViscosityCorrection_For89Variant_IsApplied`
- `ValidateVariantSpecificFields_For89_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.6 Файл: `Poverka1974_95DocumentTests.cs`
### Класс: `Poverka1974_95DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka1974_95_ReturnsCorrectVariant`
- `ValidateViscosityCorrection_For95Variant_IsApplied`
- `ValidateVariantSpecificFields_For95_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.7 Файл: `Poverka2816DocumentTests.cs`
### Класс: `Poverka2816DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka2816Data_ReturnsCorrectJson`
- `Validate2816SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.8 Файл: `Poverka3151DocumentTests.cs`
### Класс: `Poverka3151DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3151Data_ReturnsCorrectJson`
- `Validate3151SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.9 Файл: `Poverka3189DocumentTests.cs`
### Класс: `Poverka3189DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3189Data_ReturnsCorrectJson`
- `Validate3189SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.10 Файл: `Poverka3265PrPuDocumentTests.cs`
### Класс: `Poverka3265PrPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3265PrPu_ReturnsCorrectJson`
- `Validate3265PrPuSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.11 Файл: `Poverka3265UprPrDocumentTests.cs`
### Класс: `Poverka3265UprPrDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3265UprPr_ReturnsCorrectJson`
- `Validate3265UprPrSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.12 Файл: `Poverka3265UprPuDocumentTests.cs`
### Класс: `Poverka3265UprPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3265UprPu_ReturnsCorrectJson`
- `Validate3265UprPuSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.13 Файл: `Poverka3266DocumentTests.cs`
### Класс: `Poverka3266DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3266Data_ReturnsCorrectJson`
- `Validate3266SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.14 Файл: `Poverka3267DocumentTests.cs`
### Класс: `Poverka3267DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3267Data_ReturnsCorrectJson`
- `Validate3267SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.15 Файл: `Poverka3272DocumentTests.cs`
### Класс: `Poverka3272DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3272Data_ReturnsCorrectJson`
- `Validate3272SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.16 Файл: `Poverka3287DocumentTests.cs`
### Класс: `Poverka3287DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3287Data_ReturnsCorrectJson`
- `Validate3287SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.17 Файл: `Poverka3288DocumentTests.cs`
### Класс: `Poverka3288DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3288Data_ReturnsCorrectJson`
- `Validate3288SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.18 Файл: `Poverka3312PrPuDocumentTests.cs`
### Класс: `Poverka3312PrPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3312PrPu_ReturnsCorrectJson`
- `Validate3312PrPuSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.19 Файл: `Poverka3312UprPrDocumentTests.cs`
### Класс: `Poverka3312UprPrDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3312UprPr_ReturnsCorrectJson`
- `Validate3312UprPrSpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.20 Файл: `Poverka3380DocumentTests.cs`
### Класс: `Poverka3380DocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverka3380Data_ReturnsCorrectJson`
- `Validate3380SpecificFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.21 Файл: `PoverkaSikn425PrPrDocumentTests.cs`
### Класс: `PoverkaSikn425PrPrDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverkaSikn425PrPr_ReturnsCorrectJson`
- `ValidatePoverkaSikn425PrPrFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

### 3.22 Файл: `PoverkaSikn425PrPuDocumentTests.cs`
### Класс: `PoverkaSikn425PrPuDocumentTests`
- `Constructor_WithValidParameters_InitializesCorrectly`
- `GetViewDoc_WithPoverkaSikn425PrPu_ReturnsCorrectJson`
- `ValidatePoverkaSikn425PrPuFields_InJson_ArePresent`
- `GetEditDoc_WithValidId_ReturnsValidHtml`

---

## 4. **Passport (Паспорта качества)**

### Файл: `PassportDocumentTests.cs`
### Класс: `PassportDocumentTests`
### Приоритет: КРИТИЧЕСКИЙ

#### Тестовые методы:
- `Constructor_WithValidParameters_InitializesCorrectly`
- `Constructor_WithNullParameters_ThrowsArgumentException`
- `GetViewDoc_WithPassportData_ReturnsValidJsonString`
- `GetViewDoc_WithElisIntegration_IncludesElisData`
- `GetViewDoc_WithQualityParameters_IncludesAllParameters`
- `GetViewDoc_WithInvalidId_HandlesGracefully`
- `GetEditDoc_WithRequiredFields_MarksFieldsAsRequired`
- `GetEditDoc_WithElisDisabled_HidesElisSpecificFields`
- `GetEditDoc_WithValidId_ReturnsValidHtmlString`
- `SetDocFromJson_WithPassportJson_UpdatesCorrectly`
- `SetDocFromJson_WithElisJson_UpdatesElisFields`
- `SetDocFromJson_WithInvalidJson_ThrowsException`
- `ValidateJsonStructure_ForPassport_ContainsRequiredSections`
- `ValidateJsonStructure_WithQualityParameters_ContainsParameterData`
- `ValidateJsonStructure_WithMethods_ContainsMethodInfo`
- `GetPathTemplateFile_ForDifferentPassportTypes_ReturnsCorrectPaths`
- `ValidateParameterMethods_InJson_ContainsMethodInfo`
- `ValidateAdditionalInfo_InJson_ContainsRequiredFields`
- `ValidateElisIntegration_WithElisData_ProcessesCorrectly`

#### Интеграционные тесты:
- Полный цикл с ELIS: получение данных → заполнение → валидация → сохранение
- Тестирование различных типов паспортов (ГОСТ, МИ3532, EAC, Export)

---

## 5. **Jornal (Журналы)**

### Файл: `JornalDocumentTests.cs`
### Класс: `JornalDocumentTests`
### Приоритет: СРЕДНИЙ

#### Тестовые методы:
- `Constructor_WithValidParameters_InitializesCorrectly`
- `Constructor_WithNullParameters_ThrowsArgumentException`
- `GetViewDoc_WithJornalData_ReturnsValidJsonString`
- `GetViewDoc_WithDateRange_FiltersEntriesCorrectly`
- `GetViewDoc_WithInvalidId_HandlesGracefully`
- `ValidateJsonStructure_ForJornal_ContainsLogEntries`
- `ValidateJsonStructure_WithDateSorting_IsSortedCorrectly`
- `GetEditDoc_ForJornal_ReturnsEditForm`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithJornalJson_UpdatesCorrectly`
- `SetDocFromJson_WithInvalidJson_ThrowsException`
- `GetPathTemplateFile_ReturnsExistingFilePath`
- `GetPathConfigFile_ReturnsExistingConfigPath`

---

## 6. **Report (Отчеты)**

### Файл: `ReportDocumentTests.cs`
### Класс: `ReportDocumentTests`
### Приоритет: СРЕДНИЙ

#### Тестовые методы:
- `Constructor_WithValidParameters_InitializesCorrectly`
- `Constructor_WithNullParameters_ThrowsArgumentException`
- `GetViewDoc_WithReportData_ReturnsValidJsonString`
- `GetViewDoc_WithIncompleteReports_IncludesIncompleteData`
- `GetViewDoc_WithInvalidId_HandlesGracefully`
- `ValidateJsonStructure_ForReport_ContainsReportSections`
- `ValidateJsonStructure_WithIncompleteReports_ContainsCorrectStructure`
- `GetEditDoc_ForReport_ReturnsEditForm`
- `GetEditDoc_WithValidId_ReturnsValidHtml`
- `SetDocFromJson_WithReportJson_UpdatesCorrectly`
- `SetDocFromJson_WithInvalidJson_ThrowsException`
- `GetPathTemplateFile_ReturnsExistingFilePath`
- `ValidateIncompleteReportsConfiguration_WorksCorrectly`

---

## Дополнительные тестовые классы

### Файл: `DocumentConfigurationTests.cs`
### Класс: `DocumentConfigurationTests`
### Приоритет: СРЕДНИЙ

#### Тестовые методы для каждой библиотеки:
- `ValidateConfigPath_ForAct_PathExists`
- `ValidateConfigPath_ForPassport_PathExists`
- `ValidateConfigPath_ForJornal_PathExists`
- `ValidateConfigPath_ForReport_PathExists`
- `ValidateConfigPath_ForAllKmhLibraries_PathsExist`
- `ValidateConfigPath_ForAllPoverkaLibraries_PathsExist`
- `ValidateEditConfigPath_ForAct_PathExists`
- `ValidateEditConfigPath_ForPassport_PathExists`
- `ValidateEditConfigPath_ForAllLibraries_PathsExist`
- `ValidateTemplatePath_ForAct_PathExists`
- `ValidateTemplatePath_ForPassport_PathExists`
- `ValidateTemplatePath_ForAllLibraries_PathsExist`
- `LoadConfiguration_ForAllLibraries_LoadsSuccessfully`
- `ValidateConfigurationStructure_ForAllLibraries_IsCorrect`

### Файل: `DocumentInterfaceComplianceTests.cs`
### Класс: `DocumentInterfaceComplianceTests`
### Приоритет: ВЫСОКИЙ

#### Тестовые методы для всех библиотек:
- `AllLibraries_ImplementRequiredMethods_ReturnsTrue`
- `AllLibraries_HaveParameterlessConstructor_ReturnsTrue`
- `GetViewDoc_ForAllLibraries_ReturnsStringOrNull`
- `GetViewDoc_ForAllLibraries_DoesNotThrowUnexpectedException`
- `GetPathTemplateFile_ForAllLibraries_ReturnsValidPath`
- `GetPathTemplateFile_ForAllLibraries_PathExists`
- `GetEditDoc_ForAllLibraries_ReturnsStringOrNull`
- `GetEditDoc_ForAllLibraries_DoesNotThrowUnexpectedException`
- `SetDocFromJson_ForAllLibraries_DoesNotThrowUnexpected`
- `SetDocFromJson_WithNullJson_ForAllLibraries_HandlesCorrectly`
- `ValidateInheritance_AllLibraries_InheritFromDocGeneral`
- `ValidateIdDocProperty_ForAllLibraries_IsSetCorrectly`

---

## 🗂️ Дополнение существующей структуры тестов

### 📁 Текущая структура (обновлено: январь 2025):
```
Tests/
├── Tests.csproj                               ✅ (существует, обновлен)
├── Services/
│   ├── AppConfigServiceTests.cs              ✅ (существует)
│   ├── CfgAppSyncTests.cs                    ✅ (существует)
│   └── DbSchemaCacheTests.cs                 ✅ (существует)
├── controllers/                              ✅ (существует)
│   ├── ClientLogControllerTests.cs           ✅ (существует)
│   ├── DirEditorControllerTests.cs           ✅ (существует)
│   ├── ElisControllerTests.cs                ✅ (существует)
│   ├── ExportControllerTests.cs              ✅ (существует)
│   ├── HomeControllerTests.cs                ✅ (существует)
│   ├── PdfControllerTests.cs                 ✅ (существует)
│   └── PrintControllerTests.cs               ✅ (существует)
├── Libraries/                                ✅ (создана)
│   ├── BaseDocumentTest.cs                   ✅ (базовый класс)
│   ├── Core/                                 ✅ (создана)
│   │   ├── ActDocumentTests.cs               ✅ (реализовано)
│   │   ├── PassportDocumentTests.cs          ✅ (реализовано)
│   │   ├── JornalDocumentTests.cs            ✅ (реализовано)
│   │   └── ReportDocumentTests.cs            ✅ (реализовано)
│   ├── Common/                               ✅ (создана)
│   │   ├── CommonPoverka1974DocumentTests.cs ✅ (реализовано)
│   │   └── CommonSikn425DocumentTests.cs     ✅ (реализовано)
│   └── Integration/                          ✅ (создана)
│       └── DocumentInterfaceComplianceTests.cs ✅ (реализовано)
├── Fixtures/
│   ├── DocumentTestDataFixture.cs            ✅ (создан)
│   └── DocumentTestHelpers.cs                ✅ (создан)
└── UsersTests.cs                             ✅ (существует)
```

### 📁 Планируемая структура (оставшиеся тесты - 36 файлов):
```
Tests/
├── Libraries/
│   ├── KMH/ (17 файлов)                      ⏳ (планируется)
│   │   ├── KmhMprMprDocumentTests.cs         ⏳
│   │   ├── KmhMprPuDocumentTests.cs          ⏳
│   │   ├── KmhMprTprDocumentTests.cs         ⏳
│   │   ├── KmhPpDocumentTests.cs             ⏳
│   │   ├── KmhPpAreomDocumentTests.cs        ⏳
│   │   ├── KmhPrPrDocumentTests.cs           ⏳
│   │   ├── KmhPrPuDocumentTests.cs           ⏳
│   │   ├── KmhPvDocumentTests.cs             ⏳
│   │   ├── KmhPwDocumentTests.cs             ⏳
│   │   ├── KmhMi2816DocumentTests.cs         ⏳
│   │   ├── Kmh3265PrPuDocumentTests.cs       ⏳
│   │   ├── Kmh3265UprPrDocumentTests.cs      ⏳
│   │   ├── Kmh3288MprTprDocumentTests.cs     ⏳
│   │   ├── Kmh3312PrPuDocumentTests.cs       ⏳
│   │   ├── Kmh3312UprPrDocumentTests.cs      ⏳
│   │   ├── KmxSikn425PrPrDocumentTests.cs    ⏳
│   │   └── KmxSikn425PrPuDocumentTests.cs    ⏳
│   └── Poverka/ (19 файлов)                  ⏳ (планируется)
│       ├── Poverka1974DocumentTests.cs       ⏳
│       ├── Poverka1974_04DocumentTests.cs    ⏳
│       ├── Poverka1974_89DocumentTests.cs    ⏳
│       ├── Poverka1974_95DocumentTests.cs    ⏳
│       ├── Poverka2816DocumentTests.cs       ⏳
│       ├── Poverka3151DocumentTests.cs       ⏳
│       ├── Poverka3189DocumentTests.cs       ⏳
│       ├── Poverka3265PrPuDocumentTests.cs   ⏳
│       ├── Poverka3265UprPrDocumentTests.cs  ⏳
│       ├── Poverka3265UprPuDocumentTests.cs  ⏳
│       ├── Poverka3266DocumentTests.cs       ⏳
│       ├── Poverka3267DocumentTests.cs       ⏳
│       ├── Poverka3272DocumentTests.cs       ⏳
│       ├── Poverka3287DocumentTests.cs       ⏳
│       ├── Poverka3288DocumentTests.cs       ⏳
│       ├── Poverka3312PrPuDocumentTests.cs   ⏳
│       ├── Poverka3312UprPrDocumentTests.cs  ⏳
│       ├── Poverka3380DocumentTests.cs       ⏳
│       ├── PoverkaSikn425PrPrDocumentTests.cs ⏳
│       └── PoverkaSikn425PrPuDocumentTests.cs ⏳
└── [все остальные файлы уже реализованы] ✅
```

### 📊 Статистика изменений:
- **Существующие тесты до начала**: 13 файлов (контроллеры + сервисы + пользователи)
- **Реализовано (Фаза 1)**: ✅ 7 файлов библиотек + базовый класс + 2 fixture
  - BaseDocumentTest.cs + DocumentTestHelpers.cs + DocumentTestDataFixture.cs
  - 4 Core документа (Act, Passport, Jornal, Report)
  - 2 Common библиотеки (CommonPoverka1974, CommonSikn425)
  - 1 Integration тест (DocumentInterfaceComplianceTests)
- **Осталось реализовать**: ⏳ 36 файлов (17 KMH + 19 Poverka)
- **Всего после завершения**: 56+ файлов
- **Созданные папки**: `Libraries/`, `Libraries/Core/`, `Libraries/Common/`, `Libraries/Integration/`, `Fixtures/`
- **Планируемые папки**: `Libraries/KMH/`, `Libraries/Poverka/`

## 📋 Приоритизация реализации

> **💡 РЕКОМЕНДАЦИЯ**: См. [TEST_COVERAGE_PLAN_ANALYSIS.md](./TEST_COVERAGE_PLAN_ANALYSIS.md) для оптимизированного плана с:
> - Параметризованными тестами (снижение дублирования на 80%)
> - Базовыми классами для переиспользования
> - Интеграционными тестами (90+)
> - Сокращением времени реализации на 27%

### 🔥 Фаза 1 - Критическая функциональность ✅ **ЗАВЕРШЕНА**
**Цель**: Покрыть основной документооборот и интерфейсы

**Реализовано**:
1. ✅ **`BaseDocumentTest<T>`** - базовый класс для всех тестов документов
2. ✅ **`DocumentTestDataFixture`** - генераторы тестовых данных
3. ✅ **`DocumentTestHelpers`** - вспомогательные методы валидации
4. ✅ **`CommonPoverka1974DocumentTests.cs`** - базовая поверка (используется в 4 вариантах)
5. ✅ **`CommonSikn425DocumentTests.cs`** - базовый Sikn425 (используется в 4 модулях)
6. ✅ **`DocumentInterfaceComplianceTests.cs`** - параметризованные тесты соответствия интерфейсу
7. ✅ **`PassportDocumentTests.cs`** - паспорта качества (основной документооборот + ELIS)
8. ✅ **`ActDocumentTests.cs`** - акты приема-сдачи
9. ✅ **`JornalDocumentTests.cs`** - журналы
10. ✅ **`ReportDocumentTests.cs`** - отчеты

**Результат**: ✅ **100+ тестов реализовано**, базовая инфраструктура работает, проект компилируется без ошибок

**Примечание**: ActProducer, ActRoute не существуют в текущей кодовой базе

### ⚡ Фаза 2 - Высокий приоритет (3 недели)
**Цель**: Покрыть основные измерительные модули

1. **`KmhMprMprDocumentTests.cs`** - измерения массы и плотности
3. **`KmhPrPrDocumentTests.cs`** - контроль расхода
4. **`KmhPpDocumentTests.cs`** - контроль плотности
5. **`KmhPvDocumentTests.cs`** - контроль вязкости
6. **`KmhPwDocumentTests.cs`** - контроль воды

**Ожидаемый результат**: 150+ тестов, покрытие основных KMH модулей

### 📊 Фаза 3 - Средний приоритет (4 недели)
**Цель**: Покрыть специализированные модули и отчетность

1. **Остальные KMH модули** (11 файлов):
   - `KmhMprPuDocumentTests.cs`
   - `KmhMprTprDocumentTests.cs`
   - `KmhPpAreomDocumentTests.cs`
   - `KmhPrPuDocumentTests.cs`
   - `KmhMi2816DocumentTests.cs`
   - Все 3265/3288/3312 модули
   - Sikn425 модули

2. **Отчетность**:
   - `JornalDocumentTests.cs`
   - `ReportDocumentTests.cs`

**Ожидаемый результат**: 250+ тестов, покрытие всех KMH модулей

### 🔧 Фаза 4 - Специфическая функциональность (6 недель)
**Цель**: Полное покрытие всех Poverka модулей

1. **Базовые Poverka модули**:
   - `Poverka1974DocumentTests.cs` (базовый вариант)
   - Все варианты 1974 (04, 89, 95)

2. **Специфические Poverka модули** (16 файлов):
   - Все модули от 2816 до 3380
   - Sikn425 поверки

**Ожидаемый результат**: 400+ тестов, полное покрытие всех модулей

## 🎯 Метрики успеха

### Количественные метрики
- **Общее количество тестов**: 500+ тестовых методов
- **Покрытие кода**: 80%+ для каждой библиотеки
- **Покрытие критических методов**: 95%+
- **Количество тестовых классов**: 45+ классов
- **Покрытие интерфейсных методов**: 100%

> **💡 УЛУЧШЕННЫЕ МЕТРИКИ**: См. [TEST_COVERAGE_PLAN_ANALYSIS.md](./TEST_COVERAGE_PLAN_ANALYSIS.md) для:
> - 590+ тестов (вместо 500+)
> - 85%+ покрытие кода (вместо 80%+)
> - 98%+ покрытие критических методов (вместо 95%+)
> - 28 классов с параметризацией (вместо 45+ отдельных)

### Качественные метрики
- **Автоматизация**: Полная CI/CD интеграция
- **Стабильность**: <1% flaky tests
- **Производительность**: Все тесты выполняются <5 минут
- **Поддерживаемость**: Четкая структура и naming convention

### Покрытие по категориям
- **Act (3 варианта)**: 100% методов интерфейса + специфическая логика + v1.4.2 logging
- **Passport**: 100% + ELIS интеграция + валидации + v1.4.2 logging
- **KMH модули**: 90%+ основной функциональности
- **Poverka модули**: 85%+ основной функциональности
- **Jornal/Report**: 80%+ функциональности
- **Configuration**: 100% путей и структур
- **Common модули**: 100% (критично для зависимых модулей)

### Дополнительные требования
- **Документация**: README для каждой категории тестов
- **Мокирование**: Все внешние зависимости замокированы
- **Изоляция**: Тесты не зависят друг от друга
- **Читаемость**: Четкие имена тестов и комментарии
- **Производительность**: Performance тесты для GetViewDoc методов

---

## 📚 Заключение

### ✅ Текущий статус (январь 2025):

**Фаза 1 завершена!** Базовая тестовая инфраструктура полностью реализована:

- ✅ **Базовый класс `BaseDocumentTest<T>`** для всех тестов
- ✅ **Fixtures и helpers** для генерации тестовых данных
- ✅ **7 тестовых классов** для Core и Common библиотек (100+ тестов)
- ✅ **Параметризованные интеграционные тесты** для валидации интерфейсов
- ✅ **Проект компилируется без ошибок** (0 errors, 1 version warning)

### 🎯 Дальнейшая реализация:

Данный план обеспечивает:

1. **Полное покрытие** всех 42 библиотек документооборота (без ActProducer, ActRoute, ReportIncomplete)
2. **Поэтапную реализацию** с приоритизацией критической функциональности ✅ **Фаза 1 завершена**
3. **Соблюдение принципа неизменности** существующего кода ✅ **Выполнено**
4. **Четкую структуру** с отдельным классом для каждой библиотеки
5. **Комплексное тестирование** интерфейсов, конфигураций и интеграций
6. **Тестирование v1.4.2 улучшений** (GetEditDoc с Path.Combine и trace logging)

### 📊 Прогресс:

- **Реализовано**: 7 из 43 тестовых классов (16%)
- **Осталось**: 36 тестовых классов (17 KMH + 19 Poverka)
- **Время**: ~9-11 недель для завершения Фаз 2-4

План рассчитан на реализацию в течение 11-13 недель (с учетом завершенной Фазы 1) с командой из 2-3 разработчиков и обеспечит высокое качество и надежность системы документооборота TN_Doc.

---

## 🔍 Дополнительные материалы

**📄 [TEST_COVERAGE_PLAN_ANALYSIS.md](./TEST_COVERAGE_PLAN_ANALYSIS.md)** - Детальный анализ и оптимизация плана:
- Критический анализ с выявлением проблемных мест
- Оптимизированный план с параметризованными тестами
- Уменьшение дублирования кода на 80%
- Сокращение времени реализации на 27% (11 недель вместо 15)
- Добавление 90+ интеграционных тестов
- Базовые классы и fixtures для ускорения разработки
- Правильная приоритизация (Common модули первыми)