using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;
using System.Linq.Dynamic.Core;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private ApplicationDbContext _context;
        public DepartmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Add(Department department)
        {
            this._context.Departments.Add(department);
        }

        public Department? Exists(string departmentName)
        {

            return this._context.Departments.FirstOrDefault(d => d.Name.Trim().ToLower() == departmentName.Trim().ToLower() && d.Status != Lambda.Deleted);
        }

        public Task<Department?> GetDepartment(int id)
        {
            return this._context.Departments.Include(d => d.Users).Include(u => u.Roles).FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Department>?> GetDepartments(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var departments = (from tblOb in await this._context.Departments.Where(d => d.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take).ToListAsync() select tblOb);


                    if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        departments = departments.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return departments.ToList();

                }
                else
                {
                    //include search text in the query
                    var departments = (from tblOb in await this._context.Departments
                                        .Where(a => a.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        .ToListAsync()
                                        select tblOb);

                    if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        departments = departments.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);
                    }

                    return departments.ToList();

                }

            }
            else
            {
                return null;
            }
        }

        public async Task<List<Department>?> GetDepartments()
        {
            return await this._context.Departments.Include(d => d.Roles).Where(d => d.Status != Lambda.Deleted).ToListAsync();
        }

        public void Remove(Department department)
        {
            department.Status = Lambda.Deleted;
            department.DeletedDate = DateTime.Now;
        }

        public async Task<int> TotalCount()
        {
           return await this._context.Departments.CountAsync(d => d.Status != Lambda.Deleted);
        }

        public async Task<int> TotalCountFiltered(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var departments = (from tblOb in await this._context.Departments.Where(d => d.Status != Lambda.Deleted).ToListAsync() select tblOb);


                    return departments.Count();

                }
                else
                {
                    //include search text in the query
                    var departments = (from tblOb in await this._context.Departments
                                        .Where(a => a.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .ToListAsync()
                                       select tblOb);


                    return departments.Count();

                }

            }
            else
            {
                return 0;
            }
        }
    }
}
