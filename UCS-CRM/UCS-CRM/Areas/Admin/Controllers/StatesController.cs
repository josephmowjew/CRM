using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UCS_CRM.Areas.Admin.Controllers
{
    public class StatesController : Controller
    {
        // GET: StatesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: StatesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: StatesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: StatesController/Create
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

        // GET: StatesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: StatesController/Edit/5
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

        // GET: StatesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: StatesController/Delete/5
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
