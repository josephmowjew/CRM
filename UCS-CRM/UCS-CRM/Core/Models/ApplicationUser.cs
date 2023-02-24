using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCS_CRM.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(maximumLength: 70, MinimumLength = 2)]
        public string FirstName { get; set; }
        [Required]
        [StringLength(maximumLength:70, MinimumLength =2)]
        public string LastName { get; set; }
        [Required]
        [StringLength(maximumLength:10,MinimumLength = 3)]
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }

        [StringLength(maximumLength:15, MinimumLength = 10)]
        public string PhoneNumber { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; } = DateTime.Now;

        public DateTime? DeletedDate { get; set; }

        public string Status { get; set; }

        [NotMapped]
        public String FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

    }
}
