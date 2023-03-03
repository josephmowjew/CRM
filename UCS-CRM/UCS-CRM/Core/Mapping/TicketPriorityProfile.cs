using AutoMapper;
using UCS_CRM.Core.DTOs.TicketPriority;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class TicketPriorityProfile : Profile
    {
        public TicketPriorityProfile()
        {
            CreateMap<CreateTicketPriorityDTO, TicketPriority>();
            CreateMap<TicketPriority, ReadTicketPriorityDTO>();
            CreateMap<EditTicketPriorityDTO, TicketPriority>();

        }
    }
}
