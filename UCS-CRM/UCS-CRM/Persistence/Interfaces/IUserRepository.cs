using Microsoft.AspNetCore.Identity;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.ViewModels;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IUserRepository
    {
       // public Task<IEnumerable<ApplicationUser>> GetUsers();

        public Task<IEnumerable<ApplicationUser>> GetAllUsers();
        Task<List<UserViewModel>> GetUnconfirmedUsersWithRoles(CursorParams @params);

        Task<List<UserViewModel>> GetUsersWithRoles(CursorParams @params);

        Task<UserViewModel> GetUserWithRole(string email);
        Task<List<UserViewModel>> GetStuff();
        ApplicationUser? Exists(ApplicationUser applicationUser);
        Task<ApplicationUser?> GetSingleUser(string id, bool includeRelated = true);
        void Remove(ApplicationUser applicationUser);

        Task<IdentityResult> AddUserToRoleAsync(ApplicationUser applicationUser, string roleName);

        Task<IdentityResult> CreateUserAsync(ApplicationUser applicationUser, string password);

        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser applicationUser);

        Task<ApplicationUser?> FindByIdAsync(string id);
        Task<ApplicationUser?> FindByIdDeleteInclusiveAsync(string id);

        Task<ApplicationUser?> FindByEmailsync(string email);

        Task<IList<string>> GetRolesAsync(string userId);

        Task<IdentityResult> UpdateAsync(ApplicationUser applicationUser);

        Task<List<UserViewModel>> GetDeletedUsers(CursorParams @params);
        Task<int> TotalCount();

        Task<int> TotalUncomfirmedCount();

        Task<int> TotalDeletedCount();
        Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser applicationUser, IEnumerable<string> roleNames);
        Task<List<ApplicationUser>> GetUsers();
        Task<ApplicationUser?> FindUnconfirmedUserByIdAsync(string id);
        int RandomNumber();
        Task<ApplicationUser?> ConfirmUserPin(string id, int pin);
    }
}
