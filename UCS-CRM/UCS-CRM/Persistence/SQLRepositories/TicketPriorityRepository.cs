using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class TicketPriorityRepository : ITicketPriorityRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketPriorityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(TicketPriority ticketPriority)
        {
            this._context.TicketPriorities.Add(ticketPriority);
        }

        public TicketPriority Exists(string name)
        {
            return this._context.TicketPriorities.FirstOrDefault(a => a.Name.ToLower() == name.ToLower() & a.Status != Lambda.Deleted);
        }

        public async Task<TicketPriority?> GetTicketPriority(int id)
        {
            return await this._context.TicketPriorities.FirstOrDefaultAsync(x => x.Id == id & x.Status != Lambda.Deleted);
        }

        public async Task<List<TicketPriority>?> GetTicketPriorities(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var ticketPrioritys = (from tblOb in await this._context.TicketPriorities.Where(a => a.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take).ToListAsync() select tblOb);

                    //ticketPrioritys.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        ticketPrioritys = ticketPrioritys.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return ticketPrioritys.ToList();

                }
                else
                {
                    //include search text in the query
                    var ticketPrioritys = (from tblOb in await this._context.TicketPriorities
                                        .Where(a => a.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        .ToListAsync()
                                           select tblOb);

                    ticketPrioritys = ticketPrioritys.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return ticketPrioritys.ToList();

                }

            }
            else
            {
                return null;
            }


        }

        public void Remove(TicketPriority ticketPriority)
        {
            //mark the record as deleted

            ticketPriority.Status = Lambda.Deleted;
            ticketPriority.DeletedDate = DateTime.UtcNow;
        }

        public Task<int> TotalCount()
        {
            return this._context.TicketPriorities.CountAsync(a => a.Status != Lambda.Deleted);
        }
    }
}
