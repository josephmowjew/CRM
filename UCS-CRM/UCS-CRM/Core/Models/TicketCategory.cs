using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class TicketCategory :Meta
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength:150,MinimumLength =2)]
        public string Name { get; set; }
    }
}
