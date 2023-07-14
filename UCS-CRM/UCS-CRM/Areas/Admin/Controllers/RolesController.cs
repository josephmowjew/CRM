using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class RolesController : Controller
    {
        private readonly IRoleRepositorycs _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        public RolesController(IRoleRepositorycs roleRepository, IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
        }

        // GET: RolesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: RolesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: RolesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RolesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Role role)
        {
            role.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                role.DataInvalid = "";
                //check if the role name isn't already taken

                bool isTaken = await _roleRepository.Exists(role.Name);
                if (isTaken)
                {
                    ModelState.AddModelError(nameof(role.Name), $"The Role {role.Name} is already taken");

                    return PartialView("_CreateRolePartial", role);
                }
                var identityRole = new Role();
                identityRole.Name = role.Name;
                identityRole.NormalizedName = role.Name;
                identityRole.Rating = role.Rating;

               _roleRepository.AddRole(identityRole);

                await _unitOfWork.SaveToDataStore();

                role.DataInvalid = "";

                return PartialView("_CreateRolePartial");
            }

            role.DataInvalid = "true";

            var errors = ModelState.Values.SelectMany(v => v.Errors);

            return PartialView("_CreateRolePartial", role);
        }

        // GET: RolesController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var identityRole = await _roleRepository.GetRoleAsync(id);

            return Json(identityRole);
        }

        // POST: RolesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id,Role role)
        {
            role.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                role.DataInvalid = "";
                //check if the role name isn't already taken

                bool isTaken = await _roleRepository.Exists(role.Name);

                if (isTaken)
                {
                    ModelState.AddModelError(nameof(role.Name), $"The Role {role.Name} is already taken");


                    return PartialView("_EditRolePartial", role);
                }
                var identityRole = await _roleRepository.GetRoleAsync(id);

                identityRole.Name = role.Name;
                identityRole.Rating = role.Rating;

                await _roleRepository.UpdateRoleAsync(identityRole);

                return Json(new { status = "success", message = "role details updated successfully" });

            }

            return PartialView("_EditRolePartial", role);
        }

        // GET: RolesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: RolesController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            var identityRole = await _roleRepository.GetRoleAsync(id);

            await _roleRepository.remove(id);

            return Json(new { response = "successfully deleted role" });
        }

        public async Task<ActionResult> GetRoles()
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

            resultTotal =  this._roleRepository.TotalCount();
            var result =   this._roleRepository.GetRoles(CursorParameters);
            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

        }
    }
}
