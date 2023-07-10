
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.Department
{
    public class CreateDepartmentDTO
    {
        [Required]
        [StringLength(maximumLength:150)]
        public string Name { get; set; }
        [StringLength(200)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        public string? DataInvalid { get; set; } = "true";
    }
}
