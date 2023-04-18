using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.EmailAddress
{
    public class CreateEmailAddressDTO
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        public string? Owner { get; set; } 
        public string? DataInvalid { get; set; }
    }
}
