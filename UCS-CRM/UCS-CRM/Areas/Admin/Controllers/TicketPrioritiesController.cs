using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using UCS_CRM.Core.DTOs.TicketPriority;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class TicketPrioritiesController : Controller
    {
        private readonly ITicketPriorityRepository _ticketPriorityRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public TicketPrioritiesController(ITicketPriorityRepository ticketPriorityRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            this._ticketPriorityRepository = ticketPriorityRepository;
            this._mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // GET: TicketPrioritiesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: TicketPrioritiesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: TicketPrioritiesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TicketPrioritiesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTicketPriorityDTO createTicketPriorityDTO)
        {
            //check for model validity

            createTicketPriorityDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createTicketPriorityDTO.DataInvalid = "";


                //check for article title presence

                var mappedTicketPriority = this._mapper.Map<TicketPriority>(createTicketPriorityDTO);

                var ticketPriorityPresence = this._ticketPriorityRepository.Exists(mappedTicketPriority.Name);



                if (ticketPriorityPresence != null)
                {
                    createTicketPriorityDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createTicketPriorityDTO.Name), $"Another account type exists with the parameters submitted'");

                    return PartialView("_CreateTicketPriorityPartial", createTicketPriorityDTO);
                }


                //save to the database

                try
                {
                    //comment out this code
                    mappedTicketPriority.CreatedById = "1c9d8003-91b9-4eab-96a6-0bc90edd349b";

                    this._ticketPriorityRepository.Add(mappedTicketPriority);
                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreateTicketPriorityPartial", createTicketPriorityDTO);
                }
                catch (DbUpdateException ex)
                {
                    createTicketPriorityDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateTicketPriorityPartial", createTicketPriorityDTO);
                }

                catch (Exception ex)
                {

                    createTicketPriorityDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateTicketPriorityPartial", createTicketPriorityDTO);
                }




            }



            return PartialView("_CreateTicketPriorityPartial", createTicketPriorityDTO);
        }

        // GET: TicketPrioritiesController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  account type record with the id sent

            try
            {
                TicketPriority? ticketPriorityDbRecord = await this._ticketPriorityRepository.GetTicketPriority(id);

                if (ticketPriorityDbRecord is not null)
                {
                    //map the record 

                    ReadTicketPriorityDTO mappedAccountRecord = this._mapper.Map<ReadTicketPriorityDTO>(ticketPriorityDbRecord);

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

        // POST: TicketPrioritiesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditTicketPriorityDTO editTicketPriorityDTO)
        {
            editTicketPriorityDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editTicketPriorityDTO.DataInvalid = "";
                //check if the role name isn't already taken

                var ticketPriorityDB = this._ticketPriorityRepository.Exists(editTicketPriorityDTO.Name);



                bool isTaken = (ticketPriorityDB != null);

                if (isTaken)
                {

                    editTicketPriorityDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(editTicketPriorityDTO.Name), $"The Account Type  {editTicketPriorityDTO.Name} is already taken");


                    return PartialView("_EditTicketPriorityPartial", editTicketPriorityDTO);
                }



                this._mapper.Map(editTicketPriorityDTO, ticketPriorityDB);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(ticketPriorityDB);

            }



            return PartialView("_EditTicketPriorityPartial", editTicketPriorityDTO);

        }

        // GET: TicketPrioritiesController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var ticketPriorityDb = await this._ticketPriorityRepository.GetTicketPriority(id);

            if (ticketPriorityDb != null)
            {
                this._ticketPriorityRepository.Remove(ticketPriorityDb);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "account type removed from the system successfully" });
            }

            return Json(new { status = "error", message = "account type could not be found from the system" });
        }



        [HttpPost]
        public async Task<ActionResult> GetTicketPriorities()
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

                List<TicketPriority>? repoTicketPriorities = await this._ticketPriorityRepository.GetTicketPriorities(CursorParameters);


                //get total records from the database
                resultTotal = await this._ticketPriorityRepository.TotalCount();
                var result = repoTicketPriorities;
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
