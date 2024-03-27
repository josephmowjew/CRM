using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IErrorLogRepository
    {
        Task AddAsync(ErrorLog errorDetails);
    }
}
