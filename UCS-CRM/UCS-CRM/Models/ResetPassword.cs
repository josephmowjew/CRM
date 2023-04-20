using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Models
{
    public class ResetPassword
    {
        public string Email { get; set; } = null;
        public string Token { get; set; } = null;
        [Required]
        public string Password { get; set; } = null;
        [Compare("Password", ErrorMessage = "The password and confirmation does not match")]
        public string ConfirmPassword { get; set; } = null;
        
    }
}
