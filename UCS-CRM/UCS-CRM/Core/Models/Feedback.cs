namespace UCS_CRM.Core.Models
{
    public class Feedback : Meta
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public int Rating { get; set; }
    }
}
