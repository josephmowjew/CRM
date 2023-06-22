using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.DTOs.Department;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.ViewModel;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public DepartmentsController(IDepartmentRepository departmentRepository, IMapper mapper, IUnitOfWork unitOfWork, IPositionRepository positionRepository)
        {
            this._departmentRepository = departmentRepository;
            this._mapper = mapper;
            this._unitOfWork = unitOfWork;
            this._positionRepository = positionRepository;

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
     
        public async Task<ActionResult> DeletePositionOnDepartment(int positionId, int departmentId)
        {
            var departmentDb = await this._departmentRepository.GetDepartment(departmentId);

            if (departmentDb != null)
            {
                var position = await this._positionRepository.GetPosition(positionId);
                if(position != null)
                {
                    departmentDb.Positions.Remove(position);
                    await this._unitOfWork.SaveToDataStore();
                }

                return Json(new { status = "success", message = "position removed from the department successfully" });
            }

            return Json(new { status = "error", message = "position could not be removed from the department" });
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
        [HttpGet]
        public async Task<ActionResult> ViewDepartmentPositions([FromRoute]int id)
        {
            Department? departmentDb = new();

            var positions = await this.GetPositions(id);


            if (id != 0)
            {
                departmentDb = await this._departmentRepository.GetDepartment(id);
                //set department view bag when the department has been found in the system

                if(departmentDb != null)
                {
                    ViewBag.department = departmentDb;

                    ViewBag.positionsList = positions;
                  
                }

            }

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPositionDepartment(DepartmentPositionViewModel viewModel)
        {
            //check for model validity

            viewModel.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                viewModel.DataInvalid = "";


                var departmentDb = await this._departmentRepository.GetDepartment(viewModel.DepartmentId);

                if(departmentDb != null )
                {
                    var position = await this._positionRepository.GetPosition(viewModel.PositionId);

                    if(position != null)
                    {
                        departmentDb.Positions.Add(position);

                        await _unitOfWork.SaveToDataStore();
                    }
                    else
                    {
                        viewModel.DataInvalid = "true";
                    }
                }

                ViewBag.positionsList = await this.GetPositions(viewModel.DepartmentId);
                ViewBag.department = departmentDb;


            }

           

            return PartialView("_AddPositionToDepartmentPartial", viewModel);
        }

        [HttpPost]
        public async Task<ActionResult> GetDepartmentPositions([FromRoute]int id)
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

                //create a cursor params based on the data coming from the data-table

                Department? departmentDb = new();

                List<Position> positions = new();

                if (id != 0)
                {
                    departmentDb = await this._departmentRepository.GetDepartment(id);
                    //set department view bag when the department has been found in the system

                    if (departmentDb != null)
                    {
                        positions = departmentDb.Positions;

                        //sort the positions by rating if variable is not null

                        if (positions != null)
                        {
                            positions = positions.OrderByDescending(p => p.Rating).ToList();
                        }

                        
                    }

                }


                //get total records from the database
                resultTotal = positions.Count;
                var result = positions;
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });
            }
            catch (Exception ex)
            {

                return Json(new { message = ex.Message });
            }


        }

        private async Task<List<SelectListItem>> GetPositions(int departmentId)
        {
            List<SelectListItem> positionsList = new() { new SelectListItem() { Text = "Select Position", Value = "" } };

            var departmentDb = await this._departmentRepository.GetDepartment(departmentId);

            List<Position> freshPositions = new();

            List<Position> departmentPositions = new();

            if (departmentDb != null)
            {
                departmentPositions = departmentDb.Positions;
            }

            var positions = await this._positionRepository.GetPositions();

            if(positions != null && positions.Count > 0)
            {
                freshPositions = positions.Except(departmentPositions).ToList();
            }

            if(freshPositions != null && freshPositions.Count > 0)
            {
                freshPositions.ForEach(position =>
                {
                    positionsList.Add(new SelectListItem() { Text = position.Name, Value = position.Id.ToString() });   
                });
            }


            return positionsList;

        }


    }


}
