using Tests.E2E.Base;
using Tests.E2E.Pages;

namespace Tests.E2E.Tests.Dictionaries;

/// <summary>
/// E2E тесты для управления пользователями в справочниках.
/// Сценарии 1.2-1.5: CRUD операции с пользователями
/// </summary>
[TestFixture(TestName = "Справочники: Управление пользователями")]
[Category("E2E")]
[Category("Dictionaries")]
[Category("Users")]
public class UsersTests : PlaywrightTestBase
{
    private DictionariesPage _dictionaries = null!;

    // Тестовые данные
    private const string TestUserLastName = "Тестов";
    private const string TestUserFirstName = "Тест";
    private const string TestUserMiddleName = "Тестович";
    private const string TestUserOrganization = "ООО ТестОрг";
    private const string TestUserPosition = "Инженер";

    private const string UpdatedLastName = "Тестов-Измененный";
    private const string UpdatedFirstName = "Иван";
    private const string UpdatedMiddleName = "Иванович";
    private const string UpdatedOrganization = "АО НоваяОрг";
    private const string UpdatedPosition = "Старший инженер";

    [SetUp]
    public async Task SetUpTest()
    {
        _dictionaries = new DictionariesPage(Page);
    }

    /// <summary>
    /// 1.2 Добавление пользователя (все поля)
    /// </summary>
    [Test]
    [Order(1)]
    [Description("Создаёт нового пользователя со всеми заполненными полями")]
    public async Task AddUser_WhenAllFieldsFilled_ThenUserAppearsInTable()
    {
        // Arrange
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUsersAsync();
        await _dictionaries.SelectUserGroupAsync("Представители испытательной лаборатории");

        // Act - нажимаем добавить и заполняем форму
        await _dictionaries.ClickAddAsync();

        // Заполняем все поля
        await _dictionaries.SetCheckboxByLabelAsync("Активен", true);
        await _dictionaries.FillFieldByLabelAsync("Фамилия", TestUserLastName);
        await _dictionaries.FillFieldByLabelAsync("Имя", TestUserFirstName);
        await _dictionaries.FillFieldByLabelAsync("Отчество", TestUserMiddleName);
        await _dictionaries.FillFieldByLabelAsync("Организация", TestUserOrganization);
        await _dictionaries.FillFieldByLabelAsync("Должность", TestUserPosition);

        // Дополнительные чекбоксы
        await _dictionaries.SetCheckboxByLabelAsync("Сепаратор", true);
        await _dictionaries.SetCheckboxByLabelAsync("Пробелы", true);
        await _dictionaries.SetCheckboxByLabelAsync("Сокр. форма", true);

        // Подтверждаем
        await _dictionaries.ClickConfirmAsync();

        // Assert
        var userExists = await _dictionaries.HasRowWithTextAsync(TestUserLastName);
        Assert.That(userExists, Is.True, $"Пользователь '{TestUserLastName}' должен появиться в таблице");

        // Скриншот
        await TakeScreenshotAsync("1.2-add-user");
    }

