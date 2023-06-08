using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class EmailAddressRepository : IEmailAddressRepository
    {
        private readonly ApplicationDbContext _context;

        public EmailAddressRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(EmailAddress emailAddress)
        {
            //add record to the database
            this._context.EmailAddresses.Add(emailAddress);
        }

        public EmailAddress? Exists(string name)
        {
            return this._context.EmailAddresses.FirstOrDefault(s => name.ToLower().Trim() == s.Email.Trim().ToLower() && s.Status != Lambda.Deleted);
        }

        public EmailAddress? DefaultEmailAddress(string name)
        {
            return this._context.EmailAddresses.FirstOrDefault(s => name.ToLower().Trim() == s.Email.Trim().ToLower() && s.Status != Lambda.Deleted);
        }

        public async Task<EmailAddress?> GetEmailAddressAsync(int id)
        {
            return await this._context.EmailAddresses.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<EmailAddress>?> GetEmailAddresses(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var emailAddressList = (from tblObj in _context.EmailAddresses.Where(s => s.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take) select tblObj);

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        emailAddressList = emailAddressList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return emailAddressList.ToList();

                }
                else
                {
                    //include search text in the query
                    var emailAddressList = (from tblOb in _context.EmailAddresses.Where(s => s.Email.ToLower().Trim().Contains(@params.SearchTerm.ToLower().Trim()) && s.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                            select tblOb);

                    emailAddressList = emailAddressList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return emailAddressList.ToList();

                }

            }

            return null;
        }

        public async Task<List<EmailAddress>?> GetEmailAddresses()
        {
            return await this._context.EmailAddresses.Where(a => a.Status != Lambda.Deleted).ToListAsync();
        }

        public void Remove(EmailAddress emailAddress)
        {
            emailAddress.DeletedDate = DateTime.Now;
            emailAddress.Status = Lambda.Deleted;
        }

        public async Task<int> TotalActiveCount()
        {
            return await this._context.EmailAddresses.CountAsync(s => s.Status == Lambda.Active);
        }

        public async Task<int> TotalDeletedCount()
        {
            return await this._context.EmailAddresses.CountAsync(s => s.Status == Lambda.Deleted);
        }

        public async Task<EmailAddress?> GetEmailAddress(int id)
        {
            return await this._context.EmailAddresses.FirstOrDefaultAsync(x => x.Id == id & x.Status != Lambda.Deleted);
        }

        // get by account 
        public async Task<EmailAddress?> GetEmailAddressByOwner(string owner)
        {
            return await this._context.EmailAddresses.FirstOrDefaultAsync(x => x.Owner == owner & x.Status != Lambda.Deleted);
        }

    }
}
