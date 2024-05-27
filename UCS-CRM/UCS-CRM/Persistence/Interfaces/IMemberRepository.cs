using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IMemberRepository
    {
        void Add(Member member);

        void AddRange(List<Member> members);

        Task AddRangeAsync(List<Member> members);

        Member Exists(Member? member);

        Task<Member?> ExistsAsync(Member member);
        Task<List<Member>?> GetMembers(CursorParams cursorParams);
        Task<Member?> GetMemberAsync(int id);
        void Remove(Member member);
        Task<int> TotalCount();
        void DeleteUser(Member member);
        Task<Member?> GetMemberByNationalId(string nationalId);
        Task<Member?> GetLastMemberByFidxno();

        Task<List<Member>> GetMembersWithNoUserAccount();
        Task<List<Member>?> GetMembers();
        Task<ApplicationUser?> CreateUserAccount(Member member, string email, string password = "", string createdBy = "");
        Task<Member?> GetMemberByUserId(string userId);
        int RandomNumber();

    }
}
