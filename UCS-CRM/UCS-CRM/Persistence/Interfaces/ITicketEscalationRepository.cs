using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketEscalationRepository
    {
        void Add(TicketEscalation accountType);
        TicketEscalation? Exists(TicketEscalation ticketEscalation);
        Task<TicketEscalation?> GetTicketEscalation(int id);
        Task<List<TicketEscalation>?> GetTicketEscalations();
        Task<List<TicketEscalation>?> GetTicketEscalations(int? departmentId, CursorParams @params);
        Task<List<TicketEscalation>?> GetTicketEscalationsForUser(CursorParams @params, string memberId);
        Task<int> GetTicketEscalationsCount(int? departmentId, CursorParams @params);
        void Remove(TicketEscalation accountType);
        Task<int> TotalCount();
        Task<int> TotalCountForUser(string user);
    }
}