using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class TicketPriority: Meta
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength: 150, MinimumLength = 2)]
        public string Name { get; set; }
        [Required]
        public int Value { get; set; }
        public int MaximumResponseTimeHours { get; set; }
    }
}
