namespace UCS_CRM.Core.Models
{
    public class Meta
    {
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedById { get; set; }
        public virtual ApplicationUser CreatedBy { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
