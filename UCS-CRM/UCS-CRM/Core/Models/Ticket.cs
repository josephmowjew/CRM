using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Ticket :Meta
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        [StringLength(maximumLength:255)]
        public string TicketNumber { get; set; }
        [Required]
        public string Description { get; set; }
        public DateTime? ClosedDate { get; set; }
        //[Required]
        public string AssignedToId { get; set; }
        [Required]
        public int TicketPriorityId { get; set; }
        [Required]
        public int TicketCategoryId { get; set; }
        [Required]
        public int StateId { get; set; }
        public State State { get; set; }
        public TicketCategory TicketCategory { get; set; }
        public ApplicationUser AssignedTo { get; set; }
        public TicketPriority TicketPriority { get; set; }

        public ICollection<TicketAttachment> TicketAttachments { get; set; }
        public ICollection<TicketComment> TicketComments { get; set; }

        public Ticket()
        {
            TicketAttachments = new List<TicketAttachment>();
            TicketComments  = new List<TicketComment>();
        }

    }
}
