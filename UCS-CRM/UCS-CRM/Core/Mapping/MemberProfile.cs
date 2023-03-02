using AutoMapper;
using UCS_CRM.Core.DTOs.Member;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class MemberProfile : Profile
    {
        public MemberProfile()
        {
            CreateMap<CreateMemberDTO, Member>();
            CreateMap<Member, ReadMemberDTO>();
            CreateMap<EditMemberDTO, Member>();
        }
    }
}
