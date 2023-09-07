using System.Collections.Generic;

namespace TN_Doc.Models.Printer
{
    /// <summary>
    /// Абстрактная модель принтера печати документов
    /// </summary>
    public abstract class AbsPrinter
    {
        /// <summary>
        /// Получение списка доступных принтеров
        /// </summary>
        /// <returns>Список доступных принтеров</returns>
        public abstract IEnumerable<string> GetAvailablePrinters();
        
        /// <summary>
        /// Печать сформированного отчёта на заданном принтере
        /// </summary>
        /// <param name="printerName">Название принтера</param>
        public abstract void PrintDoc(string printerName);
    }
}