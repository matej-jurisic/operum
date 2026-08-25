using Operum.Model.Constants;
using Operum.Model.DTOs.Entries;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Notifications;
using Operum.Model.DTOs.TrackerConstants;
using Operum.Model.DTOs.TrackerConstants.Requests;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.DTOs.Queries;
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
                d.Fields = mapper.Map<ICollection<Field>, List<FieldDto>>(s.Fields.OrderBy(f => f.Order).ToList());
                d.OwnerName = s.Owner?.UserName;
                d.TrackerTypeName = s.TrackerType?.Name;
                d.DefaultViewId = s.DefaultViewId;
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

            // View/ViewDto mapping is hand-rolled in ViewsService (it needs to walk the
            // ordered ViewQuery join), so only Query itself is registered here.
            mapper.Register<Query, QueryDto>((s, d) =>
            {
                d.Sorts = mapper.Map<ICollection<QuerySort>, List<QuerySortDto>>(s.Sorts.OrderBy(x => x.Order).ToList());
                d.Filters = mapper.Map<ICollection<QueryFilter>, List<QueryFilterDto>>(s.Filters);
            });

            mapper.Register<QuerySort, QuerySortDto>((s, d) =>
            {
                d.Field = mapper.Map<Field, FieldDto>(s.Field);
            });
            mapper.Register<QueryFilter, QueryFilterDto>((s, d) =>
            {
                d.Field = mapper.Map<Field, FieldDto>(s.Field);
            });

            mapper.Register<TrackerConstant, TrackerConstantDto>((s, d) =>
            {
                d.Values = mapper.Map<List<TrackerConstantValue>, List<TrackerConstantValueDto>>(s.Values);
            });
            mapper.Register<TrackerConstantDto, TrackerConstant>();
            mapper.Register<CreateTrackerConstantDto, TrackerConstant>();
            mapper.Register<UpdateTrackerConstantDto, TrackerConstant>();
            mapper.Register<TrackerConstantValue, TrackerConstantValueDto>((s, d) =>
            {
                d.Filters = mapper.Map<List<TrackerConstantValueFilter>, List<TrackerConstantValueFilterDto>>(s.Filters);
            });
            mapper.Register<TrackerConstantValueFilter, TrackerConstantValueFilterDto>();

            mapper.Register<NotificationConditionFilter, NotificationConditionFilterDto>();
            mapper.Register<NotificationConditionPurposeField, NotificationConditionPurposeFieldDto>();

            mapper.Register<NotificationEvent, NotificationEventDto>((s, d) =>
            {
                d.EventType = s.EventType.ToString();
                d.TimeOfDay = s.TimeOfDay?.ToString("HH:mm");
                d.DaysOfWeek = s.DaysOfWeekMask.HasValue
                    ? DaysOfWeekMaskHelper.ToStringList(s.DaysOfWeekMask.Value)
                    : null;
            });

            mapper.Register<NotificationCondition, NotificationConditionDto>((s, d) =>
            {
                d.ValueMode = s.ValueMode.ToString();
                d.Filters = mapper.Map<List<NotificationConditionFilter>, List<NotificationConditionFilterDto>>(s.Filters);
                d.PurposeFields = mapper.Map<List<NotificationConditionPurposeField>, List<NotificationConditionPurposeFieldDto>>(s.PurposeFields);
            });

            mapper.Register<TrackerNotification, TrackerNotificationDto>((s, d) =>
            {
                d.ViewId = s.ViewId;
                d.Event = mapper.Map<NotificationEvent, NotificationEventDto>(s.Event);
                d.Condition = mapper.Map<NotificationCondition, NotificationConditionDto>(s.Condition);
            });
        }
    }
}
