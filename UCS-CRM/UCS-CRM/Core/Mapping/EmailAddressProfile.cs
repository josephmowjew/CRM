using AutoMapper;
using UCS_CRM.Core.DTOs.EmailAddress;
using UCS_CRM.Core.DTOs.State;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class EmailAddressProfile : Profile
    {
        public EmailAddressProfile()
        {
            CreateMap<CreateEmailAddressDTO, EmailAddress>();
            CreateMap<UpdateEmailAddressDTO, EmailAddress>();
            CreateMap<EmailAddress, ReadEmailAddressDTO>();

        }
    }
}
