using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

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
            return await this._context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Ticket?>> GetTickets(CursorParams @params)
        {
            //check if the count has a value in it above zero before proceeding

            if(@params.Take > 0)
            {
                //check if there is a search parameter
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var records = (from tblOb in await this._context.Tickets.Include(t => t.AssignedTo).Include(t => t.State).Include(t => t.TicketCategory).Where(t => t.Status != Lambda.Deleted).Take(@params.Take).Skip(@params.Skip).ToListAsync() select tblOb);

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

                    var records = (from tblOb in await this._context.Tickets.Include(t => t.AssignedTo).Include(t => t.State).Include(t => t.TicketCategory)
                                   .Where(t => t.Status != Lambda.Deleted
                                        && t.Title.ToLower().Trim().Contains(@params.SearchTerm) ||
                                           t.Description.ToLower().Trim().Contains(@params.SearchTerm) ||
                                           t.State.Name.ToLower().Trim().Contains(@params.SearchTerm) ||
                                           t.TicketCategory.Name.ToLower().Trim().Contains(@params.SearchTerm))
                                   .Take(@params.Take)
                                   .Skip(@params.Take)
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

        public void Remove(Ticket ticket)
        {
            ticket.Status = Lambda.Deleted;
            ticket.DeletedDate = DateTime.Now;
        }

        public async Task<int> TotalCount()
        {
            return await this._context.Tickets.CountAsync(t => t.Status != Lambda.Deleted);
        }
    }
}
