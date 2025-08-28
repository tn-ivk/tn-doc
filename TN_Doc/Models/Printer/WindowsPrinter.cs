extern alias SystemDrawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrinterSettings = SystemDrawing::System.Drawing.Printing.PrinterSettings;
using TN_Doc.Models.Services;

namespace TN_Doc.Models.Printer;

/// <summary>
/// Вспомогательный класс для работы с принтером в ОС Windows
/// </summary>
public sealed class WindowsPrinter : AbsPrinter
{
    private readonly ILogger<WindowsPrinter> _logger;
    private readonly IReportBuffer _buffer;
    
    public WindowsPrinter(ILogger<WindowsPrinter> logger, IReportBuffer buffer)
    {
        _logger = logger;
        _buffer = buffer;
    }
    
    /// <summary>
    /// Получение списка доступных принтеров в системе
    /// </summary>
    /// <returns>Список доступных принтеров в системе</returns>
    [SupportedOSPlatform("windows")]
    public override IEnumerable<string> GetAvailablePrinters()
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Получение списка принтеров поддерживается только в Windows");
            return [];
        }

        try
        {
            var printers = PrinterSettings.InstalledPrinters.Cast<object>().Cast<string>();
            return printers;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка получения списка доступных принтеров в системе: {ex.Message}");
            return [];
        }
    }
    
    /// <summary>
    /// Печать документа на заданном принтере
    /// </summary>
    /// <param name="printerName">Наименование принтера</param>
    /// <returns>Задача на печать документа pdf</returns>
    [SupportedOSPlatform("windows")]
    public override Task PrintDocAsync(string printerName)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Печать поддерживается только в Windows");
            return Task.FromException(new PlatformNotSupportedException("Печать поддерживается только в Windows"));
        }

        return Task.Run(async() =>
        {
                var printersName = GetAvailablePrinters();
                if (!printersName.Contains(printerName))
                    throw new InvalidOperationException($"Принтер '{printerName}' не доступен");

                var pdfBytes = _buffer.GetPdfBytes();
                if (pdfBytes == null || pdfBytes.Length == 0)
                    throw new InvalidOperationException("Отсутствуют подготовленные данные для печати");

                var tempPath = Path.Combine(Path.GetTempPath(), $"TN_Doc_{Guid.NewGuid()}.pdf");
                await File.WriteAllBytesAsync(tempPath, pdfBytes);

                try
                {
                    var curDir = Directory.GetCurrentDirectory();
                    using var process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.Arguments = $"-p \"{printerName}\" -f \"{tempPath}\"";
                    process.StartInfo.FileName = Path.Combine(curDir, "prutils", "winprutil.exe");
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    await Task.Delay(2000);
                    await process.WaitForExitAsync();
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { }
                }
        });
    }
}