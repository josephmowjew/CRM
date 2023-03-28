using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UCS_CRM.Persistence.SQLRepositories
{

    public class TicketEscalationRepository : ITicketEscalationRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketEscalationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(TicketEscalation accountType)
        {
            this._context.TicketEscalations.Add(accountType);
        }

        public TicketEscalation? Exists(TicketEscalation ticketEscalation)
        {
            return this._context.TicketEscalations.FirstOrDefault(a => a.TicketId == ticketEscalation.TicketId && a.DateEscalated == ticketEscalation.DateEscalated & a.Status != Lambda.Deleted);
        }

        public async Task<TicketEscalation?> GetTicketEscalation(int id)
        {
            return await this._context.TicketEscalations.FirstOrDefaultAsync(x => x.Id == id & x.Status != Lambda.Deleted);
        }

        public async Task<List<TicketEscalation>?> GetTicketEscalations(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var ticketEscalations = (from tblOb in await this._context.TicketEscalations.Where(a => a.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take).ToListAsync() select tblOb);

                    //ticketEscalations.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        ticketEscalations = ticketEscalations.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return ticketEscalations.ToList();

                }
                else
                {
                    //include search text in the query
                    var ticketEscalations = (from tblOb in await this._context.TicketEscalations
                                        .Where(a => a.Ticket.Title.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        .ToListAsync()
                                             select tblOb);

                    ticketEscalations = ticketEscalations.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return ticketEscalations.ToList();

                }

            }
            else
            {
                return null;
            }


        }

        public async Task<List<TicketEscalation>?> GetTicketEscalations()
        {
            return await this._context.TicketEscalations.Where(a => a.Status != Lambda.Deleted).ToListAsync();
        }

        public void Remove(TicketEscalation accountType)
        {
            //mark the record as deleted

            accountType.Status = Lambda.Deleted;
            accountType.DeletedDate = DateTime.UtcNow;
        }

        public Task<int> TotalCount()
        {
            return this._context.TicketEscalations.CountAsync(a => a.Status != Lambda.Deleted);
        }
    }
}
