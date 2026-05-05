using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.DTOs.Notifications;
using Operum.Model.DTOs.Notifications.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Notifications;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Notifications
{
    public class NotificationsService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper) : INotificationsService
    {
        public async Task<Result<List<TrackerNotificationDto>>> GetNotifications(string trackerId)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));
            if (!hasAccess)
                return Result.Failure(ResultStatusCodes.Forbidden);

            var notifications = await db.TrackerNotifications
                .Include(n => n.Event)
                .Include(n => n.Condition)
                    .ThenInclude(c => c.Filters)
                        .ThenInclude(f => f.Field)
                .Include(n => n.Condition)
                    .ThenInclude(c => c.PurposeFields)
                .Where(n => n.TrackerId == trackerId)
                .ToListAsync();

            return Result.Success(mapper.Map<List<TrackerNotification>, List<TrackerNotificationDto>>(notifications));
        }

        public async Task<Result<TrackerNotificationDto>> CreateNotification(string trackerId, CreateTrackerNotificationDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);

            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
                return Result.Failure(ResultStatusCodes.NotFound);

            var eventValidation = ValidateEvent(dto.Event);
            if (!eventValidation.IsSuccess)
                return Result.Failure(eventValidation.StatusCode, eventValidation.Messages);

            var conditionValidation = await ValidateCondition(trackerId, dto.ViewIds, dto.Condition);
            if (!conditionValidation.IsSuccess)
                return Result.Failure(conditionValidation.StatusCode, conditionValidation.Messages);

            var notification = BuildNotification(trackerId, dto.Name, dto.IsEnabled, dto.ViewIds, dto.Event, dto.Condition);

            await db.TrackerNotifications.AddAsync(notification);
            await db.SaveChangesAsync();

            // Pre-populate triggered entries so first tick doesn't fire about pre-existing matches
            if (notification.Condition.ValueMode == NotificationValueMode.Entry)
            {
                var existingIds = await ConditionEntryEvaluator.GetMatchingEntryIdsAsync(
                    db,
                    await LoadFullNotification(notification.Id),
                    CancellationToken.None);

                foreach (var entryId in existingIds)
                {
                    db.NotificationTriggeredEntries.Add(new NotificationTriggeredEntry
                    {
                        NotificationId = notification.Id,
                        EntryId = entryId
                    });
                }
                await db.SaveChangesAsync();
            }

            return await GetNotificationResult(trackerId, notification.Id);
        }

        public async Task<Result<TrackerNotificationDto>> UpdateNotification(string trackerId, string notificationId, UpdateTrackerNotificationDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            var notification = await db.TrackerNotifications
                .Include(n => n.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .Include(n => n.Event)
                .Include(n => n.Condition)
                    .ThenInclude(c => c.Filters)
                .Include(n => n.Condition)
                    .ThenInclude(c => c.PurposeFields)
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.TrackerId == trackerId);

            var isOwner = notification?.Tracker.OwnerId == user.Id;
            var userTracker = notification?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (notification == null || (!isOwner && userTracker?.CanEditSchema != true))
                return Result.Failure(ResultStatusCodes.NotFound);

            var eventValidation = ValidateEvent(dto.Event);
            if (!eventValidation.IsSuccess)
                return Result.Failure(eventValidation.StatusCode, eventValidation.Messages);

            var conditionValidation = await ValidateCondition(trackerId, dto.ViewIds, dto.Condition);
            if (!conditionValidation.IsSuccess)
                return Result.Failure(conditionValidation.StatusCode, conditionValidation.Messages);

            notification.Name = dto.Name;
            notification.IsEnabled = dto.IsEnabled;
            notification.ViewIds = dto.ViewIds.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(dto.ViewIds)
                : null;
            notification.IsTriggered = false;

            UpdateEvent(notification.Event, dto.Event);

            // Bulk-delete the child rows first, then detach stale tracked entities
            // so EF doesn't try to act on them a second time during SaveChanges.
            await db.NotificationConditionFilters
                .Where(f => f.ConditionId == notification.Condition.Id)
                .ExecuteDeleteAsync();
            await db.NotificationConditionPurposeFields
                .Where(f => f.ConditionId == notification.Condition.Id)
                .ExecuteDeleteAsync();
            await db.NotificationTriggeredEntries
                .Where(t => t.NotificationId == notificationId)
                .ExecuteDeleteAsync();

            foreach (var f in notification.Condition.Filters.ToList())
                db.Entry(f).State = EntityState.Detached;
            foreach (var pf in notification.Condition.PurposeFields.ToList())
                db.Entry(pf).State = EntityState.Detached;

            UpdateCondition(notification.Condition, dto.Condition);

            foreach (var f in notification.Condition.Filters)
            {
                f.ConditionId = notification.Condition.Id;
            }

            foreach (var pf in notification.Condition.PurposeFields)
            {
                pf.ConditionId = notification.Condition.Id;
            }

            // Explicitly register new children as Added — do not call Update() here
            // because it would mark the freshly-created entities as Modified instead.
            db.NotificationConditionFilters.AddRange(notification.Condition.Filters);
            db.NotificationConditionPurposeFields.AddRange(notification.Condition.PurposeFields);

            await db.SaveChangesAsync();

            return await GetNotificationResult(trackerId, notificationId);
        }

        public async Task<Result> DeleteNotification(string trackerId, string notificationId)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);

            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
                return Result.Failure(ResultStatusCodes.NotFound);

            await db.TrackerNotifications
                .Where(n => n.Id == notificationId && n.TrackerId == trackerId)
                .ExecuteDeleteAsync();

            return Result.Success();
        }

        public async Task<Result<TrackerNotificationDto>> ToggleEnabled(string trackerId, string notificationId)
        {
            var user = currentUserService.GetCurrentUser();

            var notification = await db.TrackerNotifications
                .Include(n => n.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.TrackerId == trackerId);

            var isOwner = notification?.Tracker.OwnerId == user.Id;
            var userTracker = notification?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (notification == null || (!isOwner && userTracker?.CanEditSchema != true))
                return Result.Failure(ResultStatusCodes.NotFound);

            notification.IsEnabled = !notification.IsEnabled;

            if (!notification.IsEnabled)
            {
                notification.IsTriggered = false;
                await db.NotificationTriggeredEntries
                    .Where(t => t.NotificationId == notificationId)
                    .ExecuteDeleteAsync();
            }

            db.TrackerNotifications.Update(notification);
            await db.SaveChangesAsync();

            return await GetNotificationResult(trackerId, notificationId);
        }

        private async Task<Result<TrackerNotificationDto>> GetNotificationResult(string trackerId, string notificationId)
        {
            var notification = await LoadFullNotification(notificationId);

            if (notification == null || notification.TrackerId != trackerId)
                return Result.Failure(ResultStatusCodes.NotFound);

            return Result.Success(mapper.Map<TrackerNotification, TrackerNotificationDto>(notification));
        }

        private async Task<TrackerNotification> LoadFullNotification(string notificationId)
        {
            return await db.TrackerNotifications
                .Include(n => n.Event)
                .Include(n => n.Condition)
                    .ThenInclude(c => c.Filters)
                        .ThenInclude(f => f.Field)
                .Include(n => n.Condition)
                    .ThenInclude(c => c.PurposeFields)
                .FirstAsync(n => n.Id == notificationId);
        }

        private static Result ValidateEvent(CreateNotificationEventDto dto)
        {
            if (!Enum.TryParse<NotificationEventType>(dto.EventType, out var eventType))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("event type"));

            if (eventType == NotificationEventType.Triggered)
                return Result.Success();

            if (string.IsNullOrEmpty(dto.TimeOfDay) || !TimeOnly.TryParse(dto.TimeOfDay, out _))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("time of day"));

            return eventType switch
            {
                NotificationEventType.Day when (dto.IntervalDays is null or < 1 or > 365) =>
                    Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("interval days")),
                NotificationEventType.Week when (dto.IntervalWeeks is null or < 1 or > 365) =>
                    Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("interval weeks")),
                NotificationEventType.Week when (dto.DaysOfWeek == null || dto.DaysOfWeek.Count == 0) =>
                    Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("days of week")),
                NotificationEventType.Month when (dto.LastDayOfMonth != true && (dto.DayOfMonth is null or < 1 or > 31)) =>
                    Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("day of month")),
                _ => Result.Success()
            };
        }

        private async Task<Result> ValidateCondition(string trackerId, List<string> viewIds, CreateNotificationConditionDto dto)
        {
            foreach (var viewId in viewIds)
            {
                var view = await db.Views.FindAsync(viewId);
                if (view == null || view.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("view"));
            }

            if (!Enum.TryParse<NotificationValueMode>(dto.ValueMode, out var valueMode))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("value mode"));

            if (valueMode == NotificationValueMode.Analytic)
            {
                if (string.IsNullOrEmpty(dto.AnalyticCode) || !AnalyticDefinitionList.IsValidForType(AnalyticTypes.SingleValue, dto.AnalyticCode))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("analytic code"));

                foreach (var pf in dto.PurposeFields)
                {
                    var field = await db.Fields.FindAsync(pf.FieldId);
                    if (field == null || field.TrackerId != trackerId)
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("purpose field"));
                }

                foreach (var filter in dto.Filters)
                {
                    if (!OperatorTypes.IsValid(filter.Operator))
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("operator"));
                }
            }
            else
            {
                foreach (var filter in dto.Filters)
                {
                    if (string.IsNullOrEmpty(filter.FieldId))
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("filter field"));

                    var field = await db.Fields.FindAsync(filter.FieldId);
                    if (field == null || field.TrackerId != trackerId)
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("filter field"));

                    if (!OperatorTypes.IsValid(filter.Operator))
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("operator"));
                }
            }

            return Result.Success();
        }

        private static TrackerNotification BuildNotification(
            string trackerId, string name, bool isEnabled, List<string> viewIds,
            CreateNotificationEventDto eventDto, CreateNotificationConditionDto conditionDto)
        {
            return new TrackerNotification
            {
                Name = name,
                IsEnabled = isEnabled,
                TrackerId = trackerId,
                ViewIds = viewIds.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(viewIds) : null,
                Event = BuildEvent(eventDto),
                Condition = BuildCondition(conditionDto)
            };
        }

        private static NotificationEvent BuildEvent(CreateNotificationEventDto dto)
        {
            var eventType = Enum.Parse<NotificationEventType>(dto.EventType);
            var ev = new NotificationEvent { EventType = eventType };

            if (eventType != NotificationEventType.Triggered && dto.TimeOfDay != null)
                ev.TimeOfDay = TimeOnly.Parse(dto.TimeOfDay);

            switch (eventType)
            {
                case NotificationEventType.Day:
                    ev.IntervalDays = dto.IntervalDays;
                    ev.SkipWeekendsDay = dto.SkipWeekendsDay ?? false;
                    break;
                case NotificationEventType.Week:
                    ev.IntervalWeeks = dto.IntervalWeeks;
                    ev.DaysOfWeekMask = dto.DaysOfWeek != null
                        ? Operum.Model.Constants.DaysOfWeekMaskHelper.FromStringList(dto.DaysOfWeek)
                        : 0;
                    break;
                case NotificationEventType.Month:
                    ev.DayOfMonth = dto.DayOfMonth;
                    ev.LastDayOfMonth = dto.LastDayOfMonth ?? false;
                    ev.SkipWeekendsMonth = dto.SkipWeekendsMonth ?? false;
                    break;
            }

            return ev;
        }

        private static NotificationCondition BuildCondition(CreateNotificationConditionDto dto)
        {
            var valueMode = Enum.Parse<NotificationValueMode>(dto.ValueMode);
            return new NotificationCondition
            {
                ValueMode = valueMode,
                AnalyticCode = valueMode == NotificationValueMode.Analytic ? dto.AnalyticCode : null,
                AnalyticResultType = valueMode == NotificationValueMode.Analytic ? AnalyticTypes.SingleValue : null,
                Filters = dto.Filters.Select(f => new NotificationConditionFilter
                {
                    FieldId = f.FieldId,
                    Operator = f.Operator,
                    Value = f.Value
                }).ToList(),
                PurposeFields = dto.PurposeFields.Select(pf => new NotificationConditionPurposeField
                {
                    FieldId = pf.FieldId,
                    Purpose = pf.Purpose
                }).ToList()
            };
        }

        private static void UpdateEvent(NotificationEvent ev, CreateNotificationEventDto dto)
        {
            var eventType = Enum.Parse<NotificationEventType>(dto.EventType);
            ev.EventType = eventType;
            ev.TimeOfDay = (eventType != NotificationEventType.Triggered && dto.TimeOfDay != null)
                ? TimeOnly.Parse(dto.TimeOfDay)
                : null;
            ev.IntervalDays = dto.IntervalDays;
            ev.SkipWeekendsDay = dto.SkipWeekendsDay ?? false;
            ev.IntervalWeeks = dto.IntervalWeeks;
            ev.DaysOfWeekMask = dto.DaysOfWeek != null
                ? Operum.Model.Constants.DaysOfWeekMaskHelper.FromStringList(dto.DaysOfWeek)
                : null;
            ev.DayOfMonth = dto.DayOfMonth;
            ev.LastDayOfMonth = dto.LastDayOfMonth ?? false;
            ev.SkipWeekendsMonth = dto.SkipWeekendsMonth ?? false;
        }

        private static void UpdateCondition(NotificationCondition condition, CreateNotificationConditionDto dto)
        {
            var valueMode = Enum.Parse<NotificationValueMode>(dto.ValueMode);
            condition.ValueMode = valueMode;
            condition.AnalyticCode = valueMode == NotificationValueMode.Analytic ? dto.AnalyticCode : null;
            condition.AnalyticResultType = valueMode == NotificationValueMode.Analytic ? AnalyticTypes.SingleValue : null;
            condition.Filters = dto.Filters.Select(f => new NotificationConditionFilter
            {
                FieldId = f.FieldId,
                Operator = f.Operator,
                Value = f.Value
            }).ToList();
            condition.PurposeFields = dto.PurposeFields.Select(pf => new NotificationConditionPurposeField
            {
                FieldId = pf.FieldId,
                Purpose = pf.Purpose
            }).ToList();
        }
    }
}
