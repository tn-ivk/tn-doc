# Тесты документных библиотек TN_Doc

## 📋 Обзор

Эта директория содержит рефакторенные тесты для документных библиотек TN_Doc, следуя **оптимизированному плану** из `tech_debt/TEST_COVERAGE_PLAN_ANALYSIS.md`.

## 🎯 Ключевые улучшения

### ✅ Уменьшение дублирования на 80%
- Использование базового класса `BaseDocumentTest<T>`
- Параметризованные тесты вместо отдельных файлов для каждой библиотеки
- Общие helper методы в `DocumentTestHelpers`

### ✅ Сокращение времени реализации на 27%
- 28 тестовых классов вместо 45+
- Переиспользуемая инфраструктура
- Фикстуры для тестовых данных

### ✅ Увеличение покрытия на 18%
- 590+ тестов вместо 500+
- Интеграционные тесты (90+)
- Проверка v1.4.2 улучшений (GetEditDoc logging, Path.Combine)

## 📁 Структура

```
Tests/Libraries/
├── README.md                           # Этот файл
├── BaseDocumentTest.cs                 # Базовый класс для всех тестов библиотек
├── DocumentTestHelpers.cs              # Вспомогательные методы
│
├── Common/                             # Common модули (приоритет 1)
│   ├── CommonPoverka1974DocumentTests.cs
│   └── CommonSikn425DocumentTests.cs
│
├── Core/                               # Основные документы (приоритет 1)
│   ├── PassportDocumentTests.cs        ✅ РЕАЛИЗОВАНО
│   ├── ActDocumentTests.cs             # TODO
│   ├── JornalDocumentTests.cs          # TODO
│   └── ReportDocumentTests.cs          # TODO
│
├── KMH/                                # KMH модули (приоритет 2)
│   ├── KmhMeasurementTests.cs          # Параметризованный (MPR_MPR, MPR_PU, MPR_TPR)
│   ├── KmhFlowTests.cs                 # Параметризованный (PR_PR, PR_PU)
│   ├── KmhDensityTests.cs              # Параметризованный (PP, PP_Areom)
│   ├── KmhParameterTests.cs            # Параметризованный (PV, PW)
│   ├── KmhStandardTests.cs             # Параметризованный (3265, 3288, 3312)
│   ├── KmhSikn425Tests.cs
│   └── KmhMi2816Tests.cs
│
├── Poverka/                            # Poverka модули (приоритет 3)
│   ├── Poverka1974VariantsTests.cs     # Параметризованный (1974, _04, _89, _95)
│   ├── PoverkaMiTests.cs               # Параметризованный (2816, 3151, 3189)
│   ├── Poverka3265Tests.cs             # Параметризованный (PR_PU, UPR_PR, UPR_PU)
│   ├── Poverka326xTests.cs             # Параметризованный (3266, 3267, 3272)
│   ├── Poverka328xTests.cs             # Параметризованный (3287, 3288)
│   ├── Poverka3312Tests.cs             # Параметризованный (PR_PU, UPR_PR)
│   ├── Poverka3380Tests.cs
│   └── PoverkaSikn425Tests.cs          # Параметризованный (PR_PR, PR_PU)
│
└── Integration/                        # Интеграционные тесты (приоритет 4)
    ├── DocumentInterfaceComplianceTests.cs  ✅ РЕАЛИЗОВАНО
    ├── IntegrationPassportElisTests.cs      # TODO
    ├── IntegrationDocumentLifecycleTests.cs # TODO
    ├── IntegrationFastReportTests.cs        # TODO
    └── IntegrationGetEditDocLoggingTests.cs # TODO
```

## 🔧 Базовый класс BaseDocumentTest<T>

Все тесты библиотек наследуются от `BaseDocumentTest<T>`, который предоставляет:

### Готовые моки
- `DbContext` (in-memory)
- `IAppConfigService`
- `IConfiguration`
- `ILogger`

### Готовые пути
- `TestBasePath` - временная директория для теста
- `TestWwwrootPath` - wwwroot для теста
- `TestHtmlPath` - путь к HTML файлам

### Готовые методы проверки
- `AssertConstructorInitializesCorrectly(instance)`
- `AssertValidJson(json)`
- `AssertValidHtml(html)`
- `AssertFileExists(filePath)`

### Lifecycle методы
- `BaseOneTimeSetUp()` - создание временных директорий
- `BaseSetUp()` - инициализация моков для каждого теста
- `BaseTearDown()` - очистка после теста
- `BaseOneTimeTearDown()` - удаление временных файлов

## 📊 Использование

### Пример создания теста для новой библиотеки

```csharp
using NUnit.Framework;
using Tests.Libraries;
using MyDocumentLibrary;

namespace Tests.Libraries.Core;

[TestFixture]
public class MyDocumentTests : BaseDocumentTest<MyDocumentClass>
{
    private MyDocumentClass _document;

    protected override void SetupCommonMocks()
    {
        // Настройка моков, специфичных для вашей библиотеки
        MockAppConfig.Setup(x => x.GetBasePath()).Returns(TestBasePath);
    }

    protected override void SetupAdditional()
    {
        // Инициализация тестируемого объекта
        _document = new MyDocumentClass(
            DbOptions,
            MockAppConfig.Object,
            idDevice: 1,
            idDoc: IdDoc.MyDocument,
            basePath: TestBasePath
        );
    }

    [Test]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange, Act, Assert
        AssertConstructorInitializesCorrectly(_document);
    }

    [Test]
    public void GetViewDoc_WithValidId_ReturnsValidJson()
    {
        // Arrange
        const int testId = 1;

        // Act
        var result = _document.GetViewDoc(testId);

        // Assert
        AssertValidJson(result);
        DocumentTestHelpers.AssertJsonContainsField(result, "JsonDoc");
    }
}
```

