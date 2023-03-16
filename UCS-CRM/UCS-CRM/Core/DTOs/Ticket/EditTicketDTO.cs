using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.Ticket
{
    public class EditTicketDTO
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
      
        [StringLength(maximumLength: 255)]
        public string? TicketNumber { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Status { get; set; }
        public DateTime? ClosedDate { get; set; }
        [Required]
        public string AssignedToId { get; set; }
        [Required]
        public int TicketPriorityId { get; set; }
        [Required]
        public int TicketCategoryId { get; set; }
        [Required]
        public int StateId { get; set; }

        public ICollection<TicketAttachment> TicketAttachments { get; set; }
        public ICollection<TicketComment> TicketComments { get; set; }

        public string DataInvalid { get; set; } = "true";

        public EditTicketDTO()
        {
            TicketAttachments = new List<TicketAttachment>();
            TicketComments = new List<TicketComment>();
        }
    }
}
