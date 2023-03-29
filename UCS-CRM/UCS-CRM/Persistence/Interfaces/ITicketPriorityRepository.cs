using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketPriorityRepository
    {
        void Add(TicketPriority ticketPriority);
        TicketPriority Exists(string name, int id = 0);
        Task<List<TicketPriority>?> GetTicketPriorities(CursorParams @params);
        Task<List<TicketPriority>?> GetTicketPriorities();
        Task<TicketPriority?> GetTicketPriority(int id);
        void Remove(TicketPriority ticketPriority);
        Task<int> TotalCount();
    }
}