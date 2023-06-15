using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Department : Meta
    {
        public Department()
        {
            Users = new();
        }
        public int Id { get; set; }
        [StringLength(maximumLength: 150)]
        public List<ApplicationUser> Users { get; set; }
        public string Name { get; set; }
    }
}
