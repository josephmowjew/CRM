using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.DTOs.AccountType
{
    public class ReadAccoutTypeDTO
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
