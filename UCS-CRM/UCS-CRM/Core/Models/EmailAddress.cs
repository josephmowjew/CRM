using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class EmailAddress :Meta
    {
        public int Id { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? Owner { get; set; } 
    }
}
