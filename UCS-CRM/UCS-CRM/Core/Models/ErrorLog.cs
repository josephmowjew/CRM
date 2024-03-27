using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class ErrorLog: Meta
    {

        public int Id { get; set; }
        [Required]
        public string UserFriendlyMessage { get; set; }
        [Required]
        public string DetailedMessage { get; set; }
        [Required]
        public DateTime DateOccurred { get; set; }

        public string? CreatedById { get; set; }
        public virtual ApplicationUser? CreatedBy { get; set; }
    }
}
