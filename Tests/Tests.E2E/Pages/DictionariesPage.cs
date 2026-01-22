using Microsoft.Playwright;

namespace Tests.E2E.Pages;

/// <summary>
/// Page Object для модального окна "Справочники".
/// Инкапсулирует взаимодействие с основными элементами справочников.
/// </summary>
public class DictionariesPage
{
    private readonly IPage _page;

    // Селекторы главного меню - обновлены на основе реальной структуры UI
    private const string DictionariesButtonSelector = "button:has-text('Справочники')";
    private const string ModalSelector = "dialog, [role='dialog']";
    private const string ModalCloseSelector = "[role='dialog'] button:first-of-type";

    // Селекторы меню первого уровня
    private const string PersonnelMenuSelector = "text=Персонал";
    private const string TestMethodsMenuSelector = "text=Методы испытаний";

    // Селекторы подменю Персонал
    private const string UserGroupsMenuSelector = "text=Группы пользователей";
    private const string PowersOfAttorneyMenuSelector = "text=Доверенности";
    private const string UsersMenuSelector = "text=Пользователи";

    // Селекторы таблицы (inline-редактирование)
    private const string TableSelector = "[role='dialog'] table";
    private const string TableRowSelector = "[role='dialog'] table tr";
    private const string AddButtonSelector = "[role='dialog'] button:has-text('Добавить')";
    private const string SaveButtonSelector = "[role='dialog'] button:has-text('Сохранить')";
    // Кнопка подтверждения - первая кнопка в последней ячейке строки редактирования
    private const string ConfirmButtonInRowSelector = "td:last-child button:first-child";
    // Кнопка отмены/удаления - вторая кнопка в последней ячейке
    private const string CancelButtonInRowSelector = "td:last-child button:last-child";
    // Кнопки действий для существующих строк
    private const string EditButtonSelector = "td:last-child button:first-child";
    private const string DeleteButtonSelector = "td:last-child button:last-child";

    // Ожидаемые группы пользователей
    public static readonly string[] ExpectedUserGroups =
    {
        "Представители испытательной лаборатории",
        "Представители сдающей стороны",
        "Представители принимающей стороны",
        "Представители ТНМ"
    };

    // Ожидаемые типы паспортов качества
    public static readonly string[] ExpectedPassportTypes =
    {
        "Паспорт для нефтепродукта",
        "Паспорт качества на экспорт",
        "Паспорт (ГОСТ Р50.2.40 Приложение Ж)",
        "Паспорт (ГОСТ Р50.2.40 Приложение И)",
        "Паспорт (МИ 3532-2015 Приложение 13)",
        "Паспорт (МИ 3532-2015 Приложение 14)",
        "Паспорт (МИ 3532-2015 Приложение 15)",
        "Паспорт (ТР ЕАЭС)",
        "石油品质报告 (МИ 3532-2015 Приложение 15)",
        "Паспорт (ГОСТ Р50.2.40 Приложение Ж) (Экспорт)"
    };

    public DictionariesPage(IPage page)
    {
        _page = page;
    }

    #region Открытие/закрытие справочников

