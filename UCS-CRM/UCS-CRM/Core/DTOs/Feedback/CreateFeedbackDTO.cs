using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.Feedback
{
    public class CreateFeedbackDTO
    {

        [Required]
        [StringLength(maximumLength: 255, MinimumLength = 2)]
        public string Description { get; set; }
        [Required]
        [Display(Name ="Rate (1 = Lowest, 5 = Average and 10 = Highest")]
        public int Rating { get; set; }
        public string DataInvalid { get; set; } = "true";

    }
}
