using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.DTOs.State;
using UCS_CRM.Core.DTOs.TicketCategory;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.ictofficer.Controllers
{
    [Area("ictofficer")]
    [Authorize]
    public class StatesController : Controller
    {
        private readonly IStateRepository _stateRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StatesController(IStateRepository stateRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _stateRepository = stateRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


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
        public async Task<ActionResult> Create(CreateStateDTO createStateDTO)
        {
            //check for model validity


            if (ModelState.IsValid)
            {

                createStateDTO.DataInvalid = "";


                //check for article title presence

                var mappedState = this._mapper.Map<State>(createStateDTO);

                var statePresence = this._stateRepository.Exists(mappedState.Name);
                if (statePresence != null)
                {
                    createStateDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createStateDTO.Name), $"state exists with the name submitted'");

                    return PartialView("_CreateStatePartial", createStateDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedState.CreatedById = claimsIdentitifier.Value;


                    this._stateRepository.Add(mappedState);
                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreateStatePartial", createStateDTO);
                }
                catch (DbUpdateException ex)
                {
                    createStateDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateStatePartial", createStateDTO);
                }

                catch (Exception ex)
                {
                    createStateDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateStatePartial", createStateDTO);
                }




            }



            return PartialView("_CreateStatePartial", createStateDTO);
        }

        // GET: StatesController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                State? stateDbRecord = await this._stateRepository.GetStateAsync(id);

                if (stateDbRecord is not null)
                {
                    //map the record 

                    ReadStateDTO mappedState = this._mapper.Map<ReadStateDTO>(stateDbRecord);

                    return Json(mappedState);

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

        // POST: StatesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditStateDTO editStateDTO)
        {
            editStateDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editStateDTO.DataInvalid = "";

                var ticketCategoryDB = await this._stateRepository.GetStateAsync(id);

                if (ticketCategoryDB is null)
                {
                    editStateDTO.DataInvalid = "true";

                    ModelState.AddModelError("", $"The Identifier of the record was not found taken");

                    return PartialView("_EditStatePartial", editStateDTO);
                }
                //check if the role name isn't already taken

                var stateExist = this._stateRepository.Exists(editStateDTO.Name);



                bool isTaken = (stateExist != null);
                if (isTaken)
                {

                    editStateDTO.DataInvalid = "true";
                    ModelState.AddModelError(nameof(editStateDTO.Name), $"The state {editStateDTO.Name} is already taken");


                    return PartialView("_EditStatePartial", editStateDTO);
                }



                this._mapper.Map(editStateDTO, ticketCategoryDB);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "state details updated successfully" });
            }



            return PartialView("_EditStatePartial", editStateDTO);
        }

      
        // POST: StatesController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var stateDbRecord = await this._stateRepository.GetStateAsync(id);

            if (stateDbRecord != null)
            {
                this._stateRepository.Remove(stateDbRecord);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "state has been removed from the system successfully" });
            }

            return Json(new { status = "error", message = "state could not be found from the system" });
        }
        [HttpPost]
        public async Task<ActionResult> GetStates()
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

            resultTotal = await this._stateRepository.TotalActiveCount();
            var result = await this._stateRepository.GetStates(CursorParameters);
            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });
        }
    }
}
