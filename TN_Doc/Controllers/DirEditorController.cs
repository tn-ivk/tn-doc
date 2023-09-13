using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TN_Doc.Models.DTOs;
using TN_Doc.Models.Services;

namespace TN_Doc.Controllers
{
    /// <summary>
    /// Контроллер для редактирования справочников приложения
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class DirEditorController : ControllerBase
    {
        private readonly ILogger<DirEditorController> _logger;
        private readonly DirectoryService _service;

        /// <summary>
        /// Инициализация объекта
        /// </summary>
        /// <param name="service">Сервис для взаимодействия с со справочниками</param>
        /// <exception cref="ArgumentNullException">При отсутствие сервиса взаимодействия со справочниками</exception>
        public DirEditorController(DirectoryService service) =>
            _service = service ?? throw new ArgumentNullException(nameof(service), @"Отсутствует сервис для работы со правочниками");

        /// <summary>
        /// Получения всех справочник
        /// </summary>
        /// <returns>Список доступных справочников в приложение</returns>
        /// <returns>200 - словарь приложения</returns>
        [HttpGet]
        [Route("GetDir")]
        public async Task<IActionResult> GetDirAsync() => Ok(new DirEditDTO() { DirJsonRaw = await _service.GetDirectoriesJsonAsync() });

        /// <summary>
        /// Установка нового значения словарей. Словари поступают в формате JSON.
        /// </summary>
        /// <param name="jsonPatch">Данные с новыми словарями</param>
        /// <returns>200 - если словари обновлены</returns>
        [HttpPost]
        [Route("SetDir")]
        public async Task<IActionResult> SetDirAsync([FromBody] DirEditDTO jsonPatch)
        {
            await _service.SetDirectoriesFromJsonAsync(jsonPatch.DirJsonRaw);
            return Ok();
        }

        /// <summary>
        /// Получения конфигурации используемых паспортов качества
        /// </summary>
        /// <returns>200 - конфигурация используемых паспортов</returns>
        [HttpGet]
        [Route("GetQpConfigs")]
        public async Task<IActionResult> GetQpConfigsAsync() => Ok(new QpEditDto() { QpCfgJsonRaw = await _service.GetQualityPassportConfigs() });

        /// <summary>
        /// Установка новой конфигурации проекта
        /// </summary>
        /// <param name="jsonPatch">Данные с новыми паспортами качества</param>
        /// <returns>200 - модификация паспортов прошла успешно</returns>
        [HttpPost]
        [Route("SetQpConfigs")]
        public async Task<IActionResult> SetQpConfigsAsync([FromBody] QpEditDto jsonPatch)
        {
            await _service.SetQpConfigFromJsonAsync(jsonPatch.QpCfgJsonRaw);
            return Ok();
        }
    }
}