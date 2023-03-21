using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.TicketComment
{
    public class ReadTicketCommentDTO :Meta
    {
        public int Id { get; set; }
        [Required]
        public int TicketId { get; set; }
        [Required]
        public string Comment { get; set; }
        public UCS_CRM.Core.Models.Ticket Ticket { get; set; }

        public string formattedCreatedAt => CreatedDate.ToString("dd-MM-yyyy hh:mm tt");
    }
}
