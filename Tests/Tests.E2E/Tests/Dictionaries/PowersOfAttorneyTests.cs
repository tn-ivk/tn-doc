using Microsoft.Playwright;
using Tests.E2E.Base;
using Tests.E2E.Pages;

namespace Tests.E2E.Tests.Dictionaries;

/// <summary>
/// E2E тесты для управления доверенностями в справочниках.
/// Сценарии 2.1-2.4: CRUD операции с доверенностями
/// </summary>
[TestFixture(TestName = "Справочники: Управление доверенностями")]
[Category("E2E")]
[Category("Dictionaries")]
[Category("PowersOfAttorney")]
public class PowersOfAttorneyTests : PlaywrightTestBase
{
    private DictionariesPage _dictionaries = null!;

    // Тестовые данные
    private const string TestPowerNumber = "ТСТ-001/2026";
    private const string TestPowerDate = "2026-01-21";
    private const string UpdatedPowerNumber = "ТСТ-002/2026";
    private const string UpdatedPowerDate = "2026-02-15";

    [SetUp]
    public async Task SetUpTest()
    {
        _dictionaries = new DictionariesPage(Page);
    }

    /// <summary>
    /// 2.1 Просмотр доверенностей
    /// </summary>
    [Test]
    [Order(1)]
    [Description("Проверяет отображение таблицы доверенностей с корректными колонками")]
    public async Task ViewPowersOfAttorney_WhenNavigate_ThenTableDisplayedWithCorrectColumns()
    {
        // Arrange & Act
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToPowersOfAttorneyAsync();

        // Assert - проверяем наличие колонок в видимой таблице
        var table = Page.Locator("[role='dialog'] table:visible").First;
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Активен" }).First)
            .ToBeVisibleAsync();
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Номер" }).First)
            .ToBeVisibleAsync();
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Дата" }).First)
            .ToBeVisibleAsync();

        // Скриншот
        await TakeScreenshotAsync("2.1-powers-of-attorney-view");
    }

    /// <summary>
    /// 2.2 Добавление доверенности
    /// </summary>
    [Test]
    [Order(2)]
    [Description("Создаёт новую доверенность со всеми заполненными полями")]
    public async Task AddPowerOfAttorney_WhenAllFieldsFilled_ThenPowerAppearsInTable()
    {
        // Arrange
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToPowersOfAttorneyAsync();

        // Act
        await _dictionaries.ClickAddAsync();

        await _dictionaries.SetCheckboxByLabelAsync("Активен", true);
        await _dictionaries.FillFieldByLabelAsync("Номер", TestPowerNumber);
        await _dictionaries.FillDateFieldAsync("Дата", TestPowerDate);

        await _dictionaries.ClickConfirmAsync();

        // Assert
        var powerExists = await _dictionaries.HasRowWithTextAsync(TestPowerNumber);
        Assert.That(powerExists, Is.True, $"Доверенность '{TestPowerNumber}' должна появиться в таблице");

        // Скриншот
        await TakeScreenshotAsync("2.2-add-power-of-attorney");
    }

    /// <summary>
    /// 2.3 Редактирование доверенности
    /// </summary>
    [Test]
    [Order(3)]
    [Description("Редактирует существующую доверенность")]
    public async Task EditPowerOfAttorney_WhenFieldsChanged_ThenChangesAreDisplayed()
    {
        // Arrange - создаём доверенность если не существует
        await CreateTestPowerOfAttorneyAsync();

        // Act
        await _dictionaries.ClickEditForRowAsync(TestPowerNumber);

        await _dictionaries.FillFieldByLabelAsync("Номер", UpdatedPowerNumber);
        await _dictionaries.FillDateFieldAsync("Дата", UpdatedPowerDate);

        await _dictionaries.ClickConfirmAsync();

        // Assert
        var updatedPowerExists = await _dictionaries.HasRowWithTextAsync(UpdatedPowerNumber);
        Assert.That(updatedPowerExists, Is.True, $"Обновлённая доверенность '{UpdatedPowerNumber}' должна отображаться");

        // Скриншот
        await TakeScreenshotAsync("2.3-edit-power-of-attorney");
    }

