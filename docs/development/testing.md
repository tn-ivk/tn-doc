# Тестирование

## Обзор

TN_Doc использует комплексную стратегию тестирования на базе **NUnit 4.3.2** и **.NET 8.0**. Тестовая инфраструктура организована в 4 проекта, которые покрывают разные уровни тестирования: от модульных тестов отдельных компонентов до end-to-end тестов пользовательского интерфейса.

**Текущий статус (v1.3.8):**
- **Количество тестов и доля отключённых регулярно меняются — ориентируйтесь на вывод `dotnet test`/CI.**
- **Отключённые тесты помечаются `[Ignore]` с причиной.**

Тесты используют:
- **NUnit 4.3.2** — фреймворк тестирования
- **Moq 4.20.72** — библиотека для создания mock-объектов
- **Microsoft.Playwright 1.49.0** — для E2E тестирования UI
- **Microsoft.EntityFrameworkCore.InMemory** — для тестирования с in-memory БД
- **coverlet.collector** — для сбора покрытия кода

## Последние изменения (январь 2026)

- Добавлен `ConfigEncodingTests` для проверки всех JSON-конфигов в `TN_Doc/Cfg` (UTF-8, отсутствие `U+FFFD`, валидный JSON).
- `HomeControllerTests` переведён на создание реального `HomeController` через Reflection (`RuntimeHelpers.GetUninitializedObject`) вместо test-double.
- Усилен набор negative-тестов для `PrinterService`, `WindowsPrinter`, `LinuxPrinter` (пустые параметры, исключения, платформенные сценарии, пограничные случаи строковых значений).
- Обновлены примеры в документации тестов под текущую тестовую архитектуру (без `TestableHomeController`).

---

## Структура тестовых проектов

### Tests.Unit
**Расположение:** `Tests/Tests.Unit/`
**Назначение:** Модульные тесты для изолированного тестирования отдельных компонентов приложения.

**Что тестируют:**
- **Controllers:** `HomeController`, `ExportController`, `PrintController`, `ClientLogController`
- **Services:** `AppConfigService`, `PrinterService`, `DirectoryService`, `LoggingPathService`
- **Models:** DTOs (`DirEditDTO`, `QpEditDto`), доменные модели (`ClientLogMessage`, `Data`, `ListItem`)
- **Extensions:** `ServiceCollectionExtensions`
- **Платформо-зависимые компоненты:** `LinuxPrinter`, `WindowsPrinter`
- **Конфигурации:** `ConfigEncodingTests` — проверка UTF-8, отсутствия U+FFFD и валидности JSON в `TN_Doc/Cfg`

**Отдельные проверки конфигураций:**
- `ConfigEncodingTests` проходит по всем JSON в `TN_Doc/Cfg` и ловит ошибки кодировки (U+FFFD), невалидный UTF-8 и синтаксис JSON.

**Примеры тестов:**
```csharp
// Tests/Tests.Unit/Controllers/HomeControllerTests.cs
[SetUp]
public void SetUp()
{
    _controller = CreateHomeControllerWithMocks(); // Reflection, без вызова конструктора
}

[Test]
public void GetListDevices_WhenDevicesExist_ReturnsOnlyUsedDevices()
{
    var result = _controller.GetListDevices();

    Assert.That(result, Has.Count.EqualTo(2));
}

// Tests/Tests.Unit/Services/PrinterServiceTests.cs
[Test]
public void GetPrinters_WhenCalled_ReturnsListFromAbsPrinter()
{
    var expectedPrinters = new List<string> { "Printer1" };
    _mockPrinter.Setup(p => p.GetAvailablePrinters()).Returns(expectedPrinters);

    var result = _sut.GetPrinters();

    Assert.That(result, Is.EqualTo(expectedPrinters));
    _mockPrinter.Verify(p => p.GetAvailablePrinters(), Times.Once);
}
```

**Зависимости:**
- `Tests.Shared` — общие helpers и fixtures
- `TN_Doc` — основное приложение

**Особенность:** `HomeControllerTests` создаёт контроллер через Reflection
(`RuntimeHelpers.GetUninitializedObject` + установка приватных полей), чтобы обойти статический `AppConfigService.GetInstance`.

---

### Tests.Integration
**Расположение:** `Tests/Tests.Integration/`
**Назначение:** Интеграционные тесты для проверки взаимодействия компонентов и модулей документов.

