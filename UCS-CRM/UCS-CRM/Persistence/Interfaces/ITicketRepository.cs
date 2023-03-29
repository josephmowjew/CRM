using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketRepository
    {
        void Add(Ticket ticket);
        Ticket Exists(Ticket? ticket);
        Task<List<Ticket>?> GetTickets(CursorParams @params);
        Task<List<Ticket?>> GetMemberTickets(CursorParams @params, int memberId);
        Task<List<Ticket?>> GetAssignedToTickets(CursorParams @params, string assignedToId);
        Task<Ticket?> GetTicket(int id);
        void Remove(Ticket ticket);
        Task<int> TotalCount();
        Task<int> TotalCountByMember(int memberId);
        Task<int> TotalCountByAssignedTo(string assignedTo);
        Task<int> CountTicketsByStatusAssignedTo(string status, string assignedToId);
        Task<int> CountTicketsByStatusMember(string status, int memberId);
        Task<Ticket> LastTicket();
        Task<int> CountTicketsByPriority(string priority);
        Task<int> CountTicketsByCategory(string category);
        Task<int> CountTicketsByStatus(string state);
    }
}
