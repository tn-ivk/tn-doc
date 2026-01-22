using Microsoft.Playwright;

namespace Tests.E2E.Pages;

/// <summary>
/// Page Object для модального окна "Справочники".
/// Инкапсулирует взаимодействие с основными элементами справочников.
/// </summary>
public class DictionariesPage
{
    private readonly IPage _page;

    // Селекторы главного меню
    private const string DictionariesButtonSelector = "button:has-text('Справочники'), [data-action='dictionaries'], .btn-dictionaries";
    private const string ModalSelector = ".modal, .dictionaries-modal, [role='dialog']";
    private const string ModalCloseSelector = ".modal .close, .modal [aria-label='Close'], .btn-close";

    // Селекторы меню первого уровня
    private const string PersonnelMenuSelector = "text=Персонал";
    private const string TestMethodsMenuSelector = "text=Методы испытаний";

    // Селекторы подменю Персонал
    private const string UserGroupsMenuSelector = "text=Группы пользователей";
    private const string PowersOfAttorneyMenuSelector = "text=Доверенности";
    private const string UsersMenuSelector = "text=Пользователи";

    // Селекторы таблицы
    private const string TableSelector = "table, .data-table, .grid-table";
    private const string TableRowSelector = "tbody tr";
    private const string AddButtonSelector = "button:has-text('Добавить'), .btn-add, [data-action='add']";
    private const string SaveButtonSelector = "button:has-text('Сохранить'), .btn-save, [data-action='save'], button[type='submit']";
    private const string ConfirmButtonSelector = ".fa-check, button:has-text('✓'), [data-action='confirm']";
    private const string EditButtonSelector = ".fa-edit, .fa-pencil, .fa-lock, [data-action='edit']";
    private const string DeleteButtonSelector = ".fa-trash, .fa-remove, [data-action='delete']";

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
        await _page.ClickAsync(DictionariesButtonSelector);
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
    /// Выбирает группу пользователей по названию
    /// </summary>
    public async Task SelectUserGroupAsync(string groupName)
    {
        await _page.ClickAsync($"text={groupName}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Получает список отображаемых групп пользователей
    /// </summary>
    public async Task<IReadOnlyList<string>> GetUserGroupsAsync()
    {
        var groups = new List<string>();
        foreach (var expectedGroup in ExpectedUserGroups)
        {
            var locator = _page.GetByText(expectedGroup);
            if (await locator.IsVisibleAsync())
            {
                groups.Add(expectedGroup);
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
    /// Нажимает кнопку подтверждения (галочка)
    /// </summary>
    public async Task ClickConfirmAsync()
    {
        await _page.ClickAsync(ConfirmButtonSelector);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Нажимает кнопку редактирования для строки с указанным текстом
    /// </summary>
    public async Task ClickEditForRowAsync(string rowText)
    {
        var row = _page.Locator($"{TableRowSelector}:has-text('{rowText}')");
        await row.Locator(EditButtonSelector).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Нажимает кнопку удаления для строки с указанным текстом
    /// </summary>
    public async Task ClickDeleteForRowAsync(string rowText)
    {
        var row = _page.Locator($"{TableRowSelector}:has-text('{rowText}')");
        await row.Locator(DeleteButtonSelector).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Получает количество строк в таблице
    /// </summary>
    public async Task<int> GetTableRowCountAsync()
    {
        return await _page.Locator(TableRowSelector).CountAsync();
    }

    /// <summary>
    /// Проверяет наличие строки с указанным текстом
    /// </summary>
    public async Task<bool> HasRowWithTextAsync(string text)
    {
        return await _page.Locator($"{TableRowSelector}:has-text('{text}')").CountAsync() > 0;
    }

    #endregion

    #region Работа с полями формы

    /// <summary>
    /// Заполняет текстовое поле по метке
    /// </summary>
    public async Task FillFieldByLabelAsync(string label, string value)
    {
        // Пробуем несколько стратегий поиска поля
        var selectors = new[]
        {
            $"input[aria-label='{label}']",
            $"label:has-text('{label}') + input",
            $"label:has-text('{label}') ~ input",
            $"td:has-text('{label}') + td input",
            $"th:has-text('{label}') ~ td input",
            $"[placeholder*='{label}']"
        };

        foreach (var selector in selectors)
        {
            var locator = _page.Locator(selector).First;
            if (await locator.IsVisibleAsync())
            {
                await locator.ClearAsync();
                await locator.FillAsync(value);
                return;
            }
        }

        // Если не нашли по стандартным селекторам, ищем по тексту рядом
        var fieldLocator = _page.GetByLabel(label);
        if (await fieldLocator.IsVisibleAsync())
        {
            await fieldLocator.ClearAsync();
            await fieldLocator.FillAsync(value);
        }
    }

    /// <summary>
    /// Устанавливает состояние чекбокса по метке
    /// </summary>
    public async Task SetCheckboxByLabelAsync(string label, bool isChecked)
    {
        var checkbox = _page.GetByLabel(label);
        if (await checkbox.IsVisibleAsync())
        {
            await checkbox.SetCheckedAsync(isChecked);
        }
        else
        {
            // Альтернативный поиск
            var altCheckbox = _page.Locator($"label:has-text('{label}') input[type='checkbox']");
            if (await altCheckbox.IsVisibleAsync())
            {
                await altCheckbox.SetCheckedAsync(isChecked);
            }
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
