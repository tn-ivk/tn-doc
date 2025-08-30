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
public sealed class LinuxPrinter(IReportBuffer buffer) : AbsPrinter(buffer)
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
        
        if (!process.Start())
        {
            throw new InvalidOperationException("Не удалось запустить процесс lpstat для получения списка принтеров");
        }
        
        process.WaitForExit();
        
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Ошибка выполнения lpstat: {error}");
        }
        
        var output = process.StandardOutput.ReadToEnd();
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
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
                throw new InvalidOperationException($"Принтер '{printerName}' не доступен");
                
            var pdfBytes = _buffer.GetPdfBytes();
            if (pdfBytes is null || pdfBytes.Length == 0)
                throw new InvalidOperationException("Отсутствуют подготовленные данные для печати");
                
            using var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.Arguments = $"-d {printerName} -t \"TN_Doc\"";
            process.StartInfo.FileName = "lp";
            process.StartInfo.CreateNoWindow = true;
            
            if (!process.Start())
            {
                throw new InvalidOperationException("Не удалось запустить процесс lp для печати");
            }
            
            using (var stdin = process.StandardInput.BaseStream)
            {
                stdin.Write(pdfBytes, 0, pdfBytes.Length);
                stdin.Flush();
            }
            
            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"Ошибка печати через lp: {error}");
            }
        });
    }
}