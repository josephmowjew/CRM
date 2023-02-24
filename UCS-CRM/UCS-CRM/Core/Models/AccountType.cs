using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class AccountType :Meta
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength:150,MinimumLength =5)]
        public string Name { get; set; }
    }
}
