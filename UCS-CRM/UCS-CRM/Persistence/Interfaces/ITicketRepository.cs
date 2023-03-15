using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketRepository
    {
        void Add(Ticket ticket);
        Ticket Exists(Ticket? ticket);
        Task<List<Ticket>?> GetTickets(CursorParams @params);
        Task<Ticket?> GetTicket(int id);
        void Remove(Ticket ticket);
        Task<int> TotalCount();
    }
}
