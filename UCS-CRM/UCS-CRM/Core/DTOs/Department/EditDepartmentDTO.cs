using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.Department
{
    public class EditDepartmentDTO
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength: 150)]
        public string Name { get; set; }
        [StringLength(200)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        public string? DataInvalid { get; set; }
    }
}
