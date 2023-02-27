using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountTypesController : Controller
    {
        private readonly IAccountTypeRepository _accountTypeRepository;
        public AccountTypesController(IAccountTypeRepository accountTypeRepository)
        {
            _accountTypeRepository = accountTypeRepository;
        }

        // GET: AccountTypesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: AccountTypesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AccountTypesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AccountTypesController/Create
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

        // GET: AccountTypesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AccountTypesController/Edit/5
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

        // GET: AccountTypesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AccountTypesController/Delete/5
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

        [HttpPost]
        public async Task<ActionResult> GetAccountTypes()
        {

            try
            {
                //datatable stuff
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                var start = HttpContext.Request.Form["start"].FirstOrDefault();
                var length = HttpContext.Request.Form["length"].FirstOrDefault();

                var sortColumn = HttpContext.Request.Form["columns[" + HttpContext.Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnAscDesc = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int resultTotal = 0;

                //create a cursor params based on the data coming from the datatable
                CursorParams CursorParameters = new CursorParams() { SearchTerm = searchValue, Skip = skip, SortColum = sortColumn, SortDirection = sortColumnAscDesc, Take = pageSize };

                List<AccountType>? repoAccountTypes = await this._accountTypeRepository.GetAccountTypes(CursorParameters);


                //get total records from the database
                resultTotal = await this._accountTypeRepository.TotalCount();
                var result = repoAccountTypes;
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });
            }
            catch (Exception ex)
            {

                return Json(new { message = ex.Message });
            }




            //fetch all roles from the system



            //return Json(identityRolesList.ToList());

        }
    }
}
