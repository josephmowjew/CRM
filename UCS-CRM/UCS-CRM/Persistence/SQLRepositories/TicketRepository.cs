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


                    this._ticketEscalationRepository.Add(mappedTicketEscalation);


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
          
            //get all active tickets
            List<Ticket> tickets = await this._context.Tickets
                .Include(t => t.State)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketEscalations)
                .Include(t => t.AssignedTo)
                .ThenInclude(u => u.Department)
                .Where(t => t.AssignedTo != null && t.State.Name != Lambda.Closed && t.State.Name != Lambda.Archived)
                .ToListAsync();

            //loop through the tickets
            foreach (Ticket ticket in tickets)
            {
                bool hasEscalations = ticket.TicketEscalations.Any();
                var creationTime = hasEscalations ? ticket.TicketEscalations.Last().CreatedDate : ticket.CreatedDate;
                var ticketPriorityMaxReponseTimeInHours = ticket.TicketPriority.MaximumResponseTimeHours;
                var escalationTime = creationTime.AddHours(ticketPriorityMaxReponseTimeInHours);

                if (escalationTime > DateTime.UtcNow)
                {
                    await this.EscalateTicket(ticket, null, "Previous assignee did not respond in time");
                }
                else if (ticket.AssignedTo != null)
                {
                    string title = "Ticket Reminder";
                    var bodyBuilder = new StringBuilder();
                    bodyBuilder.Append("Please be reminded that ticket number ");
                    bodyBuilder.Append(ticket.TicketNumber);
                    bodyBuilder.Append($"has been assigned to you({ticket.AssignedTo.Email}) and a response is still pending");
                    string body = bodyBuilder.ToString();

                     this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Email, title, body);
                    
                    //send to department
                     this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Department.Name, title, body);
                    

                    
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
            return await this._context.Tickets
                .Include(t=> t.TicketCategory)
                .Include(t => t.State)
                .Include(t => t.TicketComments)
                .Include(t => t.TicketAttachments)
                .Include(t => t.TicketPriority)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo).ThenInclude(a => a.Department)
                .Include(t => t.Member).ThenInclude(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        public async Task SendTicketClosureNotifications(Ticket ticket, string reason)
        {
            string status = string.Empty;
            //get the member email address

            var memberEmailAddress = ticket?.Member?.User?.Email ?? null;
            var ticketCreatorAddress = ticket.CreatedBy.Email;

            if(!string.IsNullOrEmpty(memberEmailAddress))
            {
                string title = "Ticket Closure Alert";
                var bodyBuilder = new StringBuilder();
                bodyBuilder.Append("Your ticket with reference number: ");
                bodyBuilder.Append(ticket.TicketNumber);
                bodyBuilder.Append($" has been closed because {reason}\nBut if you are not satisfied with the outcome. you have a chance to re-open it");
                string body = bodyBuilder.ToString();

                this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Email, title, body);
                

                status = "email sent";
            }   

            if(!string.IsNullOrEmpty(ticketCreatorAddress) && memberEmailAddress != ticketCreatorAddress)
            {
                string title = "Ticket Closure Alert";
                var bodyBuilder = new StringBuilder();
                bodyBuilder.Append("You have closed ticket: ");
                bodyBuilder.Append(ticket.TicketNumber);
                bodyBuilder.Append($" has been closed because {reason}\n");
                string body = bodyBuilder.ToString();

                this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Email, title, body);
               

                status = "email sent";
            }





            //return status;

        }
        public async Task SendTicketReopenedNotifications(Ticket ticket, string reason)
        {
            string status = string.Empty;
            //get the member email address

            var memberEmailAddress = ticket?.Member?.User?.Email ?? null;
            var ticketCreatorAddress = ticket.CreatedBy.Email;

            if (!string.IsNullOrEmpty(memberEmailAddress))
            {
                string title = "Ticket Reopened Alert";
                var bodyBuilder = new StringBuilder();
                bodyBuilder.Append("Your ticket with reference number: ");
                bodyBuilder.Append(ticket.TicketNumber);
                bodyBuilder.Append($" has been reopened because {reason}\n");
                string body = bodyBuilder.ToString();
                this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Email, title, body);
               

                status = "email sent";
            }

            if (!string.IsNullOrEmpty(ticketCreatorAddress) && memberEmailAddress != ticketCreatorAddress)
            {
                string title = "Ticket Reopened Alert";
                var bodyBuilder = new StringBuilder();
                bodyBuilder.Append("You have reopened ticket: ");
                bodyBuilder.Append(ticket.TicketNumber);
                bodyBuilder.Append($" has been reopened because {reason}\n");
                string body = bodyBuilder.ToString();
                this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Email, title, body);
               

                status = "email sent";
            }





            //return status;

        }
        //public async Task<List<Ticket?>> GetTickets(CursorParams @params)
        //{
        //    //check if the count has a value in it above zero before proceeding

        //    if(@params.Take > 0)
        //    {
        //        //check if there is a search parameter
        //        if (string.IsNullOrEmpty(@params.SearchTerm))
        //        {
        //            var records = (from tblOb in await this._context.Tickets
        //                           .OrderBy(t =>t.CreatedDate)
        //                           .Include(t => t.Member)
        //                           .Include(t => t.AssignedTo)
        //                           .Include(t => t.TicketAttachments)
        //                           .Include(t => t.State)
        //                           .Include(t => t.TicketCategory)
        //                           .Include(t => t.TicketPriority)
        //                           .Include(t => t.TicketEscalations)
        //                           .Where(t => t.Status != Lambda.Deleted).Skip(@params.Skip)
        //                           .Take(@params.Take)
        //                           .ToListAsync() select tblOb);

        //            //accountTypes.AsQueryable().OrderBy("gjakdgdag");

        //            if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
        //            {
        //                records = records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

        //            }


        //            return records.ToList();
        //        }
        //        else
        //        {
        //            //include search query

        //            var records = (from tblOb in await this._context.Tickets
        //                           .Where(t => t.Status != Lambda.Deleted)
        //                           .OrderBy(t => t.CreatedDate)
        //                           .Include(t => t.AssignedTo)
        //                           .Include(t => t.TicketAttachments)
        //                           .Include(t => t.State)
        //                           .Include(t => t.TicketCategory)
        //                           .Include(t => t.TicketPriority)
        //                           .Include(t => t.TicketEscalations)
        //                           .Where(t => 
        //                                   t.Title.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
        //                                   t.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
        //                                   t.State.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
        //                                   t.Member.FirstName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
        //                                   t.Member.LastName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
        //                                   t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()))
        //                             .Skip(@params.Skip)
        //                           .Take(@params.Take)
                                 
        //                           .ToListAsync()
        //                           select tblOb);

        //            //accountTypes.AsQueryable().OrderBy("gjakdgdag");

        //            if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
        //            {
        //                records = records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

        //            }

        //            return records.ToList();
        //        }
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        public async Task<List<Ticket?>> GetTickets(CursorParams @params, Department department = null, string ticketStatus = "")
        {
            if (@params.Take <= 0)
            {
                return null;
            }

            var query = this._context.Tickets
                .Include(t => t.AssignedTo)
                .Include(t => t.TicketAttachments)
                .Include(t => t.State)
                .Include(t => t.TicketCategory)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketEscalations)
                .Include(t => t.Member)
                .ThenInclude(m => m.User)
                .ThenInclude(u => u.Department)
                .Where(t => t.Status != Lambda.Deleted);

            if (!string.IsNullOrEmpty(ticketStatus))
            {
                query = query.Where(t => t.State.Name.Trim().ToLower() == ticketStatus.Trim().ToLower());
            }

            if (department != null)
            {
                query = query.Where(t => t.DepartmentId == department.Id);
            }

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                query = query.Where(t =>
                    t.Title.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.State.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.Member.FirstName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.Member.LastName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower())
                );
            }

            if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
            {
                string sortExpression = @params.SortColum + " " + @params.SortDirection;
                query = query.OrderBy(sortExpression);
            }
            else
            {
                query = query.OrderBy(t => t.CreatedDate);
            }

            return await query.Skip(@params.Skip).Take(@params.Take).ToListAsync();
        }
        public async Task<int> GetTicketsTotalFilteredAsync(CursorParams @params, Department department = null, string ticketStatus = "")
        {
            if (@params.Take <= 0)
            {
                return 0;
            }

            var query = this._context.Tickets
                .Include(t => t.AssignedTo)
                .Include(t => t.TicketAttachments)
                .Include(t => t.State)
                .Include(t => t.TicketCategory)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketEscalations)
                .Include(t => t.Member)
                 .ThenInclude(m => m.User)
                .ThenInclude(u => u.Department)
                .Where(t => t.Status != Lambda.Deleted);

            if (!string.IsNullOrEmpty(ticketStatus))
            {
                query = query.Where(t => t.State.Name.Trim().ToLower() == ticketStatus.Trim().ToLower());
            }

            if (department != null)
            {
                query = query.Where(t => t.DepartmentId == department.Id);
            }

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                query = query.Where(t =>
                    t.Title.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.State.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.Member.FirstName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.Member.LastName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                    t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower())
                );
            }

            if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
            {
                string sortExpression = @params.SortColum + " " + @params.SortDirection;
                query = query.OrderBy(sortExpression);
            }

            var records = await query.CountAsync();


            return records;
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

                query = query.OrderBy(t => t.CreatedDate);

               

                finalRecords = await query
                    .Include(t => t.AssignedTo)
                    .Include(t => t.State)
                    .Include(t => t.TicketCategory)
                    .Include(t => t.TicketPriority)
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
                    .Where(t => t.Status != Lambda.Deleted);

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
                    .Where(t => t.Status != Lambda.Deleted);

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
            //check if the count has a value in it above zero before proceeding

            if (@params.Take > 0)
            {
                //check if there is a search parameter
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var query = this._context.Tickets
                    .Where(t => (t.Status != Lambda.Deleted && t.AssignedToId == assignedToId) || t.CreatedById == assignedToId)
                    .OrderBy(t => t.CreatedDate)
                    .Include(t => t.Member)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.TicketAttachments)
                    .Include(t => t.State)
                    .Include(t => t.TicketCategory)
                    .Include(t => t.TicketPriority)
                    .AsQueryable(); // Convert to IQueryable to enable dynamic ordering

                    if (!string.IsNullOrEmpty(status))
                    {
                        query = query.Where(t => t.State.Name.Trim().ToLower() == status.Trim().ToLower());
                    }

                    if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        query = query.OrderBy(@params.SortColum + " " + @params.SortDirection);
                    }

                    var records = await query
                        .Skip(@params.Skip)
                        .Take(@params.Take)
                        .ToListAsync();

                    return records;

                }
                else
                {
                    //include search query

                    var records =  this._context.Tickets
                                                    .Where(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedToId)
                                                    .Include(t => t.AssignedTo)
                                                    .Include(t => t.Member)
                                                    .Include(t => t.TicketAttachments)
                                                    .Include(t => t.State)
                                                    .Include(t => t.TicketCategory)
                                                    .Include(t => t.TicketPriority)
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

                    if (!string.IsNullOrEmpty(status))
                    {
                        records = records.Where(t => t.State.Name.Trim().ToLower() == status.Trim().ToLower());
                    }

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = records.OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }

                    return await records.ToListAsync();
                }
            }
            else
            {
                return null;
            }
        }
        public async Task<int> GetAssignedToTicketsCountAsync(CursorParams @params, string assignedToId, string status = "")
        {
            //check if the count has a value in it above zero before proceeding

            if (@params.Take > 0)
            {
                //check if there is a search parameter
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var records =  this._context.Tickets
                                   .OrderBy(t => t.CreatedDate)
                                   .Include(t => t.Member)
                                   .Include(t => t.AssignedTo)
                                   .Include(t => t.TicketAttachments)
                                   .Include(t => t.State)
                                   .Include(t => t.TicketCategory)
                                   .Include(t => t.TicketPriority)
                                   .Where(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedToId
                                   || t.CreatedById == assignedToId);


                    if (!string.IsNullOrEmpty(status))
                    {
                        records = records.Where(t => t.State.Name.Trim().ToLower() == status.Trim().ToLower());
                    }

                    return await records.CountAsync();

                }
                else
                {
                    //include search query

                    var  records =  this._context.Tickets
                                                    .Where(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedToId)
                                                    .Where(t =>
                                                                t.Title.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                                t.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                                t.State.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                                t.Member.FirstName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                                t.Member.LastName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                                                t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()));


                    if (!string.IsNullOrEmpty(status))
                    {
                        records = records.Where(t => t.State.Name.Trim().ToLower() == status.Trim().ToLower());
                    }

                    return await records.CountAsync();


                }
            }
            else
            {
                return 0;
            }
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
                        // sending the email 
                        string title = "Un Assigned Tickets";
                        var body = "Ticket number " + ticket.TicketNumber + " has not been assigned to anyone one yet";
                        this._jobEnqueuer.EnqueueEmailJob(assignedTo.Email, title, body);
                        
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
            string body = $"The Ticket {ticket.TicketNumber} was created by not assigned to anyone";

            var emailAddress = await _context.EmailAddresses.FirstOrDefaultAsync(o => o.Owner == Lambda.CustomerServiceMemberEngagementManager);

            if(emailAddress != null)
            {
                this._jobEnqueuer.EnqueueEmailJob(emailAddress.Email, title, body);
                

               
                    //return "message sent";

              
            }

          

            //return string.Empty;
        }
        public async Task SendTicketEscalationEmail(Ticket ticket, TicketEscalation ticketEscalation, string previousAssigneeEmail)
        {
            // Send an email to the previous assignee
            string title = "Ticket Escalation";
            string body = $"Your ticket {ticket.TicketNumber} has been escalated to {ticketEscalation.EscalatedTo.Email}";
            
            this._jobEnqueuer.EnqueueEmailJob(previousAssigneeEmail, title, body);


            body = $"A ticket {ticket.TicketNumber} previously assigned to {previousAssigneeEmail} has been escalated to you. Please take note and respond to it accordingly";

            this._jobEnqueuer.EnqueueEmailJob(ticketEscalation.EscalatedTo.Email, title, body);
           
                             
            //send email to the department
             this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Department.Email, title, body);
           

      

            //return "messages sent";
        }

        public async Task SendTicketDeEscalationEmail(Ticket ticket,  string previousAssigneeEmail)
        {
            // Send an email to the previous assignee
            string title = "Ticket De-Escalation";
            string body = $"Your ticket {ticket.TicketNumber} has been de-escalated to {ticket.AssignedTo.Email}";

            this._jobEnqueuer.EnqueueEmailJob(previousAssigneeEmail, title, body);
            
           
           
            body = $"A ticket {ticket.TicketNumber} previously escalated to {previousAssigneeEmail} has been de-escalated to you {ticket.AssignedTo.Email}. Please take note and respond to it accordingly";

            this._jobEnqueuer.EnqueueEmailJob(previousAssigneeEmail, title, body);


            
            //send email to the department
             this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Department.Email, title, body);
            


            //return "messages sent";
             
          

           

          
        }

        public async Task SendTicketReassignmentEmail(string previousEmail, string newEmail, Ticket ticket)
        {
            // Send an email to the previous assignee
            string title = $"Ticket {ticket.TicketNumber} Re-assignment";
            string body = $"Your ticket {ticket.TicketNumber} has been reassigned to  {newEmail}";

            string emailResponse = string.Empty;

            if (!string.IsNullOrEmpty(previousEmail))
            {
                this._jobEnqueuer.EnqueueEmailJob(previousEmail, title, body);
               

            }


            body = $"A {ticket.TicketNumber} has been reassigned to you .Please take note and respond to it accordingly";

            this._jobEnqueuer.EnqueueEmailJob(newEmail, title, body);
           

           
            //return "messages sent";
           
            //check if department is not null
            if (ticket.AssignedTo.Department!= null)
            {
                //send email to the department
                this._jobEnqueuer.EnqueueEmailJob(ticket.AssignedTo.Department.Email, title, body);
            
            }

           

            //return string.Empty;
        }
        public async Task SendEscalatedTicketsReminder()
        {
            var tickets = new List<TicketEscalation>();

            tickets = await _context.TicketEscalations
                .Include(t => t.EscalatedTo)
                .Include(t=>t.Ticket)
                .ThenInclude(t => t.TicketPriority)
                .Where(i => DateTime.Now > i.CreatedDate
                .AddHours(i.Ticket.TicketPriority.Value) && i.Resolved == false && i.Status != Lambda.Deleted).ToListAsync();

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
                        var body = "Ticket number " + ticket.Ticket.TicketNumber + " was escalated and has not yet been resolved";
                        this._jobEnqueuer.EnqueueEmailJob(emailAddress.Email, title, body);
                        
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

