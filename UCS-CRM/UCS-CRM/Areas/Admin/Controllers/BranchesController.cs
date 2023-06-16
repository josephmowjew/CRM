using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.DTOs.Branch;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class BranchesController : Controller
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public BranchesController(IBranchRepository branchRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            this._branchRepository = branchRepository;
            this._mapper = mapper;
            this._unitOfWork = unitOfWork;
        }
        // GET: BranchController
        public ActionResult Index()
        {
            return View();
        }

        // GET: BranchController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: BranchController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateBranchDTO createBranchDTO)
        {
            //check for model validity

            createBranchDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createBranchDTO.DataInvalid = "";


                //check for article title presence

                var mappedBranch = this._mapper.Map<Branch>(createBranchDTO);

                var branchPresence = this._branchRepository.Exists(mappedBranch.Name);



                if (branchPresence != null)
                {
                    createBranchDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createBranchDTO.Name), $"Another branch exists with the parameters submitted'");

                    return PartialView("_CreateBranchPartial", createBranchDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedBranch.CreatedById = claimsIdentitifier.Value;


                    this._branchRepository.Add(mappedBranch);

                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreateBranchPartial", createBranchDTO);
                }
                catch (DbUpdateException ex)
                {
                    createBranchDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateBranchPartial", createBranchDTO);
                }

                catch (Exception ex)
                {

                    createBranchDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateBranchPartial", createBranchDTO);
                }




            }



            return PartialView("_CreateBranchPartial", createBranchDTO);
        }

        // GET: BranchController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                Branch? branchRecordDb = await this._branchRepository.GetBranch(id);

                if (branchRecordDb is not null)
                {
                    //map the record 

                    ReadBranchDTO mappedBranchRecord = this._mapper.Map<ReadBranchDTO>(branchRecordDb);

                    return Json(mappedBranchRecord);

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

        // POST: BranchController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditBranchDTO editBranchDTO)
        {
            editBranchDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editBranchDTO.DataInvalid = "";
                //check if the role name isn't already taken

                var branchDb = await this._branchRepository.GetBranch(id);

           

                var branchPresentDb = this._branchRepository.Exists(editBranchDTO.Name);



                bool isTaken = (branchPresentDb != null);
                if (isTaken)
                {

                    editBranchDTO.DataInvalid = "true";
                    ModelState.AddModelError(nameof(editBranchDTO.Name), $"The department {editBranchDTO.Name} is already taken");


                    return PartialView("_EditBranchPartial", editBranchDTO);
                }



                this._mapper.Map(editBranchDTO, branchDb);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "Branch details updated successfully" });
            }



            return PartialView("_EditBranchPartial", editBranchDTO);
        }

       
        // POST: BranchController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var branchDb = await this._branchRepository.GetBranch(id);

            if (branchDb != null)
            {
                this._branchRepository.Remove(branchDb);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "branch removed from the system successfully" });
            }

            return Json(new { status = "error", message = "branch could not be found from the system" });
        }

        [HttpPost]
        public async Task<ActionResult> GetBranches()
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

                List<Branch>? branches = await this._branchRepository.GetBranches(CursorParameters);


                //get total records from the database
                resultTotal = await this._branchRepository.TotalCountFiltered(CursorParameters);
                var result = branches;
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });
            }
            catch (Exception ex)
            {

                return Json(new { message = ex.Message });
            }


        }
    }


}
