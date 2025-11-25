using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TN.DocData;
using TN_Doc.Services;

namespace TN_Doc.Controllers;

/// <summary>
/// API контроллер для конфигуратора настроек приложения
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfiguratorController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfiguratorController> _logger;

    public ConfiguratorController(IConfigurationService configurationService, ILogger<ConfiguratorController> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Получить текущую конфигурацию приложения
    /// </summary>
    /// <returns>Конфигурация приложения</returns>
    [HttpGet("config")]
    [ProducesResponseType(typeof(CfgApp), 200)]
    [ProducesResponseType(500)]
    public IActionResult GetConfig()
    {
        try
        {
            _logger.LogInformation("Запрос на получение конфигурации приложения");
            var config = _configurationService.GetConfiguration();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении конфигурации: {ErrorMessage}", ex.Message);
            return StatusCode(500, new
            {
                error = "Не удалось получить конфигурацию",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Сохранить конфигурацию приложения
    /// </summary>
    /// <param name="config">Новая конфигурация</param>
    /// <returns>Результат сохранения</returns>
    [HttpPost("config")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SaveConfig([FromBody] CfgApp config)
    {
        if (config == null)
        {
            return BadRequest(new { error = "Конфигурация не может быть пустой" });
        }

        try
        {
            _logger.LogInformation("Запрос на сохранение конфигурации приложения");

            // Валидация конфигурации
            var validationResult = await _configurationService.ValidateConfigurationAsync(config);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Конфигурация не прошла валидацию: {Errors}", string.Join(", ", validationResult.Errors));
                return BadRequest(new
                {
                    error = "Ошибка валидации конфигурации",
                    errors = validationResult.Errors
                });
            }

            // Сохранение конфигурации
            var success = await _configurationService.SaveConfigurationAsync(config);
            if (!success)
            {
                return StatusCode(500, new { error = "Не удалось сохранить конфигурацию" });
            }

            _logger.LogInformation("Конфигурация успешно сохранена");
            return Ok(new { message = "Конфигурация успешно сохранена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении конфигурации: {ErrorMessage}", ex.Message);
            return StatusCode(500, new
            {
                error = "Не удалось сохранить конфигурацию",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Валидация конфигурации
    /// </summary>
    /// <param name="config">Конфигурация для валидации</param>
    /// <returns>Результат валидации</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResult), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ValidateConfig([FromBody] CfgApp config)
    {
        if (config == null)
        {
            return BadRequest(new { error = "Конфигурация не может быть пустой" });
        }

        try
        {
            var validationResult = await _configurationService.ValidateConfigurationAsync(config);
            return Ok(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при валидации конфигурации: {ErrorMessage}", ex.Message);
            return StatusCode(500, new
            {
                error = "Ошибка валидации",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Загрузить конфигурационный файл документа
    /// </summary>
    /// <param name="path">Путь к файлу конфигурации (относительный от корня приложения)</param>
    /// <returns>Содержимое файла в формате JSON</returns>
    [HttpGet("document-config")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> LoadDocumentConfig([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest(new { error = "Путь к файлу не может быть пустым" });
        }

        try
        {
            _logger.LogInformation("Запрос на загрузку конфигурации документа: {Path}", path);
            var content = await _configurationService.LoadDocumentConfigAsync(path);
            return Ok(content);
        }
        catch (System.IO.FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Файл конфигурации не найден: {Path}", path);
            return NotFound(new
            {
                error = "Файл конфигурации не найден",
                path = path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке конфигурации документа: {ErrorMessage}", ex.Message);
            return StatusCode(500, new
            {
                error = "Не удалось загрузить конфигурацию документа",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Сохранить конфигурационный файл документа
    /// </summary>
    /// <param name="request">Запрос на сохранение (путь и содержимое)</param>
    /// <returns>Результат сохранения</returns>
    [HttpPost("document-config")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SaveDocumentConfig([FromBody] DocumentConfigRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Path) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Путь и содержимое файла обязательны" });
        }

        try
        {
            _logger.LogInformation("Запрос на сохранение конфигурации документа: {Path}", request.Path);

            // Валидация JSON
            try
            {
                Newtonsoft.Json.JsonConvert.DeserializeObject(request.Content);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _logger.LogWarning("Невалидный JSON в запросе: {Error}", ex.Message);
                return BadRequest(new
                {
                    error = "Невалидный JSON",
                    details = ex.Message
                });
            }

            var success = await _configurationService.SaveDocumentConfigAsync(request.Path, request.Content);
            if (!success)
            {
                return StatusCode(500, new { error = "Не удалось сохранить конфигурацию документа" });
            }

            _logger.LogInformation("Конфигурация документа успешно сохранена: {Path}", request.Path);
            return Ok(new { message = "Конфигурация документа успешно сохранена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении конфигурации документа: {ErrorMessage}", ex.Message);
            return StatusCode(500, new
            {
                error = "Не удалось сохранить конфигурацию документа",
                details = ex.Message
            });
        }
    }
}

/// <summary>
/// Модель запроса на сохранение конфигурации документа
/// </summary>
public class DocumentConfigRequest
{
    /// <summary>
    /// Путь к файлу конфигурации (относительный от корня приложения)
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Содержимое файла в формате JSON
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
