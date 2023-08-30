using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TN_Doc.Controllers
{
    public class ElisController : Controller
    {
        // GET: ElisController
        public ActionResult Index()
        {
            return View();
        }

        // GET: ElisController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ElisController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ElisController/Create
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

        // GET: ElisController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ElisController/Edit/5
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

        // GET: ElisController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ElisController/Delete/5
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
