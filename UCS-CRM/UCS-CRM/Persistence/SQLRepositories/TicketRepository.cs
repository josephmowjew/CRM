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

        public TicketRepository(
            ApplicationDbContext context,
            IEmailRepository emailRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IDepartmentRepository departmentRepository,
            IMapper mapper,
            ITicketEscalationRepository ticketEscalationRepository)
        {
            _context = context;
            _emailRepository = emailRepository;
            _userRepository = userRepository;
            _departmentRepository = departmentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _ticketEscalationRepository = ticketEscalationRepository;
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

        public async Task<string> EscalateTicket(Ticket ticket, string UserId, string escalationReason)
        {
            ApplicationUser currentAssignedUser = null;
            string currentAssignedUserEmail = string.Empty;
            Role currentAssignedUserRole = null;
            Department? currentAssignedUserDepartment = null;
            List<Role> rolesOfCurrentUserDepartment = new();
            List<Role> SortedrolesOfCurrentUserDepartment = new();
            bool ticketAssignedToNewUser = false;

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

                                if (currentAssignedUserDepartment.Name.Trim().ToLower() == "Branch Networks and satellites Department".Trim().ToLower())
                                {
                                    ticket.AssignedToId = await this.AssignTicketToDepartment("Executive suite");

                                    if (!string.IsNullOrEmpty(ticket.AssignedToId))
                                    {
                                        ticketAssignedToNewUser = true;
                                    }

                                }
                                else
                                {
                                    ticket.AssignedToId = await this.AssignTicketToDepartment("Branch Networks and satellites Department");

                                    if (!string.IsNullOrEmpty(ticket.AssignedToId))
                                    {
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

                    string result = await this.SendUnassignedTicketEmail(ticket);
                }

            }

            if (ticketAssignedToNewUser != true)
            {

                return "Could not find a user to escalate the ticket to";
            }
            else
            {
                //map the create ticket escalation DTO to ticket escalation

                var mappedTicketEscalation = new TicketEscalation() { TicketId = ticket.Id, Reason = escalationReason };

                //update the escalated to to reflect to new user assigned to the ticket

                mappedTicketEscalation.EscalatedTo = ticket.AssignedTo;


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
                            return "system user not found";
                        }


                    }

                    mappedTicketEscalation.CreatedById = UserId;


                    this._ticketEscalationRepository.Add(mappedTicketEscalation);


                    await this._unitOfWork.SaveToDataStore();


                    //send emails to previous assignee and the new assignee

                    string emails_response = await this.SendTicketEscalationEmail(ticket, mappedTicketEscalation, currentAssignedUserEmail);


                    return "ticket escalated";
                }
              
                catch (Exception ex)
                {

                    return null;
                }
            }

        }

        public async Task SendTicketReminders()
        {
            string emails = string.Empty;
            //get all active tickets
            List<Ticket> tickets = await this._context.Tickets
                .Include(t => t.State)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketEscalations)
                .Include(t => t.AssignedTo)
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
                    string result = await this.EscalateTicket(ticket, null, "Previous assignee did not respond in time");
                }
                else if (ticket.AssignedTo != null)
                {
                    string title = "Ticket Reminder";
                    var bodyBuilder = new StringBuilder();
                    bodyBuilder.Append("Please be reminded that ticket number ");
                    bodyBuilder.Append(ticket.TicketNumber);
                    bodyBuilder.Append(" has been assigned to you and a response is still pending");
                    string body = bodyBuilder.ToString();

                     _emailRepository.SendMail(ticket.AssignedTo.Email, title, body).Wait();

                    emails = emails  + ticket.AssignedTo.Email + "\n";
                }
            }

            Console.WriteLine(emails.ToString());
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
        public async Task<Ticket?> GetTicket(int id)
        {
            return await this._context.Tickets
                .Include(t=> t.TicketCategory)
                .Include(t => t.State)
                .Include(t => t.Member)
                .Include(t => t.TicketComments)
                .Include(t => t.TicketAttachments)
                .Include(t => t.TicketPriority)
                .Include(t => t.AssignedTo).ThenInclude(a => a.Department)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Ticket?>> GetTickets(CursorParams @params)
        {
            //check if the count has a value in it above zero before proceeding

            if(@params.Take > 0)
            {
                //check if there is a search parameter
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var records = (from tblOb in await this._context.Tickets
                                   .OrderBy(t =>t.CreatedDate)
                                   .Include(t => t.Member)
                                   .Include(t => t.AssignedTo)
                                   .Include(t => t.TicketAttachments)
                                   .Include(t => t.State)
                                   .Include(t => t.TicketCategory)
                                   .Include(t => t.TicketPriority)
                                   .Include(t => t.TicketEscalations)
                                   .Where(t => t.Status != Lambda.Deleted).Skip(@params.Skip)
                                   .Take(@params.Take)
                                   .ToListAsync() select tblOb);

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
        public async Task<List<Ticket?>> GetMemberTickets(CursorParams @params, int memberId)
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
                                   .Where(t => t.Status != Lambda.Deleted && t.MemberId == memberId)
                                     .Skip(@params.Skip)
                                   .Take(@params.Take)                                
                                   .ToListAsync() select tblOb);

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
                    /*
                    var records = (from tblOb in await this._context.Tickets
                                   .OrderByDescending(t => t.Id)
                                   .Include(t => t.AssignedTo)
                                   .Include(t => t.TicketAttachments)
                                   .Include(t => t.State)
                                   .Include(t => t.TicketCategory)
                                   .Include(t => t.TicketPriority)
                                   .Where(t => t.Status != Lambda.Deleted && t.MemberId == memberId
                                        && t.Title.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.State.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Member.FirstName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Member.LastName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()))
                                   .Take(@params.Take)
                                   .Skip(@params.Skip)
                                   .ToListAsync()
                                   select tblOb);\
                    */

                    var records = await this._context.Tickets
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
                                                   .Take(@params.Take)
                                                   .ToListAsync();

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = (List<Ticket>)records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }

                    return records;
                }
            }
            else
            {
                return null;
            }
        }
        public async Task<List<Ticket?>> GetAssignedToTickets(CursorParams @params, string assignedToId)
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
                                   .Where(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedToId
                                   || t.CreatedById == assignedToId)
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

                    var records = await this._context.Tickets
                                                    .Where(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedToId)
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
                                                    .Take(@params.Take)
                                                    .ToListAsync();

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = (List<Ticket>)records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }

                    return records;
                }
            }
            else
            {
                return null;
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

        public async Task<int> TotalCount()
        {
            return await this._context.Tickets.CountAsync(t => t.Status != Lambda.Deleted);
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

        public async Task<int> TotalCountByMember(int memberId)
        {
            return await this._context.Tickets.CountAsync(t => t.Status != Lambda.Deleted && t.MemberId == memberId);
        }
        public async Task<int> TotalCountByAssignedTo(string assignedTo)
        {
            return await this._context.Tickets.CountAsync(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedTo || t.CreatedById == assignedTo);
        }

        public async Task<int> CountTicketsByStatusMember(string state, int memberId)
        {
            if (state == Lambda.Closed)
            {
                return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status == Lambda.Deleted & (t.State.Name.Trim().ToLower() == state.Trim().ToLower() || t.ClosedDate != null) && t.MemberId == memberId);

            }
            else
            {
                return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & t.State.Name.Trim().ToLower() == state.Trim().ToLower() && t.MemberId == memberId);
            }
          
        }
        public async Task<int> CountTicketsByStatusAssignedTo(string state, string assignedToId)
        {
            return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & t.State.Name.Trim().ToLower() == state.Trim().ToLower() && t.AssignedToId == assignedToId);
        }

        
        public async Task<string> UnAssignedTickets()
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

                        await _emailRepository.SendMail(assignedTo.Email, title, body);
                    } 
                }

                
                status = "Email sent";
            }
            catch (Exception)
            {

                status = "There was an error with this request";
            }

            return status;
        }
        public async Task<string> SendUnassignedTicketEmail(Ticket ticket)
        {
            // Send an email to the previous assignee
            string title = "Unassigned Ticket";
            string body = $"The Ticket {ticket.TicketNumber} was created by not assigned to anyone";

            var emailAddress = await _context.EmailAddresses.FirstOrDefaultAsync(o => o.Owner == Lambda.CustomerServiceMemberEngagementManager);

            if(emailAddress != null)
            {
                string emailResponse = await _emailRepository.SendMail(emailAddress.Email, title, body);

                if (string.Equals(emailResponse, "message sent", StringComparison.OrdinalIgnoreCase))
                {

                    return "message sent";

                }
            }

          

            return string.Empty;
        }
        public async Task<string> SendTicketEscalationEmail(Ticket ticket, TicketEscalation ticketEscalation, string previousAssigneeEmail)
        {
            // Send an email to the previous assignee
            string title = "Ticket Escalation";
            string body = $"Your ticket {ticket.TicketNumber} has been escalated to {ticketEscalation.EscalatedTo.Email}";

            string emailResponse = await _emailRepository.SendMail(previousAssigneeEmail, title, body);

            if (string.Equals(emailResponse, "Message sent", StringComparison.OrdinalIgnoreCase))
            {
                body = $"A ticket {ticket.TicketNumber} previously assigned to {previousAssigneeEmail} has been escalated to you {ticketEscalation.EscalatedTo.Email}. Please take note and respond to it accordingly";

                emailResponse = await _emailRepository.SendMail(previousAssigneeEmail, title, body);

                if (string.Equals(emailResponse, "Message sent", StringComparison.OrdinalIgnoreCase))
                {
                    return "messages sent";
                }
            }

            return string.Empty;
        }
        public async Task<string> EscalatedTickets()
        {
            var tickets = new List<TicketEscalation>();

            tickets = await _context.TicketEscalations.Include(t=>t.Ticket).Include(t => t.EscalatedTo).Where(i => DateTime.Now > i.CreatedDate.AddHours(1) && i.Resolved == false && i.Status != Lambda.Deleted).ToListAsync();

            // sending emails for all the issues that have not been assigned yet or they are on waiting for support
            string status = "";
            try
            {
                foreach (var ticket in tickets)
                {
                    //email to send to
                    //var levelTo = ticket.EscalationLevel == 1 ? Lambda.Manager : Lambda.SeniorManager;

                    var emailAddress = await _context.EmailAddresses.FirstOrDefaultAsync(o => o.Owner == ticket.EscalatedTo.Email);

                    if (emailAddress != null)
                    {
                        // sending the email 
                        string title = "Escalated Tickets";
                        var body = "Ticket number " + ticket.Ticket.TicketNumber + " was escalated and has not yet been resolved";

                        await _emailRepository.SendMail(emailAddress.Email, title, body);
                    }
                }


                status = "Email sent";
            }
            catch (Exception)
            {

                status = "There was an error with this request";
            }

            return status;
        }

       
    }
}

