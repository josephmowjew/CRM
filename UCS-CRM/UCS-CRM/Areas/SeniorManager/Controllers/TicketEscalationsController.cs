using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.Ticket;
using UCS_CRM.Core.DTOs.TicketComment;
using UCS_CRM.Core.DTOs.TicketEscalation;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.SeniorManager.Controllers
{
    [Area("SeniorManager")]
    [Authorize]
    public class TicketEscalationsController : Controller
    {
        private readonly ITicketEscalationRepository _ticketEscalationRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITicketCommentRepository _ticketCommentRepository;

        public TicketEscalationsController(ITicketEscalationRepository ticketEscalationRepository, IMapper mapper, IUnitOfWork unitOfWork, ITicketRepository ticketRepository,
            ITicketCommentRepository ticketCommentRepository)
        {
            this._ticketEscalationRepository = ticketEscalationRepository;
            this._mapper = mapper;
            _unitOfWork = unitOfWork;
            _ticketRepository = ticketRepository;
            this._ticketCommentRepository = ticketCommentRepository;
        }

        // GET: TicketEscalationsController/Details/5
        public ActionResult Second()
        {
            return View();
        }

        // GET: TicketEscalationsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // GET: TicketController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var esca = await _ticketEscalationRepository.GetTicketEscalation(id);

            if (esca == null)
            {
                return RedirectToAction("First");
            }
            var ticketDB = await this._ticketRepository.GetTicket(esca.TicketId);


            if (ticketDB == null)
            {
                return RedirectToAction("Second");
            }


            var mappedTicket = this._mapper.Map<ReadTicketDTO>(ticketDB);

            return View(mappedTicket);
        }
        // POST: TicketEscalationsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTicketEscalationDTO createAcccountTypeDTO)
        {
            //check for model validity

            createAcccountTypeDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createAcccountTypeDTO.DataInvalid = "";


                //check for article title presence

                var mappedTicketEscalation = this._mapper.Map<TicketEscalation>(createAcccountTypeDTO);

                var ticketEscalationPresence = this._ticketEscalationRepository.Exists(mappedTicketEscalation);



                if (ticketEscalationPresence != null)
                {
                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(mappedTicketEscalation.Ticket.Title), $"Another ticket exists with the parameters submitted'");

                    return PartialView("_CreateTicketEscalationPartial", createAcccountTypeDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedTicketEscalation.CreatedById = claimsIdentitifier.Value;


                    this._ticketEscalationRepository.Add(mappedTicketEscalation);
                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_CreateTicketEscalationPartial", createAcccountTypeDTO);
                }
                catch (DbUpdateException ex)
                {
                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_CreateTicketEscalationPartial", createAcccountTypeDTO);
                }

                catch (Exception ex)
                {

                    createAcccountTypeDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_CreateTicketEscalationPartial", createAcccountTypeDTO);
                }




            }



            return PartialView("_CreateTicketEscalationPartial", createAcccountTypeDTO);
        }

        // GET: TicketEscalationsController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //get  ticket record with the id sent

            try
            {
                TicketEscalation? ticketEscalationDbRecord = await this._ticketEscalationRepository.GetTicketEscalation(id);

                if (ticketEscalationDbRecord is not null)
                {
                    //map the record 

                    ReadTicketEscalationDTO mappedAccountRecord = this._mapper.Map<ReadTicketEscalationDTO>(ticketEscalationDbRecord);

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



        }

        // POST: TicketEscalationsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, UpdateTicketEscalationDTO editTicketEscalationDTO)
        {
            editTicketEscalationDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editTicketEscalationDTO.DataInvalid = "";

                var ticketEscalationDB = await this._ticketEscalationRepository.GetTicketEscalation(id);

                if (ticketEscalationDB is null)
                {
                    editTicketEscalationDTO.DataInvalid = "true";

                    ModelState.AddModelError("", $"The Identifier of the record was not found taken");

                    return PartialView("_EditTicketEscalationPartial", editTicketEscalationDTO);
                }
       
                this._mapper.Map(editTicketEscalationDTO, ticketEscalationDB);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "ticket category details updated successfully" });

            }



            return PartialView("_EditTicketEscalationPartial", editTicketEscalationDTO);

        }

        // GET: TicketEscalationsController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var ticketEscalationDb = await this._ticketEscalationRepository.GetTicketEscalation(id);

            if (ticketEscalationDb != null)
            {
                this._ticketEscalationRepository.Remove(ticketEscalationDb);

                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "ticket removed from the system successfully" });
            }

            return Json(new { status = "error", message = "ticket could not be found from the system" });
        }

        public async Task<ActionResult> MarkDone(int id)
        {
            //check if the role name isn't already taken

            var ticketEscalationDb = await this._ticketEscalationRepository.GetTicketEscalation(id);

            if (ticketEscalationDb != null)
            {
                //this._ticketEscalationRepository.Remove(ticketEscalationDb);

                ticketEscalationDb.Resolved = true;
                await this._unitOfWork.SaveToDataStore();


                //de-escalate ticket

                ticketEscalationDb.Ticket.AssignedToId = ticketEscalationDb.CreatedById;

                await this._ticketRepository.SendTicketDeEscalationEmail(ticketEscalationDb.Ticket, ticketEscalationDb.EscalatedTo.Email);


                await this._unitOfWork.SaveToDataStore();

                return Json(new { status = "success", message = "ticket has been marked done successfully" });
            }

            return Json(new { status = "error", message = "ticket could not be found from the system" });
        }



        [HttpPost]
        public async Task<ActionResult> GetTicketEscalations(string roleName)
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

                List<TicketEscalation>? repoTicketEscalations = await this._ticketEscalationRepository.GetTicketEscalations(null, CursorParameters);


                //get total records from the database
                resultTotal = await this._ticketEscalationRepository.GetTicketEscalationsCount(null, CursorParameters);
                var result = repoTicketEscalations;
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });
            }
            catch (Exception ex)
            {

                return Json(new { message = ex.Message });
            }




            //fetch all roles from the system



            //return Json(identityRolesList.ToList());

        }

        public async Task<ActionResult> GetTicketComments(string ticketId)
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

            resultTotal = await this._ticketCommentRepository.TotalActiveCount(int.Parse(ticketId));
            var result = await this._ticketCommentRepository.GetTicketCommentsAsync(int.Parse(ticketId), CursorParameters);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketCommentDTO>>(result);



            return Json(new { draw = draw, recordsFiltered = result.Count, recordsTotal = resultTotal, data = mappedResult });

        }
    }
}
