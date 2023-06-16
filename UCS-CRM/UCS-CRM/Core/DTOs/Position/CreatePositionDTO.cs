
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.Position
{
    public class CreatePositionDTO
    {
        [Required]
        [StringLength(maximumLength:150)]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Position (1 is the highest)")]
        public int Rating { get; set; }
        public string? DataInvalid { get; set; } = "true";
    }
}
