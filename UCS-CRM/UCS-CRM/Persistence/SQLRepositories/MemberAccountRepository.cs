using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class MemberAccountRepository : IMemberAccountRepository
    {
        public void Add(MemberAccount memberAccount)
        {
            throw new NotImplementedException();
        }

        public MemberAccount Exists(MemberAccount memberAccount)
        {
            throw new NotImplementedException();
        }

        public Task<MemberAccount> GetMemberAccountAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<MemberAccount>> GetMemberAccounts(int memberId)
        {
            throw new NotImplementedException();
        }

        public void Remove(MemberAccount memberAccount)
        {
            //mark the record as removed

            memberAccount.Status = Lambda.Deleted;
            memberAccount.DeletedDate= DateTime.UtcNow;
        }

        public Task<int> TotalCount()
        {
            throw new NotImplementedException();
        }
    }
}
