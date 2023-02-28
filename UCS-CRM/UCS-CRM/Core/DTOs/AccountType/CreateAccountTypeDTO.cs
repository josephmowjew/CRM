using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.AccountType
{
    public class CreateAccountTypeDTO
    {
        [Required]
        [StringLength(maximumLength: 150, MinimumLength = 5)]
        public string Name { get; set; }
        public string? DataInvalid { get; set; }
    }
}
