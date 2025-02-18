﻿
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.ViewModels
{
    public class ClientRegisterViewModel
    {
        [Required]
        public string Email { get; set; }
        [Display(Name = "National Id")]
        public string NationalId { get; set; }
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Confirm  Password doesn't match, Try again !")]
        public string ConfirmPassword { get; set; }
        public string Gender { get; set; } = string.Empty;
    }

    public class ConfirmPin
    {
        public int Pin { get; set; }
        public string? Email { get; set; } = "";
    }
}
