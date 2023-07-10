using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketStateTracker
{
    public class ReadTicketStateTrackerDTO
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string PreviousState { get; set; }
        public string NewState { get; set; }
        public UCS_CRM.Core.Models.Ticket Ticket { get; set; }
        public string Reason { get; set; }
    }
}
