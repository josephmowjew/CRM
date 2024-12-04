using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Services
{
    public interface IFintechMemberService
    {
       
        Task<(List<Datum>, string, bool)> GetFintechMembersAsync(int take, long Fidxno);
        Task<List<long>> SyncFintechMembersWithLocalDataStore();
        Task<List<long>> SyncMissingFintechMembers( long? startFidxno = null,
            long? endFidxno = null,
            CancellationToken cancellationToken = default);
        Task<KeyValuePair<bool, string>> CreateAllMemberUserAccounts();

        Task<string> ApiAuthenticate();
    }
}
