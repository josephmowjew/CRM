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
            Departments = new List<Department>();
            Branches = new List<Branch>();
            Escalations = new List<TicketEscalation>();
        }

        [EmailAddress]
        [Display(Name = "Secondary Email")]
        [StringLength(maximumLength: 100, MinimumLength = 5)]
        public string? SecondaryEmail { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SecondaryEmail != null && SecondaryEmail == Email)
            {
                yield return new ValidationResult("Secondary email cannot be the same as the primary email.", new[] { nameof(SecondaryEmail) });
            }
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
        public DateTime? LastPasswordChangedDate { get; set; }  
        public ICollection<Member> Members { get; set; }
        public int? MemberId { get; set; }
        public Member? Member { get; set; }
        [Display(Name = "Department Name")]

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        public ICollection<Department> Departments { get; set; }
        [Display(Name = "Branch")]
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }
        public ICollection<Branch> Branches { get; set; }

        public string? CreatedById { get; set; } = "";
        public virtual ApplicationUser? CreatedBy { get; set; }

        //public int? PositionId { get; set; }
        //public Position? Position { get; set; }
        public ICollection<TicketEscalation> Escalations { get; set; }

        public int Pin { get; set; }

        [NotMapped]
        public String FullName
        {   

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var names = value.Split(' ');
                    if (names.Length > 1)
                    {
                        FirstName = names[0];
                        LastName = string.Join(" ", names.Skip(1));
                    }
                    else
                    {
                        FirstName = value;
                        LastName = string.Empty;
                    }
                }
            }
          
            get
            {
                return FirstName + " " + LastName;
            }
        }

    }
}
