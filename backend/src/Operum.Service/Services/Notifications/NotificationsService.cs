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
                .Include(n => n.Condition)
                    .ThenInclude(c => c.ConditionFields)
                        .ThenInclude(f => f.Field)
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

            var validationResult = await ValidateDto(trackerId, dto.ViewIds, dto.Condition);
            if (!validationResult.IsSuccess)
                return Result.Failure(validationResult.StatusCode, validationResult.Messages);

            var notification = new TrackerNotification
            {
                Name = dto.Name,
                IsEnabled = dto.IsEnabled,
                TrackerId = trackerId,
                ViewIds = dto.ViewIds.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(dto.ViewIds)
                    : null,
                Condition = new NotificationCondition
                {
                    Code = dto.Condition.Code,
                    ResultType = AnalyticTypes.SingleValue,
                    Operator = dto.Condition.Operator,
                    Value = dto.Condition.Value,
                    ConditionFields = dto.Condition.ConditionFields.Select(f => new NotificationConditionField
                    {
                        FieldId = f.FieldId,
                        Purpose = f.Purpose
                    }).ToList()
                }
            };

            await db.TrackerNotifications.AddAsync(notification);
            await db.SaveChangesAsync();

            return await GetNotification(trackerId, notification.Id);
        }

        public async Task<Result<TrackerNotificationDto>> UpdateNotification(string trackerId, string notificationId, UpdateTrackerNotificationDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            var notification = await db.TrackerNotifications
                .Include(n => n.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .Include(n => n.Condition)
                    .ThenInclude(c => c.ConditionFields)
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.TrackerId == trackerId);

            var isOwner = notification?.Tracker.OwnerId == user.Id;
            var userTracker = notification?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (notification == null || (!isOwner && userTracker?.CanEditSchema != true))
                return Result.Failure(ResultStatusCodes.NotFound);

            var validationResult = await ValidateDto(trackerId, dto.ViewIds, dto.Condition);
            if (!validationResult.IsSuccess)
                return Result.Failure(validationResult.StatusCode, validationResult.Messages);

            notification.Name = dto.Name;
            notification.IsEnabled = dto.IsEnabled;
            notification.ViewIds = dto.ViewIds.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(dto.ViewIds)
                : null;

            notification.Condition.Code = dto.Condition.Code;
            notification.Condition.ResultType = AnalyticTypes.SingleValue;
            notification.Condition.Operator = dto.Condition.Operator;
            notification.Condition.Value = dto.Condition.Value;

            await db.NotificationConditionFields
                .Where(f => f.ConditionId == notification.Condition.Id)
                .ExecuteDeleteAsync();

            notification.Condition.ConditionFields = dto.Condition.ConditionFields.Select(f => new NotificationConditionField
            {
                FieldId = f.FieldId,
                Purpose = f.Purpose,
                ConditionId = notification.Condition.Id
            }).ToList();

            db.TrackerNotifications.Update(notification);
            await db.SaveChangesAsync();

            return await GetNotification(trackerId, notificationId);
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
            db.TrackerNotifications.Update(notification);
            await db.SaveChangesAsync();

            return await GetNotification(trackerId, notificationId);
        }

        private async Task<Result<TrackerNotificationDto>> GetNotification(string trackerId, string notificationId)
        {
            var notification = await db.TrackerNotifications
                .Include(n => n.Condition)
                    .ThenInclude(c => c.ConditionFields)
                        .ThenInclude(f => f.Field)
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.TrackerId == trackerId);

            if (notification == null)
                return Result.Failure(ResultStatusCodes.NotFound);

            return Result.Success(mapper.Map<TrackerNotification, TrackerNotificationDto>(notification));
        }

        private async Task<Result> ValidateDto(string trackerId, List<string> viewIds, CreateNotificationConditionDto condition)
        {
            foreach (var viewId in viewIds)
            {
                var view = await db.Views.FindAsync(viewId);
                if (view == null || view.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("view"));
            }

            if (!AnalyticDefinitionList.IsValidForType(AnalyticTypes.SingleValue, condition.Code))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("analytic code"));

            if (!OperatorTypes.IsValid(condition.Operator))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("operator"));

            foreach (var field in condition.ConditionFields)
            {
                var trackerField = await db.Fields.FindAsync(field.FieldId);
                if (trackerField == null || trackerField.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("condition field"));
            }

            return Result.Success();
        }
    }
}
