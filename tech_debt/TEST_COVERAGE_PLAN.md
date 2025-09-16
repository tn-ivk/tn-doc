# План покрытия тестами библиотек TN_Doc

## 📋 Текущее состояние проекта тестов

### 🎯 Существующая структура `Tests/`
Проект уже содержит базовую инфраструктуру тестирования:

#### ✅ Настроенные зависимости:
- **NUnit** (4.3.2) - тестовый фреймворк
- **Moq** (4.20.72) - мокирование
- **Microsoft.EntityFrameworkCore.InMemory** (7.0.20) - in-memory БД для тестов
- **HtmlAgilityPack** (1.12.1) - парсинг HTML
- **coverlet.collector** (6.0.4) - покрытие кода

#### ✅ Существующие тесты:
- **Controllers/**: `HomeController`, `PdfController`, `PrintController`, `ExportController`, `DirEditorController`, `ElisController`, `ClientLogController`
- **Services/**: `AppConfigService`
- **Users/**: `UsersTests`

#### ✅ Project References:
- `TN_Doc.csproj` (основное веб-приложение)
- `TN.DocGeneral.csproj` (общая библиотека)
- `Act.csproj`, `Passport.csproj` (документные модули)
- `TN.Utils.csproj` (утилиты)

### 🎯 Namespace Convention
Существующие тесты используют: `Tests.Controllers`, `Tests.Services`

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

## 1. **Act (Акты приема-сдачи)**

### Файл: `ActDocumentTests.cs`
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
- `GetEditDoc_WithInvalidId_ReturnsDefaultOrError`
- `SetDocFromJson_WithValidJson_DoesNotThrowException`
- `SetDocFromJson_WithInvalidJson_ThrowsException`
- `SetDocFromJson_WithEmptyJson_ThrowsException`

#### Интеграционные тесты:
- Полный цикл: создание → редактирование → сохранение → генерация отчета

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

### 📁 Текущая структура (существующие тесты):
```
Tests/
├── Tests.csproj                               ✅ (существует)
├── Services/
│   └── AppConfigServiceTests.cs              ✅ (существует)
├── controllers/                              ✅ (существует)
│   ├── ClientLogControllerTests.cs           ✅ (существует)
│   ├── DirEditorControllerTests.cs           ✅ (существует)
│   ├── ElisControllerTests.cs                ✅ (существует)
│   ├── ExportControllerTests.cs              ✅ (существует)
│   ├── HomeControllerTests.cs                ✅ (существует)
│   ├── PdfControllerTests.cs                 ✅ (существует)
│   └── PrintControllerTests.cs               ✅ (существует)
└── UsersTests.cs                             ✅ (существует)
```

### 📁 Новая структура (добавляемые тесты библиотек - 43 файла):
```
Tests/
├── Libraries/                                🆕 (новая папка)
│   ├── ActDocumentTests.cs (1)               🆕
│   ├── PassportDocumentTests.cs (1)          🆕
│   ├── JornalDocumentTests.cs (1)            🆕
│   ├── ReportDocumentTests.cs (1)            🆕
│   ├── CommonPoverka1974DocumentTests.cs (1) 🆕
│   ├── CommonSikn425DocumentTests.cs (1)     🆕
│   ├── KMH/ (17 файлов)                      🆕 (новая подпапка)
│   │   ├── KmhMprMprDocumentTests.cs         🆕
│   │   ├── KmhMprPuDocumentTests.cs          🆕
│   │   ├── KmhMprTprDocumentTests.cs         🆕
│   │   ├── KmhPpDocumentTests.cs             🆕
│   │   ├── KmhPpAreomDocumentTests.cs        🆕
│   │   ├── KmhPrPrDocumentTests.cs           🆕
│   │   ├── KmhPrPuDocumentTests.cs           🆕
│   │   ├── KmhPvDocumentTests.cs             🆕
│   │   ├── KmhPwDocumentTests.cs             🆕
│   │   ├── KmhMi2816DocumentTests.cs         🆕
│   │   ├── Kmh3265PrPuDocumentTests.cs       🆕
│   │   ├── Kmh3265UprPrDocumentTests.cs      🆕
│   │   ├── Kmh3288MprTprDocumentTests.cs     🆕
│   │   ├── Kmh3312PrPuDocumentTests.cs       🆕
│   │   ├── Kmh3312UprPrDocumentTests.cs      🆕
│   │   ├── KmxSikn425PrPrDocumentTests.cs    🆝
│   │   └── KmxSikn425PrPuDocumentTests.cs    🆕
│   ├── Poverka/ (20 файлов)                  🆕 (новая подпапка)
│   │   ├── Poverka1974DocumentTests.cs       🆕
│   │   ├── Poverka1974_04DocumentTests.cs    🆕
│   │   ├── Poverka1974_89DocumentTests.cs    🆕
│   │   ├── Poverka1974_95DocumentTests.cs    🆕
│   │   ├── Poverka2816DocumentTests.cs       🆕
│   │   ├── Poverka3151DocumentTests.cs       🆕
│   │   ├── Poverka3189DocumentTests.cs       🆕
│   │   ├── Poverka3265PrPuDocumentTests.cs   🆕
│   │   ├── Poverka3265UprPrDocumentTests.cs  🆕
│   │   ├── Poverka3265UprPuDocumentTests.cs  🆕
│   │   ├── Poverka3266DocumentTests.cs       🆕
│   │   ├── Poverka3267DocumentTests.cs       🆕
│   │   ├── Poverka3272DocumentTests.cs       🆕
│   │   ├── Poverka3287DocumentTests.cs       🆕
│   │   ├── Poverka3288DocumentTests.cs       🆕
│   │   ├── Poverka3312PrPuDocumentTests.cs   🆕
│   │   ├── Poverka3312UprPrDocumentTests.cs  🆕
│   │   ├── Poverka3380DocumentTests.cs       🆕
│   │   ├── PoverkaSikn425PrPrDocumentTests.cs 🆕
│   │   └── PoverkaSikn425PrPuDocumentTests.cs 🆕
│   ├── Configuration/                        🆕 (новая подпапка)
│   │   └── DocumentConfigurationTests.cs     🆕
│   └── Integration/                          🆕 (новая подпапка)
│       └── DocumentInterfaceComplianceTests.cs 🆕
└── [существующие файлы остаются без изменений] ✅
```

### 📊 Статистика изменений:
- **Существующие файлы**: 10 (контроллеры + сервисы + пользователи)
- **Новые файлы библиотек**: 43
- **Всего файлов после дополнения**: 53
- **Новые папки**: 4 (`Libraries/`, `Libraries/KMH/`, `Libraries/Poverka/`, `Libraries/Configuration/`, `Libraries/Integration/`)

## 📋 Приоритизация реализации

### 🔥 Фаза 1 - Критическая функциональность (2 недели)
**Цель**: Покрыть основной документооборот и интерфейсы

1. **`DocumentInterfaceComplianceTests.cs`** - валидация соответствия всех библиотек общему интерфейсу
2. **`PassportDocumentTests.cs`** - паспорта качества (основной документооборот)
3. **`ActDocumentTests.cs`** - акты приема-сдачи
4. **`DocumentConfigurationTests.cs`** - проверка конфигураций

**Ожидаемый результат**: 100+ тестов, покрытие критической функциональности

### ⚡ Фаза 2 - Высокий приоритет (3 недели)
**Цель**: Покрыть основные измерительные модули

1. **`CommonPoverka1974DocumentTests.cs`** - базовая поверка
2. **`KmhMprMprDocumentTests.cs`** - измерения массы и плотности
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
   - `CommonSikn425DocumentTests.cs`
   - `Poverka1974DocumentTests.cs`
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
- **Количество тестовых классов**: 43 класса
- **Покрытие интерфейсных методов**: 100%

### Качественные метрики
- **Автоматизация**: Полная CI/CD интеграция
- **Стабильность**: <1% flaky tests
- **Производительность**: Все тесты выполняются <5 минут
- **Поддерживаемость**: Четкая структура и naming convention

### Покрытие по категориям
- **Act**: 100% методов интерфейса + специфическая логика
- **Passport**: 100% + ELIS интеграция + валидации
- **KMH модули**: 90%+ основной функциональности
- **Poverka модули**: 85%+ основной функциональности
- **Jornal/Report**: 80%+ функциональности
- **Configuration**: 100% путей и структур

### Дополнительные требования
- **Документация**: README для каждой категории тестов
- **Мокирование**: Все внешние зависимости замокированы
- **Изоляция**: Тесты не зависят друг от друга
- **Читаемость**: Четкие имена тестов и комментарии
- **Производительность**: Performance тесты для GetViewDoc методов

---

## 📚 Заключение

Данный план обеспечивает:

1. **Полное покрытие** всех 41 библиотеки документооборота
2. **Поэтапную реализацию** с приоритизацией критической функциональности
3. **Соблюдение принципа неизменности** существующего кода
4. **Четкую структуру** с отдельным классом для каждой библиотеки
5. **Комплексное тестирование** интерфейсов, конфигураций и интеграций

План рассчитан на реализацию в течение 15 недель с командой из 2-3 разработчиков и обеспечит высокое качество и надежность системы документооборота TN_Doc.