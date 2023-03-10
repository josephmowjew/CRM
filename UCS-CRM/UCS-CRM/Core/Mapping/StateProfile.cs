using AutoMapper;
using UCS_CRM.Core.DTOs.State;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class StateProfile : Profile
    {
        public StateProfile()
        {
            CreateMap<CreateStateDTO, State>();
            CreateMap<EditStateDTO, State>();
            CreateMap<State, ReadStateDTO>();

        }
    }
}
