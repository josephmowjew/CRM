using AutoMapper;
using UCS_CRM.Core.DTOs.Branch;
using UCS_CRM.Core.DTOs.Department;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class BranchProfile : Profile
    {
        public BranchProfile()
        {
            CreateMap<CreateBranchDTO, Branch>();
            CreateMap<EditBranchDTO, Branch>();
            CreateMap<Branch, ReadBranchDTO>();
        }
    }
}
