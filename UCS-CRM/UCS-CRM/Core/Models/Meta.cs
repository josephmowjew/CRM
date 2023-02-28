using UCS_CRM.Core.Helpers;

namespace UCS_CRM.Core.Models
{
    public class Meta
    {
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedById { get; set; }
        public virtual ApplicationUser CreatedBy { get; set; }
        public string Status { get; set; } = Lambda.Active;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public DateTime? DeletedDate { get; set; }
    }
}
