using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.Ticket
{
    public class CreateTicketDTO
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
     
        [StringLength(maximumLength: 255)]
        [Display(Name = "Ticket Number")]
        public string? TicketNumber { get; set; }
        [Required]
        public string Description { get; set; }

        public string? Status { get; set; } = Lambda.Active;
        public DateTime? ClosedDate { get; set; }
      
        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }

        [Display(Name = "Member")]
        [Required]
        public int MemberId { get; set; }
        [RequiredIfNotRole("Member")]
        [Display(Name = "Ticket Priority")]
        public int? TicketPriorityId { get; set; }
        [Required]
        [Display(Name ="Ticket Category")]
        public int TicketCategoryId { get; set; }
    
        [Display(Name = "State")]
        public int? StateId { get; set; }

        public ICollection<IFormFile>? Attachments { get; set; }

        public string DataInvalid { get; set; } = "true";

        public CreateTicketDTO()
        {
            Attachments = new List<IFormFile>();

        }
        [Required]
        [Display(Name = "Initiator Type (User or Member)")]
        public string InitiatorType { get; set; } // "User" or "Member"
        [Required]
        [Display(Name = "Initiator ID")]
        public string InitiatorId { get; set; } // This will be either UserId or MemberId

    }
}
