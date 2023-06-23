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
        [Display(Name = "Ticket Number")]
        public string? TicketNumber { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Status { get; set; } = Lambda.Active;
        public DateTime? ClosedDate { get; set; }
        [Display(Name = "Reassign")]
        public string? AssignedToId { get; set; }

        [Display(Name = "Member")]
        public int? MemberId { get; set; }
        //[Required]
        [Display(Name = "Ticket Priority")]
        public int? TicketPriorityId { get; set; }
        [Required]
        [Display(Name = "Ticket Category")]
        public int TicketCategoryId { get; set; }
        [Display(Name = "State")]
        public int? StateId { get; set; }
        [Display(Name = "Attachments")]
        public ICollection<IFormFile>? Attachments { get; set; }

        public string? DataInvalid { get; set; } = "true";
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public EditTicketDTO()
        {
            Attachments = new List<IFormFile>();
        }

       
    }


    public class EditManagerTicketDTO
    {
        public int Id { get; set; }
     
        [StringLength(maximumLength: 255)]
        [Display(Name = "Ticket Number")]
        public string? TicketNumber { get; set; }
      
        [Required]
        public string Status { get; set; } = Lambda.Active;
        public DateTime? ClosedDate { get; set; }
        [Display(Name = "Assignee")]
        public string? AssignedToId { get; set; }

        [Display(Name = "Member")]
        public int? MemberId { get; set; }
        [Required]
        [Display(Name = "Ticket Priority")]
        public int TicketPriorityId { get; set; }
        [Required]
        [Display(Name = "Ticket Category")]
        public int TicketCategoryId { get; set; }
        [Display(Name = "State")]
        public int? StateId { get; set; }
        [Display(Name = "Attachments")]
        public ICollection<IFormFile>? Attachments { get; set; }

        public string? DataInvalid { get; set; } = "true";

        public EditManagerTicketDTO()
        {
            Attachments = new List<IFormFile>();
        }


    }
}
