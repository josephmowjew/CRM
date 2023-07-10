using System.ComponentModel.DataAnnotations.Schema;

namespace UCS_CRM.Core.Models
{
    public class TicketEscalation :Meta
    {
        public int Id { get; set; }
        public Ticket? Ticket { get; set; }
        public int TicketId { get; set; }
        public DateTime DateEscalated { get; set; }
        public string? Reason { get; set; }
        //public string? SecondEscalationReason { get; set; }
        //public int EscalationLevel { get; set; }
        public string EscalatedToId { get; set; }
        public ApplicationUser EscalatedTo { get; set; }
        public bool Resolved { get; set; } = false;

        [NotMapped]
        public string formattedDateEscalated => DateEscalated.ToString("dd-MM-yyyy hh:mm tt");

    }
}
