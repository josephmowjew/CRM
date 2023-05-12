using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class MemberAccount : Meta
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public decimal Balance { get; set; }
       // public List<RelatedAccount> RelatedAccounts { get; set; }
       // public AccountType? AccountType { get; set; }
    }

}
