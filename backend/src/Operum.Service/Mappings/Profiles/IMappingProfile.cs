using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Mappings.Profiles
{
    public interface IMappingProfile
    {
        void RegisterMappings(IMapper mapper);
    }
}
