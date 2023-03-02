using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketCategory
{
    public class CreateTicketCategoryDTO
    {
        [Required]
        [StringLength(maximumLength: 150, MinimumLength = 5)]
        public string Name { get; set; }
        public string? DataInvalid { get; set; }
    }
}
