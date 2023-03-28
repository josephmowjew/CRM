namespace UCS_CRM.Core.DTOs.TicketEscalation
{
    public class UpdateTicketEscalationDTO
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public DateTime DateEscalated { get; set; }
        public string? Reason { get; set; }
        public string? SecondEscalationReason { get; set; }
        public int EscalationLevel { get; set; }
        public string? DataInvalid { get; set; }
    }
}
