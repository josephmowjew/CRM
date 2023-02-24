using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class TicketAttachment
    {
        public int Id { get; set; }

        [StringLength(maximumLength:255)
        [Required]
        public string FileName { get; set; }
        [Required]
        [StringLength(maximumLength:255)]
        public string Url { get; set; }
        public int TicketId { get; set; }
        public Ticket  Ticket { get; set; }
    }
}
