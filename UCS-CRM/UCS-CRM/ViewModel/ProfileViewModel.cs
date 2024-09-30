using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public Member Member { get; set; }
    }
}
