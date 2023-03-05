using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(100, MinimumLength = 2)]
        public string Email { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 2)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}
