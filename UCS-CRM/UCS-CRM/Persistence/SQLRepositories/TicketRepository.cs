using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using AutoMapper.Execution;
using Microsoft.AspNetCore.Routing;
using UCS_CRM.Core.Services;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailRepository _emailRepository;

        public TicketRepository(ApplicationDbContext context, IEmailRepository emailRepository)
        {
            _context = context;
            _emailRepository = emailRepository;
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

        public async Task<Ticket?> GetTicket(int id)
        {
            return await this._context.Tickets
                .Include(t=> t.TicketCategory)
                .Include(t => t.State)
                .Include(t => t.AssignedTo)
                .Include(t => t.Member)
                .Include(t => t.TicketComments)
                .Include(t => t.TicketAttachments)
                .Include(t => t.TicketPriority)
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

            tickets = await _context.Tickets.Where(i => i.AssignedToId == null || i.State.Name == Lambda.NewTicket && i.Status != Lambda.Deleted).ToListAsync();
            
            // sending emails for all the issues that have not been assigned yet or they are on waiting for support
            string status = "";
            try
            {
                foreach (var ticket in tickets)
                {

                    var emailAddress = await _context.EmailAddresses.FirstOrDefaultAsync(o => o.Owner == Lambda.Manager);

                    if (emailAddress != null) {
                        // sending the email 
                        string title = "Un Assigned Tickets";
                        var body = "Ticket number " + ticket.TicketNumber + " has not been assigned to an engineer yet and is still waiting for support";

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

        public async Task<string> EscalatedTickets()
        {
            var tickets = new List<TicketEscalation>();

            tickets = await _context.TicketEscalations.Include(t=>t.Ticket).Where(i => DateTime.Now > i.CreatedDate.AddHours(1) && i.Resolved == false && i.Status != Lambda.Deleted).ToListAsync();

            // sending emails for all the issues that have not been assigned yet or they are on waiting for support
            string status = "";
            try
            {
                foreach (var ticket in tickets)
                {
                    //email to send to
                    var levelTo = ticket.EscalationLevel == 1 ? Lambda.Manager : Lambda.SeniorManager;

                    var emailAddress = await _context.EmailAddresses.FirstOrDefaultAsync(o => o.Owner == levelTo);

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

