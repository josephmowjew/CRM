using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Message : Meta
    {
        public int Id { get; set; }
        [StringLength(maximumLength: 255, MinimumLength = 2)]
        public string Title { get; set; }
        [StringLength(maximumLength:255,MinimumLength =2)]
        public string Description { get; set; }
        public string Mode { get; set; } //sms/email
        [StringLength(maximumLength:150, MinimumLength =5)]
        public string Action { get; set; } // function
    }
}
