using System.Collections.Generic;
using System.Threading.Tasks;

namespace TN_Doc.Models.Services;

/// <summary>
/// Интерфейс сервиса для взаимодействия с принтерами
/// </summary>
public interface IPrinterService
{
    /// <summary>
    /// Получение списка принтеров для печати
    /// </summary>
    /// <returns>
    /// Список принтеров
    /// </returns>
    IEnumerable<string> GetPrinters();

    /// <summary>
    /// Печать документа на конкретном принтере
    /// </summary>
    /// <param name="printerName">Список доступных принтеров</param>
    Task PrintDocAsync(string printerName);
}

