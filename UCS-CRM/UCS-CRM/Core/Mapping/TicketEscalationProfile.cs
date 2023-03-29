using AutoMapper;
using UCS_CRM.Core.DTOs.State;
using UCS_CRM.Core.DTOs.TicketEscalation;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class TicketEscalationProfile : Profile
    {
        public TicketEscalationProfile()
        {
            CreateMap<CreateTicketEscalationDTO, TicketEscalation>();
            CreateMap<UpdateTicketEscalationDTO, TicketEscalation>();
            CreateMap<TicketEscalation, ReadTicketEscalationDTO>();

        }
    }
}
