using Operum.Model.DTOs.Entries;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.DTOs.Views;
using Operum.Model.DTOs.Views.Requests;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Mappings.Profiles
{
    public class MappingProfile : IMappingProfile
    {
        public void RegisterMappings(IMapper mapper)
        {
            mapper.Register<User, UserDto>((s, d) =>
            {
                d.MailConfirmed = s.EmailConfirmed;
            });

            mapper.Register<User, PublicUserDto>();

            mapper.Register<Tracker, TrackerDto>((s, d) =>
            {
                d.Fields = mapper.Map<ICollection<Field>, List<FieldDto>>(s.Fields);
                d.OwnerName = s.Owner?.UserName;
                d.TrackerTypeName = s.TrackerType?.Name;
                d.DefaultViewIds = s.DefaultViewIds != null
                    ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.DefaultViewIds)
                    : null;
            });
            mapper.Register<TrackerDto, Tracker>();
            mapper.Register<CreateTrackerDto, Tracker>();
            mapper.Register<UpdateTrackerDto, Tracker>();

            mapper.Register<Field, FieldDto>((s, d) =>
            {
                d.SelectOptions = s.SelectOptions != null
                    ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.SelectOptions)
                    : null;
            });
            mapper.Register<FieldDto, Field>((s, d) =>
            {
                d.SelectOptions = s.SelectOptions != null
                    ? System.Text.Json.JsonSerializer.Serialize(s.SelectOptions)
                    : null;
            });
            mapper.Register<CreateFieldDto, Field>((s, d) =>
            {
                d.SelectOptions = s.SelectOptions != null
                    ? System.Text.Json.JsonSerializer.Serialize(s.SelectOptions)
                    : null;
            });
            mapper.Register<UpdateFieldDto, Field>((s, d) =>
            {
                d.SelectOptions = s.SelectOptions != null
                    ? System.Text.Json.JsonSerializer.Serialize(s.SelectOptions)
                    : null;
            });

            mapper.Register<FieldValue, FieldValueDto>((s, d) =>
            {
                d.FieldName = s.Field.Name;
                d.FieldType = s.Field.Type;
                d.Value = s.GetFieldValue();
            });

            mapper.Register<Entry, EntryDto>((s, d) =>
            {
                var sorted = s.FieldValues.OrderBy(x => x.Field.Order);
                foreach (var v in sorted)
                {
                    d.FieldValues.Add(mapper.Map<FieldValue, FieldValueDto>(v));
                }
            });

            mapper.Register<View, ViewDto>((s, d) =>
            {
                d.Sorts = mapper.Map<ICollection<ViewSort>, List<ViewSortDto>>(s.Sorts);
                d.Filters = mapper.Map<ICollection<ViewFilter>, List<ViewFilterDto>>(s.Filters);
            });

            mapper.Register<ViewSort, ViewSortDto>((s, d) =>
            {
                d.Field = mapper.Map<Field, FieldDto>(s.Field);
            });
            mapper.Register<ViewFilter, ViewFilterDto>((s, d) =>
            {
                d.Field = mapper.Map<Field, FieldDto>(s.Field);
            });

            mapper.Register<CreateViewSortDto, ViewSort>();
            mapper.Register<CreateViewFilterDto, ViewFilter>();
            mapper.Register<CreateViewDto, View>((s, d) =>
            {
                d.Sorts = [.. s.Sorts
                    .Select((sortDto, index) =>
                    {
                        var sort = mapper.Map<CreateViewSortDto, ViewSort>(sortDto);
                        sort.Order = index;
                        return sort;
                    })];
                d.Filters = mapper.Map<List<CreateViewFilterDto>, List<ViewFilter>>(s.Filters);
            });
        }
    }
}
