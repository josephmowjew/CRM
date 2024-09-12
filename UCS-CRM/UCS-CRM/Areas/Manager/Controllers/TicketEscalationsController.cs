using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using System.Security.Claims;
using UCS_CRM.Core.DTOs.Ticket;
using UCS_CRM.Core.DTOs.TicketComment;
using UCS_CRM.Core.DTOs.TicketEscalation;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Persistence.SQLRepositories;

namespace UCS_CRM.Areas.Manager.Controllers
{
    [Area("Manager")]
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
        private readonly IEmailAddressRepository _addressRepository;
        private readonly IEmailService _emailService;
        private readonly HangfireJobEnqueuer _jobEnqueuer;
        private readonly ITicketCommentRepository _ticketCommentRepository;
        public TicketEscalationsController(ITicketEscalationRepository ticketEscalationRepository, IMapper mapper, IUnitOfWork unitOfWork, ITicketRepository ticketRepository, IEmailService emailService,
            IWebHostEnvironment env, IStateRepository stateRepository, ITicketPriorityRepository priorityRepository, IMemberRepository memberRepository, IUserRepository userRepository, IEmailAddressRepository addressRepository,
            ITicketCategoryRepository ticketCategoryRepository, ITicketCommentRepository ticketCommentRepository, HangfireJobEnqueuer jobEnqueuer)
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
            _emailService = emailService;
            _addressRepository = addressRepository;
            _ticketCommentRepository = ticketCommentRepository;
            _jobEnqueuer = jobEnqueuer;
        }

        // GET: TicketEscalationsController
        public async Task<ActionResult> First()
        {
            await populateViewBags();
            return View();
        }

        // GET: TicketEscalationsController/Details/5
    

        // GET: TicketEscalationsController/Create
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
                return RedirectToAction("First");
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
                            <h2>Ticket Escalation Notice</h2>
                            <div class='ticket-info'>
                                <p>A ticket has been escalated in the system.</p>
                                <p>Please review the escalated ticket by accessing the system.</p>
                            </div>
                            <p style='text-align: center;'>
                                <a href='{Lambda.systemLinkClean}' class='cta-button'>View Escalated Ticket</a>
                            </p>
                            <p class='footer'>Thank you for your prompt attention to this matter.</p>
                        </div>
                    </body>
                    </html>";

                    //email to send to support
                    var emailAddress = await _addressRepository.GetEmailAddressByOwner(Lambda.SeniorManager);
                    this._jobEnqueuer.EnqueueEmailJob(emailAddress.Email, "Ticket Escalation", emailBody);
                    

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
                            <a href='{Lambda.systemLinkClean}' class='cta-button'>View Ticket Details</a>
                        </p>
                        <p class='footer'>Thank you for your prompt attention to this matter.</p>
                    </div>
                </body>
                </html>";

                //email to send to support

                var user = await _userRepository.GetSingleUser(ticketDB.CreatedById);

                if (user != null)
                {
                    EmailHelper.SendEmail(this._jobEnqueuer, user.Email, "Ticket Escalation", emailBody, user.SecondaryEmail);                   
                }
                var emailAddress = await _addressRepository.GetEmailAddressByOwner(Lambda.SeniorManager);

                this._jobEnqueuer.EnqueueEmailJob(emailAddress.Email, "Ticket Escalation", emailBody);
               

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



                //if (ticketEscalationPresence != null)
                //{
                //    createTicketEscalation.DataInvalid = "true";

                //    ModelState.AddModelError(nameof(mappedTicketEscalation.Ticket.Title), $"Another ticket exists with the parameters submitted'");

                //    return PartialView("_SecondTicketEscalationPartial", createTicketEscalation);
                //}


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

                //find record of the currently logged in user

                ApplicationUser currentLoggedInUser = await this._userRepository.FindByEmailsync(User.Identity.Name);

                int? currentUserDepartmentId = currentLoggedInUser.DepartmentId;

                //create a cursor params based on the data coming from the datatable
                CursorParams CursorParameters = new CursorParams() { SearchTerm = searchValue, Skip = skip, SortColum = sortColumn, SortDirection = sortColumnAscDesc, Take = pageSize };
                
                List<TicketEscalation>? repoTicketEscalations = await this._ticketEscalationRepository.GetTicketEscalations(currentUserDepartmentId, CursorParameters);


                //get total records from the database
                resultTotal = await this._ticketEscalationRepository.GetTicketEscalationsCount(null,CursorParameters);
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
