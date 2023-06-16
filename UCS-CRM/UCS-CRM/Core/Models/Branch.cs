using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Branch : Meta
    {
        public Branch()
        {
            Users = new();
        }
        public int Id { get; set; }
        [StringLength(maximumLength:150)]
        public string Name { get; set; }
        public List<ApplicationUser> Users { get; set;}
    }
}