**Что тестируют:**
- **Модули документов КМХ (~168 тестов):**
  - Измерение массового расхода: `KMH_MPR_MPR`, `KMH_MPR_PU`, `KMH_MPR_TPR`
  - Плотность: `KMH_PP`, `KMH_PP_Areom`
  - Давление и расход: `KMH_PR_PR`, `KMH_PR_PU`, `KMH_PV`, `KMH_PW`
  - СИКН-425: `KMX_Sikn425_PR_PR`, `KMX_Sikn425_PR_PU`
  - МИ 2816, МИ 3265, МИ 3288, МИ 3312

- **Базовые документы:** `Act`, `Passport`, `Jornal`, `Report`
- **Common библиотеки:** `CommonPoverka1974`, `CommonSikn425`
- **Controllers:** `HomeController`, `DirEditorController`, `ElisController` (интеграционные тесты)
- **Services:** `AppConfigService`, `CfgAppSyncTests`
- **Infrastructure:** `DocumentInterfaceComplianceTests`

**Примеры тестов:**
```csharp
// Tests/Tests.Integration/Libraries/KMH/KmhMeasurementTests.cs
[TestCase(IdDoc.KMH_MPR_MPR, typeof(KMH_MPR_MPR))]
[TestCase(IdDoc.KMH_MPR_PU, typeof(KMH_MPR_PU))]
[TestCase(IdDoc.KMH_MPR_TPR, typeof(KMH_MPR_TPR))]
public void Constructor_WithValidParameters_InitializesCorrectly(IdDoc idDoc, Type expectedType)
{
    var instance = CreateDocumentInstance(idDoc);

    Assert.That(instance, Is.Not.Null);
    Assert.That(instance, Is.InstanceOf(expectedType));
}

[TestCase(IdDoc.KMH_MPR_MPR)]
[TestCase(IdDoc.KMH_MPR_PU)]
public void GetViewDoc_WithValidId_ReturnsValidJsonString(IdDoc idDoc)
{
    var document = CreateDocumentInstance(idDoc);
    var result = document.GetViewDoc(1);

    if (result != null)
    {
        var jsonString = result.ToString();
        Assert.That(jsonString, Is.Not.Empty);
        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(jsonString));
    }
}
```

**Зависимости:**
- `Tests.Shared` — общие helpers и fixtures
- `TN_Doc` — основное приложение
- `TN.DocGeneral` — базовая библиотека документов
- `TN.Utils` — утилиты
- `tn.docgeneral/*` — все модули документов (~48 проектов)

**Особенности:**
- Использует **In-Memory Database** для изоляции тестов
- Параметризованные тесты (`TestCase`) для тестирования однотипных модулей
- Mock-объекты для `IAppConfigService` и `ILogger`

---

### Tests.E2E
**Расположение:** `Tests/Tests.E2E/`
**Назначение:** End-to-end тесты пользовательского интерфейса на базе Playwright.

**Что тестируют:**
- **Справочники (Dictionaries):**
  - Группы пользователей (User Groups)
  - Пользователи (Users)
  - Доверенности (Powers of Attorney)
  - Методы испытаний (Test Methods)

**Базовый класс:** `PlaywrightTestBase`

`PlaywrightTestBase` предоставляет:
- Автоматическое управление жизненным циклом браузера
- Конфигурация браузера (локаль `ru-RU`, viewport 1920x1080)
- Базовые helper-методы для работы с элементами
- Автоматическое создание скриншотов
- Таймауты по умолчанию (10 сек для обычных операций, 3 сек для быстрых)

**Примеры тестов:**
```csharp
// Tests/Tests.E2E/Tests/Dictionaries/TestMethodsTests.cs
[Test]
[Description("Создаёт новый метод испытания со всеми заполненными полями")]
public async Task AddTestMethod_WhenAllFieldsFilled_ThenMethodAppearsInTable()
{
    await _dictionaries.OpenAsync();
    await _dictionaries.NavigateToTestMethodsAsync();
    await _dictionaries.SelectPassportTypeAsync("Паспорт для нефтепродукта");
    await _dictionaries.SelectParameterAsync(TestParameter1);

    await _dictionaries.ClickAddAsync();
    await _dictionaries.SetCheckboxByLabelAsync("Активен", true);
    await _dictionaries.FillFieldByLabelAsync("Метод", TestMethodName);
    await _dictionaries.SetCheckboxByLabelAsync("Контроль мин. значения", true);
    await _dictionaries.FillNumberFieldAsync("Мин. значение", TestMinValue);
    await _dictionaries.FillFieldByLabelAsync("Сообщение", TestMessage);
    await _dictionaries.ClickConfirmAsync();

    var methodExists = await _dictionaries.HasRowWithTextAsync(TestMethodName);
    Assert.That(methodExists, Is.True);

    await TakeScreenshotAsync("3.3-add-test-method");
}
```

