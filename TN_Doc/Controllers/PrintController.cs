using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TN_Doc.Controllers
{
    public class PrintController : Controller
    {
        private readonly ILogger<PrintController> _logger;

        public List<string> GetListPrinters()
        {
            //var installedPrinters = PrinterSettings.InstalledPrinters;

            List<string> printers = new List<string>();

            var process = new Process()
            {
                StartInfo =
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = "-e",
                    FileName = "lpstat",
                    CreateNoWindow = true
                }
            };
            try
            {
                process.Start();
                process.WaitForExit();

                var resultOutput = process.StandardOutput.ReadToEnd();

                foreach (var item in resultOutput.Split('\n'))
                    if (!string.IsNullOrEmpty(item))
                        printers.Add(item);

                return printers;
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        public void PrintDoc(string printerName)
        {
            //Process process = new Process();
            //// Configure the process using the StartInfo properties.
            //process.StartInfo.FileName = "lpr";
            //process.StartInfo.Arguments = "-H localhost -P  RICOH_P_C200W_I -ol /opt/TN_Doc/wwwroot/PDF/PDF.pdf";
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //process.Start();

            var printersName = GetListPrinters();

            Console.WriteLine($"-H localhost -P  {printerName} -ol {Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot", "PDF", $"PDF.pdf")}");

            if (printersName.Contains(printerName))
            {
                var process = new Process()
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments
                            = $"-H localhost -P  {printerName} -ol {Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot", "PDF", $"PDF.pdf")}",
                        FileName = "lpr",
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();

                //_logger.LogInformation(process.StandardOutput.ReadToEnd());
                // _logger.LogInformation(process.StandardError.ReadToEnd());
            }
        }
    }
}