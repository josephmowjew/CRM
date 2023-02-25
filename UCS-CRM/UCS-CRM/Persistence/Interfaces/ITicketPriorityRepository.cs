using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketPriorityRepository
    {
        void Add(TicketPriority ticketPriority);
        TicketPriority Exists(TicketPriority ticketPriority);
        Task<List<TicketPriority>> GetTicketPriorities();
        Task<TicketPriority> GetTicketPriorityAsync(int id);
        void Remove(TicketPriority ticketPriority);
    }
}
