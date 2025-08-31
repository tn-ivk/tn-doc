using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TN_Doc.Models.Services;
using System.Threading.Tasks;

namespace TN_Doc.Models.Printer;

/// <summary>
/// Класс принтера для взаимодействия с печатью под Linux
/// </summary>
public sealed class LinuxPrinter : AbsPrinter
{
    private readonly IReportBuffer _buffer;

    public LinuxPrinter(IReportBuffer buffer)
    {
        _buffer = buffer;
    }
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
            var pdfBytes = _buffer.GetPdfBytes();
            if (pdfBytes is null || pdfBytes.Length == 0)
                return;
            using var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.Arguments = $"-d {printerName} -t \"TN_Doc\"";
            process.StartInfo.FileName = "lp";
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            using (var stdin = process.StandardInput.BaseStream)
            {
                stdin.Write(pdfBytes, 0, pdfBytes.Length);
                stdin.Flush();
            }
            process.WaitForExit();
        });
    }
}