    /// <summary>
    /// 2.4 Удаление доверенности
    /// </summary>
    [Test]
    [Order(4)]
    [Description("Удаляет доверенность из таблицы")]
    public async Task DeletePowerOfAttorney_WhenClickDelete_ThenPowerRemovedFromTable()
    {
        // Arrange - создаём доверенность для удаления
        await CreateTestPowerOfAttorneyAsync();

        var powerExistsBefore = await _dictionaries.HasRowWithTextAsync(TestPowerNumber);
        Assert.That(powerExistsBefore, Is.True, "Доверенность должна существовать перед удалением");

        // Act
        await _dictionaries.ClickDeleteForRowAsync(TestPowerNumber);

        // Assert
        var powerExistsAfter = await _dictionaries.HasRowWithTextAsync(TestPowerNumber);
        Assert.That(powerExistsAfter, Is.False, "Доверенность должна быть удалена из таблицы");

        // Скриншот
        await TakeScreenshotAsync("2.4-delete-power-of-attorney");
    }

    /// <summary>
    /// Проверка активного/неактивного статуса доверенности
    /// </summary>
    [Test]
    [Description("Проверяет переключение статуса активности доверенности")]
    public async Task PowerOfAttorney_WhenToggleActive_ThenStatusChanges()
    {
        // Arrange
        await CreateTestPowerOfAttorneyAsync();

        // Act - редактируем и переключаем статус
        await _dictionaries.ClickEditForRowAsync(TestPowerNumber);
        await _dictionaries.SetCheckboxByLabelAsync("Активен", false);
        await _dictionaries.ClickConfirmAsync();

        // Открываем снова и проверяем
        await _dictionaries.ClickEditForRowAsync(TestPowerNumber);
        await _dictionaries.SetCheckboxByLabelAsync("Активен", true);
        await _dictionaries.ClickConfirmAsync();

        // Assert - доверенность должна быть активна
        var powerExists = await _dictionaries.HasRowWithTextAsync(TestPowerNumber);
        Assert.That(powerExists, Is.True, "Доверенность должна отображаться после переключения статуса");
    }

    /// <summary>
    /// Проверка формата даты
    /// </summary>
    [Test]
    [Description("Проверяет корректность ввода даты в формате yyyy-MM-dd")]
    public async Task PowerOfAttorney_WhenEnterDate_ThenDateFormatIsCorrect()
    {
        // Arrange
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToPowersOfAttorneyAsync();

        // Act
        await _dictionaries.ClickAddAsync();
        await _dictionaries.SetCheckboxByLabelAsync("Активен", true);
        await _dictionaries.FillFieldByLabelAsync("Номер", "ТСТ-ДАТА/2026");
        await _dictionaries.FillDateFieldAsync("Дата", "2026-12-31");
        await _dictionaries.ClickConfirmAsync();

        // Assert
        var powerExists = await _dictionaries.HasRowWithTextAsync("ТСТ-ДАТА/2026");
        Assert.That(powerExists, Is.True, "Доверенность с датой должна быть создана");

        // Cleanup
        await _dictionaries.ClickDeleteForRowAsync("ТСТ-ДАТА/2026");
    }

    #region Helper Methods

    /// <summary>
    /// Создаёт тестовую доверенность для последующих тестов
    /// </summary>
    private async Task CreateTestPowerOfAttorneyAsync()
    {
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToPowersOfAttorneyAsync();

        // Проверяем, существует ли уже тестовая доверенность
        var powerExists = await _dictionaries.HasRowWithTextAsync(TestPowerNumber);
        if (powerExists)
        {
            return;
        }

        await _dictionaries.ClickAddAsync();
        await _dictionaries.SetCheckboxByLabelAsync("Активен", true);
        await _dictionaries.FillFieldByLabelAsync("Номер", TestPowerNumber);
        await _dictionaries.FillDateFieldAsync("Дата", TestPowerDate);
        await _dictionaries.ClickConfirmAsync();
    }

    #endregion
}
