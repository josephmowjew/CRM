using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketComment
{
    public class ReadTicketCommentDTO
    {
        public int Id { get; set; }
        [Required]
        public int TicketId { get; set; }
        [Required]
        public string Comment { get; set; }
        public UCS_CRM.Core.Models.Ticket Ticket { get; set; }
    }
}
