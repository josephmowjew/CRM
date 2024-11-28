using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.DTOs.Position;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.ictofficer.Controllers
{
    [Area("ictofficer")]
    [Authorize]
    public class PositionsController : Controller
    {
        private readonly IPositionRepository _positionsRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public PositionsController(IPositionRepository positionRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            this._positionsRepository = positionRepository;
            this._mapper = mapper;
            this._unitOfWork = unitOfWork;
        }
        // GET: PositionController
        public ActionResult Index()
        {
            return View();
        }

        // GET: PositionController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PositionController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreatePositionDTO createPositionDTO)
        {
            //check for model validity

            createPositionDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createPositionDTO.DataInvalid = "";


                //check for article title presence

                var mappedPosition = this._mapper.Map<Position>(createPositionDTO);

                var positionPresence = this._positionsRepository.Exists(mappedPosition.Id, mappedPosition.Name,mappedPosition.Rating);



                if (positionPresence != null)
                {
                    createPositionDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createPositionDTO.Name), $"Another position exists with the parameters submitted'");

                    return PartialView("_CreatePositionPartial", createPositionDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedPosition.CreatedById = claimsIdentitifier.Value;


                    this._positionsRepository.Add(mappedPosition);

                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreatePositionPartial", createPositionDTO);
                }
                catch (DbUpdateException ex)
                {
                    createPositionDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreatePositionPartial", createPositionDTO);
                }

                catch (Exception ex)
                {

                    createPositionDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreatePositionPartial", createPositionDTO);
                }




            }



            return PartialView("_CreatePositionPartial", createPositionDTO);
        }

        // GET: PositionController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                Position? positionRecordDb = await this._positionsRepository.GetPosition(id);

                if (positionRecordDb is not null)
                {
                    //map the record 

                    ReadPositionDTO mappedPositionRecord = this._mapper.Map<ReadPositionDTO>(positionRecordDb);

                    return Json(mappedPositionRecord);

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

        // POST: PositionController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditPositionDTO editPositionDTO)
        {
            editPositionDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editPositionDTO.DataInvalid = "";
                //check if the role name isn't already taken

                var departmentDb = await this._positionsRepository.GetPosition(id);

           

                var departmentPresentDb = this._positionsRepository.Exists(editPositionDTO.Id, editPositionDTO.Name, editPositionDTO.Rating);



                bool isTaken = (departmentPresentDb != null);
                if (isTaken)
                {

                    editPositionDTO.DataInvalid = "true";
                    ModelState.AddModelError(nameof(editPositionDTO.Name), $"The position {editPositionDTO.Name} is already taken");


                    return PartialView("_EditPositionPartial", editPositionDTO);
                }



                this._mapper.Map(editPositionDTO, departmentDb);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "Position details updated successfully" });
            }



            return PartialView("_EditPositionPartial", editPositionDTO);
        }

       
        // POST: PositionController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var departmentDb = await this._positionsRepository.GetPosition(id);

            if (departmentDb != null)
            {
                this._positionsRepository.Remove(departmentDb);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "position removed from the system successfully" });
            }

            return Json(new { status = "error", message = "position could not be found from the system" });
        }

        [HttpPost]
        public async Task<ActionResult> GetPositions()
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

                List<Position>? positions = await this._positionsRepository.GetPositions(CursorParameters);


                //get total records from the database
                resultTotal = await this._positionsRepository.TotalCountFiltered(CursorParameters);
                var result = positions;
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
