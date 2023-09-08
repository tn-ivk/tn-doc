using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using PDFtoPrinter;

namespace TN_Doc.Models.Printer
{
    /// <summary>
    /// Вспомогательный класс для работы с принтером в ОС Windows
    /// </summary>
    public sealed class WindowsPrinter : AbsPrinter
    {
        /// <summary>
        /// Получение списка доступных принтеров в системе
        /// </summary>
        /// <returns>Список доступных принтеров в системе</returns>
        public override IEnumerable<string> GetAvailablePrinters() 
            => PrinterSettings.InstalledPrinters.Cast<object>().Cast<string>();


        public override void PrintDoc(string printerName)
        {
            var printer = new PDFtoPrinterPrinter();
            printer.Print(new PrintingOptions(printerName, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PDF", "PDF.pdf")));
        }
    }
}