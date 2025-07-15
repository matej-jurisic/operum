using Operum.Model.DTOs;
using Operum.Model.DTOs.Entry;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.FieldValue;
using Operum.Model.DTOs.Trackers;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Mappings.Profiles
{
    public class MappingProfile : IMappingProfile
    {
        public void RegisterMappings(IMapper mapper)
        {
            mapper.Register<ApplicationUser, ApplicationUserDto>();

            mapper.RegisterCrud<Tracker, TrackerDto, CreateTrackerDto, UpdateTrackerDto>();
            mapper.RegisterCrud<Field, FieldDto, CreateFieldDto, UpdateFieldDto>();

            mapper.Register<FieldValue, FieldValueDto>((s, d) =>
            {
                d.FieldName = s.Field.Name;
                d.FieldType = s.Field.Type;
                d.Value = s.GetFieldValue();
            });

            mapper.Register<Entry, EntryDto>((s, d) =>
            {
                foreach (var v in s.FieldValues)
                {
                    d.FieldValues.Add(mapper.Map<FieldValue, FieldValueDto>(v));
                }
            });
        }
    }
}
