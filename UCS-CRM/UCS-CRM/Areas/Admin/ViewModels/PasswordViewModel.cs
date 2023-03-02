using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Areas.Admin.ViewModels
{
    public class PasswordViewModel
    {
        public string Id { get; set; }
        [Display(Name ="New Password")]
        [Required]
        public string NewPassword { get; set; }
        public string DataInvalid { get; set; }
    }
}
