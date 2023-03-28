namespace UCS_CRM.Core.Models
{
    public class TicketEscalation :Meta
    {
        public int Id { get; set; }
        public Ticket? Ticket { get; set; }
        public int TicketId { get; set; }
        public DateTime DateEscalated { get; set; }
        public string? Reason { get; set; }
        public int EscalationLevel { get; set; }

    }
}
