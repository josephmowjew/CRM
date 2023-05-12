namespace UCS_CRM.Core.DTOs.MemberAccount
{
    public class ReadRelatedAccountDTO
    {
        public string? MemberId { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public decimal Balance { get; set; }
    }
}
