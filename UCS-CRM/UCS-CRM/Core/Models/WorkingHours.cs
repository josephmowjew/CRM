using System;
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class WorkingHours
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Day of Week")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Display(Name = "Break Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan BreakStartTime { get; set; }

        [Required]
        [Display(Name = "Break End Time")]
        [DataType(DataType.Time)]
        public TimeSpan BreakEndTime { get; set; }

        [Required]
        [Display(Name = "Is Working Day")]
        public bool IsWorkingDay { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
} 