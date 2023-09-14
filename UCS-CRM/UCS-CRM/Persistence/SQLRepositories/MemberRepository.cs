using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Core.ViewModels;
using Microsoft.AspNetCore.Identity;
using NuGet.Protocol.Core.Types;

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

            int pin = RandomNumber();

            var department = await this._context.Departments.FirstOrDefaultAsync(d => d.Name.Trim().ToLower() == "Customer Service and Member Engagement".Trim().ToLower());

            var branch = await this._context.Branches.FirstOrDefaultAsync(b => b.Name.Trim().ToLower() == member.Branch.Trim().ToLower());



            ApplicationUser user = new()
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                Gender = member.Gender,
                Email = email,
                PhoneNumber = member.PhoneNumber,
                UserName = email,
                MemberId = member.Id,
                EmailConfirmed = false,
                Pin = pin,
                LastPasswordChangedDate = DateTime.Now,
                
            };

            if(department != null)
            {
                user.DepartmentId = department.Id;
            }

            if(branch != null)
            {
                user.BranchId = branch.Id;
            }


            var recordPresence = this._context.Users.FirstOrDefault(u => u.Email == email);

            if (recordPresence != null)
            {
                if(recordPresence.Status == Lambda.Deleted)
                {
                    recordPresence.Status = Lambda.Active;

                   
                }

                //associate the user with this member id 
                if(recordPresence.MemberId != member.Id)
                {
                    recordPresence.MemberId = member.Id;
                }
                
                    return recordPresence;
               
               
            }
            else
            {

                 await this._userManager.CreateAsync(user, password ?? DEFAULT_PASSWORD);
               
               

                var roleResult =  await this._userManager.AddToRoleAsync(user, "Member");

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
            Member? databaseMember = await this._context.Members.Include(m => m.User).Include(m => m.MemberAccounts).FirstOrDefaultAsync(m =>m.Id == id);

            //return record if only it has been found or return null
            return databaseMember != null ? databaseMember : null;
            
        }

        public async Task<List<Member>?> GetMembers(CursorParams @params)
        {
            //check if the request actually has a request of number of items to return

            if(@params.Take > 0) {

                //check if search parameter was used

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                   


                    var records = (from tblOb in  this._context.Members.Include(m => m.MemberAccounts).Include(m => m.User).OrderBy(m => m.Id).Skip(@params.Skip).Take(@params.Take).ToList() select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return records.ToList();
                }
                else
                {
                    //include search query

                    var records = (from tblOb in await this._context.Members.Include(m => m.User).Include(m => m.MemberAccounts).Include(m => m.User)
                                   .Where(m => m.Status != Lambda.Deleted 

                                        && m.FirstName.ToLower().Trim().Contains(@params.SearchTerm) ||
                                           m.LastName.ToLower().Trim().Contains(@params.SearchTerm) ||
                                           m.AccountNumber.ToLower().Trim().Contains(@params.SearchTerm) ||
                                           m.Address.ToLower().Trim().Contains(@params.SearchTerm))
                                   .Skip(@params.Skip)
                                   .Take(@params.Take)
                                   .ToListAsync() select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }

                    return records.ToList();
                }
            }

            return null;
        }

        public async Task<List<Member>?> GetMembers()
        {
            return await this._context.Members.Include(m => m.MemberAccounts).Where(a => a.Status != Lambda.Deleted).ToListAsync();
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

        public async Task<Member?> GetMemberByUserId(string userId)
        {
            return await this._context.Members.FirstOrDefaultAsync(m => m.User.Id == userId);

        }

        //generating a 6 digit number
        public int RandomNumber()
        {
            // generating a random number
            Random generator = new Random();
            int number = generator.Next(100000, 999999);

            return number;
        }
    }
}
