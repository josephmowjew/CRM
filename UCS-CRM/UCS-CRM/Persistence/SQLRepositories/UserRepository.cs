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
        private readonly IRoleRepositorycs roleRepositorycs;
        public UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<Role> roleManager, IRoleRepositorycs roleRepositorycs)
        {
            _context = context;
            this._userManager = userManager;
            this._roleManager = roleManager;
            this.roleRepositorycs = roleRepositorycs;
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
            return await this._context.Users.Include(u => u.Branch).Include(u => u.Department).FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == email.ToLower().Trim());
        }

        public async Task<ApplicationUser?> FindByIdAsync(string id)
        {
            return await this._context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id & u.Status != Lambda.Deleted);
        }

        public async Task<ApplicationUser?> FindDeletedUserByEmail(string email)
        {
            return await this._context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<ApplicationUser?> FindByIdDeleteInclusiveAsync(string id)
        {
            return await this._context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ApplicationUser?> FindUnconfirmedUserByIdAsync(string id)
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
                var tempUsers = this._context.Users.Where(u => u.Status == Lambda.Deleted);

                users = await tempUsers
                    .Where(u => 
                               u.FirstName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
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



        public async Task<Role> GetRoleAsync(string userId)
        {
            //find user based on the id provided

            var user = await this._userManager.FindByIdAsync(userId);


            var roleId =  this._userManager.GetRolesAsync(user).Result.FirstOrDefault();


            return await this.roleRepositorycs.GetRoleAsync(roleId);
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
                var tempUsers = this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == false);

                users = await tempUsers
                    .Where(u =>
                              u.FirstName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
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

                    if (currentRole.ToLower().Trim() != "Member".ToLower().Trim() && currentRole.Trim().ToLower() != "Administrator".Trim().ToLower() && currentRole.Trim().ToLower() != "System".Trim().ToLower())
                    {
                        userViewModels.Add(u);
                    }

                }


            });

            return userViewModels;



        }

        public async Task<List<UserViewModel>> GetUsersWithRoles(CursorParams @params)
        {
            // Initialize the mapper
            var config = new MapperConfiguration(cfg => cfg.CreateMap<ApplicationUser, UserViewModel>());
            Mapper mapper = new Mapper(config);

            // Initialize the user query
            IQueryable<ApplicationUser> userQuery = _context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == true);

            // Check if the search term has been provided
            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                var searchTerms = @params.SearchTerm.ToLower().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (searchTerms.Length == 2)
                {
                    var firstNameTerm = searchTerms[0];
                    var lastNameTerm = searchTerms[1];

                    userQuery = userQuery.Where(u => 
                        (u.FirstName.ToLower().Contains(firstNameTerm) && u.LastName.ToLower().Contains(lastNameTerm)) ||
                        (u.FirstName.ToLower().Contains(lastNameTerm) && u.LastName.ToLower().Contains(firstNameTerm))
                    );
                }
                else
                {
                    foreach (var term in searchTerms)
                    {
                        userQuery = userQuery.Where(u =>
                            u.FirstName.ToLower().Contains(term) ||
                            u.LastName.ToLower().Contains(term) ||
                            u.Gender.ToLower().Contains(term) ||
                            u.Email.ToLower().Contains(term) ||
                            u.PhoneNumber.ToLower().Contains(term)
                        );
                    }
                }
            }

            // Apply pagination
            List<ApplicationUser> users = await userQuery.Skip(@params.Skip).Take(@params.Take).ToListAsync();

            // Convert the list of user records to a list of UserViewModels
            var userViewModelUnfiltered = mapper.Map<List<UserViewModel>>(users);
            List<UserViewModel> userViewModels = new();

            // Process roles for each user
            foreach (var userViewModel in userViewModelUnfiltered)
            {
                var roles = await GetRolesAsync(userViewModel.Id);

                if (roles.Count > 0)
                {
                    userViewModel.RoleName = roles.First();

                    // Skip user with system role
                    if (!string.Equals(userViewModel.RoleName, "System", StringComparison.OrdinalIgnoreCase))
                    {
                        userViewModels.Add(userViewModel);
                    }
                }
            }

            return userViewModels;
        }

        public async Task<UserViewModel> GetUserWithRole(string email, bool confirmAccountsCheck = true)
        {
            //Initialize the mapper
            var config = new MapperConfiguration(cfg =>
                    cfg.CreateMap<ApplicationUser, UserViewModel>()
                );

            Mapper mapper = new Mapper(config);

            ApplicationUser? user = null;

            if (confirmAccountsCheck)
            {
                user = await this._context.Users.Include(u => u.Department).Where(u => u.Email == email && u.Status != Lambda.Deleted && u.EmailConfirmed == true).FirstOrDefaultAsync();

            }
            else
            {
                user = await this._context.Users.Include(u => u.Department).Where(u => u.Email == email && u.Status != Lambda.Deleted).FirstOrDefaultAsync();

            }

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

        public async Task<List<ApplicationUser>?> GetUsersInRole(string roleName)
        {
            return this._userManager.GetUsersInRoleAsync(roleName).Result.ToList();
        }

        public async Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser applicationUser, IEnumerable<string> roleNames)
        {
            return await this._userManager.RemoveFromRolesAsync(applicationUser, roleNames);
        }

        public async Task<int> TotalCount()
        {
            return await this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == true).CountAsync();
        }

        public async Task<int> TotalFilteredUsersCount(CursorParams @params)
        {
            IQueryable<ApplicationUser> userQuery = _context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == true);

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                var searchTerms = @params.SearchTerm.ToLower().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (searchTerms.Length == 2)
                {
                    var firstNameTerm = searchTerms[0];
                    var lastNameTerm = searchTerms[1];

                    userQuery = userQuery.Where(u => 
                        (u.FirstName.ToLower().Contains(firstNameTerm) && u.LastName.ToLower().Contains(lastNameTerm)) ||
                        (u.FirstName.ToLower().Contains(lastNameTerm) && u.LastName.ToLower().Contains(firstNameTerm))
                    );
                }
                else
                {
                    foreach (var term in searchTerms)
                    {
                        userQuery = userQuery.Where(u =>
                            u.FirstName.ToLower().Contains(term) ||
                            u.LastName.ToLower().Contains(term) ||
                            u.Gender.ToLower().Contains(term) ||
                            u.Email.ToLower().Contains(term) ||
                            u.PhoneNumber.ToLower().Contains(term)
                        );
                    }
                }
            }

            return await userQuery.CountAsync();
        }

        public async Task<int> TotalUncomfirmedCount(CursorParams @params)
        {
            List<ApplicationUser> users = new List<ApplicationUser>();

            int count = 0;

            if (string.IsNullOrEmpty(@params.SearchTerm))
            {

                count = await this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == false).CountAsync();

            }
            else
            {
                var tempUsers = this._context.Users.Where(u => u.Status != Lambda.Deleted && u.EmailConfirmed == false);

                count = await tempUsers
                    .Where(u =>
                              u.FirstName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.LastName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Gender.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Email.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.PhoneNumber.ToLower().Contains(@params.SearchTerm.ToLower().Trim()))
                    .CountAsync();

            }

            return count;

        }

        public async Task<int> TotalDeletedCount(CursorParams @params)
        {
            List<ApplicationUser> users = new List<ApplicationUser>();

            int count = 0;

            if (string.IsNullOrEmpty(@params.SearchTerm))
            {

                count = await this._context.Users.Where(u => u.Status == Lambda.Deleted).CountAsync();

            }
            else
            {
                var tempUsers = this._context.Users.Where(u => u.Status == Lambda.Deleted);

                count = await tempUsers
                    .Where(u =>
                               u.FirstName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.LastName.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Gender.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.Email.ToLower().Contains(@params.SearchTerm.ToLower().Trim())
                            || u.PhoneNumber.ToLower().Contains(@params.SearchTerm.ToLower().Trim()))
                    .CountAsync();

            }

            return count;
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationUser applicationUser)
        {
            return await this._userManager.UpdateAsync(applicationUser);
        }
       
        //find user if pin is correct
        public async Task<ApplicationUser?> ConfirmUserPin(string id, int pin)
        {

            return await this._context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Pin == pin && u.DeletedDate == null);

        }

       

        public async Task<ApplicationUser> FindUserByPin(int pin, string email)
        {
            return await this._context.Users.FirstOrDefaultAsync(u => u.Pin == pin && u.Email == email);
        }

        public void ConfirmUserAccount(ApplicationUser applicationUser)
        {
            applicationUser.EmailConfirmed = true;

            //invoke data store synchronization 
        }

        //generating a 6 digit number
        public int RandomNumber()
        {
            // generating a random number
            Random generator = new Random();
            int number = generator.Next(100000, 999999);

            return number;
        }

        public async Task<List<ApplicationUser>> GetUsersByDepartmentAsync(int departmentId)
        {

            return await this._context.Users.Include(u => u.Department).Where(u => u.Department.Id == departmentId).ToListAsync();
        }

    }
}
