using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketCategoryRepository
    {
        void Add(TicketCategory ticketCategory);
        TicketCategory Exists(TicketCategory ticketCategory);
        Task<List<TicketCategory>> GetTicketCategories();
        Task<TicketCategory> GetTicketCategoryAsync(int id);
        void Remove(TicketCategory ticketCategory);
    }
}
