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
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Persistence.SQLRepositories;

namespace UCS_CRM.Areas.Teller.Controllers
{
    [Area("Teller")]
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
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketsController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TicketsController(
            ITicketRepository ticketRepository, IMapper mapper,
            IUnitOfWork unitOfWork, IEmailService emailService, 
            IEmailAddressRepository addressRepository,
            ITicketCategoryRepository ticketCategoryRepository,
            IStateRepository stateRepository, 
            ITicketPriorityRepository priorityRepository,
            IWebHostEnvironment env,
            ITicketCommentRepository ticketCommentRepository, 
            IUserRepository userRepository,
            IMemberRepository memberRepository,
            ITicketEscalationRepository ticketEscalationRepository,
            IDepartmentRepository departmentRepository,
            ITicketStateTrackerRepository ticketStateTrackerRepository,
            UserManager<ApplicationUser> userManager,
            HangfireJobEnqueuer jobEnqueuer,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<TicketsController> logger,
            IHttpContextAccessor httpContextAccessor)
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
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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

                //save to the database

                try
                {
                    var userClaims = (ClaimsIdentity)User.Identity;

                    var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

                    mappedTicket.CreatedDate = await DateTimeHelper.GetNextWorkingDay(_context, DateTime.UtcNow);
                    mappedTicket.CreatedById = claimsIdentitifier.Value;

                    // Add automatic out-of-hours response if needed
                    bool isWithinBusinessHours = await DateTimeHelper.IsWithinBusinessHours(_context, DateTime.Now);
                    if (!isWithinBusinessHours)
                    {
                        var workingHours = await _context.WorkingHours.FirstOrDefaultAsync(w => !w.DeletedDate.HasValue);
                        var startTime = workingHours?.StartTime ?? new TimeSpan(8, 0, 0);
                        var endTime = workingHours?.EndTime ?? new TimeSpan(17, 0, 0);
                        
                        var outOfHoursComment = new TicketComment
                        {
                            Comment = $@"This ticket was received outside of our business hours. 
                                        It will be processed on {mappedTicket.CreatedDate:dddd, MMMM dd, yyyy} at {startTime:hh\\:mm tt}.
                                        Our business hours are Monday to Friday, {startTime:hh\\:mm tt} to {endTime:hh\\:mm tt} EAT, 
                                        excluding public holidays and lunch break.",
                            TicketId = mappedTicket.Id,
                            CreatedById = claimsIdentitifier.Value,
                            CreatedDate = DateTime.Now
                        };
                        
                        _ticketCommentRepository.Add(outOfHoursComment);

                        // Send out-of-hours notification email
                        var notificationRecipient = await this._memberRepository.GetMemberAsync(createTicketDTO.MemberId);
                        
                        if (notificationRecipient != null && !string.IsNullOrEmpty(notificationRecipient.Email))
                        {
                            string outOfHoursEmailBody = $@"
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
                                    .processing-info {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
                                    .footer {{ margin-top: 30px; text-align: center; font-style: italic; color: #666; }}
                                </style>
                            </head>
                            <body>
                                <div class='container'>
                                    <div class='logo'>
                                        <img src='https://crm.ucssacco.com/images/LOGO(1).png' alt='UCS SACCO Logo'>
                                    </div>
                                    <h2>Ticket Received Outside Business Hours</h2>
                                    <p>Hello {notificationRecipient.FullName},</p>
                                    <div class='ticket-info'>
                                        <p><strong>Ticket Number:</strong> {mappedTicket.TicketNumber}</p>
                                        <p><strong>Title:</strong> {mappedTicket.Title}</p>
                                    </div>
                                    <div class='processing-info'>
                                        <p>Your ticket was received outside our business hours and will be processed on:</p>
                                        <p><strong>Date:</strong> {mappedTicket.CreatedDate:dddd, MMMM dd, yyyy}</p>
                                        <p><strong>Time:</strong> {startTime:hh\\:mm tt}</p>
                                        <p><strong>Our Business Hours:</strong></p>
                                        <p>Monday to Friday, {startTime:hh\\:mm tt} to {endTime:hh\\:mm tt} EAT</p>
                                        <p>(Excluding public holidays and lunch break)</p>
                                    </div>
                                    <p style='text-align: center;'>
                                        <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                                    </p>
                                    <p class='footer'>Thank you for your patience. We will process your request during our next business hours.</p>
                                </div>
                            </body>
                            </html>";

                            EmailHelper.SendEmail(
                                this._jobEnqueuer, 
                                notificationRecipient.Email, 
                                $"Ticket {mappedTicket.TicketNumber} - Out of Hours Notification", 
                                outOfHoursEmailBody, 
                                notificationRecipient?.User?.SecondaryEmail
                            );
                        }
                    }

                    //get the last ticket

                    Ticket lastTicket = await this._ticketRepository.LastTicket();


                    //generate ticket number
                    var lastTicketId = lastTicket == null ? 0 : lastTicket.Id;

                    string ticketNumber = Lambda.IssuePrefix + (lastTicketId + 1);

                      var userId = !string.IsNullOrEmpty(createTicketDTO.AssignedToId) 
                        ? createTicketDTO.AssignedToId 
                        : claimsIdentitifier.Value;

                    mappedTicket.AssignedToId = userId;

                    var assignedToUser = await this._userRepository.FindByIdAsync(userId);

                    if (assignedToUser != null)
                    {
                        // Set the ticket department to the user's department
                        mappedTicket.DepartmentId = assignedToUser?.DepartmentId;
                    }


                    //assign the ticket to the user

                    if (!string.IsNullOrEmpty(createTicketDTO.AssignedToId))
                    {
                        createTicketDTO.AssignedToId = createTicketDTO.AssignedToId;
                    }
                    else
                    {
                        mappedTicket.AssignedToId = claimsIdentitifier.Value;
                    }

                    //assign ticket number to the mapped record

                    mappedTicket.TicketNumber = ticketNumber;

                    

                    //assign the ticket to the Customer Service and Member Engagement department

                    var customerServiceMemberEngagementDept = this._departmentRepository.Exists("Customer Service and Member Engagement");
                    var creditAndEvaluationsDept = this._departmentRepository.Exists("Credit and Evaluations");

                    // Get the ticket category
                    var ticketCategory = await this._ticketCategoryRepository.GetTicketCategory(createTicketDTO.TicketCategoryId);

                    if (ticketCategory?.Name?.Trim().ToLower() == "affordability check")
                    {
                        if (creditAndEvaluationsDept != null)
                        {
                            mappedTicket.DepartmentId = creditAndEvaluationsDept.Id;
                            
                            // Find and assign to a user in the Credit and Evaluations department
                            var creditDeptUsers = await _userRepository.GetUsersByDepartment(creditAndEvaluationsDept.Id);
                            var firstAvailableUser = creditDeptUsers.FirstOrDefault();
                            if (firstAvailableUser != null)
                            {
                                mappedTicket.AssignedToId = firstAvailableUser.Id;
                            }
                        }
                    }
                    else if (customerServiceMemberEngagementDept != null)
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

                        

                       
                    }

                        //get user record by created by id

                        var memberRecord = await this._memberRepository.GetMemberAsync(createTicketDTO.MemberId);


                      if(!string.IsNullOrEmpty(memberRecord.Email))
                      {
                        //generate email body for the member 

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
                                <p>A new ticket has been created in the system for you. You can check the details by clicking the button below:</p>
                                <center>
                                    <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;' style='color: #ffffff;'>View Ticket Details</a>
                                </center>
                                <p class='footer'>Thank you for using our service.</p>
                            </div>
                        </body>
                        </html>";

                        // Send email to the owner if they're not the assigned user
                        if (!string.IsNullOrEmpty(memberRecord.Email))
                        {
                            EmailHelper.SendEmail(this._jobEnqueuer, memberRecord.Email, "Ticket Creation", emailBody, null);
                        }

                        // Email to send to support
                        var emailAddress = await _addressRepository.GetEmailAddressByOwner(Lambda.Support);

                        if(emailAddress != null)
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
                                    <p>A new ticket has been created in the system for a member. Here are the details:</p>
                                    <div class='ticket-info'>
                                        <p><strong>Member Name:</strong> {memberRecord.FullName}</p>
                                    </div>
                                    <p>Please review and take necessary action as soon as possible.</p>
                                    <center>
                                        <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;' style='color: #ffffff;'>View Ticket Details</a>
                                    </center>
                                    <p class='footer'>Thank you for your prompt attention to this matter.</p>
                                </div>
                            </body>
                            </html>";
                            this._jobEnqueuer.EnqueueEmailJob(emailAddress.Email, "New Ticket Creation", supportEmailBody);
                        }
                    }

                    // Send an email to the user who is assigned to the ticket if different from the creator
                    if(assignedToUser != null)
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
                                <p>Hello,</p>
                                <p>A new ticket has been assigned to you. Here are the details:</p>
                                <div class='ticket-info'>
                                    <p><strong>Ticket Number:</strong> {mappedTicket.TicketNumber}</p>
                                    <p><strong>Title:</strong> {mappedTicket.Title}</p>
                                </div>
                                <p>Your prompt attention to this matter is crucial. Please review and take necessary action as soon as possible.</p>
                                <center>
                                    <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;' style='color: #ffffff;'>View Ticket Details</a>
                                </center>
                                <p class='footer'>Thank you for your dedication to excellent service. If you have any questions, please don't hesitate to reach out to your supervisor.</p>
                            </div>
                        </body>
                        </html>";
                        EmailHelper.SendEmail(this._jobEnqueuer, assignedToUser.Email, "New Ticket Assignment", emailBody, assignedToUser.SecondaryEmail);
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

            await populateViewBags();

            return Json(identityRole);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EditManagerTicketDTO editTicketDTO)
        {
            editTicketDTO.DataInvalid = "true";

            if (!ModelState.IsValid)
            {
                await this.populateViewBags();

                var mappedDTO = this._mapper.Map<Ticket>(editTicketDTO);
                return PartialView("_EditTicketPartial", mappedDTO);
            }

            editTicketDTO.DataInvalid = "";

            // Fetch the ticket from the database
            var ticketDB = await this._ticketRepository.GetTicket(id);
            if (ticketDB is null)
            {
                editTicketDTO.DataInvalid = "true";
                ModelState.AddModelError("", "The Identifier of the record was not found taken");
                await this.populateViewBags();

                var mappedDTO = this._mapper.Map<Ticket>(editTicketDTO);
                return PartialView("_EditTicketPartial", mappedDTO);
            }
            
            // Fetch current state and assigned user details
            string currentState = ticketDB.State.Name;
            string currentAssignedUserId = ticketDB.AssignedToId;
            string currentAssignedUserEmail = ticketDB?.AssignedTo?.Email;

            editTicketDTO.AssignedToId ??= ticketDB.AssignedToId;
            editTicketDTO.StateId ??= ticketDB.StateId;

            // Assign the ticket to the department of the assigned user
            if (ticketDB?.AssignedTo?.DepartmentId != null)
            {
                ticketDB.DepartmentId = ticketDB.AssignedTo.DepartmentId;
            }


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


            this._mapper.Map(editTicketDTO, ticketDB);


            // Detach the existing entry if it is not in the Modified state
            var existingEntry = _context.ChangeTracker.Entries<Ticket>().FirstOrDefault(e => e.Entity.Id == ticketDB.Id);
            if (existingEntry != null && existingEntry.State != EntityState.Modified)
            {
                existingEntry.State = EntityState.Detached;
            }

            // Attach the ticket to the context and set its state to Modified
            this._context.Entry(ticketDB).State = EntityState.Modified;

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
                            <h2>Ticket Modification Notice</h2>
                            <div class='ticket-info'>
                                <p>A ticket has been modified in the system.</p>
                                <p>Please review the changes by accessing the system.</p>
                            </div>
                            <p style='text-align: center;'>
                                <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                            </p>
                            <p class='footer'>Thank you for using our service.</p>
                        </div>
                    </body>
                    </html>";
                    this._jobEnqueuer.EnqueueEmailJob(user.Email, $"Ticket {ticketDB.TicketNumber} Modification", emailBody);
                }


            return Json(new { status = "success", message = "User ticket updated successfully" });
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
            CursorParams CursorParameters = new CursorParams() 
            { 
                SearchTerm = searchValue, 
                Skip = skip, 
                SortColum = string.IsNullOrEmpty(sortColumn) ? "CreatedDate" : sortColumn,  // Default sort by CreatedDate
                SortDirection = string.IsNullOrEmpty(sortColumnAscDesc) ? "DESC" : sortColumnAscDesc,  // Default sort direction DESC
                Take = pageSize 
            };

            var userClaims = (ClaimsIdentity)User.Identity;
            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            resultTotal = await this._ticketRepository.GetAssignedToTicketsCountAsync(CursorParameters, claimsIdentitifier.Value, type);
            var result = await this._ticketRepository.GetAssignedToTickets(CursorParameters, claimsIdentitifier.Value, type);

            var mappedResult = this._mapper.Map<List<ReadTicketDTO>>(result)
                                .OrderByDescending(t => t.CreatedDate)
                                .ToList();

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
                        .ticket-info p {{ margin: 5px 0; }}
                        .comment {{ background-color: #ffffff; padding: 15px; border-left: 4px solid #0056b3; margin-bottom: 20px; }}
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
                        <h2>New Comment on Ticket #{ticketDbRecord.Id}</h2>
                        <div class='ticket-info'>
                            <p>A new comment has been added to your ticket.</p>
                        </div>
                        <div class='comment'>
                            <strong>Comment:</strong><br>
                            {ticketComment.Comment}
                        </div>
                        <p style='text-align: center;'>
                            <a href='{systemUrl}' class='cta-button' style='color: #ffffff;'>View Full Details</a>
                        </p>
                        <p class='footer'>Thank you for using our service.</p>
                    </div>
                </body>
                </html>";

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

         [HttpGet]
        [Route("officer/tickets/PickTicket/{id}")]
        public async Task<ActionResult> PickTicket(int id)
        {
            //check if the ticket exists
            
            var ticket = await this._ticketRepository.GetTicketWithTracking(id);

            if(ticket == null)
            {
                return Json(new { status = "error", message = "Could not find a ticket with the identifier sent" });
            }   

            //check if ticket is already assigned to a user

            if(!string.IsNullOrEmpty(ticket.AssignedToId))
            {
                return Json(new { status = "error", message = "Ticket is already assigned to a user" });
            }

            //assign the ticket to the current user

            var currentUser = await CurrentUser.GetCurrentUserAsync(this._httpContextAccessor, this._userRepository);


            if(currentUser == null)
            {
                return Json(new { status = "error", message = "Could not find the current user" });
            }

            ticket.AssignedToId = currentUser.Id;
            //save the changes

            int recordsAffected = await this._unitOfWork.SaveToDataStoreSync();

            if(recordsAffected > 0)
            {
                //send an email to the new assignee
                this._ticketRepository.SendTicketPickedEmail(currentUser.Email, ticket);

                return Json(new { status = "success", message = "Ticket picked successfully" });
            }
            else
            {
                return Json(new { status = "error", message = "Could not pick ticket" });
            }


            return View(ticket);
        }

        [HttpPost]
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

                    // Check if the current user is assigned to the ticket or is the creator
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
                        return Json(new { status = "error", message = $"Could not close ticket as you are not currently assigned to it or the creator" });
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

                        //sync changes to the datastore

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
                     dynamicSelect.Add(new DynamicSelect { 
                        Id = item.Id.ToString(), 
                        Name = $"{item.FullName} ({item.AccountNumber}) -- {item.Branch} -- Employee #{item.EmployeeNumber}"
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
