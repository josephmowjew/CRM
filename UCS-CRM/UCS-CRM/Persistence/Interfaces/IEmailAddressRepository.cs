using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Persistence.Interfaces
{
    public interface IEmailAddressRepository
    {
        void Add(EmailAddress emailAddress);
        EmailAddress? DefaultEmailAddress(string name);
        EmailAddress? Exists(string name);
        Task<EmailAddress?> GetEmailAddress(int id);
        Task<EmailAddress?> GetEmailAddressByOwner(string owner);
        Task<EmailAddress?> GetEmailAddressAsync(int id);
        Task<List<EmailAddress>?> GetEmailAddresses();
        Task<List<EmailAddress>?> GetEmailAddresses(CursorParams @params);
        void Remove(EmailAddress emailAddress);
        Task<int> TotalActiveCount();
        Task<int> TotalDeletedCount();
    }
}