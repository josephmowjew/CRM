using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.TicketComment
{
    public class CreateTicketCommentDTO
    {
      
        [Required]
        public int TicketId { get; set; }
        [Required]
        public string Comment { get; set; }

        public string DataInvalid { get; set; } = "true";
    }
}
