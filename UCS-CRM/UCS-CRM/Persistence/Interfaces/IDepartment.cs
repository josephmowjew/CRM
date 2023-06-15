using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IDepartmentRepository
    {
        void Add(Department department);
        Department? Exists(string departmentName);
        Task<List<Department>?> GetDepartments(CursorParams @params);
        Task<List<Department>?> GetDepartments();
        Task<Department?> GetDepartment(int id);
        void Remove(Department department);
        Task<int> TotalCount();
        Task<int> TotalCountFiltered(CursorParams @params);

    }
}
