using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Tests.E2E.Base;

/// <summary>
/// Базовый класс для всех E2E тестов с поддержкой Playwright.
/// Наследует от PageTest, который автоматически управляет жизненным циклом браузера.
/// </summary>
[TestFixture]
[Category("E2E")]
public abstract class PlaywrightTestBase : PageTest
{
    /// <summary>
    /// Базовый URL приложения
    /// </summary>
    protected const string BaseUrl = "http://localhost:5000";

    /// <summary>
    /// Директория для сохранения скриншотов
    /// </summary>
    protected string ScreenshotsDirectory { get; private set; } = null!;

    /// <summary>
    /// Таймаут ожидания элементов по умолчанию (мс)
    /// </summary>
    protected const int DefaultTimeout = 10000;

    /// <summary>
    /// Короткий таймаут для быстрых операций (мс)
    /// </summary>
    protected const int ShortTimeout = 3000;

    /// <summary>
    /// Настройки браузера для тестов
    /// </summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            Locale = "ru-RU",
            ViewportSize = new ViewportSize
            {
                Width = 1920,
                Height = 1080
            },
            IgnoreHTTPSErrors = true
        };
    }

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        // Создаём директорию для скриншотов
        var baseDir = Path.GetDirectoryName(typeof(PlaywrightTestBase).Assembly.Location)!;
        ScreenshotsDirectory = Path.Combine(baseDir, "..", "..", "..", "..", "..",
            "tests", "dictionaries", "results");

        if (!Directory.Exists(ScreenshotsDirectory))
        {
            Directory.CreateDirectory(ScreenshotsDirectory);
        }
    }

    [SetUp]
    public async Task BaseSetUp()
    {
        // Устанавливаем таймаут по умолчанию
        Page.SetDefaultTimeout(DefaultTimeout);

        // Переходим на главную страницу
        await Page.GotoAsync(BaseUrl);

        // Ждём загрузки страницы
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Делает скриншот с указанным именем
    /// </summary>
    /// <param name="scenarioName">Номер и название сценария (например: "1.2-add-user")</param>
    protected async Task TakeScreenshotAsync(string scenarioName)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var fileName = $"{scenarioName}-{timestamp}.png";
        var filePath = Path.Combine(ScreenshotsDirectory, fileName);

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filePath,
            FullPage = false
        });

        TestContext.WriteLine($"Скриншот сохранён: {filePath}");
    }

    /// <summary>
    /// Ожидает появления элемента на странице
    /// </summary>
    protected async Task WaitForElementAsync(string selector, int timeout = DefaultTimeout)
    {
        await Page.Locator(selector).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeout
        });
    }

    /// <summary>
    /// Ожидает исчезновения элемента
    /// </summary>
    protected async Task WaitForElementHiddenAsync(string selector, int timeout = DefaultTimeout)
    {
        await Page.Locator(selector).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = timeout
        });
    }

    /// <summary>
    /// Проверяет, что элемент виден на странице
    /// </summary>
    protected async Task<bool> IsElementVisibleAsync(string selector)
    {
        try
        {
            return await Page.Locator(selector).IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Получает текст элемента
    /// </summary>
    protected async Task<string> GetTextAsync(string selector)
    {
        return await Page.Locator(selector).TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Кликает по элементу и ждёт стабилизации сети
    /// </summary>
    protected async Task ClickAndWaitAsync(string selector)
    {
        await Page.Locator(selector).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Заполняет текстовое поле
    /// </summary>
    protected async Task FillAsync(string selector, string value)
    {
        var locator = Page.Locator(selector);
        await locator.ClearAsync();
        await locator.FillAsync(value);
    }

    /// <summary>
    /// Устанавливает состояние чекбокса
    /// </summary>
    protected async Task SetCheckboxAsync(string selector, bool checked_)
    {
        var locator = Page.Locator(selector);
        var isChecked = await locator.IsCheckedAsync();

        if (isChecked != checked_)
        {
            await locator.ClickAsync();
        }
    }

    /// <summary>
    /// Выбирает опцию в выпадающем списке по тексту
    /// </summary>
    protected async Task SelectOptionByTextAsync(string selector, string text)
    {
        await Page.Locator(selector).SelectOptionAsync(new SelectOptionValue { Label = text });
    }

    /// <summary>
    /// Выбирает опцию в выпадающем списке по значению
    /// </summary>
    protected async Task SelectOptionByValueAsync(string selector, string value)
    {
        await Page.Locator(selector).SelectOptionAsync(new SelectOptionValue { Value = value });
    }

    /// <summary>
    /// Проверяет наличие текста на странице
    /// </summary>
    protected async Task AssertTextVisibleAsync(string text, int timeout = DefaultTimeout)
    {
        await Expect(Page.GetByText(text)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = timeout
        });
    }

    /// <summary>
    /// Проверяет количество элементов
    /// </summary>
    protected async Task AssertElementCountAsync(string selector, int expectedCount)
    {
        await Expect(Page.Locator(selector)).ToHaveCountAsync(expectedCount);
    }

    /// <summary>
    /// Ждёт пока таблица загрузится (появится хотя бы одна строка или сообщение о пустоте)
    /// </summary>
    protected async Task WaitForTableLoadAsync(string tableSelector)
    {
        // Ждём либо строки данных, либо сообщение о пустой таблице
        await Page.WaitForSelectorAsync($"{tableSelector} tbody tr, {tableSelector} .dataTables_empty, .empty-message");
    }
}
