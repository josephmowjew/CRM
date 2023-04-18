using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IMemberAccountRepository
    {
        void Add(MemberAccount memberAccount);
        MemberAccount Exists(MemberAccount memberAccount);
        Task<List<MemberAccount>?> GetMemberAccounts(CursorParams cursorParams);
        Task<MemberAccount> GetMemberAccountAsync(int id);
        Task<List<MemberAccount>?> GetMemberAccountsAsync(int memberId);
        void Remove(MemberAccount memberAccount);
        Task<int> TotalCount();
    }
}
