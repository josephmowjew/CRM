using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IBranchRepository
    {
        void Add(Branch branch);
        Branch? Exists(string branchName);
        Task<List<Branch>?> GetBranches(CursorParams @params);
        Task<List<Branch>?> GetBranches();
        Task<Branch?> GetBranch(int id);
        void Remove(Branch branch);
        Task<int> TotalCount();
        Task<int> TotalCountFiltered(CursorParams @params);

    }
}