**Page Object Model:**
- `DictionariesPage` — инкапсулирует взаимодействие со справочниками
- Методы для навигации, CRUD операций, работы с формами
- Селекторы элементов вынесены в константы

**Зависимости:**
- `Microsoft.Playwright.NUnit 1.49.0`
- `Tests.Shared` — общие helpers
- `TN_Doc` — основное приложение

**Конфигурация Playwright:**
```csharp
public override BrowserNewContextOptions ContextOptions()
{
    return new BrowserNewContextOptions
    {
        Locale = "ru-RU",
        ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
        IgnoreHTTPSErrors = true
    };
}
```

**Запуск в видимом режиме:**
```bash
set HEADED=1 && dotnet test Tests/Tests.E2E
```

---

### Tests.Shared
**Расположение:** `Tests/Tests.Shared/`
**Назначение:** Общие helpers, fixtures и утилиты для всех тестовых проектов.

**Компоненты:**
- `BaseDocumentTest.cs` — базовый класс для тестирования документов
- `DocumentTestHelpers.cs` — вспомогательные методы для работы с документами
- `Fixtures/DocumentTestDataFixture.cs` — фикстуры тестовых данных
- `Fixtures/MockConfigHelper.cs` — helper для настройки mock-объектов `IAppConfigService`

**Пример MockConfigHelper:**
```csharp
// Tests/Tests.Shared/Fixtures/MockConfigHelper.cs
public static class MockConfigHelper
{
    public static void SetupMockAppConfig(Mock<IAppConfigService> mockAppConfig, int idDevice)
    {
        mockAppConfig.Setup(a => a.GetAppCfg()).Returns(CreateTestCfgApp(idDevice));
        mockAppConfig.Setup(a => a.IsUsedElis(It.IsAny<int>())).Returns(false);
        // ... дополнительные настройки
    }

    private static CfgApp CreateTestCfgApp(int idDevice)
    {
        return new CfgApp
        {
            Devices = new List<Device> { /* ... */ }
        };
    }
}
```

**Зависимости:**
- `NUnit 4.3.2`
- `Moq 4.20.72`
- `Microsoft.EntityFrameworkCore.InMemory`
- `TN_Doc`, `TN.DocGeneral`, `TN.Utils`

---

## Запуск тестов

### Все тесты
```bash
dotnet test
```

### По проекту
```bash
# Unit-тесты
dotnet test Tests/Tests.Unit

# Интеграционные тесты
dotnet test Tests/Tests.Integration

# E2E тесты
dotnet test Tests/Tests.E2E
```

### По классу
```bash
dotnet test --filter "ClassName=HomeControllerTests"
dotnet test --filter "ClassName=KmhMeasurementTests"
dotnet test --filter "ClassName=ConfigEncodingTests"
```

### По namespace
```bash
# Все тесты КМХ
dotnet test --filter "Namespace~KMH"

# Тесты справочников
dotnet test --filter "Namespace~Dictionaries"
```

### По категории
```bash
# Только E2E тесты
dotnet test --filter "Category=E2E"

# Тесты справочников
dotnet test --filter "Category=Dictionaries"
```

### С подробным выводом
```bash
dotnet test --verbosity detailed
```

### С покрытием кода
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## E2E тесты

### Базовый класс PlaywrightTestBase

`PlaywrightTestBase` наследуется от `Microsoft.Playwright.NUnit.PageTest` и предоставляет общую функциональность для всех E2E тестов.

**Ключевые возможности:**

1. **Автоматическая инициализация браузера:**
   ```csharp
   [SetUp]
   public async Task BaseSetUp()
   {
       Page.SetDefaultTimeout(DefaultTimeout);
       await Page.GotoAsync(BaseUrl);
       await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
   }
   ```

2. **Helper-методы:**
   - `TakeScreenshotAsync(string scenarioName)` — создание скриншотов
   - `WaitForElementAsync(string selector)` — ожидание появления элемента
   - `ClickAndWaitAsync(string selector)` — клик с ожиданием загрузки
   - `FillAsync(string selector, string value)` — заполнение полей
   - `SetCheckboxAsync(string selector, bool checked)` — установка чекбоксов
   - `AssertTextVisibleAsync(string text)` — проверка наличия текста

3. **Конфигурация:**
   - Базовый URL: `http://localhost:5000`
   - Локаль: `ru-RU`
   - Viewport: 1920x1080
   - Таймауты: 10 сек (default), 3 сек (short)

### Тестирование справочников

E2E тесты справочников проверяют полный workflow пользователя:

