using AutoMapper;
using UCS_CRM.Core.DTOs.Position;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class PositionProfile : Profile
    {
        public PositionProfile()
        {
            CreateMap<CreatePositionDTO, Position>();
            CreateMap<EditPositionDTO, Position>();
            CreateMap<Position, ReadPositionDTO>();

        }
    }
}
