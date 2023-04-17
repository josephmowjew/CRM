using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.MemberAccount
{
    public class CreateMemberAccountDTO
    {
        public decimal AccountBalance { get; set; }
        [Required]
        public int AccountTypeId { get; set; }
        [Required]
        public int MemberId { get; set; }
        public UCS_CRM.Core.Models.Member Member { get; set; }
        public UCS_CRM.Core.Models.AccountType AccountType { get; set; }
    }
}
