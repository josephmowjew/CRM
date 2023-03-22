using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.DTOs.State;
using UCS_CRM.Core.DTOs.Ticket;
using UCS_CRM.Core.DTOs.TicketComment;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Persistence.SQLRepositories;

namespace UCS_CRM.Areas.Client.Controllers
{
    [Area("Manager")]
    [Authorize]
    public class ManagerTicketsController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITicketCommentRepository _ticketCommentRepository;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IStateRepository _stateRepository;
        private readonly ITicketPriorityRepository _priorityRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private IWebHostEnvironment _env;
        public ManagerTicketsController(ITicketRepository ticketRepository, IMapper mapper, IUnitOfWork unitOfWork, 
            ITicketCategoryRepository ticketCategoryRepository, IStateRepository stateRepository, ITicketPriorityRepository priorityRepository,
            IWebHostEnvironment env, ITicketCommentRepository ticketCommentRepository, IUserRepository userRepository, IMemberRepository memberRepository)
        {
            _ticketRepository = ticketRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _ticketCategoryRepository = ticketCategoryRepository;
            _stateRepository = stateRepository;
            _priorityRepository = priorityRepository;
            _env = env;
            _ticketCommentRepository = ticketCommentRepository;
            _userRepository = userRepository;
            _memberRepository = memberRepository;
        }

        // GET: TicketsController
        public async Task<ActionResult> Index()
        {
            await populateViewBags();

            return View();
        }


        // GET: TicketsController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var identityRole = await _ticketRepository.GetTicket(id);

            await populateViewBags();

            return Json(identityRole);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditManagerTicketDTO editTicketDTO)
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
        // POST: TicketsController/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            //check if the role name isn't already taken

            var ticketRecordDb = await this._ticketRepository.GetTicket(id);

            if (ticketRecordDb != null)
            {
                //only execute remove if the state is not pending

                if (ticketRecordDb.State.Name.ToLower() != Lambda.WaitingForSupport.ToLower())
                {
                    return Json(new { status = "error", message = "ticket could not be found from the system at the moment as it has been responded to, consider closing it instead" });
                }
                else
                {
                    this._ticketRepository.Remove(ticketRecordDb);

                    await this._unitOfWork.SaveToDataStore();

                    return Json(new { status = "success", message = "ticket has been removed from the system successfully" });
                }

            }

            return Json(new { status = "error", message = "ticket could not be found from the system" });
        }

        [HttpPost]
        public async Task<ActionResult> GetTickets()
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

            resultTotal = await this._ticketRepository.TotalCount();
            var result = await this._ticketRepository.GetTickets(CursorParameters);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketDTO>>(result);

            var cleanResult = new List<ReadTicketDTO>();

            //mappedResult.ForEach(record =>
            //{
            //    record.State.Tickets = null;
            //    record.TicketAttachments.Select(r => r.Ticket = null);

            //    cleanResult.Add(record);

            //});



            return Json(new { draw = draw, recordsFiltered = result.Count, recordsTotal = resultTotal, data = mappedResult });



        }
        [HttpPost]
        public async Task<ActionResult> AddTicketComment(CreateTicketCommentDTO createTicketCommentDTO)
        {
            //check if the dto has data in it

            if (string.IsNullOrEmpty(createTicketCommentDTO.TicketId.ToString()))
            {
                return Json(new { status = "error", message = "The identifier of the ticket was not passed" });
            }

            if (string.IsNullOrEmpty(createTicketCommentDTO.Comment))
            {
                return Json(new { status = "error", message = "You did not type in a comment" });
            }

            //find the ticket with the id sent

            Ticket? ticketDbRecord = await this._ticketRepository.GetTicket(createTicketCommentDTO.TicketId);

            if (ticketDbRecord == null)
            {
                return Json(new { status = "error", message = "Error finding the ticket being passed" });
            }

            //get the current user id
            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);



            TicketComment ticketComment = new TicketComment();

            ticketComment.Comment = createTicketCommentDTO.Comment.Trim();
            ticketComment.TicketId = ticketDbRecord.Id;
            ticketComment.CreatedById = claimsIdentitifier.Value;

            this._ticketCommentRepository.Add(ticketComment);

            //sync changes with the data store

            await this._unitOfWork.SaveToDataStore();


            return Json(new { status = "success", message = "ticket added successfully" });


        }
        [HttpPost]

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

            resultTotal = await this._ticketCommentRepository.TotalActiveCount();
            var result = await this._ticketCommentRepository.GetTicketCommentsAsync(int.Parse(ticketId), CursorParameters);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketCommentDTO>>(result);



            return Json(new { draw = draw, recordsFiltered = result.Count, recordsTotal = resultTotal, data = mappedResult });

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

            ticketStatesList.Add(new SelectListItem() { Text = "------ Select State ------", Value = ""  });

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
