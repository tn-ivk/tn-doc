using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using TN_DocGeneral.Services;

namespace TN_Doc.Controllers;

/// <summary>
/// Отдаёт последний сгенерированный PDF из памяти по пути /PDF/PDF.pdf.
/// Служит заменой статического файла, исключая запись на диск.
/// </summary>
public class PdfController : Controller
{
    private readonly IReportBuffer _buffer;

    public PdfController(IReportBuffer buffer)
    {
        _buffer = buffer;
    }

    [HttpGet("/PDF/PDF.pdf")]
    public IActionResult Get()
    {
        var bytes = _buffer.GetPdfBytes();
        if (bytes is null || bytes.Length == 0)
            return NotFound();

        Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
        Response.Headers[HeaderNames.Pragma] = "no-cache";
        Response.Headers[HeaderNames.Expires] = "0";
        return File(bytes, "application/pdf");
    }
}


