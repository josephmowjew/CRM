using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using AutoMapper.Execution;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly ApplicationDbContext _context;
        public TicketRepository(ApplicationDbContext context)
        {
            _context = context;
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
                    var records = (from tblOb in await this._context.Tickets.OrderByDescending(t =>t.Id).Include(t => t.Member).Include(t => t.AssignedTo).Include(t => t.TicketAttachments).Include(t => t.State).Include(t => t.TicketCategory).Include(t => t.TicketPriority).Where(t => t.Status != Lambda.Deleted).Take(@params.Take).Skip(@params.Skip).ToListAsync() select tblOb);

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

                    var records = (from tblOb in await this._context.Tickets.OrderByDescending(t => t.Id).Include(t => t.AssignedTo).Include(t => t.TicketAttachments).Include(t => t.State).Include(t => t.TicketCategory).Include(t => t.TicketPriority)
                                   .Where(t => t.Status != Lambda.Deleted
                                        && t.Title.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.State.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Member.FirstName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.Member.LastName.ToLower().Trim().Contains(@params.SearchTerm.ToLower()) ||
                                           t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower()))
                                   .Take(@params.Take)
                                   .Skip(@params.Skip)
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
                                   .OrderByDescending(t => t.Id)
                                   .Include(t => t.Member)
                                   .Include(t => t.AssignedTo)
                                   .Include(t => t.TicketAttachments)
                                   .Include(t => t.State)
                                   .Include(t => t.TicketCategory)
                                   .Include(t => t.TicketPriority)
                                   .Where(t => t.Status != Lambda.Deleted && t.MemberId == memberId)
                                   .Take(@params.Take)
                                   .Skip(@params.Skip)
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
                                                   .OrderByDescending(t => t.Id)
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
                                   .OrderByDescending(t => t.Id)
                                   .Include(t => t.Member)
                                   .Include(t => t.AssignedTo)
                                   .Include(t => t.TicketAttachments)
                                   .Include(t => t.State)
                                   .Include(t => t.TicketCategory)
                                   .Include(t => t.TicketPriority)
                                   .Where(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedToId)
                                   .Take(@params.Take)
                                   .Skip(@params.Skip)
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
                                                    .OrderByDescending(t => t.Id)
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
            return await this._context.Tickets.OrderByDescending(t => t.Id).LastOrDefaultAsync();
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

        public async Task<int> TotalCountByMember(int memberId)
        {
            return await this._context.Tickets.CountAsync(t => t.Status != Lambda.Deleted && t.MemberId == memberId);
        }
        public async Task<int> TotalCountByAssignedTo(string assignedTo)
        {
            return await this._context.Tickets.CountAsync(t => t.Status != Lambda.Deleted && t.AssignedToId == assignedTo);
        }

        public async Task<int> CountTicketsByStatusMember(string state, int memberId)
        {
            return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & t.State.Name.Trim().ToLower() == state.Trim().ToLower() && t.MemberId == memberId);
        }
        public async Task<int> CountTicketsByStatusAssignedTo(string state, string assignedToId)
        {
            return await this._context.Tickets.Include(t => t.State).CountAsync(t => t.Status != Lambda.Deleted & t.State.Name.Trim().ToLower() == state.Trim().ToLower() && t.AssignedToId == assignedToId);
        }
    }
}

