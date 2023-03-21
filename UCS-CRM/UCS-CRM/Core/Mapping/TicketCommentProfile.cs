using AutoMapper;
using UCS_CRM.Core.DTOs.TicketComment;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class TicketCommentProfile : Profile
    {
        public TicketCommentProfile()
        {
            CreateMap<CreateTicketCommentDTO, TicketComment>();
            CreateMap<TicketComment, ReadTicketCommentDTO>();
            CreateMap<EditTicketCommentDTO, TicketComment>();
        }
    }
}