    /// <summary>
    /// 1.3 Редактирование пользователя (изменение всех полей)
    /// </summary>
    [Test]
    [Order(2)]
    [Description("Редактирует существующего пользователя, изменяя все поля")]
    public async Task EditUser_WhenAllFieldsChanged_ThenChangesAreDisplayed()
    {
        // Arrange - сначала создаём пользователя
        await CreateTestUserAsync();

        // Act - редактируем
        await _dictionaries.ClickEditForRowAsync(TestUserLastName);

        // Изменяем все поля
        await _dictionaries.SetCheckboxByLabelAsync("Активен", false);
        await _dictionaries.SetCheckboxByLabelAsync("Активен", true); // Включаем обратно

        await _dictionaries.FillFieldByLabelAsync("Фамилия", UpdatedLastName);
        await _dictionaries.FillFieldByLabelAsync("Имя", UpdatedFirstName);
        await _dictionaries.FillFieldByLabelAsync("Отчество", UpdatedMiddleName);
        await _dictionaries.FillFieldByLabelAsync("Организация", UpdatedOrganization);
        await _dictionaries.FillFieldByLabelAsync("Должность", UpdatedPosition);

        // Переключаем чекбоксы
        await _dictionaries.SetCheckboxByLabelAsync("Сепаратор", false);
        await _dictionaries.SetCheckboxByLabelAsync("Пробелы", false);
        await _dictionaries.SetCheckboxByLabelAsync("Сокр. форма", false);

        // Подтверждаем
        await _dictionaries.ClickConfirmAsync();

        // Assert
        var updatedUserExists = await _dictionaries.HasRowWithTextAsync(UpdatedLastName);
        Assert.That(updatedUserExists, Is.True, $"Обновлённый пользователь '{UpdatedLastName}' должен отображаться");

        // Скриншот
        await TakeScreenshotAsync("1.3-edit-user");
    }

    /// <summary>
    /// 1.4 Переключение между группами
    /// </summary>
    [Test]
    [Order(3)]
    [Description("Проверяет корректное переключение между группами пользователей")]
    public async Task SwitchGroups_WhenSelectDifferentGroups_ThenTableUpdatesCorrectly()
    {
        // Arrange
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUsersAsync();

        // Создаём пользователя в первой группе
        await _dictionaries.SelectUserGroupAsync("Представители испытательной лаборатории");
        await CreateUserInCurrentGroupAsync("ТестЛаб", "Первый");

        // Act & Assert - переключаемся между группами

        // Переключаемся на "Представители сдающей стороны"
        await _dictionaries.SelectUserGroupAsync("Представители сдающей стороны");
        var labUserInSellerGroup = await _dictionaries.HasRowWithTextAsync("ТестЛаб");
        Assert.That(labUserInSellerGroup, Is.False,
            "Пользователь из группы 'испытательной лаборатории' не должен отображаться в 'сдающей стороны'");

        // Переключаемся на "Представители принимающей стороны"
        await _dictionaries.SelectUserGroupAsync("Представители принимающей стороны");
        var countInBuyerGroup = await _dictionaries.GetTableRowCountAsync();
        TestContext.WriteLine($"Количество пользователей в группе 'принимающей стороны': {countInBuyerGroup}");

        // Переключаемся на "Представители ТНМ"
        await _dictionaries.SelectUserGroupAsync("Представители ТНМ");
        var countInTnmGroup = await _dictionaries.GetTableRowCountAsync();
        TestContext.WriteLine($"Количество пользователей в группе 'ТНМ': {countInTnmGroup}");

        // Возвращаемся к первой группе
        await _dictionaries.SelectUserGroupAsync("Представители испытательной лаборатории");
        var labUserBack = await _dictionaries.HasRowWithTextAsync("ТестЛаб");
        Assert.That(labUserBack, Is.True,
            "Тестовый пользователь должен отображаться после возврата к исходной группе");

        // Скриншот
        await TakeScreenshotAsync("1.4-switch-groups");
    }

    /// <summary>
    /// 1.5 Удаление пользователя
    /// </summary>
    [Test]
    [Order(4)]
    [Description("Удаляет пользователя из таблицы")]
    public async Task DeleteUser_WhenClickDelete_ThenUserRemovedFromTable()
    {
        // Arrange - создаём пользователя для удаления
        await CreateTestUserAsync();

        var userExistsBefore = await _dictionaries.HasRowWithTextAsync(TestUserLastName);
        Assert.That(userExistsBefore, Is.True, "Пользователь должен существовать перед удалением");

        // Act
        await _dictionaries.ClickDeleteForRowAsync(TestUserLastName);

        // Assert
        var userExistsAfter = await _dictionaries.HasRowWithTextAsync(TestUserLastName);
        Assert.That(userExistsAfter, Is.False, "Пользователь должен быть удалён из таблицы");

        // Скриншот
        await TakeScreenshotAsync("1.5-delete-user");
    }

