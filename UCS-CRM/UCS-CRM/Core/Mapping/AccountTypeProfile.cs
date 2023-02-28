using AutoMapper;
using UCS_CRM.Core.DTOs.AccountType;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class AccountTypeProfile : Profile
    {
        public AccountTypeProfile()
        {
            CreateMap<CreateAccountTypeDTO, AccountType>();
            CreateMap<AccountType, ReadAccoutTypeDTO>();
            CreateMap<EditAccountTypeDTO, AccountType>();

        }
    }
}
