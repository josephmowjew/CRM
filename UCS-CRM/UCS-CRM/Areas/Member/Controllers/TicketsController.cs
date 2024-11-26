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
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Persistence.SQLRepositories;

namespace UCS_CRM.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ITicketCommentRepository _ticketCommentRepository;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IStateRepository _stateRepository;
        private readonly ITicketPriorityRepository _priorityRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ITicketEscalationRepository _ticketEscalationRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly IEmailAddressRepository _addressRepository;
        private readonly ITicketStateTrackerRepository _ticketStateTrackerRepository;
        private readonly HangfireJobEnqueuer _jobEnqueuer;
        private readonly ApplicationDbContext _context;

        private readonly ILogger<TicketsController> _logger;

        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        public TicketsController(ITicketRepository ticketRepository,
            IMapper mapper, 
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IEmailAddressRepository addressRepository,
            ITicketCategoryRepository ticketCategoryRepository,
            IStateRepository stateRepository, 
            ITicketPriorityRepository priorityRepository, 
            IWebHostEnvironment env,
            ITicketCommentRepository ticketCommentRepository,
            IMemberRepository memberRepository, 
            ITicketEscalationRepository ticketEscalationRepository,
            IDepartmentRepository departmentRepository,
            ITicketStateTrackerRepository ticketStateTrackerRepository,
            IUserRepository userRepository,HangfireJobEnqueuer jobEnqueuer,
            ILogger<TicketsController> logger,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _ticketRepository = ticketRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _ticketCategoryRepository = ticketCategoryRepository;
            _stateRepository = stateRepository;
            _priorityRepository = priorityRepository;
            _env = env;
            _ticketCommentRepository = ticketCommentRepository;
            _memberRepository = memberRepository;
            _ticketEscalationRepository = ticketEscalationRepository;
            _emailService = emailService;
            _addressRepository = addressRepository;
            _departmentRepository = departmentRepository;
            _ticketStateTrackerRepository = ticketStateTrackerRepository;
            _userRepository = userRepository;
            _jobEnqueuer = jobEnqueuer;
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: TicketsController
        public async Task<ActionResult> Index(string type = "")
        {
            await populateViewBags();
            ViewBag.type = type;
            var memberId = await GetMemberId();
            ViewBag.MemberId = memberId;

            return View();
        }

       
        // POST: TicketsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateTicketDTO createTicketDTO)
        {
            if (ModelState.IsValid)
            {
                createTicketDTO.DataInvalid = "";
                ViewBag.MemberId = createTicketDTO.InitiatorId;

                //search for the default state

                var defaultState =  this._stateRepository.DefaultState(Lambda.NewTicket);
                var defaultPriority = this._priorityRepository.DefaultPriority(Lambda.Lowest);


                if (defaultState == null || defaultPriority == null)
                {
                    createTicketDTO.DataInvalid = "true";

                    ModelState.AddModelError("", "Sorry but the application failed to log your ticket because of a missing state or priority, please contact administrator for assistance");

                    await populateViewBags();

                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }
                else
                {
                    createTicketDTO.StateId = defaultState.Id;
                    createTicketDTO.TicketPriorityId = defaultPriority.Id;
                }

                //check initiator information 
                var ticketInitiator = createTicketDTO.InitiatorId;

                var ticketIniatorType = createTicketDTO.InitiatorType;

                if(ticketIniatorType.ToLower().Trim() == "member".ToLower().Trim())
                {
                    if (!int.TryParse(ticketInitiator, out int ticketInitiatorMember))
                    {
                        // Set the DataInvalid flag to true and add a model error message
                        createTicketDTO.DataInvalid = "true";
                        ModelState.AddModelError(string.Empty, "Sorry but you do not have a valid account with us, please contact administrator for assistance");

                        // Populate view bags and return the partial view with the DTO
                        await populateViewBags();
                        return PartialView("_CreateTicketPartial", createTicketDTO);
                    }

                }
                else
                {
                    //return an error back to the user 
                    ModelState.AddModelError(string.Empty, "Invalid initiator type");
                    await populateViewBags();
                    return PartialView("_CreateTicketPartial", createTicketDTO);
                }

                

                //check for article title presence

                var mappedTicket = this._mapper.Map<Ticket>(createTicketDTO);

                var customerServiceMemberEngagementDept = this._departmentRepository.Exists("Customer Service and Member Engagement");
                var creditAndEvaluationsDept = this._departmentRepository.Exists("Credit and Evaluations");

                // Get the ticket category
                var ticketCategory = await this._ticketCategoryRepository.GetTicketCategory(createTicketDTO.TicketCategoryId);

                if (ticketCategory?.Name?.Trim().ToLower() == "affordability check")
                {
                    if (creditAndEvaluationsDept != null)
                    {
                        mappedTicket.DepartmentId = creditAndEvaluationsDept.Id;
                    }
                }
                else if (customerServiceMemberEngagementDept != null)
                {
                    mappedTicket.DepartmentId = customerServiceMemberEngagementDept.Id;
                }

                var statePresence = this._ticketRepository.Exists(mappedTicket);

                if (statePresence != null)
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
                    mappedTicket = this._mapper.Map<Ticket>(createTicketDTO);
                    
                    // Set effective creation date considering holidays
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

                        var memberRecord = await this._memberRepository.GetMemberByUserId(mappedTicket.CreatedById);

                        // Send out-of-hours notification email
                        var notificationRecipient = memberRecord ?? await _memberRepository.GetMemberAsync(createTicketDTO.MemberId);
                        
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
                                memberRecord?.User.Email
                            );
                        }
                    }

                    var member = await this._memberRepository.GetMemberByUserId(mappedTicket.CreatedById);

                    //set up the member id
                    mappedTicket.MemberId = member?.Id;

                    //get the last ticket

                    Ticket lastTicket = await this._ticketRepository.LastTicket();


                    //generate ticket number
                    var lastTicketId = lastTicket == null ? 0 : lastTicket.Id;

                    string ticketNumber = Lambda.IssuePrefix + (lastTicketId + 1);

                    //assign ticket number to the mapped record

                    mappedTicket.TicketNumber = ticketNumber;

                    //set ticket initiator

                    mappedTicket.InitiatorMemberId = member?.Id;



                    this._ticketRepository.Add(mappedTicket);


                  

                    //save ticket to the data store

                    await this._unitOfWork.SaveToDataStore();

                    if (createTicketDTO.Attachments.Count > 0)
                    {
                        var attachments = createTicketDTO.Attachments.Select(async attachment =>
                        {
                            string fileUrl = await Lambda.UploadFile(attachment,this._env.WebRootPath);
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
                        

                        //_emailService.SendMail(mappedTicket.Member.Address, "Ticket Creation", emailBody);

                    }
                      //email to send to support
                        var emailAddress = await _addressRepository.GetEmailAddressByOwner(Lambda.Support);

                         var userRecord = await this._userRepository.GetSingleUser(mappedTicket.CreatedById);

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
                                <h2>Ticket Request Submitted</h2>
                                <p>Hello {userRecord.FullName},</p>
                                <div class='ticket-info'>
                                    <p>Your ticket request has been successfully submitted in our system.</p>
                                    <p><strong>Ticket Number:</strong> {mappedTicket.TicketNumber}</p>
                                    <p><strong>Title:</strong> {mappedTicket.Title}</p>
                                </div>
                                <p>You can check the details by clicking the button below:</p>
                                <p style='text-align: center;'>
                                    <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                                </p>
                                <p class='footer'>Thank you for using our service.</p>
                            </div>
                        </body>
                        </html>";
                        
                        EmailHelper.SendEmail(this._jobEnqueuer, userRecord.Email, "Ticket Creation", emailBody, userRecord.SecondaryEmail);   

                        // Update the email to support
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
                                    <div class='ticket-info'>
                                        <p>A new ticket has been created in the system. Here are the details:</p>
                                        <p><strong>Member Name:</strong> {userRecord.FullName}</p>
                                        <p><strong>Ticket Number:</strong> {mappedTicket.TicketNumber}</p>
                                        <p><strong>Title:</strong> {mappedTicket.Title}</p>
                                    </div>
                                    <p>Please review and take necessary action as soon as possible.</p>
                                    <p style='text-align: center;'>
                                        <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                                    </p>
                                    <p class='footer'>Thank you for your prompt attention to this matter.</p>
                                </div>
                            </body>
                            </html>";
                            this._jobEnqueuer.EnqueueEmailJob(emailAddress.Email, "New Ticket Creation", supportEmailBody);
                        }

                    await populateViewBags();

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
            else
            {
                await populateViewBags();

                if (ModelState.ContainsKey("InitiatorId") && ModelState["InitiatorId"].Errors.Any(e => e.ErrorMessage == "The InitiatorId field is required."))
                {
                    ModelState["InitiatorId"]?.Errors.Clear();
                    ModelState.AddModelError("InitiatorId", "Sorry, you cannot create a ticket because you do not have a valid member account. Please contact administrator.");
                    // Retain the InitiatorId value
                    ViewBag.MemberId = createTicketDTO.InitiatorId;

                }
                ViewBag.MemberId = createTicketDTO.InitiatorId;

                return PartialView("_CreateTicketPartial", createTicketDTO);
            }


            
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
        public async Task<ActionResult> Edit(int id, EditTicketDTO editTicketDTO)
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
                    await this.populateViewBags();

                    return PartialView("_EditTicketPartial", this._mapper.Map<EditTicketDTO>(editTicketDTO));
                }

              
                editTicketDTO.StateId = editTicketDTO.StateId == null ? ticketDB.StateId: editTicketDTO.StateId;

                editTicketDTO.TicketNumber = ticketDB.TicketNumber;
                //check if the role name isn't already taken
                var mappedTicket = this._mapper.Map<Ticket>(editTicketDTO);

                var ticketExist = this._ticketRepository.Exists(mappedTicket);

                editTicketDTO.MemberId = ticketDB.MemberId;


              



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

            await this.populateViewBags();

            return PartialView("_EditTicketPartial", this._mapper.Map<EditTicketDTO>(editTicketDTO));

            
        }

        // GET: TicketController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var ticketDB = await this._ticketRepository.GetTicket(id);

            if(ticketDB == null)
            {
                return RedirectToAction("Index");
            }

            var userClaims = (ClaimsIdentity)User.Identity;

            var claimsIdentitifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);

            var currentUserId = claimsIdentitifier.Value;

            ViewBag.CurrentUserId = currentUserId;

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

                if(ticketRecordDb.State.Name.ToLower() != Lambda.NewTicket.ToLower())
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
        public async Task<ActionResult> deleteComment(int id)
        {
            //check if the role name isn't already taken

            var ticketCommentDbRecord = await this._ticketCommentRepository.GetTicketCommentAsync(id);

            if (ticketCommentDbRecord != null)
            {
                //only execute remove if the state is not pending

              
                    this._ticketCommentRepository.Remove(ticketCommentDbRecord);

                    await this._unitOfWork.SaveToDataStore();

                    return Json(new { status = "success", message = "comment has been removed from the system successfully" });
               

            }

            return Json(new { status = "error", message = "comment could not be found from the system" });
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
                    ticket.ClosedDate = DateTime.UtcNow;

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

        [HttpPost]
        public async Task<ActionResult> GetTickets()
        {
            try
            {
                // Datatable parameters
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                var start = HttpContext.Request.Form["start"].FirstOrDefault();
                var length = HttpContext.Request.Form["length"].FirstOrDefault();
                var sortColumn = HttpContext.Request.Form["columns[" + HttpContext.Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                var userClaims = User.Identity as ClaimsIdentity;
                if (userClaims == null)
                {
                    _logger.LogWarning("User claims identity is null");
                    return Json(new { draw = draw, recordsFiltered = 0, recordsTotal = 0, data = new List<ReadTicketDTO>(), error = "User authentication failed" });
                }

                var claimsIdentifier = userClaims.FindFirst(ClaimTypes.NameIdentifier);
                if (claimsIdentifier == null)
                {
                    _logger.LogWarning("NameIdentifier claim not found");
                    return Json(new { draw = draw, recordsFiltered = 0, recordsTotal = 0, data = new List<ReadTicketDTO>(), error = "User identification failed" });
                }

                var member = await _memberRepository.GetMemberByUserId(claimsIdentifier.Value);
                if (member == null)
                {
                    _logger.LogWarning($"Member not found for user ID: {claimsIdentifier.Value}");
                    return Json(new { draw = draw, recordsFiltered = 0, recordsTotal = 0, data = new List<ReadTicketDTO>(), error = "Member account not found" });
                }

                // Create cursor parameters
                CursorParams cursorParams = new CursorParams
                {
                    SearchTerm = searchValue,
                    Skip = skip,
                    SortColum = sortColumn,
                    SortDirection = sortColumnDirection,
                    Take = pageSize
                };

                recordsTotal = await _ticketRepository.TotalCountByMember(member.Id);
                var tickets = await _ticketRepository.GetMemberTickets(cursorParams, member.Id);

                if (tickets == null)
                {
                    _logger.LogWarning($"No tickets found for member ID: {member.Id}");
                    return Json(new { draw = draw, recordsFiltered = 0, recordsTotal = 0, data = new List<ReadTicketDTO>() });
                }

                var mappedResult = this._mapper.Map<List<ReadTicketDTO>>(tickets)
                                .OrderByDescending(t => t.CreatedDate)
                                .ToList();

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = mappedResult });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching tickets");
                return Json(new { draw = "0", recordsFiltered = 0, recordsTotal = 0, data = new List<ReadTicketDTO>(), error = "An unexpected error occurred while fetching tickets" });
            }
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

            if(ticketDbRecord == null)
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
                        <p>
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
        private  async Task<List<SelectListItem>>  GetTicketCategories()
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

        //escalate ticket
        public async Task<ActionResult> Escalate(CreateTicketEscalationDTO createTicketEscalation)
        {
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

                if (ticket != null)
                {
                    await this._ticketRepository.EscalateTicket(ticket, claimsIdentitifier.Value, createTicketEscalation.Reason);


                    // if (result != null)
                    // {
                    //     if (result.Equals("Could not find a user to escalate the ticket to", StringComparison.OrdinalIgnoreCase))
                    //     {
                    //         createTicketEscalation.DataInvalid = "true";

                    //         ModelState.AddModelError("", "Could not find a user to escalate the ticket to");

                    //         return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
                    //     }
                    //     if (result.Equals("ticket escalated", StringComparison.OrdinalIgnoreCase))
                    //     {
                    //         createTicketEscalation.DataInvalid = "";


                    //         return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
                    //     }
                    // }
                    // else
                    // {
                    //     createTicketEscalation.DataInvalid = "true";

                    //     ModelState.AddModelError("", "An error occurred while trying to escalate the ticket");

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



            return PartialView("_FirstTicketEscalationPartial", createTicketEscalation);
        }

        private async Task populateViewBags()
        {
            ViewBag.priorities = await GetTicketPriorities();
            ViewBag.categories = await GetTicketCategories();
        }

        private async Task<int?> GetMemberId()
        {
            var userClaims = (ClaimsIdentity?)User.Identity;

                var claimsIdentitifier = userClaims?.FindFirst(ClaimTypes.NameIdentifier);

                if (claimsIdentitifier != null)
                {

                    var currentUserId = claimsIdentitifier.Value;


                    var member = await this._memberRepository.GetMemberByUserId(currentUserId);

                    return member?.Id;

                }

                return null;
        }
    }
}
