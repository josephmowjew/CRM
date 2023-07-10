namespace UCS_CRM.Core.DTOs.Ticket
{
    public class CloseTicketDTO
    {
        public int Id { get; set; }
        public string Reason { get; set; }

        public string DataInvalid { get; set; } = "true";
    }
}
