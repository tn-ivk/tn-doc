using System.Collections.Generic;
using TN_Doc.Models.Printer;

namespace TN_Doc.Models.Services
{
    /// <summary>
    /// Сервис для взаимодействия с принтерами
    /// </summary>
    public sealed class PrinterService
    {
        private readonly AbsPrinter _printer;

        /// <summary>
        /// Инициализация сервиса
        /// </summary>
        /// <param name="printer">Конкретная реализация агента работы с принтером</param>
        public PrinterService(AbsPrinter printer) => _printer = printer;
        
        /// <summary>
        /// Получение списка принтеров для печати
        /// </summary>
        /// <returns>
        /// Список принтеров
        /// </returns>
        public IEnumerable<string> GetPrinters() => _printer.GetAvailablePrinters();

        /// <summary>
        /// Печать документа на конкретном принтере
        /// </summary>
        /// <param name="printerName">Список доступных принтеров</param>
        public void PrintDoc(string printerName) => _printer.PrintDoc(printerName);
    }
}