using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.Department
{
    public class EditDepartmentDTO
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength: 150)]
        public string Name { get; set; }

        public string? DataInvalid { get; set; }
    }
}
