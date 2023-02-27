using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IAccountTypeRepository
    {
        void Add(AccountType accountType);
        AccountType? Exists(AccountType accountType);
        Task<List<AccountType>?> GetAccountTypes(CursorParams @params);
        Task<AccountType?> GetAccountType(int id);
        void Remove(AccountType accountType);
        Task<int> TotalCount();
    }
}
