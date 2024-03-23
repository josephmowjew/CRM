using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCS_CRM.Core.Models
{
    public class Member : Meta
    {
        public Member()
        {
            MemberAccounts = new List<MemberAccount>();
        }
        public int Id { get; set; }
    
        public int Fidxno { get; set; }
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Surname")]
        [StringLength(maximumLength: 70, MinimumLength = 2)]
        public string? LastName { get; set; }
        [Display(Name = "Date Of Birth")]
        public DateTime? DateOfBirth { get; set; }
       
        public string? Gender { get; set; }
        public bool EmailConfirmed { get; set; } = false;

        [StringLength(maximumLength: 40, MinimumLength = 2)]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }

        [StringLength(maximumLength: 70, MinimumLength = 5)]
        public string NationalId { get; set; }
        [StringLength(maximumLength: 200, MinimumLength = 5)]
        public string? Address { get; set; }
        public ApplicationUser User { get; set; }

        [StringLength(maximumLength:20)]
        public string? PhoneNumber { get; set; }
        public string? Branch { get; set; }
        public string? Employer { get; set; }
        public List<MemberAccount> MemberAccounts { get; set; }

        public string? CreatedById { get; set; }
        public virtual ApplicationUser? CreatedBy { get; set; }

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
