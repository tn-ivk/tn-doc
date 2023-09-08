using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TN_Doc.Models.Printer
{
    /// <summary>
    /// Класс принтера для взаимодействия с печатью под Linux
    /// </summary>
    public sealed class LinuxPrinter : AbsPrinter
    {
        /// <summary>
        /// Получение списка доступных принтеров в системе
        /// </summary>
        /// <returns>Список доступных принтеров</returns>
        public override IEnumerable<string> GetAvailablePrinters()
        {
            using var process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.Arguments = "-e";
            process.StartInfo.FileName = "lpstat";
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            foreach (var item in process.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries))
                yield return item;
        }

        /// <summary>
        /// Печать сформированного отчёта на заданном принтере
        /// </summary>
        /// <param name="printerName">Название принтера</param>
        public override Task PrintDocAsync(string printerName)
        {
            return Task.Run(() =>
            {
                var printersName = GetAvailablePrinters();
                if (!printersName.Contains(printerName))
                    return;
                var consoleStr = $"-H localhost -P  {printerName} -ol {Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PDF", "PDF.pdf")}";
                Console.WriteLine(consoleStr);
                using var process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = consoleStr;
                process.StartInfo.FileName = "lpr";
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            });
        }
    }
}