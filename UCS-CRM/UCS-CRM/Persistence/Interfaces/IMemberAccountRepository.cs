using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IMemberAccountRepository
    {
        void Add(MemberAccount memberAccount);
        MemberAccount Exists(MemberAccount memberAccount);
        Task<List<MemberAccount>> GetMemberAccounts(int memberId);
        Task<MemberAccount> GetMemberAccountAsync(int id);
        void Remove(MemberAccount memberAccount);
    }
}
