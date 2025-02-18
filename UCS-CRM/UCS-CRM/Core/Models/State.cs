﻿using System.ComponentModel.DataAnnotations;

namespace UCS_CRM.Core.Models
{
    public class State : Meta
    {
        public State()
        {
            Tickets = new List<Ticket>();
        }
        public int Id { get; set; }
        [Required]
        [StringLength(maximumLength:255,MinimumLength =2)]
        public string Name { get; set; }

        public List<Ticket> Tickets { get; set; }
    }
}
