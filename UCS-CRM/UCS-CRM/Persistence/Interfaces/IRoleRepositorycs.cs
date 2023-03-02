using Microsoft.AspNetCore.Identity;
using UCS_CRM.Core.Helpers;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IRoleRepositorycs
    {
        IQueryable<IdentityRole> GetRoles(CursorParams @params);

        Task<IdentityRole> GetRoleAsync(string id);
        Task<bool> Exists(string name);
        Task<IdentityResult> remove(string id);
        Task<IdentityResult> AddRole(IdentityRole identityRole);
        Task<IdentityResult> UpdateRoleAsync(IdentityRole identityRole);
        int TotalCount();
    }
}
