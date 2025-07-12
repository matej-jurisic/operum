using Operum.Model.DTOs;
using Operum.Model.DTOs.Trackers;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Mappings.Profiles
{
    public class MappingProfile : IMappingProfile
    {
        public void RegisterMappings(IMapper mapper)
        {
            mapper.Register<ApplicationUser, ApplicationUserDto>();

            mapper.Register<Tracker, TrackerDto>();
            mapper.Register<TrackerDto, Tracker>();
            mapper.Register<CreateTrackerDto, Tracker>();
            mapper.Register<UpdateTrackerDto, Tracker>();
        }
    }
}
