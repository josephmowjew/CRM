using Microsoft.AspNetCore.Identity;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.ViewModels;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IUserRepository
    {
        public Task<IEnumerable<ApplicationUser>> GetUsers();

        public Task<IEnumerable<ApplicationUser>> GetAllUsers();
        Task<List<UserViewModel>> GetUnconfirmedUsersWithRoles();

        Task<List<UserViewModel>> GetUsersWithRoles();

        Task<UserViewModel> GetUserWithRole(string email);
        ApplicationUser? Exists(ApplicationUser applicationUser);
        Task<ApplicationUser?> GetSingleUser(string id, bool includeRelated = true);
        void Remove(ApplicationUser applicationUser);

        Task<IdentityResult> AddUserToRoleAsync(ApplicationUser applicationUser, string roleName);

        Task<IdentityResult> CreateUserAsync(ApplicationUser applicationUser, string password);

        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser applicationUser);

        Task<ApplicationUser?> FindByIdAsync(string id);

        Task<ApplicationUser?> FindByEmailsync(string email);

        Task<IList<string>> GetRolesAsync(string userId);

        Task<IdentityResult> UpdateAsync(ApplicationUser applicationUser);

        Task<List<UserViewModel>> GetDeletedUsers();
    }
}
