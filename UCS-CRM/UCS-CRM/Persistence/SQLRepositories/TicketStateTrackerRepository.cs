using Microsoft.EntityFrameworkCore;
using System.Web.WebPages;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class TicketStateTrackerRepository : ITicketStateTrackerRepository
    {
        private ApplicationDbContext _context;
        public TicketStateTrackerRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Add(TicketStateTracker ticketStateTracker)
        {
          this._context.TicketStateTrackers.Add(ticketStateTracker);
        }

        public Branch? Exists(string state)
        {
            throw new NotImplementedException();
        }

        public Task<TicketStateTracker?> GetState(int id)
        {
            return this._context.TicketStateTrackers.FirstOrDefaultAsync(ts => ts.Id == id);
        }

        public Task<List<TicketStateTracker>?> GetStates(CursorParams @params)
        {
            throw new NotImplementedException();
        }

        public Task<List<TicketStateTracker>?> GetStates()
        {
            return this._context.TicketStateTrackers.ToListAsync();
        }

        public async Task<List<TicketStateTracker>?> TicketAuditTrail(CursorParams @cursorParams, int ticketId)
        {
            List<TicketStateTracker?> finalRecords = new List<TicketStateTracker?>();

            if (cursorParams.Take > 0)
            {
                IQueryable<TicketStateTracker> query = this._context.TicketStateTrackers
                    .Where(t => t.Status != Lambda.Deleted && t.TicketId == ticketId);



                if (!string.IsNullOrEmpty(cursorParams.SearchTerm))
                {
                    string searchTermLower = cursorParams.SearchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.PreviousState.ToLower().Trim().Contains(searchTermLower) ||
                        t.NewState.ToLower().Trim().Contains(searchTermLower) ||
                        t.Reason.ToLower().Trim().Contains(searchTermLower));
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
                   .Include(t => t.CreatedBy)
                   .Include(t => t.Ticket)
                    .ToListAsync();

               
            }

            return finalRecords;

        }
        public async Task<int> TicketAuditTrailCountAsync(CursorParams @cursorParams, int ticketId)
        {
            int finalRecords = 0;

            if (cursorParams.Take > 0)
            {
                IQueryable<TicketStateTracker> query = this._context.TicketStateTrackers
                    .Where(t => t.Status != Lambda.Deleted && t.TicketId == ticketId);

                if (!string.IsNullOrEmpty(cursorParams.SearchTerm))
                {
                    string searchTermLower = cursorParams.SearchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.PreviousState.ToLower().Trim().Contains(searchTermLower) ||
                        t.NewState.ToLower().Trim().Contains(searchTermLower) ||
                        t.Reason.ToLower().Trim().Contains(searchTermLower));
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

        public void Remove(TicketStateTracker ticketStateTracker)
        {
            ticketStateTracker.Status = Lambda.Deleted;
            ticketStateTracker.DeletedDate = DateTime.UtcNow;
        }

        public Task<int> TotalCount()
        {
            return this._context.TicketStateTrackers.CountAsync(ts => ts.Status != Lambda.Deleted);
        }

        public Task<int> TotalCountFiltered(CursorParams @params)
        {
            throw new NotImplementedException();
        }
    }
}
