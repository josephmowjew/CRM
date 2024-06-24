using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;
using NuGet.Protocol;
using System.Data;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.DTOs.State;
using UCS_CRM.Core.DTOs.Ticket;
using UCS_CRM.Core.DTOs.TicketComment;
using UCS_CRM.Core.DTOs.TicketEscalation;
using UCS_CRM.Core.DTOs.TicketStateTracker;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Core.ViewModels;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Persistence.SQLRepositories;

namespace UCS_CRM.Areas.CallCenterOfficer.Controllers
{
    [Area("CallCenterOfficer")]
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly ITicketEscalationRepository _ticketEscalationRepository;
        private readonly ITicketCommentRepository _ticketCommentRepository;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IStateRepository _stateRepository;
        private readonly ITicketPriorityRepository _priorityRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private IWebHostEnvironment _env;
        private readonly IEmailAddressRepository _addressRepository;
        private readonly ITicketStateTrackerRepository _ticketStateTrackerRepository;
        private readonly HangfireJobEnqueuer _jobEnqueuer;
        private readonly UserManager<ApplicationUser> _userManager;
        public TicketsController(ITicketRepository ticketRepository, IMapper mapper, IUnitOfWork unitOfWork, IEmailService emailService, IEmailAddressRepository addressRepository,
            ITicketCategoryRepository ticketCategoryRepository, IStateRepository stateRepository, ITicketPriorityRepository priorityRepository,
            IWebHostEnvironment env, ITicketCommentRepository ticketCommentRepository, IUserRepository userRepository, IMemberRepository memberRepository, ITicketEscalationRepository ticketEscalationRepository, IDepartmentRepository departmentRepository, ITicketStateTrackerRepository ticketStateTrackerRepository, UserManager<ApplicationUser> userManager, HangfireJobEnqueuer jobEnqueuer)
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
            _ticketEscalationRepository = ticketEscalationRepository;
            _emailService = emailService;
            _addressRepository = addressRepository;
            _departmentRepository = departmentRepository;
            _ticketStateTrackerRepository = ticketStateTrackerRepository;
            _userManager = userManager;
            _jobEnqueuer = jobEnqueuer;
        }

        // GET: TicketsController
        public async Task<ActionResult> Index(string type = "")
        {
            ViewBag.type = type;
            await populateViewBags();

            //find the currently logged in user

            var findUserDb = await this._userRepository.GetUserWithRole(User.Identity.Name);

            //find the role of the currently logged in user

            if(findUserDb != null)
            {
                ViewBag.role = _userManager.GetRolesAsync(findUserDb).Result.FirstOrDefault();
            }

            return View();
        }


        // POST: TicketsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTicketDTO createTicketDTO)
        {
            await populateViewBags();

            createTicketDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createTicketDTO.DataInvalid = "";

                //search for the default state

                var defaultState = this._stateRepository.DefaultState(Lambda.NewTicket);

                if (defaultState == null)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError("", "Sorry but the application failed to log your ticket because of a missing state, please contact administrator for assistance");

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }
                else
                {
                    createTicketDTO.StateId = defaultState.Id;
                }


              

                var mappedTicket = this._mapper.Map<Ticket>(createTicketDTO);

                var ticketPresence = this._ticketRepository.Exists(mappedTicket);

                if (ticketPresence != null)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError(nameof(createTicketDTO.Title), $"title exists with the name submitted'");

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedTicket.CreatedById = claimsIdentitifier.Value;

                    
                    //get the last ticket

                    Ticket lastTicket = await this._ticketRepository.LastTicket();


                    //generate ticket number
                    var lastTicketId = lastTicket == null ? 0 : lastTicket.Id;

                    string ticketNumber = Lambda.IssuePrefix + (lastTicketId + 1);

                    //assign ticket number to the mapped record

                    mappedTicket.TicketNumber = ticketNumber;

                    mappedTicket.AssignedToId = claimsIdentitifier.Value;

                    //assign the ticket to the Customer Service and Member Engagement department

                    var customerServiceMemberEngagementDept = this._departmentRepository.Exists("Customer Service and Member Engagement");

                    if(customerServiceMemberEngagementDept != null)
                    {
                        mappedTicket.DepartmentId = customerServiceMemberEngagementDept.Id;
                    }

                    this._ticketRepository.Add(mappedTicket);




                    //save ticket to the data store

                    await this._unitOfWork.SaveToDataStore();

                    if (createTicketDTO.Attachments.Count > 0)
                    {
                        var attachments = createTicketDTO.Attachments.Select(async attachment =>
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

                        //get user record by created by id

                        var userRecord = await this._userRepository.GetSingleUser(mappedTicket.CreatedById);


                        if(userRecord != null)
                        {
                             string emailBody = "A ticket request for " + userRecord.FullName + " has been submitted in the system. </b> check the system for more details by clicking here " + Lambda.systemLink + "<br /> ";

                            //email to send to support
                            var emailAddress = await _addressRepository.GetEmailAddressByOwner(Lambda.Support);

                            if(emailAddress != null)
                            {
                                this._jobEnqueuer.EnqueueEmailJob(emailAddress.Email, "New Ticket Submitted", emailBody);
                                
                                
                            }
                        }

                       
                    }



                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }
                catch (DbUpdateException ex)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }

                catch (Exception ex)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }




            }



            return PartialView("_CreateTicketPartial", createTicketDTO);
        }

        // GET: TicketsController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var identityRole = await _ticketRepository.GetTicket(id);

            //await populateViewBags();

            return Json(identityRole);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditManagerTicketDTO editTicketDTO)
        {
            editTicketDTO.DataInvalid = "true";

            if (!ModelState.IsValid)
            {
                return PartialView("_EditTicketPartial", editTicketDTO);
            }

            editTicketDTO.DataInvalid = "";

            // Fetch the ticket from the database
            var ticketDB = await this._ticketRepository.GetTicket(id);
            if (ticketDB is null)
            {
                editTicketDTO.DataInvalid = "true";
                ModelState.AddModelError("", "The Identifier of the record was not found taken");
                return PartialView("_EditTicketPartial", editTicketDTO);
            }
            
            // Fetch current state and assigned user details
            string currentState = ticketDB.State.Name;
            string currentAssignedUserId = ticketDB.AssignedToId;
            string currentAssignedUserEmail = ticketDB?.AssignedTo?.Email;

            editTicketDTO.AssignedToId ??= ticketDB.AssignedToId;
            editTicketDTO.StateId ??= ticketDB.StateId;

            var state = await this._stateRepository.GetStateAsync((int)editTicketDTO.StateId);
            var user = await this._userRepository.FindByIdAsync(editTicketDTO.AssignedToId);

            if (state == null)
            {
                var defaultState = this._stateRepository.DefaultState(Lambda.NewTicket);
                state = defaultState;
            }

            string newState = state.Name;
            string newAssignedUserEmail = user != null ? user.Email : currentAssignedUserEmail;


            editTicketDTO.TicketNumber = ticketDB.TicketNumber;

            var claimsIdentifier = User.FindFirst(ClaimTypes.NameIdentifier);

            // Map the edit ticket to ticket
            var mappedTicket = this._mapper.Map<Ticket>(editTicketDTO);

            if (this._ticketRepository.Exists(mappedTicket) != null)
            {
                editTicketDTO.DataInvalid = "true";
                ModelState.AddModelError("Error", "This title is already taken");
                return PartialView("_EditTicketPartial", editTicketDTO);
            }

            this._mapper.Map(editTicketDTO, ticketDB);
            await this._unitOfWork.SaveToDataStore();

            // Check if the state changed
            if (!string.Equals(newState, currentState, StringComparison.OrdinalIgnoreCase))
            {
                var ticketStateTracker = new TicketStateTracker
                {
                    CreatedById = claimsIdentifier.Value,
                    TicketId = ticketDB.Id,
                    NewState = ticketDB.State.Name,
                    PreviousState = currentState,
                    Reason = "Ticket Update"
                };

                this._ticketStateTrackerRepository.Add(ticketStateTracker);
                await this._unitOfWork.SaveToDataStore();
            }

            // Process attachments in batch
            if (editTicketDTO.Attachments.Count > 0)
            {
                var attachmentTasks = editTicketDTO.Attachments.Select(async attachment =>
                {
                    string fileUrl = await Lambda.UploadFile(attachment, this._env.WebRootPath);
                    return new TicketAttachment
                    {
                        FileName = attachment.FileName,
                        TicketId = mappedTicket.Id,
                        Url = fileUrl
                    };
                });

                var mappedAttachments = await Task.WhenAll(attachmentTasks);
                mappedTicket.TicketAttachments.AddRange(mappedAttachments);
                await this._unitOfWork.SaveToDataStore();
                }

                if (currentAssignedUserId != editTicketDTO.AssignedToId)
                {
                    if (!string.IsNullOrEmpty(currentAssignedUserEmail) && !string.IsNullOrEmpty(newAssignedUserEmail))
                    {
                        await this._ticketRepository.SendTicketReassignmentEmail(currentAssignedUserEmail, newAssignedUserEmail, ticketDB);
                    }
                }

                
                if (user != null)
                {
                    string emailBody = $"A ticket has been modified in the system. <b>Check the system for more details by clicking here {Lambda.systemLink}</b>";
                    this._jobEnqueuer.EnqueueEmailJob(user.Email, $"Ticket {ticketDB.TicketNumber} Modification", emailBody);
                }


            return Json(new { status = "success", message = "User ticket updated successfully" });
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

                if (ticketRecordDb.State.Name.ToLower() != Lambda.NewTicket.ToLower())
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
            var type = HttpContext.Request.Form["ticketType"].FirstOrDefault();
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

           

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            resultTotal = await this._ticketRepository.GetAssignedToTicketsCountAsync(CursorParameters, claimsIdentitifier.Value,type);


            var result = await this._ticketRepository.GetAssignedToTickets(CursorParameters, claimsIdentitifier.Value,type);
            
           

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketDTO>>(result);

            var cleanResult = new List<ReadTicketDTO>();

            //mappedResult.ForEach(record =>
            //{
            //    record.State.Tickets = null;
            //    record.TicketAttachments.Select(r => r.Ticket = null);

            //    cleanResult.Add(record);

            //});



            //return Json(new { draw = draw, recordsFiltered = result.Count, recordsTotal = resultTotal, data = mappedResult });
            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = mappedResult });



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


            return Json(new { status = "success", message = "comment added successfully" });


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

            resultTotal = await this._ticketCommentRepository.TotalActiveCount(int.Parse(ticketId));
            var result = await this._ticketCommentRepository.GetTicketCommentsAsync(int.Parse(ticketId), CursorParameters);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketCommentDTO>>(result);



            return Json(new { draw = draw, recordsFiltered = result.Count, recordsTotal = resultTotal, data = mappedResult });

        }

        [HttpPost]
        public async Task<ActionResult> CloseTicket(CloseTicketDTO closeTicketDTO)
        {
            //check for model validity

            closeTicketDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                //find the ticket with the id sent

                var ticket = await this._ticketRepository.GetTicket(closeTicketDTO.Id);

                if (ticket == null)
                {
                    return Json(new {status="error", message = "Could not close ticket, try again or contact administrator if the error persist" });
                }
                else
                {
                    //check if the ticket was opened by the current user
                    //get the current user id

                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    var currentUserId = claimsIdentitifier.Value;

                    string currentState = ticket.State.Name;

                    //get close state

                    var closeState = this._stateRepository.Exists(Lambda.Closed);

                    if(ticket.CreatedById == currentUserId)
                    {
                        ticket.StateId = closeState.Id;

                        ticket.ClosedDate = DateTime.UtcNow;

                        //sync changes to the datastore

                        await this._unitOfWork.SaveToDataStore();

                        UCS_CRM.Core.Models.TicketStateTracker ticketStateTracker = new TicketStateTracker() { CreatedById = currentUserId, TicketId = ticket.Id, NewState = ticket.State.Name, PreviousState = currentState, Reason = closeTicketDTO.Reason };

                        this._ticketStateTrackerRepository.Add(ticketStateTracker);

                        await this._unitOfWork.SaveToDataStore();

                        //send alert emails

                         await this._ticketRepository.SendTicketClosureNotifications(ticket, closeTicketDTO.Reason);

                        return Json(new { status = "success", message = $"Ticket {ticket.TicketNumber} has been closed successfully" });

                    }
                    else
                    {
                        return Json(new {status="error", message = $"Could not close ticket as it can only be closed by{ticket.CreatedBy.Email} " });

                    }
                }
            }

            return Json(new { status="error", message = "Could not close ticket" });
        }

        [HttpPost]
        public async Task<ActionResult> ReopenTicket(CloseTicketDTO closeTicketDTO)
        {
            //check for model validity

            closeTicketDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                //find the ticket with the id sent

                var ticket = await this._ticketRepository.GetTicket(closeTicketDTO.Id);

                if (ticket == null)
                {
                    return Json(new { status = "error", message = "Could not re-open ticket, try again or contact administrator if the error persist" });
                }
                else
                {
                    //check if the ticket was opened by the current user
                    //get the current user id

                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    var currentUserId = claimsIdentitifier.Value;

                    string currentState = ticket.State.Name;

                    var reOpened = this._stateRepository.Exists(Lambda.ReOpened);

                    if (ticket.CreatedById == currentUserId)
                    {
                        ticket.StateId = reOpened.Id;

                        ticket.ClosedDate = null;

                        //sync changes to the datastore

                        await this._unitOfWork.SaveToDataStore();

                        //update the ticket change state 

                        UCS_CRM.Core.Models.TicketStateTracker ticketStateTracker = new TicketStateTracker() { CreatedById = currentUserId, TicketId = ticket.Id, NewState = ticket.State.Name, PreviousState = currentState, Reason = closeTicketDTO.Reason };

                        this._ticketStateTrackerRepository.Add(ticketStateTracker);

                        await this._unitOfWork.SaveToDataStore();

                        //send alert emails

                         await this._ticketRepository.SendTicketReopenedNotifications(ticket, closeTicketDTO.Reason);

                        return Json(new { status = "success", message = $"Ticket {ticket.TicketNumber} has been reopened successfully" });

                    }
                    else
                    {
                        return Json(new { status = "error", message = $"Could not reopen ticket as it can only by{ticket.CreatedBy.Email} " });

                    }
                }
            }

            return Json(new { status = "error", message = "Could not reopen ticket" });
        }

        [HttpGet]
        public async Task<ActionResult> FetchReassignList(int selectedValue)
        {

            //get users from the selected value department

            Department? department = await this._departmentRepository.GetDepartment(selectedValue);

            if (department == null)
                return BadRequest();


            //var users = await this._userRepository.GetUsers();

            var staff = department.Users.Where(u => u.Email.Trim().ToLower() != User.Identity.Name.Trim().ToLower()).ToList();

            var usersList = new List<SelectListItem>();


            //usersList.Add(new SelectListItem() { Text = "---- Select Assignee -------", Value = "" });

            staff.ForEach(user =>
            {
                usersList.Add(new SelectListItem() { Text = user.FullName, Value = user.Id.ToString() });
            });

            return Json(usersList);
        }

        [HttpGet]
        public async Task<JsonResult> GetAllMembersJson(int page = 1, int pageSize = 20, string searchValue = "")
        {

            var skip = (page - 1) * pageSize;


            var members = await this._memberRepository.GetMembers(new CursorParams() { Take = pageSize, Skip = skip, SearchTerm = searchValue });

            List<DynamicSelect> dynamicSelect = new List<DynamicSelect>();

            if (members.Any())
            {
                foreach (var item in members)
                {
                    dynamicSelect.Add(new DynamicSelect { Id = item.Id.ToString(), Name = item.FullName + " (" + item.AccountNumber +
                    ")" + " -- " + item.Branch,
                        
                    });
                }
            }



            return Json(dynamicSelect);
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

            //filter closed state

            ticketStates = ticketStates.Where(ts => ts.Name.Trim().ToLower() != Lambda.Closed.Trim().ToLower()).ToList();

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
            //var users = await this._userRepository.GetUsers();

            var staff = await this._userRepository.GetStuff();

            var usersList = new List<SelectListItem>();

            usersList.Add(new SelectListItem() { Text = "---- Select Assignee -------", Value = "" });

            //exclude myself from the list

            staff = staff.Where(s => s.Email.ToLower().Trim() != User.Identity.Name.ToLower().Trim()).ToList();

            staff.ForEach(user =>
            {
                usersList.Add(new SelectListItem() { Text = user.FullName, Value = user.Id.ToString() });
            });

            return usersList;

        }

        private async Task<List<SelectListItem>> GetDepartments()
        {
            var departmentsList = new List<SelectListItem>();

            string currentUserDepartment = string.Empty;

            //fetch all departments from the system.

            var dbDepartments = await this._departmentRepository.GetDepartments();

            //get the current department of the logged in clerk

            ApplicationUser? currentUser = await this._userRepository.FindByEmailsync(User.Identity.Name);

            if (currentUser != null)
            {
                currentUserDepartment = currentUser.Department.Name;
            }

            //filter the department list from the database to remove the current user department
           var filteredDepartmentList = dbDepartments.ToList();

            filteredDepartmentList.ForEach(d =>
            {
                departmentsList.Add(new SelectListItem() { Text = d.Name, Value = d.Id.ToString() });
            });

            return departmentsList;
        }
       
        [HttpPost]
        public async Task<ActionResult> GetTicketAuditData(int ticketId)
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

            resultTotal = await this._ticketStateTrackerRepository.TicketAuditTrailCountAsync(CursorParameters, ticketId);
            var result = await this._ticketStateTrackerRepository.TicketAuditTrail(CursorParameters, ticketId);

            //map the results to a read DTO

            var mappedResult = this._mapper.Map<List<ReadTicketStateTrackerDTO>>(result);

            return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = mappedResult });

        }

        //escalate ticket
        public async Task<ActionResult> Escalate(CreateTicketEscalationDTO createTicketEscalation)
        {
            //check for model validity

            createTicketEscalation.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                ApplicationUser currentAssignedUser = null;
                string currentAssignedUserEmail = string.Empty;
                Role currentAssignedUserRole = null;
                Department? currentAssignedUserDepartment = null;
                List<Role> rolesOfCurrentUserDepartment = new();
                List<Role> SortedrolesOfCurrentUserDepartment = new();
                createTicketEscalation.DataInvalid = "";
                bool ticketAssignedToNewUser = false;
                //createTicketEscalation.EscalationLevel = 1;

                //get the ticket in question

                var ticket = await this._ticketRepository.GetTicket(createTicketEscalation.TicketId);

                //get the current user id

                var userClaims = (ClaimsIdentity)User.Identity;

                var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                if(ticket != null)
                {
                     await this._ticketRepository.EscalateTicket(ticket, claimsIdentitifier.Value, createTicketEscalation.Reason);
                    

                    // if (result != null)
                    // {
                    //     if(result.Equals("Could not find a user to escalate the ticket to", StringComparison.OrdinalIgnoreCase))
                    //     {
                    //         createTicketEscalation.DataInvalid = "true";

                    //         ModelState.AddModelError("", "Could not find a user to escalate the ticket to");

                    //         return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
                    //     }
                    //     if(result.Equals("ticket escalated", StringComparison.OrdinalIgnoreCase))
                    //     {
                    //         createTicketEscalation.DataInvalid = "";


                    //         return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
                    //     }
                    // }
                    // else
                    // {
                    //     createTicketEscalation.DataInvalid = "true";

                    //     ModelState.AddModelError("","An error occurred while trying to escalate the ticket");

                    //     return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
                    // }
                }
                }
                else
                {

                    createTicketEscalation.DataInvalid = "true";

                    ModelState.AddModelError("", "Could not find a ticket with the identifier sent");

                    return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
                }



            //if(ticket != null)
            //{
            //    //get the user assigned to the ticket if available
            //    currentAssignedUser = ticket.AssignedTo;

            //    currentAssignedUserEmail = currentAssignedUser.Email;

            //    if (currentAssignedUser != null)
            //    {
            //         currentAssignedUserRole = await this._userRepository.GetRoleAsync(currentAssignedUser.Id);

            //        //check if the role of the user has been returned 

            //        if(currentAssignedUserRole  != null)
            //        {

            //            //get the department of the current assigned user
            //            currentAssignedUserDepartment = await this._departmentRepository.GetDepartment(currentAssignedUser.Department.Id);

            //            //get roles that are associated with this department
            //            rolesOfCurrentUserDepartment = currentAssignedUserDepartment.Roles;

            //            //order the roles according to rating

            //            SortedrolesOfCurrentUserDepartment = rolesOfCurrentUserDepartment.OrderBy(d => d.Rating).ToList();


            //            //loop through the list of roles in the current department

            //            if(SortedrolesOfCurrentUserDepartment.Count > 0)
            //            {
            //                //remove roles that are less than the one that the current assigned user is already in
            //                SortedrolesOfCurrentUserDepartment = SortedrolesOfCurrentUserDepartment.Where(r => r.Rating > currentAssignedUserRole.Rating).ToList();

            //                if(SortedrolesOfCurrentUserDepartment.Count > 0)
            //                {
            //                    for (int i = 0; i < SortedrolesOfCurrentUserDepartment.Count; i++)
            //                    {

            //                        var listOfUsers = await this._userRepository.GetUsersInRole(SortedrolesOfCurrentUserDepartment[i].Name);

            //                        //filter users to only those on the same branch
            //                        listOfUsers = listOfUsers.Where(u => u.BranchId == currentAssignedUser.BranchId).ToList();

            //                        //get the first user if available

            //                        var newTicketHandler = listOfUsers.FirstOrDefault();

            //                        if (newTicketHandler != null)
            //                        {
            //                            //assign the ticket this person and break out of the loop

            //                            ticket.AssignedToId = newTicketHandler.Id;

            //                            ticketAssignedToNewUser = true;

            //                            //break out of the loop
            //                            break;
            //                        }

            //                    }

            //                }
            //                else
            //                {
            //                    //assign the ticket to a manager with a role rating higher than the current user even if the manager is in a different department but same branch


            //                    //check if the ticket is already in the the branch networks and satellites department

            //                    if(currentAssignedUserDepartment.Name.Trim().ToLower() == "Branch Networks and satellites Department".Trim().ToLower())
            //                    {
            //                        ticket.AssignedToId = await this.AssignTicketToDepartment("Executive suite");

            //                        if(!string.IsNullOrEmpty(ticket.AssignedToId))
            //                        {
            //                            ticketAssignedToNewUser = true;
            //                        }

            //                    }
            //                    else
            //                    {
            //                        ticket.AssignedToId = await this.AssignTicketToDepartment("Branch Networks and satellites Department");

            //                        if (!string.IsNullOrEmpty(ticket.AssignedToId))
            //                        {
            //                            ticketAssignedToNewUser = true;
            //                        }

            //                    }


            //                }

            //            }

            //        }
            //        else
            //        {
            //            //Do something if the current user has no role
            //        }
            //    }
            //    else
            //    {
            //        //do something is the ticket is not assigned to anyone

            //        string result = await this._ticketRepository.SendUnassignedTicketEmail(ticket);
            //    }

            //}

            //if (ticketAssignedToNewUser != true)
            //{


            //    createTicketEscalation.DataInvalid = "true";

            //    ModelState.AddModelError("", $"Could not find a user to escalate the ticket to'");

            //    return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
            //}
            //else
            //{
            //    //map the create ticket escalation DTO to ticket escalation

            //    var mappedTicketEscalation = this._mapper.Map<TicketEscalation>(createTicketEscalation);

            //    //update the escalated to to reflect to new user assigned to the ticket

            //    mappedTicketEscalation.EscalatedTo = ticket.AssignedTo;


            //    //save to the database

            //    try
            //    {
            //        var userClaims = (ClaimsIdentity)User.Identity;

            //        var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            //        mappedTicketEscalation.CreatedById = claimsIdentitifier.Value;


            //        this._ticketEscalationRepository.Add(mappedTicketEscalation);


            //        await this._unitOfWork.SaveToDataStore();


            //        //send emails to previous assignee and the new assignee

            //        string emails_response = await this._ticketRepository.SendTicketEscalationEmail(ticket, mappedTicketEscalation, currentAssignedUserEmail);


            //        return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
            //    }
            //    catch (DbUpdateException ex)
            //    {
            //        createTicketEscalation.DataInvalid = "true";

            //        ModelState.AddModelError(string.Empty, ex.InnerException.Message);

            //        return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
            //    }

            //    catch (Exception ex)
            //    {

            //        createTicketEscalation.DataInvalid = "true";

            //        ModelState.AddModelError(string.Empty, ex.Message);

            //        return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
            //    }
            //}

            return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);

        }

        private async Task<List<SelectListItem>> GetMembers()
        {
            var members = await this._memberRepository.GetMembers(new CursorParams() { Take = 10, Skip = 0});

            

            var membersList = new List<SelectListItem>();

            membersList.Add(new SelectListItem() { Text = "---- Select Member -------", Value = "" });

            members.ForEach(member =>
            {
                membersList.Add(new SelectListItem() { Text = member.FullName +" (" + member.AccountNumber +
                    ")" +" -- "+ member.Branch, Value = member.Id.ToString() });
            });

            return membersList;

        }

        private async Task populateViewBags()
        {
            ViewBag.priorities = await GetTicketPriorities();
            ViewBag.categories = await GetTicketCategories();
            ViewBag.states = await GetTicketStates();
            //ViewBag.members = await GetMembers();
            ViewBag.assignees = await GetAssignees();
            ViewBag.departments = await GetDepartments();
        }

        private async Task<string> AssignTicketToDepartment(string departmentName)
        {
            string assignedToId = string.Empty;

            ApplicationUser currentAssignedUser = null;
            Role currentAssignedUserRole = null;
            Department? newDepartment = null;
            List<Role> rolesOfCurrentUserDepartment = new();
            List<Role> SortedrolesOfCurrentUserDepartment = new();

            newDepartment = this._departmentRepository.Exists(departmentName);

            if(newDepartment == null)
            {
                return assignedToId;
            }

            //get roles that are associated with this department
            rolesOfCurrentUserDepartment = newDepartment.Roles;

            //order the roles according to rating

            SortedrolesOfCurrentUserDepartment = rolesOfCurrentUserDepartment.OrderBy(d => d.Rating).ToList();
            //remove roles that are less than the one that the current assigned user is already in
            //SortedrolesOfCurrentUserDepartment = SortedrolesOfCurrentUserDepartment.Where(r => r.Rating > currentAssignedUserRole.Rating).ToList();

            if (SortedrolesOfCurrentUserDepartment.Count > 0)
            {
                for (int i = 0; i < SortedrolesOfCurrentUserDepartment.Count; i++)
                {

                    var listOfUsers = await this._userRepository.GetUsersInRole(SortedrolesOfCurrentUserDepartment[i].Name);

                    //get the first user if available

                    var newTicketHandler = listOfUsers.FirstOrDefault();

                    if (newTicketHandler != null)
                    {
                        //assign the ticket this person and break out of the loop

                        assignedToId = newTicketHandler.Id;
                    }

                }

            }

            return assignedToId;
        }



    }
}
