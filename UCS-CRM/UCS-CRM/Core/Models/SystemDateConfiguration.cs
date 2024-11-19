using System;
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Models
{
    public class SystemDateConfiguration
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Time Zone")]
        public string TimeZone { get; set; }
        
        [Required]
        [Display(Name = "Date Format")]
        public string DateFormat { get; set; }
        
        [Required]
        [Display(Name = "First Day of Week")]
        public DayOfWeek FirstDayOfWeek { get; set; }
        
        [Required]
        public bool UseSystemTime { get; set; }
        
        public DateTime? CustomDateTime { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
} 