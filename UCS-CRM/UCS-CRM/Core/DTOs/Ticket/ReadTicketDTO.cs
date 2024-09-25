using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.Ticket
{
    public class ReadTicketDTO
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        [StringLength(maximumLength: 255)]
        public string TicketNumber { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        [Required]
        public string AssignedToId { get; set; }

        [Display(Name = "Member")]
        public int? MemberId { get; set; }
        [Required]
        public int TicketPriorityId { get; set; }
        [Required]
        public int TicketCategoryId { get; set; }
        [Required]
        public int StateId { get; set; }
        public UCS_CRM.Core.Models.State State { get; set; }
        public UCS_CRM.Core.Models.Member Member { get; set; }
        public UCS_CRM.Core.Models.TicketCategory TicketCategory { get; set; }
        public ApplicationUser AssignedTo { get; set; }
        public UCS_CRM.Core.Models.TicketPriority TicketPriority { get; set; }

        public ICollection<TicketAttachment> TicketAttachments { get; set; }
        public ICollection<Models.TicketComment> TicketComments { get; set; }
        public ICollection<Models.TicketEscalation> TicketEscalations { get; set; }

        public string CreatedById { get; set; }

        public ApplicationUser CreatedBy { get; set; }

        public ICollection<Core.Models.TicketStateTracker>? StateTrackers { get; set; }

        public ReadTicketDTO()
        {
            TicketAttachments = new List<TicketAttachment>();
            TicketComments = new List<UCS_CRM.Core.Models.TicketComment>();
            TicketEscalations = new List<Models.TicketEscalation>();
        }

        public string Period
        {
            get
            {
                TimeSpan diff = DateTime.Now - CreatedDate;
                string period = string.Empty;

                if (diff.TotalDays >= 1)
                {
                    if (diff.TotalDays > 1)
                        period = string.Format("{0:%d} days ago", diff);
                    else
                        period = string.Format("{0:%d} day ago", diff);
                }
                else if (diff.TotalHours >= 1)
                {
                    if (diff.TotalHours > 1)
                        period = string.Format("{0:%h} hours ago", diff);
                    else
                        period = string.Format("{0:%h} hour ago", diff);
                }
                else if (diff.TotalMinutes >= 1)
                {
                    period = string.Format("{0:%m} minutes ago", diff);
                }
                else
                {
                    period = string.Format("{0:%s} seconds ago", diff);
                }

                return period;


            }


        }

        public string InitiatorType { get; set; } // "User" or "Member"
        public string InitiatorId { get; set; }
        public string InitiatorName { get; set; }
        public string InitiatorDepartmentName { get; set; }

    }
}
