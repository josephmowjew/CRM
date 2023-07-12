using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.Report
{
    public class TicketReportDTO
    {
        [Display(Name= "State")]
        public int? StateId { get; set; }
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }
        [Display(Name = "Branch")]
        public string? Branch { get; set; }
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
    }
}
