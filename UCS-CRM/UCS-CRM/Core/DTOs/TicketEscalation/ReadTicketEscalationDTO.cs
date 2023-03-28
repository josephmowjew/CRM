namespace UCS_CRM.Core.DTOs.TicketEscalation
{
    public class ReadTicketEscalationDTO
    {
        public int Id { get; set; }
        public Core.Models.Ticket? Ticket { get; set; }
        public int TicketId { get; set; }
        public DateTime DateEscalated { get; set; }
        public string? Reason { get; set; }
        public int EscalationLevel { get; set; }
        public string? DataInvalid { get; set; }
    }
}
