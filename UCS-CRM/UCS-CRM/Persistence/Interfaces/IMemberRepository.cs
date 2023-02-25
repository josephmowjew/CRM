using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IMemberRepository
    {
        void Add(Member member);
        Member Exists(Member member);
        Task<List<Member>> GetMembers();
        Task<Member> GetMemberAsync(int id);
        void Remove(Member member);
    }
}
