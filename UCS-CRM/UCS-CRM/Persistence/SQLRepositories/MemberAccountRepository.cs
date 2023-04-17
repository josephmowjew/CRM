using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class MemberAccountRepository : IMemberAccountRepository
    {
        private readonly ApplicationDbContext _context;
        public MemberAccountRepository(ApplicationDbContext context)
        {
            this._context = context;
        }
        public void Add(MemberAccount memberAccount)
        {
             this._context.MemberAccounts.Add(memberAccount);
        }

        public MemberAccount Exists(MemberAccount memberAccount)
        {
            throw new NotImplementedException();
        }

        public async Task<MemberAccount> GetMemberAccountAsync(int id)
        {
            return await this._context.MemberAccounts.FirstOrDefaultAsync(m =>  m.Id == id && m.Status != Lambda.Deleted);
        }

        public async Task<List<MemberAccount>> GetMemberAccounts(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var memberAccounts = (from tblOb in await this._context.MemberAccounts.Where(a => a.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take).ToListAsync() select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        memberAccounts = memberAccounts.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return memberAccounts.ToList();

                }
                else
                {
                    //include search text in the query
                    var memberAccounts = (from tblOb in await this._context.MemberAccounts.Include(m => m.AccountType)
                                        .Where(a => a.AccountType.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        .ToListAsync()
                                        select tblOb);

                    memberAccounts = memberAccounts.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return memberAccounts.ToList();

                }

            }
            else
            {
                return null;
            }
        }

        public async Task<List<MemberAccount>?> GetMemberAccountsAsync(int memberId)
        {
            return await this._context.MemberAccounts.Include(m => m.AccountType).Where(m => m.MemberId == memberId).ToListAsync();
        }

        public void Remove(MemberAccount memberAccount)
        {
            //mark the record as removed

            memberAccount.Status = Lambda.Deleted;
            memberAccount.DeletedDate= DateTime.UtcNow;
        }

        public async Task<int> TotalCount()
        {
            return await this._context.MemberAccounts.CountAsync(m => m.Status != Lambda.Deleted);
        }
    }
}
