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
            CreateMap<CreateTicketDTO, Ticket>()
                .ForMember(dest => dest.InitiatorUserId, opt => opt.MapFrom(src => 
                    src.InitiatorType == "User" ? src.InitiatorId : null))
                .ForMember(dest => dest.InitiatorMemberId, opt => opt.MapFrom(src => 
                    src.InitiatorType == "Member" ? int.Parse(src.InitiatorId) : (int?)null));

            CreateMap<Ticket, ReadTicketDTO>()
                .ForMember(dest => dest.InitiatorType, opt => opt.MapFrom(src => 
                    src.InitiatorUserId != null ? "User" : "Member"))
                .ForMember(dest => dest.InitiatorId, opt => opt.MapFrom(src => 
                    src.InitiatorUserId ?? src.InitiatorMemberId.ToString()))
                .ForMember(dest => dest.InitiatorName, opt => opt.MapFrom(src => 
                    src.InitiatorUserId != null 
                        ? (src.InitiatorUser != null 
                            ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase($"{src.InitiatorUser.FirstName} {src.InitiatorUser.LastName}".ToLower())
                            : "No initiator found")
                        : (src.InitiatorMember != null 
                            ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase($"{src.InitiatorMember.FirstName} {src.InitiatorMember.LastName}".ToLower())
                            : "No initiator found")))
                .ForMember(dest => dest.InitiatorDepartmentName, opt => opt.MapFrom(src => 
                    src.InitiatorUserId != null 
                        ? src.InitiatorUser.Department.Name
                        : "Member")); // Assuming members don't have departments, otherwise adjust this

            CreateMap<EditTicketDTO, Ticket>();
            CreateMap<EditManagerTicketDTO, Ticket>(); 
           
            CreateMap<EditManagerTicketDTO, EditTicketDTO>();
        }
    }
}
