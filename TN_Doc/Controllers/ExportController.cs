using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace TN_Doc.Controllers
{
    public class ExportController : Controller
    {
        private readonly ILogger<PrintController> _logger;

        public List<string> GetListFormats()
        {
            return new List<string>() { "PDF", ""};
        }

        public void ExportDoc(string format)
        {

        }
    }
}