    /// <summary>
    /// Открывает модальное окно справочников
    /// </summary>
    public async Task OpenAsync()
    {
        // Используем GetByRole для более надёжного поиска кнопки
        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Справочники" });
        await button.ClickAsync();
        await _page.WaitForSelectorAsync(ModalSelector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible
        });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Закрывает модальное окно справочников
    /// </summary>
    public async Task CloseAsync()
    {
        await _page.ClickAsync(ModalCloseSelector);
        await _page.WaitForSelectorAsync(ModalSelector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Hidden
        });
    }

    /// <summary>
    /// Проверяет, открыто ли модальное окно
    /// </summary>
    public async Task<bool> IsOpenAsync()
    {
        try
        {
            return await _page.Locator(ModalSelector).IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Навигация по меню

    /// <summary>
    /// Переходит в раздел "Персонал"
    /// </summary>
    public async Task NavigateToPersonnelAsync()
    {
        await _page.ClickAsync(PersonnelMenuSelector);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Переходит в раздел "Методы испытаний"
    /// </summary>
    public async Task NavigateToTestMethodsAsync()
    {
        await _page.ClickAsync(TestMethodsMenuSelector);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Переходит в подраздел "Группы пользователей"
    /// </summary>
    public async Task NavigateToUserGroupsAsync()
    {
        await _page.ClickAsync(UserGroupsMenuSelector);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Переходит в подраздел "Доверенности"
    /// </summary>
    public async Task NavigateToPowersOfAttorneyAsync()
    {
        await _page.ClickAsync(PowersOfAttorneyMenuSelector);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Переходит в подраздел "Пользователи"
    /// </summary>
    public async Task NavigateToUsersAsync()
    {
        await _page.ClickAsync(UsersMenuSelector);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    #endregion

    #region Работа с группами пользователей

    /// <summary>
    /// Выбирает группу пользователей по названию (через combobox в разделе "Пользователи")
    /// </summary>
    public async Task SelectUserGroupAsync(string groupName)
    {
        // В разделе "Пользователи" группа выбирается через combobox "Группа:"
        var groupCombobox = _page.GetByLabel("Группа:");
        if (await groupCombobox.IsVisibleAsync())
        {
            await groupCombobox.SelectOptionAsync(new SelectOptionValue { Label = groupName });
        }
        else
        {
            // Fallback: клик по тексту в таблице (для раздела "Группы пользователей")
            await _page.Locator($"[role='dialog'] table td:has-text('{groupName}')").ClickAsync();
        }
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Получает список отображаемых групп пользователей (из таблицы)
    /// </summary>
    public async Task<IReadOnlyList<string>> GetUserGroupsAsync()
    {
        var groups = new List<string>();
        // Ищем только в ячейках таблицы, не в выпадающих списках
        var tableCells = _page.Locator("[role='dialog'] table td");
        var count = await tableCells.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var text = await tableCells.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text) && ExpectedUserGroups.Contains(text.Trim()))
            {
                groups.Add(text.Trim());
            }
        }
        return groups;
    }

    #endregion

    #region CRUD операции с таблицей

    /// <summary>
    /// Нажимает кнопку "Добавить"
    /// </summary>
    public async Task ClickAddAsync()
    {
        await _page.ClickAsync(AddButtonSelector);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Нажимает кнопку подтверждения (галочка/сохранение) в строке редактирования
    /// </summary>
    public async Task ClickConfirmAsync()
    {
        // Находим строку редактирования (с checkbox - признак редактирования)
        var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='checkbox'])").Last;

        // Находим последнюю ячейку с кнопками действий
        var actionCell = editingRow.Locator("td").Last;

        // Первая кнопка - сохранение/подтверждение
        var confirmButton = actionCell.GetByRole(AriaRole.Button).First;

        if (await confirmButton.IsVisibleAsync())
        {
            await confirmButton.ClickAsync();
        }

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Нажимает кнопку редактирования для строки с указанным текстом
    /// </summary>
    public async Task ClickEditForRowAsync(string rowText)
    {
        var row = _page.Locator($"[role='dialog'] table tr:has-text('{rowText}')").First;
        // Первая кнопка в строке - редактирование
        var editButton = row.GetByRole(AriaRole.Button).First;
        await editButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Нажимает кнопку удаления для строки с указанным текстом
    /// </summary>
    public async Task ClickDeleteForRowAsync(string rowText)
    {
        var row = _page.Locator($"[role='dialog'] table tr:has-text('{rowText}')").First;
        // Вторая кнопка в строке - удаление
        var deleteButton = row.GetByRole(AriaRole.Button).Nth(1);
        await deleteButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Получает количество строк в таблице (исключая заголовок)
    /// </summary>
    public async Task<int> GetTableRowCountAsync()
    {
        // Считаем строки с данными (исключая заголовок и строку редактирования)
        var rows = _page.Locator("[role='dialog'] table tbody tr, [role='dialog'] table tr").Filter(new LocatorFilterOptions
        {
            HasNot = _page.Locator("th")
        });
        return await rows.CountAsync();
    }

    /// <summary>
    /// Проверяет наличие строки с указанным текстом
    /// </summary>
    public async Task<bool> HasRowWithTextAsync(string text)
    {
        var row = _page.Locator($"[role='dialog'] table tr:has-text('{text}')");
        return await row.CountAsync() > 0;
    }

    #endregion

    #region Работа с полями формы

    // Маппинг названий полей на индексы текстовых полей в строке пользователя
    // Порядок: Фамилия(0), Имя(1), Отчество(2), Организация(3), Должность(4)
    private static readonly Dictionary<string, int> UserFieldTextboxIndexes = new()
    {
        { "Фамилия", 0 },
        { "Имя", 1 },
        { "Отчество", 2 },
        { "Организация", 3 },
        { "Должность", 4 }
    };

    /// <summary>
    /// Заполняет текстовое поле по метке (для inline-формы в таблице)
    /// </summary>
    public async Task FillFieldByLabelAsync(string label, string value)
    {
        // Находим строку редактирования (последняя строка с checkbox - признак редактирования)
        var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='checkbox'])").Last;

        // Для пользователей используем прямую индексацию textbox'ов
        if (UserFieldTextboxIndexes.TryGetValue(label, out var textboxIndex))
        {
            // Находим все текстовые поля (role='textbox' или input[type='text']) в строке
            var textboxes = editingRow.GetByRole(AriaRole.Textbox);
            var count = await textboxes.CountAsync();

            if (count > textboxIndex)
            {
                var textbox = textboxes.Nth(textboxIndex);
                await textbox.ClearAsync();
                await textbox.FillAsync(value);
                return;
            }
        }

        // Fallback: ищем по индексу колонки
        var columnIndex = await GetColumnIndexByHeaderAsync(label);
        if (columnIndex >= 0)
        {
            var cells = editingRow.Locator("td");
            var cell = cells.Nth(columnIndex);
            var input = cell.GetByRole(AriaRole.Textbox).First;

            if (await input.CountAsync() > 0)
            {
                await input.ClearAsync();
                await input.FillAsync(value);
                return;
            }
        }
    }

    /// <summary>
    /// Получает индекс колонки по заголовку (columnheader)
    /// </summary>
    private async Task<int> GetColumnIndexByHeaderAsync(string headerText)
    {
        // Ищем заголовки в первой строке таблицы
        var headers = _page.Locator("[role='dialog'] table th");
        var count = await headers.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var text = await headers.Nth(i).TextContentAsync();
            if (text?.Trim().Equals(headerText, StringComparison.OrdinalIgnoreCase) == true)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Устанавливает состояние чекбокса по метке (для inline-формы в таблице)
    /// </summary>
    public async Task SetCheckboxByLabelAsync(string label, bool isChecked)
    {
        // Находим строку редактирования (с checkbox)
        var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='checkbox'])").Last;

        // Маппинг чекбоксов по позиции в строке пользователей
        // Порядок: Активен(0), Сепаратор(1), Пробелы(2), Сокр.форма(3)
        var checkboxIndexes = new Dictionary<string, int>
        {
            { "Активен", 0 },
            { "Cепаратор", 1 },
            { "Сепаратор", 1 },
            { "Пробелы", 2 },
            { "Сокр. форма", 3 },
            { "Сокр.форма", 3 }
        };

        if (checkboxIndexes.TryGetValue(label, out var checkboxIndex))
        {
            var checkboxes = editingRow.GetByRole(AriaRole.Checkbox);
            var count = await checkboxes.CountAsync();

            if (count > checkboxIndex)
            {
                var checkbox = checkboxes.Nth(checkboxIndex);
                await checkbox.SetCheckedAsync(isChecked);
                return;
            }
        }

        // Fallback: ищем по индексу колонки
        var columnIndex = await GetColumnIndexByHeaderAsync(label);
        if (columnIndex >= 0)
        {
            var cells = editingRow.Locator("td");
            var cell = cells.Nth(columnIndex);
            var checkbox = cell.GetByRole(AriaRole.Checkbox).First;

            if (await checkbox.IsVisibleAsync())
            {
                await checkbox.SetCheckedAsync(isChecked);
                return;
            }
        }

        // Fallback для стандартных форм (методы испытаний)
        var labelCheckbox = _page.GetByLabel(label);
        if (await labelCheckbox.IsVisibleAsync())
        {
            await labelCheckbox.SetCheckedAsync(isChecked);
        }
    }

    /// <summary>
    /// Выбирает значение в выпадающем списке по метке
    /// </summary>
    public async Task SelectByLabelAsync(string label, string optionText)
    {
        var select = _page.GetByLabel(label);
        if (await select.IsVisibleAsync())
        {
            await select.SelectOptionAsync(new SelectOptionValue { Label = optionText });
        }
        else
        {
            // Альтернативный поиск
            var altSelect = _page.Locator($"label:has-text('{label}') + select, label:has-text('{label}') ~ select");
            if (await altSelect.IsVisibleAsync())
            {
                await altSelect.SelectOptionAsync(new SelectOptionValue { Label = optionText });
            }
        }
    }

    /// <summary>
    /// Заполняет поле даты
    /// </summary>
    public async Task FillDateFieldAsync(string label, string date)
    {
        var dateInput = _page.GetByLabel(label);
        if (await dateInput.IsVisibleAsync())
        {
            await dateInput.FillAsync(date);
        }
        else
        {
            var altInput = _page.Locator($"input[type='date'][aria-label='{label}'], label:has-text('{label}') + input[type='date']");
            if (await altInput.IsVisibleAsync())
            {
                await altInput.FillAsync(date);
            }
        }
    }

    /// <summary>
    /// Заполняет числовое поле (spinbutton)
    /// </summary>
    public async Task FillNumberFieldAsync(string label, string value)
    {
        var numberInput = _page.GetByLabel(label);
        if (await numberInput.IsVisibleAsync())
        {
            await numberInput.ClearAsync();
            await numberInput.FillAsync(value);
        }
        else
        {
            var altInput = _page.Locator($"input[type='number'][aria-label='{label}'], label:has-text('{label}') + input[type='number']");
            if (await altInput.IsVisibleAsync())
            {
                await altInput.ClearAsync();
                await altInput.FillAsync(value);
            }
        }
    }

    #endregion

    #region Методы испытаний

    /// <summary>
    /// Выбирает тип паспорта качества
    /// </summary>
    public async Task SelectPassportTypeAsync(string passportType)
    {
        await _page.ClickAsync($"text={passportType}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Выбирает параметр для методов испытаний
    /// </summary>
    public async Task SelectParameterAsync(string parameterName)
    {
        // Параметры обычно в выпадающем списке или списке
        var paramLocator = _page.GetByText(parameterName);
        if (await paramLocator.IsVisibleAsync())
        {
            await paramLocator.ClickAsync();
        }
        else
        {
            // Если это select
            var selectLocator = _page.Locator("select").Filter(new LocatorFilterOptions { HasText = parameterName });
            if (await selectLocator.CountAsync() > 0)
            {
                await selectLocator.SelectOptionAsync(new SelectOptionValue { Label = parameterName });
            }
        }
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Получает список отображаемых типов паспортов
    /// </summary>
    public async Task<IReadOnlyList<string>> GetVisiblePassportTypesAsync()
    {
        var types = new List<string>();
        foreach (var expectedType in ExpectedPassportTypes)
        {
            var locator = _page.GetByText(expectedType);
            if (await locator.IsVisibleAsync())
            {
                types.Add(expectedType);
            }
        }
        return types;
    }

    #endregion

    #region Валидация

    /// <summary>
    /// Проверяет наличие ошибки валидации
    /// </summary>
    public async Task<bool> HasValidationErrorAsync()
    {
        var errorSelectors = new[]
        {
            ".validation-error",
            ".error-message",
            ".field-validation-error",
            ".invalid-feedback",
            "[class*='error']",
            ".has-error"
        };

        foreach (var selector in errorSelectors)
        {
            if (await _page.Locator(selector).IsVisibleAsync())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Получает текст ошибки валидации
    /// </summary>
    public async Task<string?> GetValidationErrorTextAsync()
    {
        var errorSelectors = new[]
        {
            ".validation-error",
            ".error-message",
            ".field-validation-error",
            ".invalid-feedback"
        };

        foreach (var selector in errorSelectors)
        {
            var locator = _page.Locator(selector);
            if (await locator.IsVisibleAsync())
            {
                return await locator.TextContentAsync();
            }
        }
        return null;
    }

    #endregion

    #region Диалоги

    /// <summary>
    /// Ожидает и обрабатывает диалог подтверждения
    /// </summary>
    public async Task HandleConfirmDialogAsync(bool accept)
    {
        _page.Dialog += (_, dialog) =>
        {
            if (accept)
                dialog.AcceptAsync();
            else
                dialog.DismissAsync();
        };
    }

    /// <summary>
    /// Проверяет появление диалога сохранения изменений
    /// </summary>
    public async Task<bool> HasUnsavedChangesDialogAsync()
    {
        // Ищем модальное окно с текстом о сохранении
        var dialogTexts = new[]
        {
            "Сохранить изменения",
            "Несохранённые изменения",
            "Вы уверены",
            "Данные будут потеряны"
        };

        foreach (var text in dialogTexts)
        {
            if (await _page.GetByText(text).IsVisibleAsync())
            {
                return true;
            }
        }
        return false;
    }

    #endregion
}
