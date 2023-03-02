using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface ITicketCategoryRepository
    {
        void Add(TicketCategory accountType);
        TicketCategory? Exists(string name);
        Task<List<TicketCategory>?> GetTicketCategories(CursorParams @params);
        Task<TicketCategory?> GetTicketCategory(int id);
        void Remove(TicketCategory accountType);
        Task<int> TotalCount();
    }
}