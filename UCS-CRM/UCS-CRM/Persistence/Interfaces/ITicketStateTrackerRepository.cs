using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketStateTrackerRepository
    {
        void Add(TicketStateTracker ticketStateTracker);
        Branch? Exists(string branchName);
        Task<List<TicketStateTracker>?> GetStates(CursorParams @params);
        Task<List<TicketStateTracker>?> GetStates();
        Task<TicketStateTracker?> GetState(int id);
        void Remove(TicketStateTracker ticketStateTracker);
        Task<int> TotalCount();
        Task<int> TotalCountFiltered(CursorParams @params);
    }
}
