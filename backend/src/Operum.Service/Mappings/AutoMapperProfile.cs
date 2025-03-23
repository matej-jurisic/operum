using AutoMapper;
using Operum.Model.DTOs;
using Operum.Model.Models;

namespace YourProject.Service.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<ApplicationUser, ApplicationUserDto>();
        }
    }
}
