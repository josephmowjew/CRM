using AutoMapper;
using UCS_CRM.Core.DTOs.State;
using UCS_CRM.Core.DTOs.Ticket;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            CreateMap<CreateTicketDTO, Ticket>();
            CreateMap<EditTicketDTO, Ticket>();
            CreateMap<EditManagerTicketDTO, Ticket>(); 
            CreateMap<Ticket, ReadTicketDTO>();
            CreateMap<EditManagerTicketDTO, EditTicketDTO>();

        }
    }
}
