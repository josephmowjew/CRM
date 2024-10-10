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

        public void AddRange(List<Member> members)
        {
            this._context.AddRange(members);
        }

        public async Task AddRangeAsync(List<Member> members)
        {
            await this._context.AddRangeAsync(members);
        }

        public void DeleteUser(Member member)
        {
            member.User.Status = Lambda.Deleted;
            member.User.DeletedDate = DateTime.Now;

        }

        public async Task<ApplicationUser?> CreateUserAccount(Member member, string email,string password = "", string createdBy = "")
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

            if(!string.IsNullOrEmpty(createdBy))
            {
                user.CreatedById = createdBy;
            }

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

                var userResult = await this._userManager.CreateAsync(user,DEFAULT_PASSWORD);

                if (userResult.Succeeded)
                {
                    var roleResult = await this._userManager.AddToRoleAsync(user, "Member");

                    if (roleResult.Succeeded)
                    {
                        return user;
                    }

                }
               
               

               


                return null;
            }
        }

        public Member? Exists(Member member)
        {
            return this._context.Members.FirstOrDefault(m => m.AccountNumber.Trim().ToLower() == member.AccountNumber.Trim().ToLower());
        }

        public async Task<Member?> ExistsAsync(Member member)
        {
            return await this._context.Members.FirstOrDefaultAsync(m => m.Fidxno == member.Fidxno);
        }

        public async Task<Member?> GetMemberAsync(int id)
        {
            Member? databaseMember = await this._context.Members.Include(m => m.User).Include(m => m.MemberAccounts).FirstOrDefaultAsync(m =>m.Id == id);

            //return record if only it has been found or return null
            return databaseMember != null ? databaseMember : null;
            
        }

    public async Task<List<Member>?> GetMembers(CursorParams @params)
    {
        if (@params.Take <= 0) return null;

        IQueryable<Member> query = _context.Members
            .Include(m => m.MemberAccounts)
            .Include(m => m.User)
            .Where(m => m.Status != "Deleted");

        if (!string.IsNullOrEmpty(@params.SearchTerm))
        {
            var searchTerms = @params.SearchTerm.ToLower().Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (searchTerms.Length == 2)
            {
                var firstNameTerm = searchTerms[0];
                var lastNameTerm = searchTerms[1];

                query = query.Where(m => 
                    (m.FirstName.ToLower().Contains(firstNameTerm) && m.LastName.ToLower().Contains(lastNameTerm)) ||
                    (m.FirstName.ToLower().Contains(lastNameTerm) && m.LastName.ToLower().Contains(firstNameTerm))
                );
            }
            else
            {
                foreach (var term in searchTerms)
                {
                    query = query.Where(m => m.FirstName.ToLower().Contains(term)
                        || m.LastName.ToLower().Contains(term)
                        || m.AccountNumber.ToLower().Contains(term));
                }
            }
        }

        if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
        {
            query = query.OrderBy($"{@params.SortColum} {@params.SortDirection}");
        }
        else
        {
            query = query.OrderBy(m => m.Id);
        }

        
            return await query.Skip(@params.Skip).Take(@params.Take).ToListAsync();
           
        }

        public async Task<List<Member>?> GetMembersJson(CursorParams @params)
        {
            if (@params.Take <= 0) return null;

            IQueryable<Member> query = _context.Members
                .Include(m => m.MemberAccounts)
                .Include(m => m.User)
                .Where(m => m.Status != "Deleted");

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                var searchTerms = @params.SearchTerm.ToLower().Trim()
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (searchTerms.Length == 2)
                {
                    var firstNameTerm = searchTerms[0];
                    var lastNameTerm = searchTerms[1];

                    query = query.Where(m => 
                        (m.FirstName.ToLower().Contains(firstNameTerm) && m.LastName.ToLower().Contains(lastNameTerm)) ||
                        (m.FirstName.ToLower().Contains(lastNameTerm) && m.LastName.ToLower().Contains(firstNameTerm))
                    );
                }
                else
                {
                    foreach (var term in searchTerms)
                    {
                        query = query.Where(m => m.FirstName.ToLower().Contains(term)
                            || m.LastName.ToLower().Contains(term)
                            || m.AccountNumber.ToLower().Contains(term)
                            || m.EmployeeNumber.ToLower().Contains(term));
                    }
                }
            }

            if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
            {
                query = query.OrderBy($"{@params.SortColum} {@params.SortDirection}");
            }
            else
            {
                query = query.OrderBy(m => m.Id);
            }

                if (!string.IsNullOrEmpty(@params.SearchTerm))
                {
                    return await query.ToListAsync();
                }
                else
                {
                    return await query.Skip(@params.Skip).Take(@params.Take).ToListAsync();
                }    
            }

        public async Task<int> TotalFilteredMembersCount(CursorParams @params)
        {
            IQueryable<Member> memberQuery = _context.Members.Where(m => m.Status != "Deleted");

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                var searchTerms = @params.SearchTerm.ToLower().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (searchTerms.Length == 2)
                {
                    var firstNameTerm = searchTerms[0];
                    var lastNameTerm = searchTerms[1];

                    memberQuery = memberQuery.Where(m => 
                        (m.FirstName.ToLower().Contains(firstNameTerm) && m.LastName.ToLower().Contains(lastNameTerm)) ||
                        (m.FirstName.ToLower().Contains(lastNameTerm) && m.LastName.ToLower().Contains(firstNameTerm))
                    );
                }
                else
                {
                    foreach (var term in searchTerms)
                    {
                        memberQuery = memberQuery.Where(m =>
                            m.FirstName.ToLower().Contains(term) ||
                            m.LastName.ToLower().Contains(term) ||
                            m.AccountNumber.ToLower().Contains(term) ||
                            m.EmployeeNumber.ToLower().Contains(term) ||
                            m.Gender.ToLower().Contains(term) ||
                            m.PhoneNumber.ToLower().Contains(term)
                        );
                    }
                }
            }

            return await memberQuery.CountAsync();
        }

        public async Task<List<Member>?> GetMembers()
        {
            return await this._context.Members.Include(m => m.MemberAccounts).Where(a => a.Status != Lambda.Deleted).ToListAsync();
        }

        public async Task<List<Member>>GetMembersWithNoUserAccount()
        {
            //get member accounts with associated user account
            return await this._context.Members.Include(m => m.User).Where(m => m.User == null).ToListAsync();
        }

       


        public void Remove(Member member)
        {
               member.Status = Lambda.Deleted;
               member.DeletedDate  = DateTime.Now;
        }

        public async Task<int> TotalCount()
        {
            return await this._context.Members.CountAsync(m => m.Status != Lambda.Deleted);
        }

        public async Task<Member?> GetMemberByNationalId(string nationalId)
        {
            return await this._context.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.NationalId.Trim().ToLower() == nationalId.Trim().ToLower());
        }

        public async Task<Member?> GetUnregisteredMemberByNationalId(string nationalId)
        {
            return await this._context.Members
                .Include(m => m.User)
                .Include(m => m.MemberAccounts)
                .FirstOrDefaultAsync(m => 
                    m.NationalId.Trim().ToLower() == nationalId.Trim().ToLower() && 
                    m.AccountStatus.ToLower().Contains("normal"));
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

        public async Task<Member?> GetLastMemberByFidxno()
        {
            return await this._context.Members.OrderByDescending(m => m.Fidxno).FirstOrDefaultAsync();
        }
    }
}
