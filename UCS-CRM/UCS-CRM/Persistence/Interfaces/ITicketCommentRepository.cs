using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketCommentRepository
    {

        void Add(TicketComment ticketComment);
        TicketComment? Exists(TicketComment ticketComment);
        Task<List<TicketComment>?> GetTicketCommentsAsync(int ticketId, CursorParams @params);
        Task<TicketComment?> GetTicketCommentAsync(int id);
        void Remove(TicketComment ticketComment);
        Task<int> TotalActiveCount();
    }
}
