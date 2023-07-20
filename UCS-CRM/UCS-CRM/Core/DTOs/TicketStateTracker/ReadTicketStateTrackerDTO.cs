using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.TicketStateTracker
{
    public class ReadTicketStateTrackerDTO: Meta
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string PreviousState { get; set; }
        public string NewState { get; set; }
        public UCS_CRM.Core.Models.Ticket Ticket { get; set; }
        public string Reason { get; set; }
        public string formattedCreatedAt => CreatedDate.ToString("hh:mm tt - dd-MM-yyyy ");
    }
}
