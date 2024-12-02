namespace UCS_CRM.Core.Models;
using UCS_CRM.Core.Models;

using System.ComponentModel.DataAnnotations;

public class FailedRegistration: Meta
    {
        public int Id { get; set; }
        public string NationalId { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; }
        public string? Notes { get; set; }
    }