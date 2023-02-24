using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class State : Meta
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength:255,MinimumLength =2)]
        public string Name { get; set; }
    }
}
