using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Channels;
using UCS_CRM.Core.DTOs.TicketCategory;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class TicketCategoriesController : Controller
    {
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public TicketCategoriesController(ITicketCategoryRepository ticketCategoryRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            this._ticketCategoryRepository = ticketCategoryRepository;
            this._mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // GET: TicketCategoriesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: TicketCategoriesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: TicketCategoriesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TicketCategoriesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTicketCategoryDTO createAcccountTypeDTO)
        {
            //check for model validity

            createAcccountTypeDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createAcccountTypeDTO.DataInvalid = "";


                //check for article title presence

                var mappedTicketCategory = this._mapper.Map<TicketCategory>(createAcccountTypeDTO);

                var ticketCategoryPresence = this._ticketCategoryRepository.Exists(mappedTicketCategory.Name,mappedTicketCategory.Id);



                if (ticketCategoryPresence != null)
                {
                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createAcccountTypeDTO.Name), $"Another account type exists with the parameters submitted'");

                    return PartialView("_CreateTicketCategoryPartial", createAcccountTypeDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedTicketCategory.CreatedById = claimsIdentitifier.Value;
                  

                    this._ticketCategoryRepository.Add(mappedTicketCategory);
                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreateTicketCategoryPartial", createAcccountTypeDTO);
                }
                catch (DbUpdateException ex)
                {
                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateTicketCategoryPartial", createAcccountTypeDTO);
                }

                catch (Exception ex)
                {

                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateTicketCategoryPartial", createAcccountTypeDTO);
                }




            }



            return PartialView("_CreateTicketCategoryPartial", createAcccountTypeDTO);
        }

        // GET: TicketCategoriesController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                TicketCategory? ticketCategoryDbRecord = await this._ticketCategoryRepository.GetTicketCategory(id);

                if (ticketCategoryDbRecord is not null)
                {
                    //map the record 

                    ReadTicketCategoryDTO mappedAccountRecord = this._mapper.Map<ReadTicketCategoryDTO>(ticketCategoryDbRecord);

                    return Json(mappedAccountRecord);

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



            return View();
        }

        // POST: TicketCategoriesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditTicketCategoryDTO editTicketCategoryDTO)
        {
            editTicketCategoryDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editTicketCategoryDTO.DataInvalid = "";

                var ticketCategoryDB = await this._ticketCategoryRepository.GetTicketCategory(id);

                if(ticketCategoryDB is null)
                {
                    editTicketCategoryDTO.DataInvalid = "true";

                    ModelState.AddModelError("", $"The Identifier of the record was not found taken");

                    return PartialView("_EditTicketCategoryPartial", editTicketCategoryDTO);
                }
                //check if the role name isn't already taken

                var ticketCategoryExist = this._ticketCategoryRepository.Exists(editTicketCategoryDTO.Name,id);



                bool isTaken = (ticketCategoryExist != null);

                if (isTaken)
                {

                    editTicketCategoryDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(editTicketCategoryDTO.Name), $"The Account Type  {editTicketCategoryDTO.Name} is already taken");


                    return PartialView("_EditTicketCategoryPartial", editTicketCategoryDTO);
                }



                this._mapper.Map(editTicketCategoryDTO, ticketCategoryDB);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(ticketCategoryDB);

            }



            return PartialView("_EditTicketCategoryPartial", editTicketCategoryDTO);

        }

        // GET: TicketCategoriesController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var ticketCategoryDb = await this._ticketCategoryRepository.GetTicketCategory(id);

            if (ticketCategoryDb != null)
            {
                this._ticketCategoryRepository.Remove(ticketCategoryDb);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "account type removed from the system successfully" });
            }

            return Json(new { status = "error", message = "account type could not be found from the system" });
        }



        [HttpPost]
        public async Task<ActionResult> GetTicketCategories()
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

                List<TicketCategory>? repoTicketCategories = await this._ticketCategoryRepository.GetTicketCategories(CursorParameters);


                //get total records from the database
                resultTotal = await this._ticketCategoryRepository.TotalCount();
                var result = repoTicketCategories;
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
