using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Core.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            this._userManager = userManager;
        }
        public void Add(Member member)
        {
            this._context.Add(member);
        }

        public void DeleteUser(Member member)
        {
            member.User.Status = Lambda.Deleted;
            member.User.DeletedDate = DateTime.Now;

        }

        public async Task<ApplicationUser?> CreateUserAccount(Member member, string email,string password = "")
        {
            string DEFAULT_PASSWORD = "P@$$w0rd";
            //create a user record from the member information

            ApplicationUser user = new()
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                Gender = member.Gender,
                Email = email,
                PhoneNumber = member.PhoneNumber,
                UserName = email,
                MemberId = member.Id,
                EmailConfirmed = true,
            };


            var recordPresence = this._context.Users.FirstOrDefault(u => u.Email == email);

            if (recordPresence != null)
            {
                if(recordPresence.Status == Lambda.Deleted)
                {
                    recordPresence.Status = Lambda.Active;

                   
                }
                
                    return recordPresence;
               
               
            }
            else
            {

                 await this._userManager.CreateAsync(user, password ?? DEFAULT_PASSWORD);
               
               

                var roleResult =  await this._userManager.AddToRoleAsync(user, "Client");

                if(roleResult.Succeeded)
                {
                    return user;
                }

                return null;
            }
        }

        public Member? Exists(Member member)
        {
            return this._context.Members.FirstOrDefault(m => m.AccountNumber.Trim().ToLower() == member.AccountNumber.Trim().ToLower());
        }

        public async Task<Member?> GetMemberAsync(int id)
        {
            Member? databaseMember = await this._context.Members.Include(m => m.User).FirstOrDefaultAsync(m =>m.Id == id);

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
                   
                    var records = (from tblOb in await this._context.Members.Include(m => m.User).Where(m => m.Status != Lambda.Deleted).Take(cursorParams.Take).Skip(cursorParams.Skip).ToListAsync() select tblOb);

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

                    var records = (from tblOb in await this._context.Members.Include(m => m.User)
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

        public async Task<Member?> GetMemberByNationalId(string nationalId)
        {
            return await this._context.Members.FirstOrDefaultAsync(m => m.NationalId == nationalId);
        }
    }
}
