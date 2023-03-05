using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;
using UCS_CRM.Core.Models;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.ViewModels
{
    public class UserViewModel : ApplicationUser
    {
        TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
        [Display(Name = "Role Name")]
        public string RoleName { get; set; }
        public string DataInvalid { get; set; } = "true";
        public string FormattedFirstName => myTI.ToTitleCase(FirstName);
        public string FormattedLastName => myTI.ToTitleCase(LastName);
        public string FormattedGender => (!string.IsNullOrEmpty(Gender)) ? myTI.ToTitleCase(Gender) : "";
        public string formattedCreatedDate => CreatedDate.ToString("dd-MM-yyyy");
        public string formattedLastLogin => LastLogin.ToString("dd-MM-yyyy hh:mm tt");


        [NotMapped]
        public List<SelectListItem> GenderList { get; set; } = new()
        {
            new SelectListItem { Value = "male", Text = "Male" },
            new SelectListItem { Value = "female", Text = "Female" },

        };
    }
}
