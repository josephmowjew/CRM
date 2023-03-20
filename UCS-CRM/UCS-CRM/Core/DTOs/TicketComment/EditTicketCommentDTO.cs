using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketComment
{
    public class EditTicketCommentDTO
    {
        public int Id { get; set; }
        [Required]
        public int TicketId { get; set; }
        [Required]
        public string Comment { get; set; }
    }
}
