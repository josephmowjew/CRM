using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.DTOs.Department;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public DepartmentsController(IDepartmentRepository departmentRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            this._departmentRepository = departmentRepository;
            this._mapper = mapper;
            this._unitOfWork = unitOfWork;
        }
        // GET: DepartmentController
        public ActionResult Index()
        {
            return View();
        }

        // GET: DepartmentController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DepartmentController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateDepartmentDTO createDepartmentDTO)
        {
            //check for model validity

            createDepartmentDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createDepartmentDTO.DataInvalid = "";


                //check for article title presence

                var mappedDepartment = this._mapper.Map<Department>(createDepartmentDTO);

                var departmentPresence = this._departmentRepository.Exists(mappedDepartment.Name);



                if (departmentPresence != null)
                {
                    createDepartmentDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createDepartmentDTO.Name), $"Another department exists with the parameters submitted'");

                    return PartialView("_CreateDepartmentPartial", createDepartmentDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedDepartment.CreatedById = claimsIdentitifier.Value;


                    this._departmentRepository.Add(mappedDepartment);

                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreateDepartmentPartial", createDepartmentDTO);
                }
                catch (DbUpdateException ex)
                {
                    createDepartmentDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateDepartmentPartial", createDepartmentDTO);
                }

                catch (Exception ex)
                {

                    createDepartmentDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateDepartmentPartial", createDepartmentDTO);
                }




            }



            return PartialView("_CreateDepartmentPartial", createDepartmentDTO);
        }

        // GET: DepartmentController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                Department? departmentRecordDb = await this._departmentRepository.GetDepartment(id);

                if (departmentRecordDb is not null)
                {
                    //map the record 

                    ReadDepartmentDTO mappedDepartmentRecord = this._mapper.Map<ReadDepartmentDTO>(departmentRecordDb);

                    return Json(mappedDepartmentRecord);

                }
                else
                {
                    return Json(new { status = "error", message = "record not found" });
                }
            }
            catch (Exception ex)
            {

                return Json(new { status = "error", message = ex.Message });
            }
        }

        // POST: DepartmentController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditDepartmentDTO editDepartmentDTO)
        {
            editDepartmentDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editDepartmentDTO.DataInvalid = "";
                //check if the role name isn't already taken

                var departmentDb = await this._departmentRepository.GetDepartment(id);

           

                var departmentPresentDb = this._departmentRepository.Exists(editDepartmentDTO.Name);



                bool isTaken = (departmentPresentDb != null);
                if (isTaken)
                {

                    editDepartmentDTO.DataInvalid = "true";
                    ModelState.AddModelError(nameof(editDepartmentDTO.Name), $"The department {editDepartmentDTO.Name} is already taken");


                    return PartialView("_EditDepartmentPartial", editDepartmentDTO);
                }



                this._mapper.Map(editDepartmentDTO, departmentDb);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "Department details updated successfully" });
            }



            return PartialView("_EditDepartmentPartial", editDepartmentDTO);
        }

       
        // POST: DepartmentController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var departmentDb = await this._departmentRepository.GetDepartment(id);

            if (departmentDb != null)
            {
                this._departmentRepository.Remove(departmentDb);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "department removed from the system successfully" });
            }

            return Json(new { status = "error", message = "department could not be found from the system" });
        }

        [HttpPost]
        public async Task<ActionResult> GetDepartments()
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

                List<Department>? departments = await this._departmentRepository.GetDepartments(CursorParameters);


                //get total records from the database
                resultTotal = await this._departmentRepository.TotalCountFiltered(CursorParameters);
                var result = departments;
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
