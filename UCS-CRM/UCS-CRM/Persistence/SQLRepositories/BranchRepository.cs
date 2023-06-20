using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;
using System.Linq.Dynamic.Core;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class BranchRepository : IBranchRepository
    {
        private ApplicationDbContext _context;
        public BranchRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Add(Branch branch)
        {
            this._context.Branches.Add(branch);
        }

        public Branch? Exists(string branchName)
        {

            return this._context.Branches.FirstOrDefault(d => d.Name.Trim().ToLower() == branchName.Trim().ToLower() && d.Status != Lambda.Deleted);
        }

        public Task<Branch?> GetBranch(int id)
        {
            return this._context.Branches.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Branch>?> GetBranches(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var branchs = (from tblOb in await this._context.Branches.Where(d => d.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take).ToListAsync() select tblOb);


                    if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        branchs = branchs.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return branchs.ToList();

                }
                else
                {
                    //include search text in the query
                    var branchs = (from tblOb in await this._context.Branches
                                        .Where(a => a.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        .ToListAsync()
                                       select tblOb);
                    if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        branchs = branchs.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return branchs.ToList();

                }

            }
            else
            {
                return null;
            }
        }

        public async Task<List<Branch>?> GetBranches()
        {
            return await this._context.Branches.Where(d => d.Status != Lambda.Deleted).ToListAsync();
        }

        public void Remove(Branch branch)
        {
            branch.Status = Lambda.Deleted;
            branch.DeletedDate = DateTime.Now;
        }

        public async Task<int> TotalCount()
        {
            return await this._context.Branches.CountAsync();
        }

        public async Task<int> TotalCountFiltered(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var branchs = (from tblOb in await this._context.Branches.Where(d => d.Status != Lambda.Deleted).ToListAsync() select tblOb);


                    return branchs.Count();

                }
                else
                {
                    //include search text in the query
                    var branchs = (from tblOb in await this._context.Branches
                                        .Where(a => a.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .ToListAsync()
                                       select tblOb);


                    return branchs.Count();

                }

            }
            else
            {
                return 0;
            }
        }
    }
}
