using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IWorkingHoursRepository
    {
        Task<WorkingHours> GetWorkingHours();
        Task UpdateWorkingHours(WorkingHours workingHours);
        Task<bool> HasWorkingHours();
        Task<WorkingHours> GetActiveWorkingHours();
        void AddWorkingHours(WorkingHours workingHours);
    }
}
