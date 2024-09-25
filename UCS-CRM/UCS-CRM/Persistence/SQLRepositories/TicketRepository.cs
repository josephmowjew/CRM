using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using AutoMapper.Execution;
using Microsoft.AspNetCore.Routing;
using UCS_CRM.Core.Services;
using System.Net.Mail;
using System.Security.Claims;
using AutoMapper;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.AspNetCore.Identity;
using System.Web.WebPages;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Hangfire;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailRepository _emailRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITicketEscalationRepository _ticketEscalationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HangfireJobEnqueuer _jobEnqueuer;

       


        public TicketRepository(
            ApplicationDbContext context,
            IEmailRepository emailRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IDepartmentRepository departmentRepository,
            IMapper mapper,
            ITicketEscalationRepository ticketEscalationRepository,
            UserManager<ApplicationUser> userManager,
            HangfireJobEnqueuer jobEnqueuer)
        {
            _context = context;
            
            _emailRepository = emailRepository;
            _userRepository = userRepository;
            _departmentRepository = departmentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _ticketEscalationRepository = ticketEscalationRepository;
            this._userManager = userManager;
            this._jobEnqueuer = jobEnqueuer;

        }

        public void Add(Ticket ticket)
        {
          
            this._context.Tickets.Add(ticket);
        }

        public Ticket? Exists(Ticket ticket)
        {
            //check if the ticket already exist

            return this._context.Tickets.Where(t => t.TicketCategoryId == ticket.TicketCategoryId & t.State== ticket.State).FirstOrDefault();

           
        }

        public async Task EscalateTicket(Ticket ticket, string UserId, string escalationReason)
        {
            ApplicationUser currentAssignedUser = null;
            string currentAssignedUserEmail = string.Empty;
            Role currentAssignedUserRole = null;
            Department? currentAssignedUserDepartment = null;
            List<Role> rolesOfCurrentUserDepartment = new();
            List<Role> SortedrolesOfCurrentUserDepartment = new();
            bool ticketAssignedToNewUser = false;
            ApplicationUser newOfficialTicketHandler = null;

            if (ticket != null)
            {
                //get the user assigned to the ticket if available
                currentAssignedUser = ticket.AssignedTo;

                currentAssignedUserEmail = currentAssignedUser.Email;

                if (currentAssignedUser != null)
                {
                    currentAssignedUserRole = await this._userRepository.GetRoleAsync(currentAssignedUser.Id);

                    //check if the role of the user has been returned 

                    if (currentAssignedUserRole != null)
                    {

                        //get the department of the current assigned user
                        currentAssignedUserDepartment = await this._departmentRepository.GetDepartment(currentAssignedUser.Department.Id);

                        //get roles that are associated with this department
                        rolesOfCurrentUserDepartment = currentAssignedUserDepartment.Roles;

                        //order the roles according to rating

                        SortedrolesOfCurrentUserDepartment = rolesOfCurrentUserDepartment.OrderBy(d => d.Rating).ToList();


                        //loop through the list of roles in the current department

                        if (SortedrolesOfCurrentUserDepartment.Count > 0)
                        {
                            //remove roles that are less than the one that the current assigned user is already in
                            SortedrolesOfCurrentUserDepartment = SortedrolesOfCurrentUserDepartment.Where(r => r.Rating > currentAssignedUserRole.Rating).ToList();

                            if (SortedrolesOfCurrentUserDepartment.Count > 0)
                            {
                                for (int i = 0; i < SortedrolesOfCurrentUserDepartment.Count; i++)
                                {

                                    var listOfUsers = await this._userRepository.GetUsersInRole(SortedrolesOfCurrentUserDepartment[i].Name);

                                    //filter users to only those on the same branch
                                    listOfUsers = listOfUsers.Where(u => u.BranchId == currentAssignedUser.BranchId).ToList();

                                    //get the first user if available

                                    var newTicketHandler = listOfUsers.FirstOrDefault();

                                    if (newTicketHandler != null)
                                    {
                                        //assign the ticket this person and break out of the loop

                                        ticket.AssignedToId = newTicketHandler.Id;

                                        //assign the ticket to a new department

                                        ticket.DepartmentId = newTicketHandler.DepartmentId;

                                        newOfficialTicketHandler = newTicketHandler;

                                        ticketAssignedToNewUser = true;

                                        //break out of the loop
                                        break;
                                    }

                                }

                            }
                            else
                            {
                                //assign the ticket to a manager with a role rating higher than the current user even if the manager is in a different department but same branch


                                //check if the ticket is already in the the branch networks and satellites department

                                if(currentAssignedUserDepartment.Name.Equals("Branches",StringComparison.OrdinalIgnoreCase))
                                {
                                    ticket.AssignedToId = await this.AssignTicketToDepartment("Customer Service and Member Engagement");

                                    if (!string.IsNullOrEmpty(ticket.AssignedToId))
                                    {

                                        var customerServiceMemberEngagementDept = this._departmentRepository.Exists("Customer Service and Member Engagement");

                                        if (customerServiceMemberEngagementDept != null)
                                        {
                                            ticket.DepartmentId = customerServiceMemberEngagementDept.Id;
                                        }
                                        ticketAssignedToNewUser = true;

                                       
                                    }
                                }

                                else if (currentAssignedUserDepartment.Name.Trim().ToLower() == "Branch Networks and satellites Department".Trim().ToLower())
                                {
                                    ticket.AssignedToId = await this.AssignTicketToDepartment("Executive suite");

                                    if (!string.IsNullOrEmpty(ticket.AssignedToId))
                                    {

                                        var customerServiceMemberEngagementDept = this._departmentRepository.Exists("Executive suite");

                                        if (customerServiceMemberEngagementDept != null)
                                        {
                                            ticket.DepartmentId = customerServiceMemberEngagementDept.Id;
                                        }
                                        ticketAssignedToNewUser = true;
                                    }

                                }
                                else
                                {
                                    ticket.AssignedToId = await this.AssignTicketToDepartment("Branch Networks and satellites Department");

                                    if (!string.IsNullOrEmpty(ticket.AssignedToId))
                                    {
                                        var customerServiceMemberEngagementDept = this._departmentRepository.Exists("Branch Networks and satellites Department");

                                        if (customerServiceMemberEngagementDept != null)
                                        {
                                            ticket.DepartmentId = customerServiceMemberEngagementDept.Id;
                                        }

                                        ticketAssignedToNewUser = true;
                                    }

                                }


                            }

                        }

                    }
                    else
                    {
                        //Do something if the current user has no role
                    }
                }
                else
                {
                    //do something is the ticket is not assigned to anyone

                     await this.SendUnassignedTicketEmail(ticket);
                }

            }

            if (ticketAssignedToNewUser != true)
            {

                //return "Could not find a user to escalate the ticket to";
            }
            else
            {
                //map the create ticket escalation DTO to ticket escalation

                var mappedTicketEscalation = new TicketEscalation() { TicketId = ticket.Id, Reason = escalationReason };

                //update the escalated to to reflect to new user assigned to the ticket

                //mappedTicketEscalation.EscalatedTo = ticket.AssignedTo;

                newOfficialTicketHandler = await this._userRepository.FindByIdAsync(ticket.AssignedToId);

                mappedTicketEscalation.EscalatedTo = newOfficialTicketHandler;

                


                //save to the database

                try
                {
                    //check if userId has been passed

                    if (string.IsNullOrEmpty(UserId))
                    {
                        //get the system user

                         ApplicationUser? systemUser = (await this._userRepository.GetUsersInRole("system")).FirstOrDefault();

                        if (systemUser != null)
                        {
                            UserId = systemUser.Id;
                        }
                        else
                        {
                            //return "system user not found";
                        }


                    }

                    mappedTicketEscalation.CreatedById = UserId;
                    mappedTicketEscalation.DateEscalated = DateTime.Now;


                    this._ticketEscalationRepository.Add(mappedTicketEscalation);

                    // Attach the ticket to the context and set its state to Modified
                    // Detach the existing entry if it is not in the Modified state
                    var existingEntry = _context.ChangeTracker.Entries<Ticket>().FirstOrDefault(e => e.Entity.Id == ticket.Id);
                    if (existingEntry != null && existingEntry.State != EntityState.Modified)
                    {
                        existingEntry.State = EntityState.Detached;
                    }

                    // Attach the ticket to the context and set its state to Modified
                    this._context.Entry(ticket).State = EntityState.Modified;

                    await this._unitOfWork.SaveToDataStore();


                    //send emails to previous assignee and the new assignee

                     await this.SendTicketEscalationEmail(ticket, mappedTicketEscalation, currentAssignedUserEmail);


                    //return "ticket escalated";
                }
              
                catch (Exception ex)
                {

                    //return null;
                }
            }

        }

        public async Task SendTicketReminders()
        {

            // Get all active tickets
            List<Ticket> tickets = await this._context.Tickets
                .Include(t => t.State)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketEscalations)
                .Include(t => t.AssignedTo)
                .ThenInclude(u => u.Department)
                .Where(t => t.Status != Lambda.Deleted &&
                            t.AssignedTo != null &&
                            t.State.Name != Lambda.Closed &&
                            t.State.Name != Lambda.Resolved &&
                            t.State.Name != Lambda.Archived)
                .ToListAsync();


            //loop through the tickets
            foreach (Ticket ticket in tickets)
            {
                bool hasEscalations = ticket.TicketEscalations.Any();
                var creationTime = hasEscalations ? ticket.TicketEscalations.Last().CreatedDate : ticket.CreatedDate;
                var ticketPriorityMaxReponseTimeInHours = ticket.TicketPriority.MaximumResponseTimeHours;
                var escalationTime = creationTime.AddHours(ticketPriorityMaxReponseTimeInHours);

                if (DateTime.UtcNow > escalationTime)
                {
                    await this.EscalateTicket(ticket, null, "Previous assignee did not respond in time");
                }
                else if (ticket.AssignedTo != null)
                {
                    try
                    {
                        string title = "Ticket Reminder";
                        string body = $@"
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
                                <h2>Ticket Reminder</h2>
                                <div class='ticket-info'>
                                    <p>Dear {ticket.AssignedTo.Email},</p>
                                    <p>This is a friendly reminder that ticket number <strong>{ticket.TicketNumber}</strong> has been assigned to you and is still pending a response.</p>
                                    <p>Please take action on this ticket as soon as possible.</p>
                                </div>
                                <p>
                                    <a href='https://crm.ucssacco.com' class='cta-button' style='color: #ffffff;'>View Ticket</a>
                                </p>
                                <p class='footer'>Thank you for your prompt attention to this matter.</p>
                            </div>
                        </body>
                        </html>";

                        EmailHelper.SendEmail(_jobEnqueuer, ticket.AssignedTo.Email, title, body, ticket.AssignedTo?.SecondaryEmail);
                        
                        //send to department
                        this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Department.Name, title, body);
                    }
                    catch (Exception ex)
                    {
                        var errorLog = new ErrorLog
                        {
                            UserFriendlyMessage = "An error occurred while sending ticket reminder.",
                            DetailedMessage = ex.ToString(),
                            DateOccurred = DateTime.UtcNow,
                            CreatedById = ticket.AssignedTo.Id
                        };
                        
                        _context.ErrorLogs.Add(errorLog);
                        // Assuming you have a method to save the error log
                        await _unitOfWork.SaveToDataStore();
                    }
                }
            }

          
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

            if (newDepartment == null)
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
                    //Note for later, get the user with least assigned on closed tickets assigned to them

                    var newTicketHandler = listOfUsers.FirstOrDefault();

                    if (newTicketHandler != null)
                    {
                        //assign the ticket this person and break out of the loop

                        assignedToId = newTicketHandler.Id;

                        if(!string.IsNullOrEmpty(assignedToId))
                        {

                            return assignedToId;
                        };
                    }

                }

            }

            return assignedToId;
        }
        public async Task<Ticket?> GetTicket(int id)
        {
            // Retrieve the ticket using AsNoTracking for performance
            var ticket = await this._context.Tickets
                .Include(t => t.TicketCategory)
                .Include(t => t.State)
                .Include(t => t.TicketComments)
                .Include(t => t.TicketAttachments)
                .Include(t => t.TicketPriority)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo).ThenInclude(a => a.Department)
                .Include(t => t.Member).ThenInclude(t => t.User)
                .Include(t => t.InitiatorMember)
                .Include(t => t.InitiatorUser)
                .ThenInclude(t => t!.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

           

            return ticket;


        }
        public async Task SendTicketClosureNotifications(Ticket ticket, string reason)
        {
            string status = string.Empty;
            //get the member email address

            var memberEmailAddress = ticket?.Member?.User?.Email ?? null;
            var ticketCreatorAddress = ticket.CreatedBy.Email;
            var ticketInitiatorAddress = ticket.InitiatorUser?.Email ?? null;

            if (!string.IsNullOrEmpty(memberEmailAddress) || (!string.IsNullOrEmpty(ticketCreatorAddress) && memberEmailAddress != ticketCreatorAddress))
            {
                try
                {
                    string title = "Ticket Closure Alert";
                    var bodyBuilder = new StringBuilder();
                    
                    if (!string.IsNullOrEmpty(memberEmailAddress))
                    {
                        bodyBuilder.Append($@"
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
                                <h2>Ticket Closure Notification</h2>
                                <div class='ticket-info'>
                                    <p>Your ticket with reference number: {ticket.TicketNumber} has been closed.</p>
                                    <p>Reason: {reason}</p>
                                    <p>If you are not satisfied with the outcome, you have the option to re-open it.</p>
                                </div>
                                <p style='text-align: center;'>
                                    <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                                </p>
                                <p class='footer'>If you have any questions, please contact our support team.</p>
                            </div>
                        </body>
                        </html>");
                        
                        EmailHelper.SendEmail(_jobEnqueuer, ticket.AssignedTo.Email, title, bodyBuilder.ToString(), ticket.AssignedTo?.SecondaryEmail);
                    }
                    
                    if (!string.IsNullOrEmpty(ticketCreatorAddress) && memberEmailAddress != ticketCreatorAddress)
                    {
                        bodyBuilder.Clear()
                                   .Append($@"
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
                                <h2>Ticket Closure Notification</h2>
                                <div class='ticket-info'>
                                    <p>You have closed ticket: {ticket.TicketNumber}</p>
                                    <p>Reason: {reason}</p>
                                </div>
                                <p style='text-align: center;'>
                                    <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                                </p>
                                <p class='footer'>If you have any questions, please contact our support team.</p>
                            </div>
                        </body>
                        </html>");
                        EmailHelper.SendEmail(_jobEnqueuer, ticketCreatorAddress, title, bodyBuilder.ToString(), ticket.AssignedTo?.SecondaryEmail);
                    }
    
                    if (!string.IsNullOrEmpty(ticketInitiatorAddress) && ticketInitiatorAddress != ticketCreatorAddress && ticketInitiatorAddress != memberEmailAddress)
                    {
                        bodyBuilder.Clear()
                                   .Append($@"
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
                                <h2>Ticket Closure Notification</h2>
                                <div class='ticket-info'>
                                    <p>Your ticket with reference number: {ticket.TicketNumber} has been closed.</p>
                                    <p>Reason: {reason}</p>
                                    <p>If you are not satisfied with the outcome, you have the option to re-open it.</p>
                                </div>
                                <p style='text-align: center;'>
                                    <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                                </p>
                                <p class='footer'>If you have any questions, please contact our support team.</p>
                            </div>
                        </body>
                        </html>");
                        EmailHelper.SendEmail(_jobEnqueuer, ticketInitiatorAddress, title, bodyBuilder.ToString(), ticket.AssignedTo?.SecondaryEmail);
                    }

                    status = "email sent";
                }
                catch (Exception ex)
                {
                    var errorLog = new ErrorLog
                    {
                        UserFriendlyMessage = "An error occurred while sending the ticket closure email.",
                        DetailedMessage = ex.ToString(),
                        DateOccurred = DateTime.UtcNow,
                        CreatedById = ticket.CreatedById
                    };

                    await _context.ErrorLogs.AddAsync(errorLog);
                    await _context.SaveChangesAsync();

                    status = "email sending failed";
                }
            }


            //return status;

        }
        public async Task SendTicketReopenedNotifications(Ticket ticket, string reason)
        {
            if (ticket == null || string.IsNullOrEmpty(reason))
            {
                return;
            }

            var memberEmailAddress = ticket.Member?.User?.Email;
            var ticketCreatorAddress = ticket.CreatedBy?.Email;

            if (string.IsNullOrEmpty(memberEmailAddress) && string.IsNullOrEmpty(ticketCreatorAddress))
            {
                return;
            }

            const string title = "Ticket Reopened Alert";

            async Task SendEmailAndLogError(string recipient, string emailBody, string errorMessage, string createdById)
            {
                try
                {
                    EmailHelper.SendEmail(_jobEnqueuer, recipient, title, emailBody, ticket.AssignedTo?.SecondaryEmail);
                }
                catch (Exception ex)
                {
                    var errorLog = new ErrorLog
                    {
                        UserFriendlyMessage = errorMessage,
                        DetailedMessage = ex.ToString(),
                        DateOccurred = DateTime.UtcNow,
                        CreatedById = createdById
                    };
                    _context.ErrorLogs.Add(errorLog);
                    await _context.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrEmpty(memberEmailAddress))
            {
                string bodyMember = $@"
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
                        <h2>Ticket Reopened</h2>
                        <div class='ticket-info'>
                            <p>Your ticket with reference number: {ticket.TicketNumber} has been reopened.</p>
                            <p>Reason: {reason}</p>
                        </div>
                        <p style='text-align: center;'>
                            <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                        </p>
                        <p class='footer'>If you have any questions, please contact our support team.</p>
                    </div>
                </body>
                </html>";
                await SendEmailAndLogError(ticket.AssignedTo.Email, bodyMember,
                    "Failed to send ticket reopened alert email", ticket.AssignedTo.Id);
            }

            if (!string.IsNullOrEmpty(ticketCreatorAddress) && memberEmailAddress != ticketCreatorAddress)
            {
                string bodyCreator = $@"
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
                        <h2>Ticket Reopened</h2>
                        <div class='ticket-info'>
                            <p>You have reopened ticket: {ticket.TicketNumber}.</p>
                            <p>Reason for reopening: {reason}</p>
                        </div>
                        <p style='text-align: center;'>
                            <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                        </p>
                        <p class='footer'>If you have any questions, please contact our support team.</p>
                    </div>
                </body>
                </html>";
                await SendEmailAndLogError(ticketCreatorAddress, bodyCreator,
                    "Failed to send ticket reopened alert email to creator", ticket.CreatedBy?.Id);
            }
        }

         private IQueryable<Ticket> GetBaseQuery(CursorParams @params, Department department = null, string ticketStatus = "")
        {
            var query = _context.Tickets.Where(t => t.Status != Lambda.Deleted);

            if (!string.IsNullOrEmpty(ticketStatus))
            {
                var ticketStatusLower = ticketStatus.Trim().ToLower();
                query = query.Where(t => t.State.Name.ToLower() == ticketStatusLower);
            }

            if (department != null)
            {
                query = query.Where(t => t.DepartmentId == department.Id);
            }

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                var searchTermLower = @params.SearchTerm.ToLower().Trim();
                query = query.Where(t =>
                    EF.Functions.Like(t.Title.ToLower(), $"%{searchTermLower}%") ||
                    EF.Functions.Like(t.Description.ToLower(), $"%{searchTermLower}%") ||
                    EF.Functions.Like(t.State.Name.ToLower(), $"%{searchTermLower}%") ||
                    EF.Functions.Like(t.Member.FirstName.ToLower(), $"%{searchTermLower}%") ||
                    EF.Functions.Like(t.Member.LastName.ToLower(), $"%{searchTermLower}%") ||
                    EF.Functions.Like(t.TicketCategory.Name.ToLower(), $"%{searchTermLower}%")
                );
            }

            return query;
        }

       public async Task<List<Ticket>> GetTickets(CursorParams @params, Department department = null, string ticketStatus = "")
      {
        if (@params.Take <= 0)
        {
            return new List<Ticket>();
        }

        var query = GetBaseQuery(@params, department, ticketStatus);

        // Apply sorting
        if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
        {
            query = query.ApplySorting(@params.SortColum, @params.SortDirection);
        }
        else
        {
            query = query.OrderBy(t => t.CreatedDate);
        }

        // Include necessary related entities
        query = query
            .Include(t => t.TicketEscalations)
            .Include(t => t.Member)
            .Include(t => t.TicketPriority)
            .Include(t => t.TicketCategory)
            .Include(t => t.State)
            .Include(t => t.InitiatorUser)
            .Include(t => t.InitiatorUser!.Department)
            .Include(t => t.InitiatorMember)
            .Include(t => t.AssignedTo);

        query = query.OrderByDescending(t => t.CreatedDate); // Changed to OrderByDescending

        // Apply pagination and select fields
        var result = await query
            .Skip(@params.Skip)
            .Take(@params.Take)
            .Select(t => new Ticket
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                Member = new UCS_CRM.Core.Models.Member 
                { 
                    AccountNumber = t.Member.AccountNumber,
                    Branch = t.Member.Branch,
                },
                TicketPriority = t.TicketPriority,
                TicketCategory = t.TicketCategory,
                State = t.State,
                CreatedDate = t.CreatedDate,
                AssignedTo = t.AssignedTo != null ? new ApplicationUser { FullName = t.AssignedTo.FullName } : null,
                TicketEscalations = t.TicketEscalations.Select(te => new TicketEscalation { Id = te.Id, Status = te.Status }).ToList(),
                InitiatorMemberId = t.InitiatorMemberId,
                InitiatorUserId = t.InitiatorUserId,
                InitiatorUser = t.InitiatorUser != null ? new ApplicationUser 
                { 
                    Id = t.InitiatorUser.Id, 
                    FullName = t.InitiatorUser.FullName,
                    FirstName = t.InitiatorUser.FirstName,
                    LastName = t.InitiatorUser.LastName,
                    Department = t.InitiatorUser.Department != null ? new Department { Name = t.InitiatorUser.Department.Name } : null
                } : null,
                InitiatorMember = t.InitiatorMember != null ? new UCS_CRM.Core.Models.Member 
                { 
                    Id = t.InitiatorMember.Id, 
                    AccountNumber = t.InitiatorMember.AccountNumber,
                    FirstName = t.InitiatorMember.FirstName,
                    LastName = t.InitiatorMember.LastName
                } : null
            })
            .ToListAsync();

        return result;
    }

    public async Task<int> GetTicketsTotalFilteredAsync(CursorParams @params, Department department = null, string ticketStatus = "")
    {
        if (@params.Take <= 0)
        {
            return 0;
        }

        var query = GetBaseQuery(@params, department, ticketStatus);
        return await query.CountAsync();
    }
        public async Task<List<Ticket>> GetMemberEngagementOfficerReport(DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0, int departmentId = 0)
        {
            List<Ticket> MemberTicketList = new();
            //get all user that are in a member engagement role

            List<ApplicationUser> memberEngagementUsers = (await this._userManager.GetUsersInRoleAsync(Lambda.MemberEngagementsOfficer)).ToList();

            List<Ticket?> finalRecords = new List<Ticket?>();

                IQueryable<Ticket> query = this._context.Tickets
                    .Where(t => t.Status != Lambda.Deleted);

                if (categoryId > 0)
                {
                    query = query.Where(t => t.TicketCategoryId == categoryId);
                }

                if (stateId > 0)
                {
                    query = query.Where(t => t.StateId == stateId);
                }

                if (startDate != null)
                {
                    query = query.Where(t => t.CreatedDate >= startDate);
                }

                if (endDate != null)
                {
                    query = query.Where(t => t.CreatedDate <= endDate);
                }

                if (departmentId > 0)
                {
                    query = query.Where(t => t.AssignedTo.DepartmentId == departmentId);
                }



            if (!string.IsNullOrEmpty(branch))
                {
                    query = query.Where(t => t.Member.Branch == branch);
                }

                query = query.OrderByDescending(t => t.CreatedDate); // Changed to OrderByDescending

               

                finalRecords = await query
                    .Include(t => t.AssignedTo)
                    .Include(t => t.State)
                    .Include(t => t.TicketCategory)
                    .Include(t => t.TicketPriority)
                    .Include(t => t.InitiatorUser)
                    .ThenInclude(t => t!.Department)
                    .Include(t => t.InitiatorMember)
                    .ToListAsync();

            

            finalRecords.ForEach(finalRecord =>
            {
                if (memberEngagementUsers.Contains(finalRecord.AssignedTo))
                {
                    MemberTicketList.Add(finalRecord);
                }
            });


            return MemberTicketList;
        }


        //ticket reports
        public async Task<List<Ticket?>> GetTicketReports(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0)
        {
            List<Ticket?> finalRecords = new List<Ticket?>();

            if (cursorParams.Take > 0)
            {
                IQueryable<Ticket> query = this._context.Tickets
                    .OrderByDescending(t => t.CreatedDate) // Changed to OrderByDescending
                    .Where(t => t.Status != Lambda.Deleted);

                if (categoryId > 0)
                {
                    query = query.Where(t => t.TicketCategoryId == categoryId);
                }

                if (stateId > 0)
                {
                    query = query.Where(t => t.StateId == stateId);
                }

                if (startDate !=null)
                {
                    query = query.Where(t => t.CreatedDate >= startDate);
                }

                if (endDate != null)
                {
                    query = query.Where(t => t.CreatedDate <= endDate);
                }

                if (!string.IsNullOrEmpty(cursorParams.SearchTerm))
                {
                    string searchTermLower = cursorParams.SearchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.Title.ToLower().Trim().Contains(searchTermLower) ||
                        t.Description.ToLower().Trim().Contains(searchTermLower) ||
                        t.State.Name.ToLower().Trim().Contains(searchTermLower) ||
                        t.Member.FirstName.ToLower().Trim().Contains(searchTermLower) ||
                        t.Member.LastName.ToLower().Trim().Contains(searchTermLower) ||
                        t.TicketCategory.Name.ToLower().Trim().Contains(searchTermLower));
                }

                if (!string.IsNullOrEmpty(branch))
                {
                    query = query.Where(t => t.Member.Branch == branch);
                }

                query = query.OrderBy(t => t.CreatedDate);

                if (!string.IsNullOrEmpty(cursorParams.SortColum) && !string.IsNullOrEmpty(cursorParams.SortDirection))
                {
                    query = query.OrderBy(cursorParams.SortColum + " " + cursorParams.SortDirection);
                }

                if (cursorParams.Skip > 0)
                {
                    query = query.Skip(cursorParams.Skip);
                }

                finalRecords = await query.Take(cursorParams.Take)
                    .Include(t => t.Member)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.TicketAttachments)
                    .Include(t => t.State)
                    .Include(t => t.TicketCategory)
                    .Include(t => t.TicketPriority)
                    .Include(t => t.TicketEscalations)
                    .Include(t => t.InitiatorUser)
                    .ThenInclude(t => t!.Department)
                    .Include(t => t.InitiatorMember)
                    .ToListAsync();
            }

            return finalRecords;
        }

        public async Task<int> GetTicketReportsCount(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int stateId = 0, int categoryId = 0)
        {
           int finalRecords = 0;

            if (cursorParams.Take > 0)
            {
                IQueryable<Ticket> query = this._context.Tickets
                    .OrderByDescending(t => t.CreatedDate) // Changed to OrderByDescending
                    .Where(t => t.Status != Lambda.Deleted);

                if (categoryId > 0)
                {
                    query = query.Where(t => t.TicketCategoryId == categoryId);
                }

                if (stateId > 0)
                {
                    query = query.Where(t => t.StateId == stateId);
                }

                if (startDate != null)
                {
                    query = query.Where(t => t.CreatedDate >= startDate);
                }

                if (endDate != null)
                {
                    query = query.Where(t => t.CreatedDate <= endDate);
                }

                if (!string.IsNullOrEmpty(cursorParams.SearchTerm))
                {
                    string searchTermLower = cursorParams.SearchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.Title.ToLower().Trim().Contains(searchTermLower) ||
                        t.Description.ToLower().Trim().Contains(searchTermLower) ||
                        t.State.Name.ToLower().Trim().Contains(searchTermLower) ||
                        t.Member.FirstName.ToLower().Trim().Contains(searchTermLower) ||
                        t.Member.LastName.ToLower().Trim().Contains(searchTermLower) ||
                        t.TicketCategory.Name.ToLower().Trim().Contains(searchTermLower));
                }

                if (!string.IsNullOrEmpty(branch))
                {
                    query = query.Where(t => t.Member.Branch == branch);
                }

                query = query.OrderBy(t => t.CreatedDate);

                if (!string.IsNullOrEmpty(cursorParams.SortColum) && !string.IsNullOrEmpty(cursorParams.SortDirection))
                {
                    query = query.OrderBy(cursorParams.SortColum + " " + cursorParams.SortDirection);
                }

               
                finalRecords = await query.CountAsync();
            }

            return finalRecords;
        }

        //ticket reports
        public async Task<List<Ticket?>> GetEscalatedTicketsData(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0)
        {
            List<Ticket?> finalRecords = new List<Ticket?>();

            if (cursorParams.Take > 0)
            {
                IQueryable<Ticket> query = this._context.Tickets
                    .Where(t => t.Status != Lambda.Deleted)
                    .OrderByDescending(t => t.CreatedDate); // Changed to OrderByDescending

                if (categoryId > 0)
                {
                    query = query.Where(t => t.TicketCategoryId == categoryId);
                }

               
                if (startDate != null)
                {
                    query = query.Where(t => t.CreatedDate >= startDate);
                }

                if (endDate != null)
                {
                    query = query.Where(t => t.CreatedDate <= endDate);
                }

                if (!string.IsNullOrEmpty(cursorParams.SearchTerm))
                {
                    string searchTermLower = cursorParams.SearchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.Title.ToLower().Trim().Contains(searchTermLower) ||
                        t.Description.ToLower().Trim().Contains(searchTermLower) ||
                        t.State.Name.ToLower().Trim().Contains(searchTermLower) ||
                        t.Member.FirstName.ToLower().Trim().Contains(searchTermLower) ||
                        t.Member.LastName.ToLower().Trim().Contains(searchTermLower) ||
                        t.TicketCategory.Name.ToLower().Trim().Contains(searchTermLower));
                }

                if (!string.IsNullOrEmpty(branch))
                {
                    query = query.Where(t => t.Member.Branch == branch);
                }

                query = query.OrderBy(t => t.CreatedDate);

                if (!string.IsNullOrEmpty(cursorParams.SortColum) && !string.IsNullOrEmpty(cursorParams.SortDirection))
                {
                    query = query.OrderBy(cursorParams.SortColum + " " + cursorParams.SortDirection);
                }

                if (cursorParams.Skip > 0)
                {
                    query = query.Skip(cursorParams.Skip);
                }

                finalRecords = await query.Take(cursorParams.Take)
                    .Include(t => t.Member)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.TicketAttachments)
                    .Include(t => t.State)
                    .Include(t => t.TicketCategory)
                    .Include(t => t.TicketPriority)
                    .Include(t => t.CreatedBy)
                    .Include(t => t.TicketEscalations)
                    .ThenInclude(ts => ts.EscalatedTo)
                    .Include(t => t.InitiatorUser)
                    .ThenInclude(t => t!.Department)
                    .Include(t => t.InitiatorMember)
                    .ToListAsync();
            }

            return finalRecords;
        }

        public async Task<int> GetEscalatedTicketsDataCountAsync(CursorParams cursorParams, DateTime? startDate, DateTime? endDate, string branch = "", int categoryId = 0)
        {
            int finalRecords = 0;

            if (cursorParams.Take > 0)
            {
                IQueryable<Ticket> query = this._context.Tickets
                    .Where(t => t.Status != Lambda.Deleted)
                    .OrderByDescending(t => t.CreatedDate); // Changed to OrderByDescending

                if (categoryId > 0)
                {
                    query = query.Where(t => t.TicketCategoryId == categoryId);
                }


                if (startDate != null)
                {
                    query = query.Where(t => t.CreatedDate >= startDate);
                }

                if (endDate != null)
                {
                    query = query.Where(t => t.CreatedDate <= endDate);
                }

                if (!string.IsNullOrEmpty(cursorParams.SearchTerm))
                {
                    string searchTermLower = cursorParams.SearchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.Title.ToLower().Trim().Contains(searchTermLower) ||
                        t.Description.ToLower().Trim().Contains(searchTermLower) ||
                        t.State.Name.ToLower().Trim().Contains(searchTermLower) ||
                        t.Member.FirstName.ToLower().Trim().Contains(searchTermLower) ||
                        t.Member.LastName.ToLower().Trim().Contains(searchTermLower) ||
                        t.TicketCategory.Name.ToLower().Trim().Contains(searchTermLower));
                }

                if (!string.IsNullOrEmpty(branch))
                {
                    query = query.Where(t => t.Member.Branch == branch);
                }

                query = query.OrderBy(t => t.CreatedDate);

                if (!string.IsNullOrEmpty(cursorParams.SortColum) && !string.IsNullOrEmpty(cursorParams.SortDirection))
                {
                    query = query.OrderBy(cursorParams.SortColum + " " + cursorParams.SortDirection);
                }


                finalRecords = await query.CountAsync();
                    
                   
            }

            return finalRecords;
        }
        public async Task<List<Ticket?>> GetClosedTickets(CursorParams @params)
        {
            //check if the count has a value in it above zero before proceeding
            
            if (@params.Take > 0)
            {
                //check if there is a search parameter
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var records = (from tblOb in await this._context.Tickets
                                   .OrderBy(t => t.CreatedDate)
                                   .Include(t => t.Member)
                                   .Include(t => t.AssignedTo)
                                   .Include(t => t.TicketAttachments)
                                   .Include(t => t.State)
                                   .Include(t => t.TicketCategory)
                                   .Include(t => t.TicketPriority)
                                   .Include(t => t.TicketEscalations)
                                   .Include(t => t.InitiatorUser)
                                   .Include(t => t.InitiatorUser!.Department)
                                    .Include(t => t.InitiatorMember)
                                   .OrderByDescending(t => t.CreatedDate) // Changed to OrderByDescending
                                   .Where(t => t.Status != Lambda.Deleted)
                                    .Where(t => t.ClosedDate != null || t.State.Name == Lambda.Closed)
                                      .Skip(@params.Skip)
                                   .Take(@params.Take)                                 
                                   .ToListAsync()
                                   select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return records.ToList();
                }
                else
                {
                    //include search query

                    var records = (from tblOb in await this._context.Tickets
                                   .Where(t => t.Status != Lambda.Deleted)
                                    .Where(t => t.ClosedDate != null)
                                   .OrderBy(t => t.CreatedDate)
                                   .Include(t => t.AssignedTo)
                                   .Include(t => t.TicketAttachments)
                                   .Include(t => t.State)
                                   .Include(t => t.TicketCategory)
                                   .Include(t => t.TicketPriority)
                                   .Include(t => t.InitiatorUser)
                                    .Include(t => t.InitiatorUser!.Department)
                                    .Include(t => t.InitiatorMember)
                                   .Include(t => t.TicketEscalations)
                                   .Where(t =>
                                           t.Title.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.State.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Member.FirstName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Member.LastName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()))
                                    .Skip(@params.Skip)
                                   .Take(@params.Take)
                             
                                   .ToListAsync()
                                   select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }

                    return records.ToList();
                }
            }
            else
            {
                return null;
            }
        }
        public async Task<List<Ticket?>> GetMemberTickets(CursorParams @params, int memberId, string stateName = "")
        {
            //check if the count has a value in it above zero before proceeding

            if (@params.Take > 0)
            {
                //check if there is a search parameter
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var query = this._context.Tickets
                     .OrderBy(t => t.CreatedDate)
                     .Include(t => t.Member)
                     .Include(t => t.AssignedTo)
                     .Include(t => t.TicketAttachments)
                     .Include(t => t.State)
                     .Include(t => t.TicketCategory)
                     .Include(t => t.TicketPriority)
                     .Include(t => t.TicketEscalations)
                     .Include(t => t.InitiatorUser)
                     .Include(t => t.InitiatorUser!.Department)
                     .Include(t => t.InitiatorMember)
                     .OrderByDescending(t => t.CreatedDate) // Changed to OrderByDescending
                     .Where(t => t.Status != Lambda.Deleted && t.MemberId == memberId)
                     .AsQueryable(); // Convert to IQueryable for dynamic ordering

                    if (!string.IsNullOrEmpty(stateName))
                    {
                        query = query.Where(t => t.State.Name.Trim().ToLower() == stateName.Trim().ToLower());
                    }

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        query = query.OrderBy($"{@params.SortColum} {@params.SortDirection}");
                    }

                    var records = await query
                        .Skip(@params.Skip)
                        .Take(@params.Take)
                        .ToListAsync();

                    return records.ToList();
                }
                else
                {


                    var query =  this._context.Tickets
                                                   .Where(t => t.Status != Lambda.Deleted && t.MemberId == memberId)
                                                   .Include(t => t.AssignedTo)
                                                   .Include(t => t.TicketAttachments)
                                                   .Include(t => t.State)
                                                   .Include(t => t.TicketCategory)
                                                   .Include(t => t.TicketPriority)
                                                   .Include(t => t.InitiatorUser)
                                                   .Include(t => t.InitiatorUser!.Department)
                                                   .Include(t => t.InitiatorMember)
                                                   .OrderByDescending(t => t.CreatedDate) // Changed to OrderByDescending
                                                   .Where(t =>
                                                               t.Title.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                               t.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                               t.State.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                               t.Member.FirstName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                               t.Member.LastName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                               t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()))
                                                   .OrderBy(t => t.CreatedDate)
                                                   .Skip(@params.Skip)
                                                   .Take(@params.Take);

                    if (!string.IsNullOrEmpty(stateName))
                    {
                        query = query.Where(t => t.State.Name.Trim().ToLower() == stateName.Trim().ToLower());
                    }


                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        query = query.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }

                    return await query.ToListAsync();
                }
            }
            else
            {
                return null;
            }
        }

    public async Task<List<Ticket?>> GetAssignedToTickets(CursorParams @params, string assignedToId, string status = "")
    {
        if (@params.Take <= 0)
        {
            return null;
        }

        var query = this._context.Tickets
            .Where(t => t.Status != Lambda.Deleted &&
                        (t.AssignedToId == assignedToId || 
                        t.CreatedById == assignedToId || 
                        (t.AssignedToId == null && t.InitiatorMember != null)))
            .OrderByDescending(t => t.CreatedDate)
            .Include(t => t.Member)
            .Include(t => t.AssignedTo)
            .Include(t => t.TicketAttachments)
            .Include(t => t.State)
            .Include(t => t.TicketCategory)
            .Include(t => t.TicketPriority)
            .Include(t => t.InitiatorUser)
            .Include(t => t.InitiatorUser!.Department)
            .Include(t => t.InitiatorMember)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.State.Name.Trim().ToLower() == status.Trim().ToLower());
        }

        if (!string.IsNullOrEmpty(@params.SearchTerm))
        {
            var searchTerm = @params.SearchTerm.ToLower().Trim();
            query = query.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                t.Description.ToLower().Contains(searchTerm) ||
                t.State.Name.ToLower().Contains(searchTerm) ||
                t.Member.FirstName.ToLower().Contains(searchTerm) ||
                t.Member.LastName.ToLower().Contains(searchTerm) ||
                t.TicketCategory.Name.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
        {
            query = query.OrderBy(@params.SortColum + " " + @params.SortDirection);
        }

        return await query
            .Skip(@params.Skip)
            .Take(@params.Take)
            .ToListAsync();
    }
        public async Task<int> GetAssignedToTicketsCountAsync(CursorParams @params, string assignedToId, string status = "")
        {
            var query = this._context.Tickets
                .Where(t => t.Status != Lambda.Deleted &&
                            (t.AssignedToId == assignedToId || 
                            t.CreatedById == assignedToId || 
                            (t.AssignedToId == null && t.InitiatorMember != null)));

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.State.Name.Trim().ToLower() == status.Trim().ToLower());
            }

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                var searchTerm = @params.SearchTerm.ToLower().Trim();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchTerm) ||
                    t.Description.ToLower().Contains(searchTerm) ||
                    t.State.Name.ToLower().Contains(searchTerm) ||
                    t.Member.FirstName.ToLower().Contains(searchTerm) ||
                    t.Member.LastName.ToLower().Contains(searchTerm) ||
                    t.TicketCategory.Name.ToLower().Contains(searchTerm));
            }

            return await query.CountAsync();
        }
        public async Task<Ticket> LastTicket()
        {
            return await this._context.Tickets.OrderByDescending(t => t.CreatedDate).FirstOrDefaultAsync();
        }

        public void Remove(Ticket ticket)
        {
            ticket.Status = Lambda.Deleted;
            ticket.DeletedDate = DateTime.Now;
        }

        public async Task<int> TotalCount(string stateName = "")
        {
            var tickets = this._context.Tickets.Where((t => t.Status != Lambda.Deleted));

            if(!string.IsNullOrEmpty(stateName))
            {
                tickets = tickets.Where(t => t.State.Name.Trim().ToLower() == stateName.Trim().ToLower());
            }
            return await tickets.CountAsync();
        }
        public async Task<int> TotalClosedCount()
        {
            return await this._context.Tickets.CountAsync(t => t.Status == Lambda.Closed || t.ClosedDate != null);
        }
        // count tickets by state
        public async Task<int> CountTicketsByStatus(string state)
        {
            if (state == Lambda.Closed)
            {
                return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & (t.State.Name.Trim().ToLower() == state.Trim().ToLower() || t.ClosedDate != null));
            }
            else {

            }
            return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & t.State.Name.Trim().ToLower() == state.Trim().ToLower());
        }
        // count tickets by category
        public async Task<int> CountTicketsByCategory(string category)
        {
            return await this._context.Tickets.Include(t => t.TicketCategory).CountAsync(t => t.Status != Lambda.Deleted & t.TicketCategory.Name.Trim().ToLower() == category.Trim().ToLower());
        }
        // count tickets by category
        public async Task<int> CountTicketsByPriority(string priority)
        {
            return await this._context.Tickets.Include(t => t.TicketPriority).CountAsync(t => t.Status != Lambda.Deleted & t.TicketPriority.Name.Trim().ToLower() == priority.Trim().ToLower());
        }
        public async Task<int> TotalCountByMember(int memberId, string stateName = "")
        {
            //List<Ticket> tickets = new();

            var tickets =  this._context.Tickets.Where(t => t.Status != Lambda.Deleted && t.MemberId == memberId);

            if(!string.IsNullOrEmpty(stateName))
            {
                tickets = tickets.Where(t => t.State.Name.Trim().ToLower() == stateName.Trim().ToLower());
            }

            return await tickets.CountAsync();
        }
        public async Task<int> TotalCountByAssignedTo(string assignedTo)
        {
            return await this._context.Tickets.CountAsync(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedTo || t.CreatedById == assignedTo);
        }
        public async Task<int> CountTicketsByStatusMember(string state, int memberId)
        {

                return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & t.State.Name.Trim().ToLower() == state.Trim().ToLower() && t.MemberId == memberId);
          
        }
        public async Task<int> CountTicketsByStatusAssignedTo(string state, string assignedToId)
        {
            return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & t.State.Name.Trim().ToLower() == state.Trim().ToLower() && t.AssignedToId == assignedToId);
        }
        public async Task<int> CountTicketsByStatusCreatedByOrAssignedTo(string state, string identifier)
        {
            return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & t.State.Name.Trim().ToLower() == state.Trim().ToLower() && t.CreatedById == identifier || t.AssignedToId == identifier);
        }
        public async Task UnAssignedTickets()
        {
            var tickets = new List<Ticket>();

            tickets = await _context.Tickets.Include(t => t.AssignedTo).Where(i => i.AssignedToId == null || i.State.Name == Lambda.NewTicket && i.Status != Lambda.Deleted).ToListAsync();
            
            // sending emails for all the issues that have not been assigned yet or they are on waiting for support
            string status = "";
            try
            {
                foreach (var ticket in tickets)
                {

                    var assignedTo = await _context.EmailAddresses.FirstOrDefaultAsync(o => o.Owner == Lambda.CustomerServiceMemberEngagementManager);

                    if (assignedTo != null) {
                        try {
                            // sending the email 
                            string title = "Un Assigned Tickets";
                            var body = $@"
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
                                    <h2>Unassigned Ticket</h2>
                                    <div class='ticket-info'>
                                        <p>Ticket number {ticket.TicketNumber} has not been assigned to anyone yet.</p>
                                    </div>
                                    <p>
                                        <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                                    </p>
                                    <p class='footer'>Please assign this ticket to an appropriate team member.</p>
                                </div>
                            </body>
                            </html>";
                           
                            
                            EmailHelper.SendEmail(this._jobEnqueuer, assignedTo.Email, title, body, ticket.AssignedTo?.SecondaryEmail);
                        }
                        catch (Exception ex)
                        {
                            var errorLog = new ErrorLog
                            {
                                UserFriendlyMessage = "An error occurred while sending the unassigned ticket email.",
                                DetailedMessage = ex.ToString(),
                                DateOccurred = DateTime.UtcNow
                            };

                            _context.ErrorLogs.Add(errorLog);
                            await _unitOfWork.SaveToDataStore();
                        }
                    }
                }

                
                status = "Email sent";
            }
            catch (Exception)
            {

                status = "There was an error with this request";
            }

            //return status;
        }
        public async Task SendUnassignedTicketEmail(Ticket ticket)
        {
            // Send an email to the previous assignee
            string title = "Unassigned Ticket";
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
                        <h2>Unassigned Ticket</h2>
                        <div class='ticket-info'>
                            <p>Ticket number {ticket.TicketNumber} has been created but not assigned to anyone yet.</p>
                        </div>
                        <p>
                            <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                        </p>
                        <p class='footer'>Please assign this ticket to an appropriate team member.</p>
                    </div>
                </body>
                </html>";

            var emailAddress = await _context.EmailAddresses.FirstOrDefaultAsync(o => o.Owner == Lambda.CustomerServiceMemberEngagementManager);

            try
            {
                if (emailAddress != null)
                {
                  

                    EmailHelper.SendEmail(this._jobEnqueuer, emailAddress.Email, title, emailBody, ticket.AssignedTo?.SecondaryEmail);
                }
            }
            catch (Exception ex)
            {
                var errorLog = new ErrorLog
                {
                    UserFriendlyMessage = "An error occurred while sending the email.",
                    DetailedMessage = ex.ToString(),
                    DateOccurred = DateTime.UtcNow
                };

                //add the error log to the context
                _context.ErrorLogs.Add(errorLog);

                // Sync changes to the database
                await _unitOfWork.SaveToDataStore();
            }

            //return string.Empty;
        }
        public async Task SendTicketEscalationEmail(Ticket ticket, TicketEscalation ticketEscalation, string previousAssigneeEmail)
        {
            // Send an email to the previous assignee
            string title = "Ticket Escalation";
            string body = $"Your ticket {ticket.TicketNumber} has been escalated to {ticketEscalation.EscalatedTo.Email}";
            
            this._jobEnqueuer.EnqueueEmailJob(previousAssigneeEmail, title, body);


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
                            <p>Ticket {ticket.TicketNumber}, previously assigned to {previousAssigneeEmail}, has been escalated to you.</p>
                            <p>Please review the ticket details and respond accordingly.</p>
                        </div>
                        <p style='text-align: center;'>
                            <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                        </p>
                        <p class='footer'>Thank you for your prompt attention to this matter.</p>
                    </div>
                </body>
                </html>";

            EmailHelper.SendEmail(this._jobEnqueuer, ticketEscalation.EscalatedTo.Email, title, body, ticketEscalation.EscalatedTo.SecondaryEmail);
           
                             
            //send email to the department
             this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Department.Email, title, body);
           

      

            //return "messages sent";
        }

        public async Task SendTicketDeEscalationEmail(Ticket ticket,  string previousAssigneeEmail)
        {
            // Send an email to the previous assignee
            string title = "Ticket De-Escalation";
            string body = $@"
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
                        <h2>Ticket De-Escalation Notice</h2>
                        <div class='ticket-info'>
                            <p>Your ticket {ticket.TicketNumber} has been de-escalated back to {ticket.AssignedTo.Email}.</p>
                            <p>Please note this change and take appropriate action.</p>
                        </div>
                        <p style='text-align: center;'>
                            <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                        </p>
                        <p class='footer'>Thank you for your attention to this matter.</p>
                    </div>
                </body>
                </html>";

            this._jobEnqueuer.EnqueueEmailJob(previousAssigneeEmail, title, body);
            
           
           
            body = $@"
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
                        <h2>Ticket De-Escalation Notice</h2>
                        <div class='ticket-info'>
                            <p>A ticket {ticket.TicketNumber} previously escalated to {previousAssigneeEmail} has been de-escalated to you ({ticket.AssignedTo.Email}).</p>
                            <p>Please take note and respond to it accordingly.</p>
                        </div>
                        <p style='text-align: center;'>
                            <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                        </p>
                        <p class='footer'>Thank you for your prompt attention to this matter.</p>
                    </div>
                </body>
                </html>";

            this._jobEnqueuer.EnqueueEmailJob(previousAssigneeEmail, title, body);


            
            //send email to the department
             this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Department.Email, title, body);
            


            //return "messages sent";
             
          

           

          
        }

        public async Task SendTicketReassignmentEmail(string previousEmail, string newEmail, Ticket ticket)
        {
            // Send an email to the previous assignee
            string title = $"Ticket {ticket.TicketNumber} Re-assignment";
            string body = $@"
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
                    <h2>Ticket Reassignment Notice</h2>
                    <div class='ticket-info'>
                        <p>Your ticket {ticket.TicketNumber} has been reassigned to {newEmail}.</p>
                        <p>Please take note of this change.</p>
                    </div>
                    <p style='text-align: center;'>
                        <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                    </p>
                    <p class='footer'>Thank you for your attention to this matter.</p>
                </div>
            </body>
            </html>";

            string emailResponse = string.Empty;

            if (!string.IsNullOrEmpty(previousEmail))
            {
                this._jobEnqueuer.EnqueueEmailJob(previousEmail, title, body);
               

            }


            body = $@"
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
                    <h2>Ticket Assignment Notice</h2>
                    <div class='ticket-info'>
                        <p>Ticket {ticket.TicketNumber} has been assigned to you.</p>
                        <p>Please take note and respond to it accordingly.</p>
                    </div>
                    <p style='text-align: center;'>
                        <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                    </p>
                    <p class='footer'>Thank you for your prompt attention to this matter.</p>
                </div>
            </body>
            </html>";

            if (ticket != null && ticket.AssignedTo != null && newEmail == ticket.AssignedTo.Email)
            {
                EmailHelper.SendEmail(this._jobEnqueuer, newEmail, title, body, ticket.AssignedTo.SecondaryEmail);
            }
            else
            {
                this._jobEnqueuer.EnqueueEmailJob(newEmail, title, body);
            }

           

           
            //check if department is not null
            if (ticket.AssignedTo.Department!= null)
            {
                //send email to the department
                this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Department.Email, title, body);
            
            }


        }
        public async Task SendEscalatedTicketsReminder()
        {
            var tickets = new List<TicketEscalation>();

            tickets = await _context.TicketEscalations
              .Include(t => t.EscalatedTo)
              .Include(t => t.Ticket)
              .ThenInclude(t => t.TicketPriority)
              .Where(i => DateTime.Now > i.CreatedDate.AddHours(i.Ticket.TicketPriority.Value) &&
                          i.Resolved == false &&
                          i.Status != Lambda.Deleted)
              .ToListAsync();


            // sending reminder email
            string status = "";
            try
            {
                foreach (var ticket in tickets)
                {

                    //var emailAddress = await _context.EmailAddresses.FirstOrDefaultAsync(o => o.Owner == ticket.EscalatedTo.Email);

                    var emailAddress = ticket.EscalatedTo;

                    if (emailAddress != null)
                    {
                        // sending the email 
                        string title = "Escalated Tickets";
                        var body = $@"
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
                                <h2>Escalated Ticket Reminder</h2>
                                <div class='ticket-info'>
                                    <p>Ticket number {ticket.Ticket.TicketNumber} was escalated and has not yet been resolved.</p>
                                    <p>Please review and take necessary action.</p>
                                </div>
                                <p style='text-align: center;'>
                                    <a href='{Lambda.systemLinkClean}' class='cta-button' style='color: #ffffff;'>View Ticket Details</a>
                                </p>
                                <p class='footer'>Thank you for your prompt attention to this matter.</p>
                            </div>
                        </body>
                        </html>";
                        EmailHelper.SendEmail(this._jobEnqueuer, emailAddress.Email, title, body, emailAddress.SecondaryEmail);                        
                    }
                }


                status = "Email sent";
            }
            catch (Exception)
            {

                status = "There was an error with this request";
            }

            //return status;
        }

       public async Task<object> GetTicketInitiator(int ticketId)
        {
            var ticket = await _context.Tickets
                .Include(t => t.InitiatorUser)
                .Include(t => t.InitiatorMember)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            return ticket?.GetInitiator();
        }
        public async Task<string> SendDepartmentEmail(Department department, string emailSubject, string emailBody)
        {
            // Send an email to the previous assignee
            string title = emailSubject;
            string body = emailBody;

            var emailAddress = department.Email;

            if (!string.IsNullOrEmpty(emailAddress))
            {
                this._jobEnqueuer.EnqueueEmailJob(emailAddress, title, body);
            
    

               return "message sent";

                
            }



            return string.Empty;
        }

       
    }
}

