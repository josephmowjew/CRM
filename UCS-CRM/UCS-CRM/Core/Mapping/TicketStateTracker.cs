using AutoMapper;
using UCS_CRM.Core.DTOs.TicketStateTracker;

namespace UCS_CRM.Core.Mapping
{
    public class TicketStateTracker : Profile
    {
        public TicketStateTracker()
        {
            CreateMap<CreateTicketStateTrackerDTO, UCS_CRM.Core.Models.TicketStateTracker>();
            CreateMap<UpdateTicketStateTrackerDTO, UCS_CRM.Core.Models.TicketStateTracker>();
            CreateMap<UCS_CRM.Core.Models.TicketStateTracker, ReadTicketStateTrackerDTO>();
        }
    }
}