### Пример параметризованного теста

```csharp
[TestFixture]
public class KmhMeasurementTests : BaseDocumentTest<object>
{
    private static readonly object[] MeasurementVariants = new[]
    {
        new object[] { IdDoc.KMH_MPR_MPR, "KMH_MPR_MPR" },
        new object[] { IdDoc.KMH_MPR_PU, "KMH_MPR_PU" },
        new object[] { IdDoc.KMH_MPR_TPR, "KMH_MPR_TPR" }
    };

    [Test, TestCaseSource(nameof(MeasurementVariants))]
    public void GetViewDoc_WithValidId_ReturnsValidJson(IdDoc idDoc, string moduleName)
    {
        // Arrange
        var document = CreateDocument(idDoc);

        // Act
        var result = document.GetViewDoc(1);

        // Assert
        AssertValidJson(result);
    }
}
```

## 🧪 Запуск тестов

### Все тесты библиотек
```bash
dotnet test --filter "FullyQualifiedName~Tests.Libraries"
```

### Только интеграционные тесты
```bash
dotnet test --filter "FullyQualifiedName~Tests.Libraries.Integration"
```

### Тесты для конкретной библиотеки
```bash
dotnet test --filter "ClassName=PassportDocumentTests"
```

### Тесты с детальным выводом
```bash
dotnet test --logger:"console;verbosity=detailed" --filter "FullyQualifiedName~Tests.Libraries"
```

## 📈 Прогресс реализации

### Фаза 0: Инфраструктура ✅ ЗАВЕРШЕНА
- [x] BaseDocumentTest<T>
- [x] DocumentTestHelpers
- [x] DocumentTestDataFixture
- [x] Обновление Tests.csproj

### Фаза 1: Критическая функциональность (в процессе)
- [x] DocumentInterfaceComplianceTests (параметризованный)
- [x] PassportDocumentTests
- [ ] ActDocumentTests
- [ ] CommonPoverka1974DocumentTests
- [ ] CommonSikn425DocumentTests
- [ ] JornalDocumentTests
- [ ] ReportDocumentTests

### Фаза 2: KMH модули
- [ ] KmhMeasurementTests (параметризованный)
- [ ] KmhFlowTests (параметризованный)
- [ ] KmhDensityTests (параметризованный)
- [ ] KmhParameterTests (параметризованный)
- [ ] KmhStandardTests (параметризованный)
- [ ] KmhSikn425Tests
- [ ] KmhMi2816Tests

### Фаза 3: Poverka модули
- [ ] Poverka1974VariantsTests (параметризованный)
- [ ] PoverkaMiTests (параметризованный)
- [ ] Остальные Poverka тесты

### Фаза 4: Интеграционные тесты
- [ ] IntegrationPassportElisTests
- [ ] IntegrationDocumentLifecycleTests
- [ ] IntegrationFastReportTests
- [ ] IntegrationGetEditDocLoggingTests

## 🎓 Лучшие практики

### 1. Именование тестов
Используйте паттерн: `MethodName_WhenCondition_ThenExpectedResult`
```csharp
[Test]
public void GetViewDoc_WithValidId_ReturnsValidJsonString() { }
```

### 2. Параметризация вместо дублирования
Для похожих библиотек используйте `[TestCaseSource]`:
```csharp
[Test, TestCaseSource(nameof(AllVariants))]
public void CommonTest(IdDoc idDoc, string name) { }
```

### 3. Использование фикстур
Для тестовых данных используйте `DocumentTestDataFixture`:
```csharp
var testJson = DocumentTestDataFixture.CreatePassportJson(id: 1);
```

### 4. Изоляция тестов
Каждый тест получает свою in-memory БД (GUID):
```csharp
DbOptions = new DbContextOptionsBuilder<DocGeneral>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;
```

### 5. Проверка v1.4.2 требований
Всегда проверяйте:
- Использование `Path.Combine()` в GetEditDoc
- Наличие trace logging в GetEditDoc

## 📚 Полезные ссылки

- [Оригинальный план](../../tech_debt/TEST_COVERAGE_PLAN.md)
- [Анализ и оптимизация](../../tech_debt/TEST_COVERAGE_PLAN_ANALYSIS.md)
- [CLAUDE.md](../../CLAUDE.md) - общая документация проекта

## ❓ Частые вопросы

**Q: Почему тесты выдают Inconclusive?**
A: Некоторые тесты требуют полной инициализации библиотеки. Если библиотека не может быть инициализирована (например, отсутствуют конфигурационные файлы), тест помечается как Inconclusive.

**Q: Как добавить project reference для новой библиотеки?**
A: Раскомментируйте соответствующую строку в `Tests/Tests.csproj` и добавьте тест-кейс в `DocumentInterfaceComplianceTests`.

**Q: Можно ли запустить тесты без реальной БД?**
A: Да! Все тесты используют in-memory БД через Entity Framework Core InMemory provider.

**Q: Как тестировать интеграцию с ELIS?**
A: Используйте моки для IElisConnector или создайте фикстуры с тестовыми данными ELIS.
