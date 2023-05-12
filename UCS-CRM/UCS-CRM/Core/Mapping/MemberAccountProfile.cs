using AutoMapper;
using UCS_CRM.Core.DTOs.MemberAccount;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class MemberAccountProfile : Profile
    {
        public MemberAccountProfile()
        {
            CreateMap<MemberAccount, ReadMemberAccountDTO>();
            CreateMap<CreateMemberAccountDTO, MemberAccount>();
            CreateMap<EditMemberAccountDTO, MemberAccount>();
        }
    }
}