    /// <summary>
    /// Проверка обязательности поля "Фамилия"
    /// </summary>
    [Test]
    [Description("Проверяет, что поле 'Фамилия' является обязательным")]
    public async Task AddUser_WhenLastNameEmpty_ThenValidationError()
    {
        // Arrange
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUsersAsync();
        await _dictionaries.SelectUserGroupAsync("Представители испытательной лаборатории");

        // Act
        await _dictionaries.ClickAddAsync();
        await _dictionaries.FillFieldByLabelAsync("Фамилия", ""); // Пустое поле
        await _dictionaries.FillFieldByLabelAsync("Имя", TestUserFirstName);
        await _dictionaries.ClickConfirmAsync();

        // Assert - должна быть ошибка валидации
        var hasError = await _dictionaries.HasValidationErrorAsync();

        // Если нет явной ошибки, проверяем что пользователь не добавился
        if (!hasError)
        {
            var emptyUserAdded = await _dictionaries.HasRowWithTextAsync(TestUserFirstName);
            Assert.That(emptyUserAdded, Is.False, "Пользователь без фамилии не должен быть добавлен");
        }
        else
        {
            Assert.That(hasError, Is.True, "Должна отображаться ошибка валидации");
        }
    }

    /// <summary>
    /// Проверка обязательности поля "Имя"
    /// </summary>
    [Test]
    [Description("Проверяет, что поле 'Имя' является обязательным")]
    public async Task AddUser_WhenFirstNameEmpty_ThenValidationError()
    {
        // Arrange
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUsersAsync();
        await _dictionaries.SelectUserGroupAsync("Представители испытательной лаборатории");

        // Act
        await _dictionaries.ClickAddAsync();
        await _dictionaries.FillFieldByLabelAsync("Фамилия", TestUserLastName);
        await _dictionaries.FillFieldByLabelAsync("Имя", ""); // Пустое поле
        await _dictionaries.ClickConfirmAsync();

        // Assert
        var hasError = await _dictionaries.HasValidationErrorAsync();

        if (!hasError)
        {
            var userAdded = await _dictionaries.HasRowWithTextAsync(TestUserLastName);
            Assert.That(userAdded, Is.False, "Пользователь без имени не должен быть добавлен");
        }
        else
        {
            Assert.That(hasError, Is.True, "Должна отображаться ошибка валидации");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Создаёт тестового пользователя для последующих тестов
    /// </summary>
    private async Task CreateTestUserAsync()
    {
        await _dictionaries.OpenAsync();
        await _dictionaries.NavigateToPersonnelAsync();
        await _dictionaries.NavigateToUsersAsync();
        await _dictionaries.SelectUserGroupAsync("Представители испытательной лаборатории");

        // Проверяем, существует ли уже тестовый пользователь
        var userExists = await _dictionaries.HasRowWithTextAsync(TestUserLastName);
        if (userExists)
        {
            return; // Пользователь уже существует
        }

        await _dictionaries.ClickAddAsync();
        await _dictionaries.SetCheckboxByLabelAsync("Активен", true);
        await _dictionaries.FillFieldByLabelAsync("Фамилия", TestUserLastName);
        await _dictionaries.FillFieldByLabelAsync("Имя", TestUserFirstName);
        await _dictionaries.ClickConfirmAsync();
    }

    /// <summary>
    /// Создаёт пользователя в текущей выбранной группе
    /// </summary>
    private async Task CreateUserInCurrentGroupAsync(string lastName, string firstName)
    {
        await _dictionaries.ClickAddAsync();
        await _dictionaries.SetCheckboxByLabelAsync("Активен", true);
        await _dictionaries.FillFieldByLabelAsync("Фамилия", lastName);
        await _dictionaries.FillFieldByLabelAsync("Имя", firstName);
        await _dictionaries.ClickConfirmAsync();
    }

    #endregion
}
