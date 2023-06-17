using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using TN.DocData;

namespace TN_Doc.Controllers
{
    public class ExportController : Controller
    {
        private readonly ILogger<PrintController> _logger;

        public List<string> GetListFormats()
        {
            return new List<string>() { "pdf", "excel", "ods", "xml" };
        }

        public void ExportDoc(int IdDevice, IdDoc IdDoc, int id, string format)
        {
            //FileInfo fileInfo = new FileInfo(format);
            //File.Copy("", "", true);
            System.IO.File.Copy(Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot", "PDF", $"PDF.pdf"), Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot", "PDF", $"PDF2.pdf"), false);
        }
    }
}
