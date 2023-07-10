using AutoMapper;
using UCS_CRM.Core.DTOs.Feedback;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class FeedbackProfile : Profile
    {
        public FeedbackProfile()
        {
            CreateMap<CreateFeedbackDTO, Feedback>();
            CreateMap<EditFeedbackDTO, Feedback>();
            CreateMap<Feedback, ReadFeedbackDTO>();

        }
    }
}
