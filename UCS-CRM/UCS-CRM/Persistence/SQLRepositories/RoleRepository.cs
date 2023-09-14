using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class RoleRepository : IRoleRepositorycs
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<Role> _roleManager;
        public RoleRepository(ApplicationDbContext context, RoleManager<Role> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }
        public void AddRole(Role identityRole)
        {
              this._context.Roles.Add(identityRole);
            //return await this._roleManager.CreateAsync(identityRole);
        }

        public async Task<IdentityResult> UpdateRoleAsync(Role identityRole)
        {
            return await this._roleManager.UpdateAsync(identityRole);
        }

        public async Task<Role> GetRole(string id)
        {
            return await this._context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            var roles =  await this._context.Roles.ToListAsync();

            return roles;
        }
        public List<Role> GetRoles(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var identityRolesList = (from tblObj in this._context.Roles.Skip(@params.Skip).Take(@params.Take) select tblObj);

                    if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        identityRolesList = identityRolesList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return identityRolesList.ToList();

                }
                else
                {
                    //include search text in the query
                    var identityRolesList = (from tblOb in  _roleManager.Roles.Where(r => r.Name.ToLower().Trim().Contains(@params.SearchTerm))
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        select tblOb);

                    identityRolesList = identityRolesList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return identityRolesList.ToList();

                }

            }

            return null;
        }
        public async Task<bool> Exists(string name,int rating)
        {
            Role? identityRole = null;
            if (rating != 0)
            {
                identityRole = await this._context.Roles.FirstOrDefaultAsync(r => r.Name.Trim().ToLower() == name.Trim().ToLower() && r.Rating == rating);
            }
             

           return (identityRole != null ? true : false);
        }

        public async Task<IdentityResult> remove(string id)
        {
            var record = await _roleManager.FindByIdAsync(id);

            var result =  await _roleManager.DeleteAsync(record);

            return result;

            
        }

        public async Task<Role> GetRoleAsync(string roleName)
        {
            return await this._roleManager.FindByNameAsync(roleName);
        }

        public async Task<Role> GetRoleByIdAsync(string roleId)
        {
            return await this._roleManager.FindByIdAsync(roleId);
        }



        public int TotalCount()
        {
            return  this._roleManager.Roles.Count();
        }
    }
}
