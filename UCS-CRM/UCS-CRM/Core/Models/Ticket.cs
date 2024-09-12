using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCS_CRM.Core.Models
{
    public class Ticket : Meta
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
      
        public string? AssignedToId { get; set; }
        public int? MemberId { get; set; }
        [Required]
        public int TicketPriorityId { get; set; }
        [Required]
        public int TicketCategoryId { get; set; }
        [Required]
        public int StateId { get; set; }
        public State State { get; set; }
        public TicketCategory TicketCategory { get; set; }
        public ApplicationUser AssignedTo { get; set; }
        public Member Member { get; set; }
        public TicketPriority TicketPriority { get; set; }

        public ICollection<TicketAttachment>? TicketAttachments { get; set; }
        public ICollection<TicketComment>? TicketComments { get; set; }

        public List<TicketEscalation> TicketEscalations { get; set; }
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string? InitiatorUserId { get; set; }
        public int? InitiatorMemberId { get; set; }

        [ForeignKey("InitiatorUserId")]
        public ApplicationUser? InitiatorUser { get; set; }

        [ForeignKey("InitiatorMemberId")]
        public Member? InitiatorMember { get; set; }

        public Ticket()
        {
            TicketAttachments = new List<TicketAttachment>();
            TicketComments  = new List<TicketComment>();
            TicketEscalations = new List<TicketEscalation>();
        }

        public object GetInitiator()
        {
            return InitiatorUser ?? (object)InitiatorMember;
        }
    }
}
