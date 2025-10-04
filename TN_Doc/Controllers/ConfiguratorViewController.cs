using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TN_Doc.Controllers;

/// <summary>
/// Контроллер для отображения страницы конфигуратора
/// </summary>
public class ConfiguratorViewController : Controller
{
    private readonly ILogger<ConfiguratorViewController> _logger;

    public ConfiguratorViewController(ILogger<ConfiguratorViewController> logger)
    {
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Главная страница конфигуратора
    /// </summary>
    [Route("configurator")]
    public IActionResult Index()
    {
        _logger.LogInformation("Открыта страница конфигуратора");
        return View();
    }
}
