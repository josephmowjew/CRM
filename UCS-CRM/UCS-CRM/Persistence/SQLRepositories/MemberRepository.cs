using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly ApplicationDbContext _context;

        public MemberRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Add(Member member)
        {
            this._context.Add(member);
        }

        public void DeleteUser(Member member)
        {
            member.ApplicationUser.Status = Lambda.Deleted;
            member.DeletedDate = DateTime.Now;

        }

        public Member? Exists(Member member)
        {
            return this._context.Members.FirstOrDefault(m => m.AccountNumber.Trim().ToLower() == member.AccountNumber.Trim().ToLower());
        }

        public async Task<Member?> GetMemberAsync(int id)
        {
            Member? databaseMember = await this._context.Members.FirstOrDefaultAsync(m =>m.Id == id);

            //return record if only it has been found or return null
            return databaseMember != null ? databaseMember : null;
            
        }

        public async Task<List<Member>?> GetMembers(CursorParams cursorParams)
        {
            //check if the request actually has a request of number of items to return

            if(cursorParams.Take > 0) {

                //check if search parameter was used

                if (string.IsNullOrEmpty(cursorParams.SearchTerm))
                {
                   
                    var records = (from tblOb in await this._context.Members.Where(m => m.Status != Lambda.Deleted).Take(cursorParams.Take).Skip(cursorParams.Skip).ToListAsync() select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(cursorParams.SortColum) && !string.IsNullOrEmpty(cursorParams.SortDirection))
                    {
                        records = records.AsQueryable().OrderBy(cursorParams.SortColum + " " + cursorParams.SortDirection);

                    }


                    return records.ToList();
                }
                else
                {
                    //include search query

                    var records = (from tblOb in await this._context.Members
                                   .Where(m => m.Status != Lambda.Deleted 
                                        && m.FirstName.ToLower().Trim().Contains(cursorParams.SearchTerm) ||
                                           m.LastName.ToLower().Trim().Contains(cursorParams.SearchTerm) ||
                                           m.AccountNumber.ToLower().Trim().Contains(cursorParams.SearchTerm) ||
                                           m.Address.ToLower().Trim().Contains(cursorParams.SearchTerm))
                                   .Take(cursorParams.Take)
                                   .Skip(cursorParams.Take)
                                   .ToListAsync() select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(cursorParams.SortColum) && !string.IsNullOrEmpty(cursorParams.SortDirection))
                    {
                        records = records.AsQueryable().OrderBy(cursorParams.SortColum + " " + cursorParams.SortDirection);

                    }

                    return records.ToList();
                }
            }

            return null;
        }

        public void Remove(Member member)
        {
               member.Status = Lambda.Deleted;
               member.DeletedDate  = DateTime.Now;
        }

        public async Task<int> TotalCount()
        {
            return await this._context.Members.CountAsync();
        }


    }
}
