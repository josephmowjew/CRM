using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Services
{
    public interface IFintechMemberService
    {
        Task<List<Datum>> GetAllFintechMembersAsync();
        Task<(List<Datum>, bool)> GetFintechMembersAsync(int take, long Fidxno);
        Task SyncFintechMembersWithLocalDataStore();

        Task<KeyValuePair<bool, string>> CreateAllMemberUserAccounts();

        Task<string> ApiAuthenticate();
    }
}
