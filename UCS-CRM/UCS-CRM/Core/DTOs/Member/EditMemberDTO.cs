using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.Member
{
    public class EditMemberDTO
    {
        public int Id { get; set; }
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        public int Fidxno { get; set; }
        [Display(Name = "Surname")]
        [StringLength(maximumLength: 70, MinimumLength = 2)]
        public string? LastName { get; set; }
        [Display(Name = "Date Of Birth")]
        public DateTime? DateOfBirth { get; set; }
        [Required]
        public string? Gender { get; set; }
        [StringLength(maximumLength: 40, MinimumLength = 2)]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }
        [StringLength(maximumLength: 70, MinimumLength = 5)]
        [Display(Name = "Nationa Id")]
        public string NationalId { get; set; }
        [StringLength(maximumLength: 200, MinimumLength = 5)]
        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Branch { get; set; }

        public string? Employer { get; set; }
    }
}
