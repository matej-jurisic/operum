using Operum.Model.DTOs;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Entry;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.FieldValue;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
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
            mapper.Register<ApplicationUser, ApplicationUserDto>();

            mapper.Register<Tracker, TrackerDto>((s, d) =>
            {
                d.Fields = mapper.Map<ICollection<Field>, List<FieldDto>>(s.Fields);
            });
            mapper.Register<TrackerDto, Tracker>();
            mapper.Register<CreateTrackerDto, Tracker>();
            mapper.Register<UpdateTrackerDto, Tracker>();

            mapper.RegisterCrud<Field, FieldDto, CreateFieldDto, UpdateFieldDto>();

            mapper.Register<FieldValue, FieldValueDto>((s, d) =>
            {
                d.FieldName = s.Field.Name;
                d.FieldType = s.Field.Type;
                d.Value = s.GetFieldValue();
            });

            mapper.Register<Entry, EntryDto>((s, d) =>
            {
                var sortedByName = s.FieldValues.OrderBy(x => x.Field.Name);
                foreach (var v in sortedByName)
                {
                    d.FieldValues.Add(mapper.Map<FieldValue, FieldValueDto>(v));
                }
            });
            mapper.Register<AnalyticRequiredDataType, AnalyticRequiredDataTypeDto>();
            mapper.Register<CreateAnalyticRequiredDataTypeDto, AnalyticRequiredDataType>();
            mapper.Register<CreateAnalyticRequestDto, Analytic>((s, d) =>
            {
                d.AnalyticRequiredDataTypes = mapper.Map<List<CreateAnalyticRequiredDataTypeDto>, List<AnalyticRequiredDataType>>(s.AnalyticRequiredDataTypes);
            });
            mapper.Register<Analytic, AnalyticDto>((s, d) =>
            {
                d.AnalyticRequiredDataTypes = mapper.Map<List<AnalyticRequiredDataType>, List<AnalyticRequiredDataTypeDto>>(s.AnalyticRequiredDataTypes);
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
