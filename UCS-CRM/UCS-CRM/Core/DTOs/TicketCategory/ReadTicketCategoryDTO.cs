using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketCategory
{
    public class ReadTicketCategoryDTO
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
