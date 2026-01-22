using Tests.E2E.Base;
using Tests.E2E.Pages;

namespace Tests.E2E.Tests.Dictionaries;

/// <summary>
/// E2E тесты для раздела "Группы пользователей" справочников.
/// Сценарий 1.1: Открытие справочников и проверка групп
/// </summary>
[TestFixture(TestName = "Справочники: Группы пользователей")]
[Category("E2E")]
[Category("Dictionaries")]
[Category("UserGroups")]
public class UserGroupsTests : PlaywrightTestBase
{
    private DictionariesPage _dictionaries = null!;

    [SetUp]
    public async Task SetUpTest()
    {
        _dictionaries = new DictionariesPage(Page);
    }

    /// <summary>
    /// 1.1 Открытие справочников и проверка групп
    /// </summary>
    [Test]
    [Description("Проверяет, что модальное окно справочников открывается и отображаются все 4 группы пользователей")]
    public async Task OpenDictionaries_WhenClickDictionariesButton_ThenModalOpensWithAllUserGroups()
    {
        // Arrange & Act
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUserGroupsAsync();

        // Assert - проверяем наличие всех 4 групп
        var groups = await _dictionaries.GetUserGroupsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(groups, Has.Count.EqualTo(4), "Должно быть 4 группы пользователей");

            foreach (var expectedGroup in DictionariesPage.ExpectedUserGroups)
            {
                Assert.That(groups, Does.Contain(expectedGroup),
                    $"Группа '{expectedGroup}' должна отображаться");
            }
        });

        // Скриншот
        await TakeScreenshotAsync("1.1-user-groups-list");
    }

    /// <summary>
    /// 1.1.1 Проверка группы "Представители испытательной лаборатории"
    /// </summary>
    [Test]
    [Description("Проверяет наличие группы 'Представители испытательной лаборатории'")]
    public async Task UserGroups_WhenNavigateToGroups_ThenTestLabRepresentativesGroupExists()
    {
        // Arrange & Act
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUserGroupsAsync();

        // Assert
        await AssertTextVisibleAsync("Представители испытательной лаборатории");
    }

    /// <summary>
    /// 1.1.2 Проверка группы "Представители сдающей стороны"
    /// </summary>
    [Test]
    [Description("Проверяет наличие группы 'Представители сдающей стороны'")]
    public async Task UserGroups_WhenNavigateToGroups_ThenSellerRepresentativesGroupExists()
    {
        // Arrange & Act
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUserGroupsAsync();

        // Assert
        await AssertTextVisibleAsync("Представители сдающей стороны");
    }

    /// <summary>
    /// 1.1.3 Проверка группы "Представители принимающей стороны"
    /// </summary>
    [Test]
    [Description("Проверяет наличие группы 'Представители принимающей стороны'")]
    public async Task UserGroups_WhenNavigateToGroups_ThenBuyerRepresentativesGroupExists()
    {
        // Arrange & Act
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUserGroupsAsync();

        // Assert
        await AssertTextVisibleAsync("Представители принимающей стороны");
    }

    /// <summary>
    /// 1.1.4 Проверка группы "Представители ТНМ"
    /// </summary>
    [Test]
    [Description("Проверяет наличие группы 'Представители ТНМ'")]
    public async Task UserGroups_WhenNavigateToGroups_ThenTnmRepresentativesGroupExists()
    {
        // Arrange & Act
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUserGroupsAsync();

        // Assert
        await AssertTextVisibleAsync("Представители ТНМ");
    }

    /// <summary>
    /// Проверка, что группы только для чтения (нет кнопок редактирования/удаления)
    /// </summary>
    [Test]
    [Description("Проверяет, что раздел групп пользователей доступен только для чтения")]
    public async Task UserGroups_WhenViewGroups_ThenNoEditOrDeleteButtons()
    {
        // Arrange & Act
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUserGroupsAsync();

        // Assert - проверяем отсутствие кнопок редактирования
        var hasAddButton = await IsElementVisibleAsync("[data-action='add'], button:has-text('Добавить')");
        var hasEditButton = await IsElementVisibleAsync("[data-action='edit'], .fa-edit, .fa-pencil");
        var hasDeleteButton = await IsElementVisibleAsync("[data-action='delete'], .fa-trash");

        Assert.Multiple(() =>
        {
            Assert.That(hasAddButton, Is.False, "Кнопка 'Добавить' не должна отображаться для групп");
            Assert.That(hasEditButton, Is.False, "Кнопка редактирования не должна отображаться для групп");
            Assert.That(hasDeleteButton, Is.False, "Кнопка удаления не должна отображаться для групп");
        });
    }
}