**Тестовые сценарии для Users (пользователи):**
- Навигация к разделу
- Переключение между группами пользователей
- Добавление нового пользователя
- Редактирование существующего пользователя
- Удаление пользователя
- Валидация обязательных полей

**Тестовые сценарии для Test Methods (методы испытаний):**
- Навигация по паспортам качества (10 типов)
- Переключение между параметрами
- Добавление метода испытания
- Редактирование метода
- Удаление метода
- Валидация числовых полей

**Тестовые сценарии для Powers of Attorney (доверенности):**
- Отображение списка доверенностей
- Добавление новой доверенности
- Редактирование доверенности
- Удаление доверенности
- Валидация полей даты

**Тестовые сценарии для User Groups (группы пользователей):**
- Проверка наличия 4 стандартных групп:
  - Представители испытательной лаборатории
  - Представители сдающей стороны
  - Представители принимающей стороны
  - Представители ТНМ

### Конфигурация Playwright

**Установка браузеров:**
```bash
# Первая установка
dotnet build Tests/Tests.E2E
pwsh Tests/Tests.E2E/bin/Debug/net8.0/playwright.ps1 install

# Или через глобальный инструмент
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

**Запуск в видимом режиме (headed mode):**
```bash
# Windows
set HEADED=1 && dotnet test Tests/Tests.E2E

# Linux/macOS
HEADED=1 dotnet test Tests/Tests.E2E
```

**Отладка:**
```bash
# Режим отладки с паузами
set PWDEBUG=1 && dotnet test Tests/Tests.E2E

# Медленное выполнение (для наблюдения)
set SLOW_MO=1000 && dotnet test Tests/Tests.E2E
```

**Скриншоты:**
Скриншоты сохраняются автоматически при вызове `TakeScreenshotAsync()`:
- Директория: `tests/dictionaries/results/`
- Формат имени: `{scenarioName}-{timestamp}.png`
- Пример: `3.3-add-test-method-2026-01-23-14-30-45.png`

---

## Написание тестов

### Naming Convention

**Формат:** `MethodName_WhenCondition_ThenExpectedResult`

**Примеры:**
```csharp
// Unit-тесты
GetListDevices_WhenDevicesExist_ReturnsOnlyUsedDevices()
GetNameDBForDevice_WhenDeviceNotFound_ReturnsEmpty()
PrintPdf_WhenValidPath_CallsPrinterPrint()

// Интеграционные тесты
Constructor_WithValidParameters_InitializesCorrectly()
GetViewDoc_WithInvalidId_HandlesGracefully()

// E2E тесты
AddTestMethod_WhenAllFieldsFilled_ThenMethodAppearsInTable()
EditUser_WhenChangeLastName_ThenUpdatedValueDisplayed()
DeletePowerOfAttorney_WhenConfirmed_ThenRemovedFromTable()
```

### Использование Moq

**Создание mock-объектов:**
```csharp
private Mock<ILogger<HomeController>> _loggerMock;
private Mock<IAppConfigService> _appConfigMock;

[SetUp]
public void SetUp()
{
    _loggerMock = new Mock<ILogger<HomeController>>();
    _appConfigMock = new Mock<IAppConfigService>();

    // Настройка поведения
    _appConfigMock.Setup(a => a.GetAppCfg()).Returns(CreateTestCfgApp());
    _appConfigMock.Setup(a => a.IsUsedElis(It.IsAny<int>())).Returns(false);
}
```

**Проверка вызовов:**
```csharp
// Проверка, что метод был вызван один раз
_mockPrinter.Verify(p => p.Print(It.IsAny<string>()), Times.Once);

