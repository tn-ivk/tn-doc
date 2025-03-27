using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TN_Doc.Models.Services;

namespace TN_Doc.Controllers
{

    
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
                return null;
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

        #region old_code

                // public List<string> GetListPrinters()
        // {
        //     List<string> printers = new List<string>();
        //     var process = new Process()
        //     {
        //         StartInfo =
        //         {
        //             RedirectStandardOutput = true,
        //             RedirectStandardError = true,
        //             UseShellExecute = false,
        //             WindowStyle = ProcessWindowStyle.Hidden,
        //             Arguments = "-e",
        //             FileName = "lpstat",
        //             CreateNoWindow = true,
        //             
        //         }
        //     };
        //     process.Start();
        //     process.WaitForExit();
        //     var resultOutput = process.StandardOutput.ReadToEnd();
        //     foreach (var item in resultOutput.Split('\n'))
        //         if (!string.IsNullOrEmpty(item))
        //             printers.Add(item);
        //     
        //     return printers;
        // }
        //
        // public void PrintDoc(string printerName)
        // {
        //     var printersName = GetListPrinters();
        //     Console.WriteLine($"-H localhost -P  {printerName} -ol {Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot", "PDF", $"PDF.pdf")}");
        //     if (printersName.Contains(printerName))
        //     {
        //         var process = new Process()
        //         {
        //             StartInfo =
        //             {
        //                 UseShellExecute = false,
        //                 WindowStyle = ProcessWindowStyle.Hidden,
        //                 Arguments
        //                     = $"-H localhost -P  {printerName} -ol {Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot", "PDF", $"PDF.pdf")}",
        //                 FileName = "lpr",
        //                 CreateNoWindow = true
        //             }
        //         };
        //         process.Start();
        //         process.WaitForExit();
        //     }
        //     

        #endregion
    }
}