using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


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
        public override IEnumerable<string> GetAvailablePrinters() => new List<string>() { "" };


        /// <summary>
        /// Печать документа на заданном принтере
        /// </summary>
        /// <param name="printerName">Наименование принтера</param>
        /// <returns>Задача на печать документа pdf</returns>
        public override Task PrintDocAsync(string printerName)
        {
            return Task.Run(async() =>
            {
                    var curDir = Directory.GetCurrentDirectory();
                    var printersName = GetAvailablePrinters();
                    if (!printersName.Contains(printerName))
                        return;
                    using var process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.Arguments = $"-p \"{printerName}\" -f \"{Path.Combine(curDir, "wwwroot", "PDF", "PDF.pdf")}\"";
                    process.StartInfo.FileName = Path.Combine(curDir, "prutils", "winprutil.exe");
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    await Task.Delay(2000);
                    await process.WaitForExitAsync();
            });
        }
    }
}