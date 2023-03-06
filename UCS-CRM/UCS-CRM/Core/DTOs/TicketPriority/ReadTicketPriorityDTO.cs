using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketPriority
{
    public class ReadTicketPriorityDTO
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Priority Level")]
        public int Value { get; set; }
        [Display(Name = "Maximum Response TimeHours")]
        public int MaximumResponseTimeHours { get; set; }
    }
}
