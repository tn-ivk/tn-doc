using System.Linq;
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
        // Сначала проверяем, не открыто ли уже модальное окно
        var existingModal = _page.Locator("#modal-window.show");
        if (await existingModal.CountAsync() > 0)
        {
            // Окно уже открыто, закрываем его
            await CloseAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // Используем GetByRole для более надёжного поиска кнопки
        var button = _page.GetByRole(AriaRole.Button, new() { Name = "Справочники" });
        await button.ClickAsync(new LocatorClickOptions { Force = true });
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
        // Находим основное модальное окно справочников (с классом .show)
        var mainModal = _page.Locator("#modal-window.show, [role='dialog'].show").First;

        // Пытаемся найти кнопку закрытия в header
        var closeButton = mainModal.Locator(".modal-header .close-btn, .modal-header .btn-close, .modal-header button.close, .modal-header [aria-label='Close']").First;

        if (await closeButton.CountAsync() > 0)
        {
            await closeButton.ClickAsync(new LocatorClickOptions { Force = true });
        }
        else
        {
            // Fallback: нажимаем Escape для закрытия модального окна
            await _page.Keyboard.PressAsync("Escape");
        }

        // Ждём исчезновения класса .show у модального окна
        try
        {
            await _page.WaitForSelectorAsync("#modal-window.show", new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 5000
            });
        }
        catch
        {
            // Если таймаут - пробуем Escape
            await _page.Keyboard.PressAsync("Escape");
            try
            {
                await _page.WaitForSelectorAsync("#modal-window.show", new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = 2000
                });
            }
            catch
            {
                // Игнорируем
            }
        }
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
        var menuItem = _page.Locator(PersonnelMenuSelector).First;
        await menuItem.ClickAsync(new LocatorClickOptions { Force = true });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Переходит в раздел "Методы испытаний"
    /// </summary>
    public async Task NavigateToTestMethodsAsync()
    {
        var menuItem = _page.Locator(TestMethodsMenuSelector).First;
        await menuItem.ClickAsync(new LocatorClickOptions { Force = true });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Переходит в подраздел "Группы пользователей"
    /// </summary>
    public async Task NavigateToUserGroupsAsync()
    {
        var menuItem = _page.Locator(UserGroupsMenuSelector).First;
        await menuItem.ClickAsync(new LocatorClickOptions { Force = true });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Переходит в подраздел "Доверенности"
    /// </summary>
    public async Task NavigateToPowersOfAttorneyAsync()
    {
        var menuItem = _page.Locator(PowersOfAttorneyMenuSelector).First;
        await menuItem.ClickAsync(new LocatorClickOptions { Force = true });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Переходит в подраздел "Пользователи"
    /// </summary>
    public async Task NavigateToUsersAsync()
    {
        var menuItem = _page.Locator(UsersMenuSelector).First;
        await menuItem.ClickAsync(new LocatorClickOptions { Force = true });
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
        // Ждём загрузку страницы
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Находим видимую кнопку "Добавить" в активном контейнере
        // Кнопка имеет класс .add-user-btn
        var addButton = _page.Locator(".add-user-btn:visible").First;

        // Если не нашли специфичную кнопку, ищем по тексту в видимой области
        if (await addButton.CountAsync() == 0)
        {
            addButton = _page.Locator("[role='dialog'] button:has-text('Добавить'):visible").First;
        }

        // Прокручиваем к кнопке
        await addButton.ScrollIntoViewIfNeededAsync();

        // Используем force: true для обхода перекрытия элементом table-container
        await addButton.ClickAsync(new LocatorClickOptions { Force = true });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Нажимает кнопку подтверждения (галочка/сохранение) в строке редактирования
    /// </summary>
    public async Task ClickConfirmAsync()
    {
        // Находим строку редактирования - она содержит текстовые поля ввода
        // В режиме редактирования строка имеет input[type='text'] (не только checkbox)
        var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='text'])").Last;

        // Если не нашли по текстовым полям, ищем по checkbox (для доверенностей)
        if (await editingRow.CountAsync() == 0)
        {
            editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='checkbox'])").Last;
        }

        // Находим последнюю ячейку с кнопками действий
        var actionCell = editingRow.Locator("td").Last;

        // Первая кнопка в первом div - это кнопка подтверждения (fa-unlock)
        var confirmButton = actionCell.Locator("div:first-child button, button").First;

        if (await confirmButton.CountAsync() > 0)
        {
            // Прокручиваем к кнопке и кликаем с Force для обхода перекрытий
            await confirmButton.ScrollIntoViewIfNeededAsync();
            await confirmButton.ClickAsync(new LocatorClickOptions { Force = true });
        }

        // Ожидаем завершения сетевых запросов
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Небольшая пауза для стабилизации UI после сохранения
        await _page.WaitForTimeoutAsync(300);
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
        await deleteButton.ClickAsync(new LocatorClickOptions { Force = true });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Обработка диалога подтверждения удаления (PromiseConfirm)
        var confirmDialog = _page.Locator("#PromiseConfirm.show, .modal.show:has-text('подтвер')");
        if (await confirmDialog.CountAsync() > 0)
        {
            var confirmButton = confirmDialog.Locator("button:has-text('Да'), button:has-text('OK'), .btn-primary").First;
            if (await confirmButton.CountAsync() > 0)
            {
                await confirmButton.ClickAsync(new LocatorClickOptions { Force = true });
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }
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

    // Маппинг названий полей на индексы текстовых полей в строке доверенности
    // Порядок: Номер(0), Дата(1) - первый textbox это номер, второй это дата
    private static readonly Dictionary<string, int> LicenseFieldTextboxIndexes = new()
    {
        { "Номер", 0 },
        { "Дата", 1 }
    };

    // Маппинг названий полей на индексы текстовых полей в строке метода испытаний
    // Порядок (для textbox): Метод(0), Сообщение(1)
    // Мин. значение - это spinbutton/number field
    private static readonly Dictionary<string, int> MethodFieldTextboxIndexes = new()
    {
        { "Метод", 0 },
        { "Сообщение", 1 }
    };

    // Маппинг чекбоксов для методов испытаний
    // Порядок: Активен(0), Контроль мин. значения(1), Применять по умолчанию(2)
    private static readonly Dictionary<string, int> MethodCheckboxIndexes = new()
    {
        { "Активен", 0 },
        { "Контроль мин. значения", 1 },
        { "Применять по умолчанию", 2 }
    };

    /// <summary>
    /// Заполняет текстовое поле по метке (для inline-формы в таблице)
    /// </summary>
    public async Task FillFieldByLabelAsync(string label, string value)
    {
        // Находим строку редактирования - она содержит input[type='text']
        var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='text'])").Last;

        // Если не нашли по текстовым полям, ищем по checkbox
        if (await editingRow.CountAsync() == 0)
        {
            editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='checkbox'])").Last;
        }

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

        // Для доверенностей используем отдельный маппинг
        if (LicenseFieldTextboxIndexes.TryGetValue(label, out var licTextboxIndex))
        {
            var textboxes = editingRow.GetByRole(AriaRole.Textbox);
            var count = await textboxes.CountAsync();

            if (count > licTextboxIndex)
            {
                var textbox = textboxes.Nth(licTextboxIndex);
                await textbox.ClearAsync();
                await textbox.FillAsync(value);
                return;
            }
        }

        // Для методов испытаний используем отдельный маппинг
        if (MethodFieldTextboxIndexes.TryGetValue(label, out var methodTextboxIndex))
        {
            var textboxes = editingRow.GetByRole(AriaRole.Textbox);
            var count = await textboxes.CountAsync();

            if (count > methodTextboxIndex)
            {
                var textbox = textboxes.Nth(methodTextboxIndex);
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
        // Находим строку редактирования - сначала по текстовым полям, затем по checkbox
        var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='text'])").Last;

        if (await editingRow.CountAsync() == 0)
        {
            editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='checkbox'])").Last;
        }

        // Маппинг чекбоксов по позиции в строке пользователей
        // Порядок: Активен(0), Сепаратор(1), Пробелы(2), Сокр.форма(3)
        var userCheckboxIndexes = new Dictionary<string, int>
        {
            { "Cепаратор", 1 },
            { "Сепаратор", 1 },
            { "Пробелы", 2 },
            { "Сокр. форма", 3 },
            { "Сокр.форма", 3 }
        };

        // Для пользователей - специфичные чекбоксы
        if (userCheckboxIndexes.TryGetValue(label, out var userCheckboxIndex))
        {
            var checkboxes = editingRow.GetByRole(AriaRole.Checkbox);
            var count = await checkboxes.CountAsync();

            if (count > userCheckboxIndex)
            {
                var checkbox = checkboxes.Nth(userCheckboxIndex);
                await checkbox.SetCheckedAsync(isChecked);
                return;
            }
        }

        // Для методов испытаний - специфичные чекбоксы
        if (MethodCheckboxIndexes.TryGetValue(label, out var methodCheckboxIndex))
        {
            var checkboxes = editingRow.GetByRole(AriaRole.Checkbox);
            var count = await checkboxes.CountAsync();

            if (count > methodCheckboxIndex)
            {
                var checkbox = checkboxes.Nth(methodCheckboxIndex);
                await checkbox.SetCheckedAsync(isChecked);
                return;
            }
        }

        // Общий маппинг "Активен" - первый чекбокс (index 0) во всех таблицах
        if (label == "Активен")
        {
            var checkboxes = editingRow.GetByRole(AriaRole.Checkbox);
            if (await checkboxes.CountAsync() > 0)
            {
                var checkbox = checkboxes.First;
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
        // Для inline-таблицы пользователей - ищем select в строке редактирования
        if (label == "Доверенность")
        {
            var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='text'])").Last;
            if (await editingRow.CountAsync() > 0)
            {
                // Select "Доверенность" в строке пользователя
                var selectInRow = editingRow.Locator("select").First;
                if (await selectInRow.CountAsync() > 0)
                {
                    // Пробуем выбрать по частичному совпадению текста
                    var options = selectInRow.Locator("option");
                    var count = await options.CountAsync();

                    for (int i = 0; i < count; i++)
                    {
                        var option = options.Nth(i);
                        var text = await option.TextContentAsync();
                        if (text != null && text.Contains(optionText))
                        {
                            var value = await option.GetAttributeAsync("value");
                            if (value != null)
                            {
                                await selectInRow.SelectOptionAsync(new SelectOptionValue { Value = value });
                                return;
                            }
                        }
                    }

                    // Пробуем по точному label
                    try
                    {
                        await selectInRow.SelectOptionAsync(new SelectOptionValue { Label = optionText });
                        return;
                    }
                    catch
                    {
                        // Игнорируем, пробуем следующий способ
                    }
                }
            }
        }

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
        // Для inline-таблицы (доверенности) - дата это обычный текстовый input
        if (LicenseFieldTextboxIndexes.TryGetValue(label, out var licTextboxIndex))
        {
            var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='text'])").Last;
            if (await editingRow.CountAsync() > 0)
            {
                var textboxes = editingRow.GetByRole(AriaRole.Textbox);
                var count = await textboxes.CountAsync();

                if (count > licTextboxIndex)
                {
                    var textbox = textboxes.Nth(licTextboxIndex);
                    await textbox.ClearAsync();
                    await textbox.FillAsync(date);
                    return;
                }
            }
        }

        // Стандартный поиск по label
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
        // Проверяем, является ли значение числовым
        var isNumeric = double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _);

        // Для inline-таблицы методов испытаний - ищем spinbutton в строке редактирования
        if (label == "Мин. значение")
        {
            var editingRow = _page.Locator("[role='dialog'] table tr:has(input[type='text'])").Last;
            if (await editingRow.CountAsync() > 0)
            {
                // Spinbutton - первый числовой input в строке
                var spinbuttons = editingRow.GetByRole(AriaRole.Spinbutton);
                if (await spinbuttons.CountAsync() > 0)
                {
                    var spinbutton = spinbuttons.First;
                    await spinbutton.ClearAsync();

                    // Если значение не числовое, используем PressSequentially для попытки ввода
                    // HTML5 input[type=number] отфильтрует нечисловые символы
                    if (!isNumeric)
                    {
                        await spinbutton.PressSequentiallyAsync(value);
                    }
                    else
                    {
                        await spinbutton.FillAsync(value);
                    }
                    return;
                }
            }
        }

        // Стандартный поиск по label
        var numberInput = _page.GetByLabel(label);
        if (await numberInput.IsVisibleAsync())
        {
            await numberInput.ClearAsync();
            if (!isNumeric)
            {
                await numberInput.PressSequentiallyAsync(value);
            }
            else
            {
                await numberInput.FillAsync(value);
            }
        }
        else
        {
            var altInput = _page.Locator($"input[type='number'][aria-label='{label}'], label:has-text('{label}') + input[type='number']");
            if (await altInput.IsVisibleAsync())
            {
                await altInput.ClearAsync();
                if (!isNumeric)
                {
                    await altInput.PressSequentiallyAsync(value);
                }
                else
                {
                    await altInput.FillAsync(value);
                }
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
        // Проверяем, активен ли элемент уже (имеет класс .active)
        var activeItem = _page.Locator($"[role='dialog'] .list-group-item.active:has-text('{passportType}'), [role='dialog'] li.active:has-text('{passportType}')").First;
        if (await activeItem.CountAsync() > 0)
        {
            // Элемент уже выбран, пропускаем клик
            return;
        }

        // Ищем тип паспорта только в меню (list-group), не в select-ах
        var passportItem = _page.Locator($"[role='dialog'] .list-group-item:has-text('{passportType}'), [role='dialog'] li:has-text('{passportType}')").First;

        if (await passportItem.CountAsync() > 0)
        {
            // Используем force: true для обхода перекрытия элементом col-3
            await passportItem.ClickAsync(new LocatorClickOptions { Force = true });
        }
        else
        {
            // Fallback: ищем по точному тексту
            var exactMatch = _page.GetByRole(AriaRole.Listitem, new() { Name = passportType });
            if (await exactMatch.CountAsync() > 0)
            {
                await exactMatch.First.ClickAsync(new LocatorClickOptions { Force = true });
            }
            else
            {
                // Последняя попытка - клик по тексту в первом найденном элементе
                await _page.Locator($"text={passportType}").First.ClickAsync(new LocatorClickOptions { Force = true });
            }
        }

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Выбирает параметр для методов испытаний
    /// </summary>
    public async Task SelectParameterAsync(string parameterName)
    {
        // Ищем видимый select параметров в активном контейнере
        var parameterSelect = _page.Locator(".parameter-select:visible").First;

        if (await parameterSelect.IsVisibleAsync())
        {
            // Находим опцию, содержащую нужный текст (частичное совпадение)
            var options = parameterSelect.Locator("option");
            var count = await options.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var option = options.Nth(i);
                var text = await option.TextContentAsync();
                if (text != null && text.Contains(parameterName))
                {
                    var value = await option.GetAttributeAsync("value");
                    if (value != null)
                    {
                        await parameterSelect.SelectOptionAsync(new SelectOptionValue { Value = value });
                        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        return;
                    }
                }
            }
        }

        // Fallback: пробуем выбрать по точному тексту
        try
        {
            var select = _page.Locator("[role='dialog'] select.parameter-select:visible").First;
            await select.SelectOptionAsync(new SelectOptionValue { Label = parameterName });
        }
        catch
        {
            // Игнорируем ошибку
        }

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Получает список отображаемых типов паспортов
    /// </summary>
    public async Task<IReadOnlyList<string>> GetVisiblePassportTypesAsync()
    {
        var types = new List<string>();

        // Ищем типы паспортов только в меню (list-group), не в select-ах
        var sideMenu = _page.Locator("[role='dialog'] .list-group, [role='dialog'] .side-menu, [role='dialog'] .passport-types-menu");
        var menuItems = sideMenu.Locator(".list-group-item, .side-menu-item, a[data-passport-type]");

        var count = await menuItems.CountAsync();
        for (int i = 0; i < count; i++)
        {
            var item = menuItems.Nth(i);
            if (await item.IsVisibleAsync())
            {
                var text = await item.TextContentAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var trimmedText = text.Trim();
                    // Проверяем, что это один из ожидаемых типов паспортов
                    if (ExpectedPassportTypes.Any(ep => trimmedText.Contains(ep) || ep.Contains(trimmedText)))
                    {
                        types.Add(trimmedText);
                    }
                }
            }
        }

        // Fallback: если список пуст, проверяем каждый ожидаемый тип в таблице
        if (types.Count == 0)
        {
            foreach (var expectedType in ExpectedPassportTypes)
            {
                // Ищем только в элементах списка, не в option
                var locator = _page.Locator($"[role='dialog'] .list-group-item:has-text('{expectedType}'), [role='dialog'] li:has-text('{expectedType}')").First;
                if (await locator.CountAsync() > 0 && await locator.IsVisibleAsync())
                {
                    types.Add(expectedType);
                }
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
