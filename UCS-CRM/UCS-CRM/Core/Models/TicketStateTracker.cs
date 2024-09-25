using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class TicketStateTracker : Meta
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        [Required]
        public string PreviousState { get; set; }
        [Required]
        public string NewState { get; set; }
        public Ticket Ticket { get; set; }
        [StringLength(200)]
        [Required]
        public string Reason { get; set; }
       
    }
}
