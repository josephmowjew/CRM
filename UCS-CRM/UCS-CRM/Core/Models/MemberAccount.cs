using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class MemberAccount : Meta
    {
        public int Id { get; set; }
        public decimal AccountBalance { get; set; }
        [Required]
        public int AccountTypeId { get; set; }
        [Required]
        public int MemberId { get; set; }
        public Member Member { get; set; }
        public AccountType AccountType { get; set; }
    }
}
