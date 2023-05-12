namespace UCS_CRM.Core.Models
{
    public class RelatedAccount
    {
        public int Id { get; set; }
        public string? MemberId { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public decimal Balance { get; set; } 
    }
}
