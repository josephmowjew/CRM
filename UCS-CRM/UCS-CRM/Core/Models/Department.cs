using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Department : Meta
    {
        public Department()
        {
            Users = new();
            Positions = new();
        }
        public int Id { get; set; }
        [StringLength(maximumLength: 150)]
        public string Name { get; set; }
        public List<ApplicationUser> Users { get; set; }
        public List<Position> Positions { get; set; }
    }
}
