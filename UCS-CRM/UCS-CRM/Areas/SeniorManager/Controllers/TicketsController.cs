using AutoMapper;
using Hangfire;
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
using UCS_CRM.Core.DTOs.TicketEscalation;
using UCS_CRM.Core.DTOs.TicketStateTracker;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Persistence.SQLRepositories;

namespace UCS_CRM.Areas.SeniorManager.Controllers
{
    [Area("SeniorManager")]
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITicketCommentRepository _ticketCommentRepository;
        private readonly ITicketEscalationRepository _ticketEscalationRepository;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IStateRepository _stateRepository;
        private readonly ITicketPriorityRepository _priorityRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IMapper _mapper;
        private readonly ITicketStateTrackerRepository _ticketStateTrackerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly HangfireJobEnqueuer _jobEnqueuer;
        private readonly IEmailAddressRepository _addressRepository;
        private IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IErrorLogService _errorLogService;
        public TicketsController(ITicketRepository ticketRepository, IMapper mapper, IUnitOfWork unitOfWork, 
            ITicketCategoryRepository ticketCategoryRepository, IStateRepository stateRepository, ITicketPriorityRepository priorityRepository,IEmailAddressRepository addressRepository,
            IWebHostEnvironment env, ITicketCommentRepository ticketCommentRepository, IUserRepository userRepository, IMemberRepository memberRepository, ITicketEscalationRepository ticketEscalationRepository, ITicketStateTrackerRepository ticketStateTrackerRepository, IEmailService emailService, HangfireJobEnqueuer jobEnqueuer, ApplicationDbContext context, IConfiguration configuration, ILogger<TicketsController> logger, IDepartmentRepository departmentRepository, IErrorLogService errorLogService )
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
            _ticketStateTrackerRepository = ticketStateTrackerRepository;
            _emailService = emailService;
            _jobEnqueuer = jobEnqueuer;
            _addressRepository = addressRepository;
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _departmentRepository = departmentRepository;
            _errorLogService = errorLogService;
        }

        // GET: TicketsController
        public async Task<ActionResult> Index(string type = "")
        {
            await populateViewBags();
            ViewBag.type = type;
            return View();
        }

        // GET: TicketsController
        public async Task<ActionResult> Closed()
        {
            //await populateViewBags();

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
                    await populateViewBags();
                    return PartialView("_EditTicketPartial", this._mapper.Map<EditTicketDTO>(editTicketDTO));
               
                }

                string currentState = ticketDB.State.Name;
                string currentAssignedUserId = ticketDB.AssignedToId;
                string currentAssignedUserEmail = ticketDB?.AssignedTo?.Email;
                int newStateId = editTicketDTO.StateId == null ? ticketDB.StateId : (int)editTicketDTO.StateId;
                string newAssignedUserEmail = (await this._userRepository.FindByIdAsync(editTicketDTO.AssignedToId)).Email;
                string newState = (await this._stateRepository.GetStateAsync(newStateId)).Name;
                editTicketDTO.AssignedToId = editTicketDTO.AssignedToId == null ? ticketDB.AssignedToId : editTicketDTO.AssignedToId;

                editTicketDTO.StateId = editTicketDTO.StateId == null ? ticketDB.StateId : editTicketDTO.StateId;

                editTicketDTO.TicketNumber = ticketDB.TicketNumber;

                editTicketDTO.AssignedToId = editTicketDTO.AssignedToId == null ? ticketDB.AssignedToId : editTicketDTO.AssignedToId;

                 // Assign the ticket to the department of the assigned user
                if (ticketDB?.AssignedTo?.DepartmentId != null)
                {
                    ticketDB.DepartmentId = ticketDB.AssignedTo.DepartmentId;
                }

                //check if the role name isn't already taken
                var mappedTicket = this._mapper.Map<Ticket>(editTicketDTO);

                this._mapper.Map(editTicketDTO, ticketDB);
                //save changes to data store

                await this._unitOfWork.SaveToDataStore();

                // Detach the existing entry if it is not in the Modified state
                var existingEntry = _context.ChangeTracker.Entries<Ticket>().FirstOrDefault(e => e.Entity.Id == ticketDB.Id);
                if (existingEntry != null && existingEntry.State != EntityState.Modified)
                {
                    existingEntry.State = EntityState.Detached;
                }

                // Attach the ticket to the context and set its state to Modified
                this._context.Entry(ticketDB).State = EntityState.Modified;

                await this._unitOfWork.SaveToDataStore();

                var claimsIdentitifier = User.FindFirst(ClaimTypes.NameIdentifier);

                if (newState.Trim().ToLower() != currentState.Trim().ToLower())
                {

                    //update the ticket change state 

                    UCS_CRM.Core.Models.TicketStateTracker ticketStateTracker = new TicketStateTracker() { CreatedById = claimsIdentitifier.Value, TicketId = ticketDB.Id, NewState = ticketDB.State.Name, PreviousState = currentState, Reason = "Ticket Update" };

                    this._ticketStateTrackerRepository.Add(ticketStateTracker);

                    await this._unitOfWork.SaveToDataStore();
                }

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

                if (currentAssignedUserId != editTicketDTO.AssignedToId)
                {

                    await this._ticketRepository.SendTicketReassignmentEmail(currentAssignedUserEmail, newAssignedUserEmail, ticketDB);
                }

                string emailBody = $@"
                <html>
                <head>
                    <style>
                        @import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@300;400;700&display=swap');
                        body {{ font-family: 'Montserrat', sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 20px auto; padding: 20px; background-color: #f9f9f9; border-radius: 5px; }}
                        h2 {{ color: #0056b3; }}
                        .cta-button {{ display: inline-block; background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 3px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Ticket Modification Notice</h2>
                        <p>A ticket has been modified in the system.</p>
                        <p>Please review the changes by accessing the system:</p>
                        <p><a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a></p>
                    </div>
                </body>
                </html>";

                //email to send to support

                var user = await _userRepository.GetSingleUser(ticketDB.CreatedById);

                if (user != null)
                {
                    EmailHelper.SendEmail(this._jobEnqueuer, user.Email, $"Ticket {ticketDB.TicketNumber} Modification", emailBody, user.SecondaryEmail);                   
                }


                return Json(new { status = "success", message = "user ticket updated successfully" });
            }


            await populateViewBags();
            return PartialView("_EditTicketPartial", this._mapper.Map<EditTicketDTO>(editTicketDTO));
         
        }

        // GET: TicketController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var ticketDB = await this._ticketRepository.GetTicket(id);

            if (ticketDB == null)
            {
                return RedirectToAction("Index");
            }


            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var currentUserId = claimsIdentitifier.Value;

            ViewBag.CurrentUserId = currentUserId;

            ViewBag.ticketId = id;

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

                if (ticketRecordDb.State.Name.ToLower() != Lambda.NewTicket.ToLower())
                {
                    return Json(new { status = "error", message = "ticket could not be found from the system at the moment as it has been responded to, consider closing it instead" });
                }
                else
                {
                    this._ticketRepository.Remove(ticketRecordDb);

                    await this._unitOfWork.SaveToDataStore();

                    // Detach the existing entry if it is not in the Modified state
                    var existingEntry = _context.ChangeTracker.Entries<Ticket>().FirstOrDefault(e => e.Entity.Id == ticketRecordDb.Id);
                    if (existingEntry != null && existingEntry.State != EntityState.Modified)
                    {
                        existingEntry.State = EntityState.Detached;
                    }

                    // Attach the ticket to the context and set its state to Modified
                    this._context.Entry(ticketRecordDb).State = EntityState.Modified;

                    await this._unitOfWork.SaveToDataStore();

                    return Json(new { status = "success", message = "ticket has been removed from the system successfully" });
                }

            }

            return Json(new { status = "error", message = "ticket could not be found from the system" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTicketDTO createTicketDTO)
        {
            await populateViewBags();
            createTicketDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                createTicketDTO.DataInvalid = "";
                var defaultState = this._stateRepository.DefaultState(Lambda.NewTicket);

                if (defaultState == null)
                {
                    createTicketDTO.DataInvalid = "true";
                    ModelState.AddModelError("", "Sorry but the application failed to log your ticket because of a missing state, please contact administrator for assistance");
                    await populateViewBags();
                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }

                createTicketDTO.StateId = defaultState.Id;
                var mappedTicket = this._mapper.Map<Ticket>(createTicketDTO);
                
                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;
                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
                    
                    // Set effective creation date considering holidays
                    mappedTicket.CreatedDate = await DateTimeHelper.GetNextWorkingDay(_context, DateTime.Now);
                    mappedTicket.CreatedById = claimsIdentitifier.Value;

                    // Add automatic out-of-hours response if needed
                    // bool isWithinBusinessHours = await DateTimeHelper.IsWithinBusinessHours(_context, DateTime.Now);
                    // if (!isWithinBusinessHours)
                    // {
                    //     var workingHours = await _context.WorkingHours.FirstOrDefaultAsync(w => !w.DeletedDate.HasValue);
                    //     var startTime = workingHours?.StartTime ?? new TimeSpan(8, 0, 0);
                    //     var endTime = workingHours?.EndTime ?? new TimeSpan(17, 0, 0);
                        
                    //     var outOfHoursComment = new TicketComment
                    //     {
                    //         Comment = $@"This ticket was received outside of our business hours. 
                    //                     It will be processed on {mappedTicket.CreatedDate:dddd, MMMM dd, yyyy} at {startTime:hh\\:mm tt}.
                    //                     Our business hours are Monday to Friday, {startTime:hh\\:mm tt} to {endTime:hh\\:mm tt} EAT, 
                    //                     excluding public holidays and lunch break.",
                    //         TicketId = mappedTicket.Id,
                    //         CreatedById = claimsIdentitifier.Value,
                    //         CreatedDate = DateTime.Now
                    //     };
                        
                    //     _ticketCommentRepository.Add(outOfHoursComment);
                    // }

                    // Get the last ticket and generate number
                    Ticket lastTicket = await this._ticketRepository.LastTicket();
                    var lastTicketId = lastTicket == null ? 0 : lastTicket.Id;
                    string ticketNumber = Lambda.IssuePrefix + (lastTicketId + 1);
                    mappedTicket.TicketNumber = ticketNumber;

                    // Handle assignment
                    var userId = !string.IsNullOrEmpty(createTicketDTO.AssignedToId)
                        ? createTicketDTO.AssignedToId
                        : claimsIdentitifier.Value;
                    mappedTicket.AssignedToId = userId;

                    var assignedToUser = await this._userRepository.FindByIdAsync(userId);
                    if (assignedToUser != null)
                    {
                        mappedTicket.DepartmentId = assignedToUser.DepartmentId;
                    }

                    var exixtingTicket = this._ticketRepository.GetTicketByTicketNumber(mappedTicket.TicketNumber);
                    if (exixtingTicket != null)
                    {
                        lastTicket = await this._ticketRepository.LastTicket();
                        lastTicketId = lastTicket == null ? 0 : lastTicket.Id;
                        ticketNumber = Lambda.IssuePrefix + (lastTicketId + 1);
                        mappedTicket.TicketNumber = ticketNumber;
                    }

                    this._ticketRepository.Add(mappedTicket);
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
                    }

                    // Send notifications
                    await SendTicketCreationNotifications(mappedTicket, createTicketDTO.MemberId);

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

        private async Task SendTicketCreationNotifications(Ticket ticket, int memberId)
        {
            var memberRecord = await this._memberRepository.GetMemberAsync(memberId);
            if (memberRecord?.Email != null)
            {
                string emailBody = $@"
                <html>
                <head>
                    <style>
                        @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                        body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                        .logo {{ text-align: center; margin-bottom: 20px; }}
                        .logo img {{ max-width: 150px; }}
                        h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                        .ticket-info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
                        .cta-button {{ display: inline-block; background-color: #0056b3; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }}
                        .cta-button:hover {{ background-color: #003d82; }}
                        .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='logo'>
                            <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                        </div>
                        <h2>New Ticket Created</h2>
                        <p>Hello {memberRecord.FullName},</p>
                        <div class='ticket-info'>
                            <p>A new ticket has been created in the system for you with the following details:</p>
                            <p><strong>Ticket Number:</strong> {ticket.TicketNumber}</p>
                            <p><strong>Title:</strong> {ticket.Title}</p>
                        </div>
                        <p>You can check the details by clicking the button below:</p>
                        <p style='text-align: center;'>
                            <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                        </p>
                        <p class='footer'>Thank you for using our service.</p>
                    </div>
                </body>
                </html>";

                EmailHelper.SendEmail(this._jobEnqueuer, memberRecord.Email, "Ticket Creation", emailBody, null);

                // Send notification to support team
                var supportEmail = await _addressRepository.GetEmailAddressByOwner(Lambda.Support);
                if (supportEmail != null)
                {
                    string supportEmailBody = $@"
                    <html>
                    <head>
                        <style>
                            @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                            body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                            .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                            .logo {{ text-align: center; margin-bottom: 20px; }}
                            .logo img {{ max-width: 150px; }}
                            h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                            .ticket-info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
                            .ticket-info p {{ margin: 5px 0; }}
                            .cta-button {{ display: inline-block; background-color: #0056b3; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }}
                            .cta-button:hover {{ background-color: #003d82; }}
                            .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='logo'>
                                <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                            </div>
                            <h2>New Ticket Created</h2>
                            <p>Hello Support Team,</p>
                            <div class='ticket-info'>
                                <p>A new ticket has been created in the system for a member. Here are the details:</p>
                                <p><strong>Member Name:</strong> {memberRecord.FullName}</p>
                                <p><strong>Ticket Number:</strong> {ticket.TicketNumber}</p>
                                <p><strong>Title:</strong> {ticket.Title}</p>
                            </div>
                            <p>Please review and take necessary action as soon as possible.</p>
                            <p style='text-align: center;'>
                                <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                            </p>
                            <p class='footer'>Thank you for your prompt attention to this matter.</p>
                        </div>
                    </body>
                    </html>";
                    this._jobEnqueuer.EnqueueEmailJob(supportEmail.Email, "New Ticket Creation", supportEmailBody);
                }

                // Add notification for assigned user
                if (ticket.AssignedTo != null)
                {
                    string assignmentEmailBody = $@"
                    <html>
                    <head>
                        <style>
                            @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;700&family=Montserrat:wght@300;400;700&display=swap');
                            body {{ font-family: 'Montserrat', sans-serif; line-height: 1.8; color: #333; background-color: #f4f4f4; }}
                            .container {{ max-width: 600px; margin: 20px auto; padding: 30px; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
                            .logo {{ text-align: center; margin-bottom: 20px; }}
                            .logo img {{ max-width: 150px; }}
                            h2 {{ color: #0056b3; text-align: center; font-weight: 700; font-family: 'Playfair Display', serif; }}
                            .ticket-info {{ background-color: #f0f7ff; border-left: 4px solid #0056b3; padding: 15px; margin: 20px 0; }}
                            .ticket-info p {{ margin: 5px 0; }}
                            .cta-button {{ display: inline-block; background-color: #0056b3; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }}
                            .cta-button:hover {{ background-color: #003d82; }}
                            .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='logo'>
                                <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                            </div>
                            <h2>New Ticket Assignment</h2>
                            <p>Hello {ticket.AssignedTo.FullName},</p>
                            <div class='ticket-info'>
                                <p>A new ticket has been assigned to you. Here are the details:</p>
                                <p><strong>Ticket Number:</strong> {ticket.TicketNumber}</p>
                                <p><strong>Title:</strong> {ticket.Title}</p>
                                <p><strong>Member:</strong> {memberRecord?.FullName}</p>
                            </div>
                            <p>Your prompt attention to this matter is crucial. Please review and take necessary action as soon as possible.</p>
                            <p style='text-align: center;'>
                                <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                            </p>
                            <p class='footer'>Thank you for your dedication to excellent service. If you have any questions, please don't hesitate to reach out to your supervisor.</p>
                        </div>
                    </body>
                    </html>";

                    try
                    {
                        EmailHelper.SendEmail(
                            this._jobEnqueuer,
                            ticket.AssignedTo.Email,
                            $"New Ticket Assignment - {ticket.TicketNumber}",
                            assignmentEmailBody,
                            ticket.AssignedTo.SecondaryEmail
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to send assignment email for ticket {ticket.TicketNumber}: {ex.Message}");
                    }
                }
            }
        }
        public async Task<ActionResult> CloseTicket(CloseTicketDTO closeTicketDTO)
        {
            closeTicketDTO.DataInvalid = "true";

            if (ModelState.IsValid)
            {
                var ticket = await this._ticketRepository.GetTicket(closeTicketDTO.Id);

                if (ticket == null)
                {
                    return Json(new { status = "error", message = "Could not close ticket, try again or contact administrator if the error persists" });
                }
                else
                {
                    var userClaims = (ClaimsIdentity)User.Identity;
                    var claimsIdentifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
                    var currentUserId = claimsIdentifier.Value;

                    string currentState = ticket.State.Name;

                    // Check if the current user is assigned to the ticket
                    if (ticket.AssignedToId == currentUserId || ticket.CreatedById == currentUserId)
                    {
                        var closeState = this._stateRepository.Exists(Lambda.Closed);

                        ticket.StateId = closeState.Id;
                        ticket.ClosedDate = DateTime.Now;

                        // Detach the existing entry if it is not in the Modified state
                        var existingEntry = _context.ChangeTracker.Entries<Ticket>().FirstOrDefault(e => e.Entity.Id == ticket.Id);
                        if (existingEntry != null && existingEntry.State != EntityState.Modified)
                        {
                            existingEntry.State = EntityState.Detached;
                        }

                        // Attach the ticket to the context and set its state to Modified
                        this._context.Entry(ticket).State = EntityState.Modified;

                        await this._unitOfWork.SaveToDataStore();

                        UCS_CRM.Core.Models.TicketStateTracker ticketStateTracker = new TicketStateTracker() 
                        { 
                            CreatedById = currentUserId, 
                            TicketId = ticket.Id, 
                            NewState = ticket.State.Name, 
                            PreviousState = currentState, 
                            Reason = closeTicketDTO.Reason 
                        };

                        this._ticketStateTrackerRepository.Add(ticketStateTracker);

                        await this._unitOfWork.SaveToDataStore();

                        // Send alert emails
                        await this._ticketRepository.SendTicketClosureNotifications(ticket, closeTicketDTO.Reason);

                        return Json(new { status = "success", message = $"Ticket {ticket.TicketNumber} has been closed successfully" });
                    }
                    else
                    {
                        return Json(new { status = "error", message = $"Could not close ticket as you are not currently assigned to it" });
                    }
                }
            }

            return Json(new { status = "error", message = "Could not close ticket" });
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

                        // Detach the existing entry if it is not in the Modified state
                        var existingEntry = _context.ChangeTracker.Entries<Ticket>().FirstOrDefault(e => e.Entity.Id == ticket.Id);
                        if (existingEntry != null && existingEntry.State != EntityState.Modified)
                        {
                            existingEntry.State = EntityState.Detached;
                        }

                        // Attach the ticket to the context and set its state to Modified
                        this._context.Entry(ticket).State = EntityState.Modified;

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


        // POST: TicketsController/close/5
        [HttpPost]
        public async Task<ActionResult> Close(int id)
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
                    ticketRecordDb.ClosedDate = DateTime.Now;
                    await this._unitOfWork.SaveToDataStore();

                    // Detach the existing entry if it is not in the Modified state
                    var existingEntry = _context.ChangeTracker.Entries<Ticket>().FirstOrDefault(e => e.Entity.Id == ticketRecordDb.Id);
                    if (existingEntry != null && existingEntry.State != EntityState.Modified)
                    {
                        existingEntry.State = EntityState.Detached;
                    }

                    // Attach the ticket to the context and set its state to Modified
                    this._context.Entry(ticketRecordDb).State = EntityState.Modified;

                    await this._unitOfWork.SaveToDataStore();

                    return Json(new { status = "success", message = "ticket has been closed successfully" });
                }

            }

            return Json(new { status = "error", message = "ticket could not be found from the system" });
        }


        //escalate ticket
        [HttpPost]
        public async Task<ActionResult> Escalate(CreateTicketEscalationDTO createTicketEscalation)
        {
            //check for model validity

            createTicketEscalation.DataInvalid = "true";

            if (ModelState.IsValid)
            {

                createTicketEscalation.DataInvalid = "";
                createTicketEscalation.EscalationLevel = 2;


                //check for article title presence

                var mappedTicketEscalation = this._mapper.Map<TicketEscalation>(createTicketEscalation);

                var ticketEscalationPresence = this._ticketEscalationRepository.Exists(mappedTicketEscalation);



                if (ticketEscalationPresence != null)
                {
                    createTicketEscalation.DataInvalid = "true";

                    ModelState.AddModelError(nameof(mappedTicketEscalation.Ticket.Title), $"Another ticket exists with the parameters submitted'");

                    return PartialView("_SecondTicketEscalationPartial", createTicketEscalation);
                }


                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedTicketEscalation.CreatedById = claimsIdentitifier.Value;


                    this._ticketEscalationRepository.Add(mappedTicketEscalation);
                    await this._unitOfWork.SaveToDataStore();


                    return PartialView("_SecondTicketEscalationPartial", createTicketEscalation);
                }
                catch (DbUpdateException ex)
                {
                    createTicketEscalation.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.InnerException.Message);

                    return PartialView("_SecondTicketEscalationPartial", createTicketEscalation);
                }

                catch (Exception ex)
                {

                    createTicketEscalation.DataInvalid = "true";

                    ModelState.AddModelError(string.Empty, ex.Message);

                    return PartialView("_SecondTicketEscalationPartial", createTicketEscalation);
                }




            }



            return PartialView("_SecondTicketEscalationPartial", createTicketEscalation);
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
        [HttpPost]
        public async Task<ActionResult> GetTickets(string status)
        {
            var form = HttpContext.Request.Form;
            var type = form["ticketType"].FirstOrDefault();
            var draw = form["draw"].FirstOrDefault();
            int.TryParse(form["start"].FirstOrDefault(), out int skip);
            int.TryParse(form["length"].FirstOrDefault(), out int pageSize);

            var sortColumn = form[$"columns[{form["order[0][column]"].FirstOrDefault()}][name]"].FirstOrDefault();
            var sortColumnAscDesc = form["order[0][dir]"].FirstOrDefault();
            var searchValue = form["search[value]"].FirstOrDefault();

            var findUserDb = await this._userRepository.GetUserWithRole(User.Identity.Name);
            bool isExecutive = findUserDb?.Department?.Name?.Trim().ToUpper() == "EXECUTIVE SUITE";

            var cursorParameters = new CursorParams 
            { 
                SearchTerm = searchValue, 
                Skip = skip, 
                SortColum = sortColumn, 
                SortDirection = sortColumnAscDesc, 
                Take = pageSize 
            };

            var resultTotal = isExecutive 
                ? await this._ticketRepository.TotalCount(type)
                : await this._ticketRepository.GetTicketsTotalFilteredAsync(cursorParameters, findUserDb.Department, type);

            var result = isExecutive
                ? await this._ticketRepository.GetTickets(cursorParameters, null, type)
                : await this._ticketRepository.GetTickets(cursorParameters, findUserDb.Department, type);

            var mappedResult = this._mapper.Map<List<ReadTicketDTO>>(result);

            return Json(new { 
                draw, 
                recordsFiltered = resultTotal, 
                recordsTotal = resultTotal, 
                data = mappedResult 
            });
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

            
            // Send emails to all stakeholders
            var ticket = await this._ticketRepository.GetTicket(ticketDbRecord.Id);
            var stakeholders = new List<ApplicationUser> { ticket.CreatedBy, ticket.AssignedTo };
            if (ticket.Member?.User != null)
            {
                stakeholders.Add(ticket.Member.User);
            }

            // Get all users involved in ticket escalations
            var cursorParams = new CursorParams { Take = int.MaxValue }; // Retrieve all escalations
            var ticketEscalations = await this._ticketEscalationRepository.GetTicketEscalations(ticketDbRecord.Id, cursorParams);
            if (ticketEscalations != null)
            {
                foreach (var escalation in ticketEscalations)
                {
                    if (escalation.EscalatedTo != null && !stakeholders.Contains(escalation.EscalatedTo))
                    {
                        stakeholders.Add(escalation.EscalatedTo);
                    }
                }
            }

            foreach (var stakeholder in stakeholders)
            {
                string systemUrl = $"{_configuration["HostingSettings:Protocol"]}://{_configuration["HostingSettings:Host"]}";
                string emailBody = $"A new comment has been added to ticket #{ticketDbRecord.Id}:<br><br>" +
                                   $"<strong>Comment:</strong> {ticketComment.Comment}<br><br>" +
                                   $"Please <a href='{systemUrl}'>click here</a> to view the full details in the system.";

                string primaryEmail = stakeholder.Email ?? string.Empty;
                string secondaryEmail = stakeholder.SecondaryEmail ?? string.Empty;

                if (!string.IsNullOrEmpty(primaryEmail))
                {
                    try
                    {
                        EmailHelper.SendEmail(this._jobEnqueuer, primaryEmail, 
                            $"New Comment on Ticket #{ticketDbRecord.Id}", 
                            emailBody, 
                            secondaryEmail);
                    }
                    catch (Exception ex)
                    {
                        // Log the error, but don't throw to prevent crashing
                        _logger.LogError($"Failed to send email to {primaryEmail}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Skipped sending email for stakeholder with null primary email");
                }
            }


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

            resultTotal = await this._ticketCommentRepository.TotalActiveCount(int.Parse(ticketId));
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

            ticketStates = ticketStates.Where(ts => ts.Name.Trim().ToLower() != Lambda.Closed.Trim().ToLower()).ToList();


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
            var users = await this._userRepository.GetStuff();

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
                membersList.Add(new SelectListItem() { Text = member.FullName + " (" + member.AccountNumber +
                    ")", Value = member.Id.ToString() });
            });

            return membersList;

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



        private async Task populateViewBags()
        {
            ViewBag.priorities = await GetTicketPriorities();
            ViewBag.categories = await GetTicketCategories();
            ViewBag.assignees = await GetAssignees();
            ViewBag.states = await GetTicketStates();
            ViewBag.departments = await GetDepartments();
        }

       
        

         [HttpGet]
        public async Task<IActionResult> GetInitiators(string type, string search, int page = 1)
        {
            int pageSize = 10;
            CursorParams cursorParams = new CursorParams
            {
                Skip = (page - 1) * pageSize,
                Take = pageSize,
                SearchTerm = search
            };

            if (type == "User")
            {
                var users = await _userRepository.GetUsersWithRoles(cursorParams);
                var totalCount = await _userRepository.TotalFilteredUsersCount(cursorParams);
                var results = users.Select(u => new { id = u.Id, text = $"{u.FirstName} {u.LastName}" });
                return Json(new { 
                    results = results,
                    pagination = new { more = (page * pageSize) < totalCount }
                });
            }
            else if (type == "Member")
            {
                var members = await _memberRepository.GetMembers(cursorParams);
                var totalCount = await _memberRepository.TotalFilteredMembersCount(cursorParams);
                var results = members.Select(m => new { id = m.Id.ToString(), text = $"{m.FirstName} {m.LastName}" });
                return Json(new { 
                    results = results,
                    pagination = new { more = (page * pageSize) < totalCount }
                });
            }
            return Json(new { results = new List<object>(), pagination = new { more = false } });
        }


        [HttpGet]
        public async Task<IActionResult> FetchAssigneesByDepartment(int departmentId)
        {
            try 
            {
                var currentUserEmail = User?.Identity?.Name;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    _logger.LogWarning("Current user email is null when fetching assignees");
                    return BadRequest("User not authenticated");
                }

                var staff = await _userRepository.GetUsersByDepartment(departmentId);
                
                var assignees = staff
                    .Where(u => !string.IsNullOrEmpty(u.Email) && 
                               !string.IsNullOrEmpty(u.FullName) &&
                               !u.Email.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
                    .Select(user => new 
                    { 
                        value = user.Id.ToString(),
                        text = user.FullName
                    })
                    .ToList();

                return Json(assignees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assignees for department {DepartmentId}", departmentId);
                await _errorLogService.LogErrorAsync(ex);
                return BadRequest("Failed to fetch assignees");
            }
        }

        [HttpGet]
        public async Task<ActionResult> FetchReassignList(int selectedValue)
        {
            try
            {
                var currentUserEmail = User?.Identity?.Name;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    _logger.LogWarning("Current user email is null when fetching reassign list");
                    return BadRequest("User not authenticated");
                }

                var department = await _departmentRepository.GetDepartment(selectedValue);
                if (department == null)
                {
                    _logger.LogWarning("Department not found: {DepartmentId}", selectedValue);
                    return BadRequest("Department not found");
                }

                var staff = department.Users
                    .Where(u => !string.IsNullOrEmpty(u.Email) && 
                               !string.IsNullOrEmpty(u.FullName) &&
                               !u.Email.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
                    .Select(user => new SelectListItem 
                    { 
                        Text = user.FullName,
                        Value = user.Id.ToString()
                    })
                    .ToList();

                return Json(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reassign list for department {DepartmentId}", selectedValue);
                await _errorLogService.LogErrorAsync(ex);
                return BadRequest("Failed to fetch reassign list");
            }
        }




    }
}
