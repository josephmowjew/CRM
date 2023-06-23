using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.ViewModel
{
    public class DepartmentRoleViewModel
    {
        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        [Required]
        [Display(Name = "Position")]
        public string RoleId { get; set; }

        public string? DataInvalid { get; set; } = "true";
    }
}
