using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.ViewModels;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{

    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        public UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<Role> roleManager)
        {
            _context = context;
            this._userManager = userManager;
            this._roleManager = roleManager;
        }

        public async Task<IdentityResult> AddUserToRoleAsync(ApplicationUser applicationUser, string roleName)
        {
            //add user to role
            return await this._userManager.AddToRoleAsync(applicationUser, roleName);
        }

        public async Task<IdentityResult> CreateUserAsync(ApplicationUser applicationUser, string password)
        {
            return await this._userManager.CreateAsync(applicationUser, password);
        }

        public ApplicationUser? Exists(ApplicationUser applicationUser)
        {
            return this._context.Users.FirstOrDefault(u => u.Email == applicationUser.Email && u.Status != Lambda.Deleted);
        }

        public async Task<ApplicationUser?> FindByEmailsync(string email)
        {
            return await this._context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == email.ToLower().Trim());
        }

        public async Task<ApplicationUser?> FindByIdAsync(string id)
        {
            return await this._context.Users.FirstOrDefaultAsync(u => u.Id == id & u.Status != Lambda.Deleted);
        }

        public async Task<ApplicationUser?> FindByIdDeleteInclusiveAsync(string id)
        {
            return await this._context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser applicationUser)
        {
            return await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsers()
        {
            return await this._context.Users.ToListAsync();
        }

        public async Task<List<UserViewModel>> GetDeletedUsers(CursorParams @params)
        {
            //Initialize the mapper
            var config = new MapperConfiguration(cfg =>
                    cfg.CreateMap<ApplicationUser, UserViewModel>()
                );

            Mapper mapper = new Mapper(config);

            List<ApplicationUser> users = new List<ApplicationUser>();

            if (string.IsNullOrEmpty(@params.SearchTerm))
            {

                users = await this._context.Users.Where(u => u.Status == Lambda.Deleted ).Skip(@params.Skip).Take(@params.Take).ToListAsync();

            }
            else
            {
                users = await this._context.Users
                    .Where(u => u.Status == Lambda.Deleted
                            || u.FirstName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.LastName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Gender.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Email.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.PhoneNumber.ToLower().Contains(@params.SearchTerm.ToLower().Trim()))
                    .Skip(@params.Skip)
                    .Take(@params.Take)
                    .ToListAsync();

            }

            //convert the list of users records to a list of UserViewModels

            var userViewModelUnFiltered = mapper.Map<List<UserViewModel>>(users);
            List<UserViewModel> userViewModels = new();

            userViewModelUnFiltered.ForEach(u => {
                //get a list of roles of the particular user
                var roles = this.GetRolesAsync(u.Id).Result;

                if (roles.Count > 0)
                {
                    u.RoleName = roles.First();

                    //add the updated user to the userViewModels class

                    userViewModels.Add(u);
                }


            });

            return userViewModels;
        }

        public async Task<IList<string>> GetRolesAsync(string userId)
        {
            //find user based on the id provided

            var user = await this._userManager.FindByIdAsync(userId);



            return await this._userManager.GetRolesAsync(user);
        }

        public async Task<ApplicationUser?> GetSingleUser(string id, bool includeRelated = true)
        {
           
                return await this._context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedDate == null);

        }

        public async Task<List<UserViewModel>> GetUnconfirmedUsersWithRoles(CursorParams @params)
        {
            //Initialize the mapper
            var config = new MapperConfiguration(cfg =>
                    cfg.CreateMap<ApplicationUser, UserViewModel>()
                );

            Mapper mapper = new Mapper(config);
            List<ApplicationUser> users = new List<ApplicationUser>();


            if (string.IsNullOrEmpty(@params.SearchTerm))
            {

                users = await this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == false).Skip(@params.Skip).Take(@params.Take).ToListAsync();

            }
            else
            {
                users = await this._context.Users
                    .Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == false
                            || u.FirstName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.LastName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Gender.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Email.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.PhoneNumber.ToLower().Contains(@params.SearchTerm.ToLower().Trim()))
                    .Skip(@params.Skip)
                    .Take(@params.Take)
                    .ToListAsync();

            }

            //convert the list of users records to a list of UserViewModels

            var userViewModelUnFiltered = mapper.Map<List<UserViewModel>>(users);
            List<UserViewModel> userViewModels = new();

            userViewModelUnFiltered.ForEach(u => {
                //get a list of roles of the particular user
                var roles = this.GetRolesAsync(u.Id).Result;

                if (roles.Count > 0)
                {
                    u.RoleName = roles.First();

                    //add the updated user to the userViewModels class

                    userViewModels.Add(u);
                }


            });

            return userViewModels;
        }

        public async Task<List<ApplicationUser>> GetUsers()
        {
            return await this._context.Users.Where(u =>  u.EmailConfirmed == true && u.Status != Lambda.Deleted).ToListAsync();

        }

        public async Task<List<UserViewModel>> GetStuff()
        {
            //Initialize the mapper
            var config = new MapperConfiguration(cfg =>
                    cfg.CreateMap<ApplicationUser, UserViewModel>()
                );

            List<ApplicationUser> users = new List<ApplicationUser>();

            Mapper mapper = new Mapper(config);

            //check if the search term has been provided

          
            users = await this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == true).ToListAsync();



            //convert the list of users records to a list of UserViewModels

            var userViewModelUnFiltered = mapper.Map<List<UserViewModel>>(users);
            List<UserViewModel> userViewModels = new();

            userViewModelUnFiltered.ForEach(u => {
                //get a list of roles of the particular user
                var roles = this.GetRolesAsync(u.Id).Result;

                if (roles.Count > 0)
                {
                    u.RoleName = roles.First();

                    string currentRole = roles.First();

                    //add the updated user to the userViewModels class

                    if (currentRole.ToLower().Trim() != "Member".ToLower().Trim() && currentRole.Trim().ToLower() != "Administrator".Trim().ToLower())
                    {
                        userViewModels.Add(u);
                    }

                }


            });

            return userViewModels;



        }

        public async Task<List<UserViewModel>> GetUsersWithRoles(CursorParams @params)
        {
            //Initialize the mapper
            var config = new MapperConfiguration(cfg =>
                    cfg.CreateMap<ApplicationUser, UserViewModel>()
                );

            List<ApplicationUser> users = new List<ApplicationUser>();
            Mapper mapper = new Mapper(config);

            //check if the search term has been provided

            if(string.IsNullOrEmpty(@params.SearchTerm)) {

               users = await this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == true).Skip(@params.Skip).Take(@params.Take).ToListAsync();

            }
            else
            {
                users = await this._context.Users
                    .Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == true 
                            || u.FirstName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.LastName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Gender.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Email.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.PhoneNumber.ToLower().Contains(@params.SearchTerm.ToLower().Trim()))
                    .Skip(@params.Skip)
                    .Take(@params.Take)
                    .ToListAsync();

            }


            //convert the list of users records to a list of UserViewModels

            var userViewModelUnFiltered = mapper.Map<List<UserViewModel>>(users);
            List<UserViewModel> userViewModels = new();

            userViewModelUnFiltered.ForEach(u => {
                //get a list of roles of the particular user
                var roles = this.GetRolesAsync(u.Id).Result;

                if (roles.Count > 0)
                {
                    u.RoleName = roles.First();

                    //add the updated user to the userViewModels class

                    userViewModels.Add(u);
                }


            });

            return userViewModels;
        }

        public async Task<UserViewModel> GetUserWithRole(string email)
        {
            //Initialize the mapper
            var config = new MapperConfiguration(cfg =>
                    cfg.CreateMap<ApplicationUser, UserViewModel>()
                );

            Mapper mapper = new Mapper(config);

            var user = await this._context.Users.Where(u => u.Email == email && u.Status != Lambda.Deleted && u.EmailConfirmed == true).FirstOrDefaultAsync();

            //convert the list of users records to a list of UserViewModels

            UserViewModel userViewModel = null;

            if (user != null)
            {
                var userViewModelUnFiltered = mapper.Map<UserViewModel>(user);




                //get a list of roles of the particular user
                var roles = this.GetRolesAsync(userViewModelUnFiltered.Id).Result;

                if (roles.Count > 0)
                {
                    userViewModelUnFiltered.RoleName = roles.First();

                    //add the updated user to the userViewModels class

                    userViewModel = userViewModelUnFiltered;
                }
            }


            return userViewModel;
        }

        public void Remove(ApplicationUser applicationUser)
        {
            applicationUser.DeletedDate = DateTime.Now;
            //save changes 
        }

        public async Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser applicationUser, IEnumerable<string> roleNames)
        {
            return await this._userManager.RemoveFromRolesAsync(applicationUser, roleNames);
        }

        public async Task<int> TotalCount()
        {
            return await this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == true).CountAsync();
        }

        public async Task<int> TotalUncomfirmedCount()
        {
            return await this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == false).CountAsync();
        }

        public async Task<int> TotalDeletedCount()
        {
            return await this._context.Users.Where(u => u.Status == Lambda.Deleted).CountAsync();
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationUser applicationUser)
        {
            return await this._userManager.UpdateAsync(applicationUser);
        }



       
    }
}
