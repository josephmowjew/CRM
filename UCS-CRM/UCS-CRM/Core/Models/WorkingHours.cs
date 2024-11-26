using System;
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class WorkingHours :Meta
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }
        
        [Required]
        [Display(Name = "Break Start")]
        public TimeSpan BreakStartTime { get; set; }
        
        [Required]
        [Display(Name = "Break End")]
        public TimeSpan BreakEndTime { get; set; }
        

    }
} 