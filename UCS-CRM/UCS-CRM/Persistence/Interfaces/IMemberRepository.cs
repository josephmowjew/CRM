using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IMemberRepository
    {
        void Add(Member member);
        Member Exists(Member? member);
        Task<List<Member>?> GetMembers(CursorParams cursorParams);
        Task<Member?> GetMemberAsync(int id);
        void Remove(Member member);
        Task<int> TotalCount();
        void DeleteUser(Member member);

        Task<ApplicationUser?> CreateUserAccount(Member member, string email);
        Task<List<Member>?> GetMembers();
    }
}
