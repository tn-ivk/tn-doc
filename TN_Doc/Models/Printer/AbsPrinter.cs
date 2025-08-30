using System.Collections.Generic;
using System.Threading.Tasks;
using TN_DocGeneral.Services;

namespace TN_Doc.Models.Printer;

/// <summary>
/// Абстрактная модель принтера печати документов
/// </summary>
public abstract class AbsPrinter(IReportBuffer buffer)
{
    protected IReportBuffer _buffer = buffer;

    /// <summary>
    /// Получение списка доступных принтеров
    /// </summary>
    /// <returns>Список доступных принтеров</returns>
    public abstract IEnumerable<string> GetAvailablePrinters();
    
    /// <summary>
    /// Печать сформированного отчёта на заданном принтере
    /// </summary>
    /// <param name="printerName">Название принтера</param>
    public abstract Task PrintDocAsync(string printerName);
}