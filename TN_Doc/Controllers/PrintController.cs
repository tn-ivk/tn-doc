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
                    CreateNoWindow = true,
                    
                }
            };
            process.Start();
            process.WaitForExit();
            var resultOutput = process.StandardOutput.ReadToEnd();
            foreach (var item in resultOutput.Split('\n'))
                if (!string.IsNullOrEmpty(item))
                    printers.Add(item);
            
            return printers;
        }

        public void PrintDoc(string printerName)
        {
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
            }
            
            /// printer service
            /// printerHelper________
            /// |                    |
            /// WindowsPrinterHelper |
            ///                      LinuxPrinteHelper 
            
        }
    }
}