using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.AccountType
{
    public class EditAccountTypeDTO
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength: 150, MinimumLength = 5)]
        public string Name { get; set; }
        public string DataInvalid { get; set; }
    }
}
