using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class TicketCategoryRepository : ITicketCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(TicketCategory accountType)
        {
            this._context.TicketCategories.Add(accountType);
        }

        public TicketCategory? Exists(string name, int id)
        {
            return this._context.TicketCategories.FirstOrDefault(a => a.Name.ToLower() == name.ToLower() & a.Status != Lambda.Deleted && a.Id != id);
        }

        public async Task<TicketCategory?> GetTicketCategory(int id)
        {
            return await this._context.TicketCategories.FirstOrDefaultAsync(x => x.Id == id & x.Status != Lambda.Deleted);
        }

        public async Task<List<TicketCategory>?> GetTicketCategories(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var accountTypes = (from tblOb in await this._context.TicketCategories.Where(a => a.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take).ToListAsync() select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        accountTypes = accountTypes.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return accountTypes.ToList();

                }
                else
                {
                    //include search text in the query
                    var accountTypes = (from tblOb in await this._context.TicketCategories
                                        .Where(a => a.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        .ToListAsync()
                                        select tblOb);

                    accountTypes = accountTypes.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return accountTypes.ToList();

                }

            }
            else
            {
                return null;
            }


        }

        public void Remove(TicketCategory accountType)
        {
            //mark the record as deleted

            accountType.Status = Lambda.Deleted;
            accountType.DeletedDate = DateTime.UtcNow;
        }

        public Task<int> TotalCount()
        {
            return this._context.TicketCategories.CountAsync(a => a.Status != Lambda.Deleted);
        }
    }
}
