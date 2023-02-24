using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class TicketComment : Meta
    {
        public int Id { get; set; }
        [Required]
        public int TicketId { get; set; }
        [Required]
        public string Comment { get; set; }
        public Ticket Ticket { get; set; }
    }
}
