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
                        : "Member"))
                .ForMember(dest => dest.Period, opt => opt.MapFrom(src => 
                    (DateTime.UtcNow - src.CreatedDate).TotalDays >= 1 
                        ? src.CreatedDate.ToString("MMM dd, yyyy HH:mm")
                        : (DateTime.UtcNow - src.CreatedDate).TotalHours >= 1 
                            ? $"{Math.Floor((DateTime.UtcNow - src.CreatedDate).TotalHours)} hours ago"
                            : (DateTime.UtcNow - src.CreatedDate).TotalMinutes >= 1
                                ? $"{Math.Floor((DateTime.UtcNow - src.CreatedDate).TotalMinutes)} minutes ago"
                                : $"{Math.Floor((DateTime.UtcNow - src.CreatedDate).TotalSeconds)} seconds ago"));

            CreateMap<EditTicketDTO, Ticket>();
            CreateMap<EditManagerTicketDTO, Ticket>(); 
           
            CreateMap<EditManagerTicketDTO, EditTicketDTO>();
        }
    }
}
