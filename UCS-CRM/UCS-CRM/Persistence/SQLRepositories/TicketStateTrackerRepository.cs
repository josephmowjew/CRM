using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
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
