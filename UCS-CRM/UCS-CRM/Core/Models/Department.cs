using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Department : Meta
    {
        public int Id { get; set; }
        [StringLength(maximumLength: 150)]
        public string Name { get; set; }
    }
}
