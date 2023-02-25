using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketAttachementRepository
    {
        void Add(TicketAttachment ticketAttachment);
        TicketAttachment Exists(TicketAttachment ticketAttachment);
        Task<List<TicketAttachment>> GetTicketAttachmentsAsync(int ticketId);
        Task<TicketAttachment> GetTicketAttachmentAsync(int id);
        void Remove(TicketAttachment ticketAttachment);
    }
}
