using System.ComponentModel.DataAnnotations;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.DTOs.State
{
    public class ReadStateDTO
    {
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength: 255, MinimumLength = 2)]
        public string Name { get; set; }

        public List<Ticket> Tickets { get; set; }
    }
}
