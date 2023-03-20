using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Helpers;
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
        public string Status { get; set; } = Lambda.Active;
        public DateTime? ClosedDate { get; set; }
      
        public string? AssignedToId { get; set; }
        [Required]
        public int TicketPriorityId { get; set; }
        [Required]
        public int TicketCategoryId { get; set; }
        
        public int? StateId { get; set; }
        public ICollection<IFormFile>? Attachments { get; set; }

        public string? DataInvalid { get; set; } = "true";

        public EditTicketDTO()
        {
            Attachments = new List<IFormFile>();
        }

       
    }
}
