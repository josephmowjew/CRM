using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.MemberAccount
{
    public class ReadMemberAccountDTO 
    {
        public int Id { get; set; }

        public ReadMemberAccountDTO()
        {
            RelatedAccounts = new();
        }
        public string? MemberId { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public decimal Balance { get; set; }
        public List<ReadRelatedAccountDTO>? RelatedAccounts { get; set; }
        public Models.AccountType? AccountType { get; set; }

        public string FormattedAmount => Balance.ToString("MWK 0.00");
    }
}
