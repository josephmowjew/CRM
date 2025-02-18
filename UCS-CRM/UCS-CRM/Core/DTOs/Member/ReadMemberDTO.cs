﻿using System.ComponentModel.DataAnnotations;
using System.Globalization;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.Member
{
    public class ReadMemberDTO
    {

        public ReadMemberDTO()
        {
            MemberAccounts = new List<Models.MemberAccount>();

        }
        TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

        public int Fidxno { get; set; }
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
        public string EmployeeNumber { get; set; }
        public string AccountStatus { get; set; }
        public int MemberId { get; set; }
        public string? AccountName { get; set; }
        public decimal Balance { get; set; }
        //public List<RelatedAccount> RelatedAccounts { get; set; }

        public string FormattedFirstName => (!string.IsNullOrEmpty(FirstName)) ? myTI.ToTitleCase(FirstName) : "";
        public string FormattedLastName => (!string.IsNullOrEmpty(LastName)) ? myTI.ToTitleCase(LastName) : "";
        public string FormattedAddress => (!string.IsNullOrEmpty(Address)) ? myTI.ToTitleCase(Address) : "";
        public string FormattedGender => (!string.IsNullOrEmpty(Gender)) ? myTI.ToTitleCase(Gender) : "";
        public string formattedDateOfBirth => DateOfBirth?.ToString("dd-MM-yyyy");
        public string FormattedAmount => Balance.ToString("MWK 0.00");

        public ApplicationUser User { get; set; }

        public List<Models.MemberAccount> MemberAccounts { get; set; }
    }
}
