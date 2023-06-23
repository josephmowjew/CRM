using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCS_CRM.Core.Models
{
    public class Role: IdentityRole
    {
        public Role():base()
        {
            Departments = new List<Department>();

        }

       

        [NotMapped]
        public string DataInvalid { get; set; } = "true";

        public int Rating { get; set; }

        public List<Department> Departments { get; set; }
    }
}
