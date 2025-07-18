using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TN_Doc.Models.Services;

namespace TN_Doc.Controllers;

/// <summary>
/// Контроллер для печати документов
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
[Produces("application/json")]
[Consumes("application/json")]
public class PrintController : ControllerBase
{
    private readonly PrinterService _service;
    readonly ILogger<PrintController> _logger;

    /// <summary>
    /// Инициализация контроллера
    /// </summary>
    /// <param name="printService">Сервис взаимодействия с принтерами</param>
    /// <exception cref="ArgumentNullException">При отсутствие с сервиса взаимодейтсвия с принтерами</exception>
    public PrintController(PrinterService printService, ILogger<PrintController> logger)
    {
        _service = printService ?? throw new ArgumentNullException(nameof(printService), @"Отсутствует сервис взаимодействия с принтером");
        _logger = logger;
    }

    /// <summary>
    /// Получение доступных принтеров в системе
    /// </summary>
    /// <returns>Список доступных принтеров в системе</returns>
    [HttpGet]
    public IActionResult GetListPrinters()
    {
        _logger.LogTrace("Попытка получения списка принтеров...");
        try
        {
            var printers = _service.GetPrinters().ToArray();
            if(printers.Any())
                _logger.LogTrace($"Список принтеров:\n{string.Join("\n", printers)}");
            else
                _logger.LogWarning($"В системе отсутствуют установленные принтеры");
            return Ok(printers);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при получении списка принтеров: {ex.Message}");
            return StatusCode(500, "Произошла ошибка при получении списка принтеров");
        }
           
    }
    
    /// <summary>
    /// Печать документа на конкретном принтере
    /// </summary>
    /// <param name="printerName">Наименование принтера для печати</param>
    [HttpGet]
    public async Task<IActionResult> PrintDoc(string printerName)
    {
        _logger.LogTrace($"Печать документа на принтере: {printerName}");
        await _service.PrintDocAsync(printerName);
        return Ok();
    }
}