using AutoMapper;
using UCS_CRM.Core.DTOs.TicketCategory;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class TicketCategoryProfile : Profile
    {
        public TicketCategoryProfile()
        {
            CreateMap<CreateTicketCategoryDTO, TicketCategory>();
            CreateMap<TicketCategory, ReadTicketCategoryDTO>();
            CreateMap<EditTicketCategoryDTO, TicketCategory>();

        }
    }
}
