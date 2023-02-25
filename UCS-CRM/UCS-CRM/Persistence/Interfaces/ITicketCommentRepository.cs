using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketCommentRepository
    {

        void Add(TicketComment ticketComment);
        TicketComment Exists(TicketComment ticketComment);
        Task<List<TicketComment>> GetTicketCommentsAsync(int ticketId);
        Task<TicketComment> GetTicketCommentAsync(int id);
        void Remove(TicketComment ticketComment);
    }
}
