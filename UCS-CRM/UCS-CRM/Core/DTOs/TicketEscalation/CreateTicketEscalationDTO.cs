using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketEscalation
{
    public class CreateTicketEscalationDTO
    {
        public int TicketId { get; set; }
        public DateTime? DateEscalated { get; set; } = DateTime.Now;
        public string? Reason { get; set; }
        [Display(Name ="Reason")]
        public string? SecondEscalationReason { get; set; }
        public int? EscalationLevel { get; set; }
        public string? DataInvalid { get; set; }
    }
}
