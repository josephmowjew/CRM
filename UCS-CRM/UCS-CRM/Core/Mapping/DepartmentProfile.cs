using AutoMapper;
using UCS_CRM.Core.DTOs.Department;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Mapping
{
    public class DepartmentProfile : Profile
    {
        public DepartmentProfile()
        {
            CreateMap<CreateDepartmentDTO, Department>();
            CreateMap<EditDepartmentDTO, Department>();
            CreateMap<Department, ReadDepartmentDTO>();
        }
    }
}
