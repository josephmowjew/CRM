using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class CustomTicketReport
    {
        public bool ShowTicketNumber { get; set; } = true;
        public bool ShowTitle { get; set; } = true;
        public bool ShowDescription { get; set; } = true;
        public bool ShowMemberName { get; set; } = true;
        public bool ShowMemberEmployeeNumber { get; set; } = true;
        public bool ShowAssignedTo { get; set; } = true;
        public bool ShowPriority { get; set; } = true;
        public bool ShowCategory { get; set; } = true;
        public bool ShowState { get; set; } = true;
        public bool ShowCreatedDate { get; set; } = true;
        public bool ShowClosedDate { get; set; } = true;
        public bool ShowDepartment { get; set; } = true;
        public bool ShowInitiator { get; set; } = true;
        public bool ShowComments { get; set; } = true;
        public bool ShowEscalations { get; set; } = true;

        // Additional filters
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Branch { get; set; }
        public int? StateId { get; set; }
        public int? CategoryId { get; set; }
        public int? PriorityId { get; set; }
        public int? DepartmentId { get; set; }
        public string? AssignedToId { get; set; }
    }
} 