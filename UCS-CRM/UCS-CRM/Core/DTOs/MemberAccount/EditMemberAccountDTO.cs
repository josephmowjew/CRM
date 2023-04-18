using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.MemberAccount
{
    public class EditMemberAccountDTO
    {
        public int Id { get; set; }
        public decimal AccountBalance { get; set; }
        [Required]
        public int AccountTypeId { get; set; }
        [Required]
        public int MemberId { get; set; }

    }
}
