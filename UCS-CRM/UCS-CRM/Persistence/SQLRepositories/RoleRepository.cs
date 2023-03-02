using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class RoleRepository : IRoleRepositorycs
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        public RoleRepository(ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }
        public async Task<IdentityResult> AddRole(IdentityRole identityRole)
        {
            return await this._roleManager.CreateAsync(identityRole);
        }

        public async Task<IdentityResult> UpdateRoleAsync(IdentityRole identityRole)
        {
            return await this._roleManager.UpdateAsync(identityRole);
        }
        public IQueryable<IdentityRole> GetRoles(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var identityRolesList = (from tblObj in _roleManager.Roles.Skip(@params.Skip).Take(@params.Take) select tblObj);

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        identityRolesList = identityRolesList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return identityRolesList;

                }
                else
                {
                    //include search text in the query
                    var identityRolesList = (from tblOb in  _roleManager.Roles.Where(r => r.Name.ToLower().Trim().Contains(@params.SearchTerm))
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        select tblOb);

                    identityRolesList = identityRolesList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return identityRolesList;

                }

            }

            return null;
        }
        public async Task<bool> Exists(string name)
        {
            var identityRole = await _roleManager.FindByNameAsync(name);

           return (identityRole != null ? true : false);
        }

        public async Task<IdentityResult> remove(string id)
        {
            var record = await _roleManager.FindByIdAsync(id);

            var result =  await _roleManager.DeleteAsync(record);

            return result;

            
        }

        public async Task<IdentityRole> GetRoleAsync(string id)
        {
            return await this._roleManager.FindByIdAsync(id);
        }

         public int TotalCount()
        {
            return  this._roleManager.Roles.Count();
        }
    }
}
