using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketPriority
{
    public class EditTicketPriorityDTO
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength: 150, MinimumLength = 5)]
        public string? Name { get; set; }
        [Required]
        [Display(Name = "Priority Level")]
        public int Value { get; set; }
        [Display(Name = "Maximum Response Time in Hours")]
        public int MaximumResponseTimeHours { get; set; }
        public string? DataInvalid { get; set; }
    }
}
