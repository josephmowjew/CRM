using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketStateTracker
{
    public class CreateTicketStateTrackerDTO
    {
        public int TicketId { get; set; }
        [Required]
        public string PreviousState { get; set; }
        [Required]
        public string NewState { get; set; }
        [StringLength(200)]
        [Required]
        public string Reason { get; set; }
    }
}
