using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.Ticket;
using UCS_CRM.Core.DTOs.TicketEscalation;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Areas.Clerk.Controllers
{
    [Area("Clerk")]
    [Authorize]
    public class TicketEscalationsController : Controller
    {
        private readonly ITicketEscalationRepository _ticketEscalationRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private IWebHostEnvironment _env;
        private readonly IStateRepository _stateRepository;
        private readonly ITicketPriorityRepository _priorityRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        public TicketEscalationsController(ITicketEscalationRepository ticketEscalationRepository, IMapper mapper, IUnitOfWork unitOfWork, ITicketRepository ticketRepository, 
            IWebHostEnvironment env, IStateRepository stateRepository, ITicketPriorityRepository priorityRepository, IMemberRepository memberRepository, IUserRepository userRepository, 
            ITicketCategoryRepository ticketCategoryRepository)
        {
            this._ticketEscalationRepository = ticketEscalationRepository;
            this._mapper = mapper;
            _unitOfWork = unitOfWork;
            _ticketRepository = ticketRepository;
            _env = env;
            _stateRepository = stateRepository;
            _priorityRepository = priorityRepository;
            _memberRepository = memberRepository;
            _userRepository = userRepository;
            _ticketCategoryRepository = ticketCategoryRepository;
        }

        // GET: TicketEscalationsController
        public async Task<ActionResult> Index()
        {
            //await populateViewBags();
            return View();
        }

        // GET: TicketEscalationsController/Details/5
    

        // GET: TicketEscalationsController/Create
        // GET: TicketController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var ticketDB = await this._ticketRepository.GetTicket(id);

            if (ticketDB == null)
            {
                return RedirectToAction("Index");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditTicket(int id, EditManagerTicketDTO editTicketDTO)
        {
            editTicketDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                editTicketDTO.DataInvalid = "";

                var ticketDB = await this._ticketRepository.GetTicket(id);

                if (ticketDB is null)
                {
                    editTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError("", $"The Identifier of the record was not found taken");

                    return PartialView("_EditTicketPartial", editTicketDTO);
                }


                editTicketDTO.StateId = editTicketDTO.StateId == null ? ticketDB.StateId : editTicketDTO.StateId;

                editTicketDTO.TicketNumber = ticketDB.TicketNumber;
                //check if the role name isn't already taken
                var mappedTicket = this._mapper.Map<Ticket>(editTicketDTO);

                this._mapper.Map(editTicketDTO, ticketDB);

                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                if (editTicketDTO.Attachments.Count > 0)
                {
                    var attachments = editTicketDTO.Attachments.Select(async attachment =>
                    {
                        string fileUrl = await Lambda.UploadFile(attachment, this._env.WebRootPath);
                        return new TicketAttachment()
                        {
                            FileName = attachment.FileName,
                            TicketId = mappedTicket.Id,
                            Url = fileUrl
                        };
                    });

                    var mappedAttachments = await Task.WhenAll(attachments);

                    mappedTicket.TicketAttachments.AddRange(mappedAttachments);

                    await this._unitOfWork.SaveToDataStore();
                }

                return Json(new { status = "success", message = "user ticket updated successfully" });
            }



            return PartialView("_EditTicketPartial", editTicketDTO);
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



        [HttpPost]
        public async Task<ActionResult> GetTicketEscalations(int escalationLevel)
        {

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

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

                List<TicketEscalation>? repoTicketEscalations = await this._ticketEscalationRepository.GetTicketEscalationsForUser(CursorParameters, claimsIdentitifier.Value);


                //get total records from the database
                resultTotal = await this._ticketEscalationRepository.TotalCountForUser(claimsIdentitifier.Value);
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


        private async Task<List<SelectListItem>> GetTicketCategories()
        {
            var ticketCategories = await this._ticketCategoryRepository.GetTicketCategories();

            var ticketCategoriesList = new List<SelectListItem>();

            ticketCategoriesList.Add(new SelectListItem() { Text = "------ Select Category ------", Value = "" });

            ticketCategories.ForEach(category =>
            {
                ticketCategoriesList.Add(new SelectListItem() { Text = category.Name, Value = category.Id.ToString() });
            });

            return ticketCategoriesList;

        }

        private async Task<List<SelectListItem>> GetTicketPriorities()
        {
            var ticketPriorities = await this._priorityRepository.GetTicketPriorities();

            var ticketPrioritiesList = new List<SelectListItem>();

            ticketPrioritiesList.Add(new SelectListItem() { Text = "------ Select Priority ------", Value = "" });

            ticketPriorities.ForEach(priority =>
            {
                ticketPrioritiesList.Add(new SelectListItem() { Text = priority.Name, Value = priority.Id.ToString() });
            });

            return ticketPrioritiesList;

        }

        private async Task<List<SelectListItem>> GetTicketStates()
        {
            var ticketStates = await this._stateRepository.GetStates();

            var ticketStatesList = new List<SelectListItem>();

            ticketStatesList.Add(new SelectListItem() { Text = "------ Select State ------", Value = "" });

            ticketStates.ForEach(state =>
            {
                ticketStatesList.Add(new SelectListItem() { Text = state.Name, Value = state.Id.ToString() });
            });

            return ticketStatesList;

        }

        private async Task<List<SelectListItem>> GetAssignees()
        {
            var users = await this._userRepository.GetUsers();

            var usersList = new List<SelectListItem>();

            usersList.Add(new SelectListItem() { Text = "---- Select Assignee -------", Value = "" });

            users.ForEach(user =>
            {
                usersList.Add(new SelectListItem() { Text = user.FullName, Value = user.Id.ToString() });
            });

            return usersList;

        }

        private async Task<List<SelectListItem>> GetMembers()
        {
            var members = await this._memberRepository.GetMembers();

            var membersList = new List<SelectListItem>();

            membersList.Add(new SelectListItem() { Text = "---- Select Member -------", Value = "" });

            members.ForEach(member =>
            {
                membersList.Add(new SelectListItem() { Text = member.FullName, Value = member.Id.ToString() });
            });

            return membersList;

        }

        private async Task populateViewBags()
        {
            ViewBag.priorities = await GetTicketPriorities();
            ViewBag.categories = await GetTicketCategories();
            ViewBag.assignees = await GetAssignees();
            ViewBag.states = await GetTicketStates();
            ViewBag.members = await GetMembers();
        }


    }
}
