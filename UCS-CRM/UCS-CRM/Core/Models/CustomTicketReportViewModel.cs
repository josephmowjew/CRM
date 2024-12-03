using System.Collections.Generic;

namespace UCS_CRM.Core.Models
{
    public class CustomTicketReportViewModel
    {
        public IEnumerable<Ticket> Model { get; set; }
        public CustomTicketReport Configuration { get; set; }
    }
} 