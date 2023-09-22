using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketPriority
{
    public class CreateTicketPriorityDTO
    {
        [Required]
        [StringLength(maximumLength: 150, MinimumLength = 5)]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Priority Level")]
        public int Value { get; set; }
        [Display(Name = "Maximum Response Time in Hours")]
        public double MaximumResponseTimeHours { get; set; }
        public string? DataInvalid { get; set; }
    }
}
