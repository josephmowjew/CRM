using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Role: IdentityRole
    {
        [MaxLength(255)]
        public override string Id { get; set; } = "";
        [Display(Name = "Role Name")]
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public override string Name { get; set; }
        public string? DataInvalid { get; set; }
    }
}
