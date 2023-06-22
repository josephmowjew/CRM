using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.ViewModel
{
    public class DepartmentPositionViewModel
    {
        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        [Required]
        [Display(Name = "Position")]
        public int PositionId { get; set; }

        public string? DataInvalid { get; set; } = "true";
    }
}
