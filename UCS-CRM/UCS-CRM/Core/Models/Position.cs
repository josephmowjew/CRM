
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Position : Meta
    {
        public Position()
        {
            Users = new();
            Departments = new();
        }
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength:150)]
        public string Name { get; set; }
        public int Rating { get; set; }
        public List<ApplicationUser> Users { get; set; }
        public List<Department> Departments { get; set; }
    }
}