// Проверка с конкретным параметром
_mockLogger.Verify(
    l => l.Log(
        LogLevel.Error,
        It.IsAny<EventId>(),
        It.IsAny<It.IsAnyType>(),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    Times.Never
);
```

### Fixtures и Helpers

**Использование MockConfigHelper:**
```csharp
[SetUp]
public void SetUp()
{
    _mockAppConfig = new Mock<IAppConfigService>();
    MockConfigHelper.SetupMockAppConfig(_mockAppConfig, idDevice: 1);
}
```

**Создание тестовых данных:**
```csharp
private static CfgApp CreateTestCfgApp()
{
    return new CfgApp
    {
        UseSecurityFeatures = true,
        Devices = new List<Device>
        {
            new Device
            {
                Use = true,
                IdDevice = 1,
                Name = "TestDevice1",
                Docs = new List<Document> { /* ... */ }
            }
        }
    };
}
```

### In-Memory Database для интеграционных тестов

```csharp
private DbContextOptions<DocGeneral> _dbOptions;

[SetUp]
public void SetUp()
{
    // Новая БД для каждого теста (изоляция)
    _dbOptions = new DbContextOptionsBuilder<DocGeneral>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .EnableSensitiveDataLogging()
        .Options;
}

[Test]
public void GetViewDoc_WithValidId_ReturnsData()
{
    // Arrange
    using var context = new DocGeneral(_dbOptions);
    context.Documents.Add(new Document { Id = 1, Name = "Test" });
    context.SaveChanges();

    // Act
    var result = document.GetViewDoc(1);

    // Assert
    Assert.That(result, Is.Not.Null);
}
```

### Параметризованные тесты

```csharp
// Тестирование нескольких модулей КМХ одновременно
[TestCase(IdDoc.KMH_MPR_MPR, typeof(KMH_MPR_MPR))]
[TestCase(IdDoc.KMH_MPR_PU, typeof(KMH_MPR_PU))]
[TestCase(IdDoc.KMH_MPR_TPR, typeof(KMH_MPR_TPR))]
public void Constructor_WithValidParameters_InitializesCorrectly(IdDoc idDoc, Type expectedType)
{
    var instance = CreateDocumentInstance(idDoc);
    Assert.That(instance, Is.InstanceOf(expectedType));
}
```

### Отключение тестов

Используйте атрибут `[Ignore]` с обязательным указанием причины:

```csharp
[Test]
[Ignore("Требуется реализация IPrinterService для тестирования")]
public void PrintLastPdf_WhenCalled_CallsPrinterService()
{
    // ...
}
```

---

## Текущий статус

### Работающие тесты (основные группы)

**Tests.Unit:**
- Controllers: `HomeController`, `ExportController`
- Services: `LoggingPathService`, `DirectoryService`
- Models: DTOs, доменные модели
- Extensions: `ServiceCollectionExtensions`

**Tests.Integration:**
- **КМХ документы (~168 тестов):**
  - `KMH_MPR_*`, `KMH_PR_*`, `KMH_PP_*`, `KMH_PV`, `KMH_PW`
  - `KMH3265_*`, `KMH3288_*`, `KMH3312_*`
  - `KMX_Sikn425_*`, `KMH_MI2816`
- **Common библиотеки:** `CommonSikn425`
- **Базовые документы:** `Act`, `Passport`, `Jornal`, `Report`

**Tests.E2E:**
- Справочники: Users, User Groups, Powers of Attorney, Test Methods
- CRUD операции, валидация, навигация

### Отключенные тесты (основные причины)

**Причины отключения:**

1. **Не реализованные зависимости:**
   - `PrintControllerTests` — требует интерфейс `IPrinterService`
   - `ClientLogControllerTests` — контроллер не реализован
   - `PdfControllerTests` — контроллер не реализован

2. **Проблемы с инфраструктурой:**
   - `ConfigurationCacheService` — не реализован
   - `DbSchemaCache` — не реализован

3. **Требуют доработки:**
   - Некоторые тесты КМХ с зависимостями от реальных данных БД
   - Интеграционные тесты с внешними сервисами (ELIS)

### Планы по улучшению

1. **Реализовать отсутствующие интерфейсы:**
   - `IPrinterService` для тестирования печати
   - Базовые реализации отсутствующих контроллеров

2. **Увеличить покрытие:**
   - DirEditorController (полное покрытие)
   - ElisController (интеграция с ELIS)
   - Модули документов (100% покрытие базовых операций)

3. **E2E тесты:**
   - Тестирование генерации документов
   - Workflow печати
   - Интеграция с ELIS

4. **CI/CD интеграция:**
   - Автоматический запуск тестов в GitHub Actions
   - Отчёты о покрытии кода
   - Playwright screenshots artifacts

---

## Best Practices

### Изоляция тестов
- Каждый тест должен быть независим от других
- Используйте `[SetUp]` для инициализации, `[TearDown]` для очистки
- In-Memory БД создавайте с уникальным именем для каждого теста

### Читаемость
- Используйте `Assert.Multiple()` для группировки связанных проверок
- Комментируйте сложную логику тестов
- Используйте осмысленные имена переменных

### Стабильность E2E тестов
- Используйте явные ожидания (`WaitForLoadStateAsync`)
- Избегайте жёстко закодированных задержек (`Task.Delay`)
- Используйте `Force: true` для кликов, если элемент перекрыт

### Производительность
- Используйте параметризованные тесты для однотипных проверок
- Группируйте тесты по категориям для выборочного запуска
- Минимизируйте использование реальных внешних зависимостей

---

**Дата актуализации:** 2026-01-28
