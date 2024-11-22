
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IFailedRegistrationRepository
    {
        Task AddAsync(FailedRegistration failedRegistration);
        Task<int> GetUnresolvedCountAsync();
        Task<List<FailedRegistration>> GetUnresolvedAsync();
        Task MarkAsResolvedAsync(int id, string resolvedBy, string notes);
    }
}