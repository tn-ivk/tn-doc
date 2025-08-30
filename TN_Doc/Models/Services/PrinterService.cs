using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TN_Doc.Models.Printer;

namespace TN_Doc.Models.Services;

/// <summary>
/// Сервис для взаимодействия с принтерами
/// </summary>
public sealed class PrinterService : IPrinterService
{
    private readonly AbsPrinter _printer;
    private readonly ILogger<PrinterService> _logger;

    /// <summary>
    /// Инициализация сервиса
    /// </summary>
    /// <param name="printer">Конкретная реализация агента работы с принтером</param>
    /// <param name="logger">Логгер для записи ошибок</param>
    public PrinterService(AbsPrinter printer, ILogger<PrinterService> logger)
    {
        _printer = printer ?? throw new ArgumentNullException(nameof(printer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Получение списка принтеров для печати
    /// </summary>
    /// <returns>
    /// Список принтеров
    /// </returns>
    public IEnumerable<string> GetPrinters()
    {
        try
        {
            return _printer.GetAvailablePrinters();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка принтеров");
            throw;
        }
    }

    /// <summary>
    /// Печать документа на конкретном принтере
    /// </summary>
    /// <param name="printerName">Список доступных принтеров</param>
    public async Task PrintDocAsync(string printerName)
    {
        try
        {
            await _printer.PrintDocAsync(printerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при печати документа на принтере {PrinterName}", printerName);
            throw;
        }
    }
}