using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Member : Meta
    {
        public int Id { get; set; }

        [StringLength(maximumLength: 10, MinimumLength = 2)]
        public string AccountNumber { get; set; }

        [StringLength(maximumLength: 70, MinimumLength = 5)]
        public string NationaId { get; set; }
        [StringLength(maximumLength: 200, MinimumLength = 5)]
        public string Address { get; set; }
    }
}
