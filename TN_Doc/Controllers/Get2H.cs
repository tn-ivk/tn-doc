using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TN_Doc.Controllers
{
    public class Get2H : Controller
    {
        // GET: Get2H
        public ActionResult Index()
        {

            //FastReport generatePDF("c:\WEBSERVER\ReportsCache\pdf.pdf");
            //PDFName = "http://server/ReportsCache/pdf.pdf";
            //return View(PDFName);

            return View();
        }

        // GET: Get2H/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Get2H/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Get2H/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
            
        }

        // GET: Get2H/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Get2H/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Get2H/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Get2H/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
