
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.Branch
{
    public class CreateBranchDTO
    {
        [Required]
        [StringLength(maximumLength:150)]
        public string Name { get; set; }
        public string? DataInvalid { get; set; } = "true";
    }
}
