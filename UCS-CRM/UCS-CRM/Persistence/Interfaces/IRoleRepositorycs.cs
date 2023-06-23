using Microsoft.AspNetCore.Identity;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IRoleRepositorycs
    {
        IQueryable<Role> GetRoles(CursorParams @params);

        Task<Role> GetRoleAsync(string id);

        Task<List<Role>> GetRolesAsync();
        Task<bool> Exists(string name);
        Task<IdentityResult> remove(string id);
        void AddRole(Role identityRole);
        Task<IdentityResult> UpdateRoleAsync(Role identityRole);
        int TotalCount();
    }
}
