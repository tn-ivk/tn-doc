using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TN_Doc.Models.DTOs;
using TN_DocGeneral.Services;

namespace TN_Doc.Controllers;

/// <summary>
/// Контроллер для редактирования справочников приложения
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class DirEditorController : ControllerBase
{
    private readonly ILogger<DirEditorController> _logger;
    private readonly IAppConfigService _service;
    private readonly IConfigurationCacheService _configCache;

    /// <summary>
    /// Инициализация объекта
    /// </summary>
    /// <param name="service">Сервис для взаимодействия с со справочниками</param>
    /// <exception cref="ArgumentNullException">При отсутствие сервиса взаимодействия со справочниками</exception>
    public DirEditorController(ILogger<DirEditorController> logger, IConfiguration configuration, IConfigurationCacheService configCache)
    {
        _service = AppConfigService.GetInstance(configuration);
        _logger = logger;
        _configCache = configCache;
    }


    /// <summary>
    /// Получения всех справочник
    /// </summary>
    /// <returns>Список доступных справочников в приложение</returns>
    /// <returns>200 - словарь приложения</returns>
    [HttpGet]
    [Route("GetDir")]
    public async Task<IActionResult> GetDirAsync()
    {
        _logger.LogTrace("Получения всех справочников");
        var directories = new DirEditDTO { DirJsonRaw = await _service.GetDictionariesJsonAsync() };
        return Ok(directories);
    } 

    /// <summary>
    /// Установка нового значения словарей. Словари поступают в формате JSON.
    /// </summary>
    /// <param name="jsonPatch">Данные с новыми словарями</param>
    /// <returns>200 - если словари обновлены</returns>
    [HttpPost]
    [Route("SetDir")]
    public async Task<IActionResult> SetDirAsync([FromBody] DirEditDTO jsonPatch)
    {
        _logger.LogTrace("Установка нового значения словарей");
        await _service.SetDirectoriesJsonAsync(jsonPatch.DirJsonRaw);
        _configCache.ClearCache();
        return Ok();
    }

    /// <summary>
    /// Получения конфигурации используемых паспортов качества
    /// </summary>
    /// <returns>200 - конфигурация используемых паспортов</returns>
    [HttpGet]
    [Route("GetQpConfigs")]
    public async Task<IActionResult> GetQpConfigsAsync()
    {
        _logger.LogTrace("Получения конфигурации используемых паспортов качества");
        return Ok(new QpEditDto() { QpCfgJsonRaw = await _service.GetQualityPassportConfigs() });
    }

    /// <summary>
    /// Установка новой конфигурации проекта
    /// </summary>
    /// <param name="jsonPatch">Данные с новыми паспортами качества</param>
    /// <returns>200 - модификация паспортов прошла успешно</returns>
    [HttpPost]
    [Route("SetQpConfigs")]
    public async Task<IActionResult> SetQpConfigsAsync([FromBody] QpEditDto jsonPatch)
    {
        _logger.LogTrace("Установка новой конфигурации проекта");
        await _service.SetQpConfigFromJsonAsync(jsonPatch.QpCfgJsonRaw);
        _configCache.ClearCache();
        return Ok();
    }

    /// <summary>
    /// Добавление метода испытаний в справочник
    /// </summary>
    /// <param name="dto">Данные метода испытаний</param>
    /// <returns>200 с ID добавленного метода, 400 при ошибке</returns>
    [HttpPost]
    [Route("AddMethod")]
    public async Task<IActionResult> AddMethodAsync([FromBody] AddMethodDto dto)
    {
        _logger.LogTrace($"Добавление метода испытаний '{dto.MethodName}' для параметра {dto.ParameterId}");

        if (string.IsNullOrWhiteSpace(dto.EditConfigFilePath))
            return BadRequest("EditConfigFilePath is required");

        if (string.IsNullOrWhiteSpace(dto.MethodName))
            return BadRequest("MethodName is required");

        var methodId = await _service.AddMethodToConfigAsync(
            dto.EditConfigFilePath,
            dto.ParameterId,
            dto.MethodName,
            dto.IsDefault,
            dto.LimitValueActivate,
            dto.LimitValue,
            dto.LimitValueString);

        if (methodId < 0)
            return BadRequest("Failed to add method");

        _configCache.ClearCache();
        return Ok(new { id = methodId });
    }
}
