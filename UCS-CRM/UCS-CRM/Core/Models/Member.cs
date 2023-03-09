using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class Member : Meta
    {
        public int Id { get; set; }
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }
        [Display(Name = "Surname")]
        [StringLength(maximumLength: 70, MinimumLength = 2)]
        public string? LastName { get; set; }
        [Display(Name = "Date Of Birth")]
        public DateTime? DateOfBirth { get; set; }
        [Required]
        public string? Gender { get; set; }

        [StringLength(maximumLength: 10, MinimumLength = 2)]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }

        [StringLength(maximumLength: 70, MinimumLength = 5)]
        public string NationalId { get; set; }
        [StringLength(maximumLength: 200, MinimumLength = 5)]
        public string? Address { get; set; }
        public ApplicationUser User { get; set; }
        [StringLength(maximumLength:20)]
        public string? PhoneNumber { get; set; }

    }
}
