using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCS_CRM.Core.Helpers;

namespace UCS_CRM.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            Members = new List<Member>();
        }
        [Required]
        [StringLength(maximumLength: 70, MinimumLength = 2)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Required]
        [StringLength(maximumLength:70, MinimumLength =2)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        [Required]
       
        public string Gender { get; set; }

        [StringLength(maximumLength:15, MinimumLength = 10)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; } = DateTime.Now;

        public DateTime? DeletedDate { get; set; }

        public string Status { get; set; } = Lambda.Active;

        public DateTime LastLogin { get; set; }

        [NotMapped]
        public String FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }
        public ICollection<Member> Members { get; set; }
        public int? MemberId { get; set; }
        public Member? Member { get; set; }

    }
}
