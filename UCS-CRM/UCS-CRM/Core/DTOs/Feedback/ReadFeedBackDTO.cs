using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.Feedback
{
    public class ReadFeedbackDTO
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength: 255, MinimumLength = 2)]
       public string Description { get; set; }
        public int Rating { get; set; }
        public string DataInvalid { get; set; } = "true";
    }
}
