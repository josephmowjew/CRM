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
        Task<List<TicketEscalation>?> GetTicketEscalations(CursorParams @params);
        void Remove(TicketEscalation accountType);
        Task<int> TotalCount();
    }
}