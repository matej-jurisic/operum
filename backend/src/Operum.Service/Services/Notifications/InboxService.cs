using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Notifications;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Notifications
{
    public class InboxService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper) : IInboxService
    {
        private const int MaxPageSize = 50;

        public async Task<Result<InboxPageDto>> GetInbox(int skip, int take)
        {
            var userId = currentUserService.GetCurrentUser().Id;

            skip = Math.Max(0, skip);
            take = Math.Clamp(take <= 0 ? 20 : take, 1, MaxPageSize);

            var query = db.InboxNotifications.Where(i => i.UserId == userId);

            // Fetch one extra row to tell the client whether another page exists.
            var rows = await query
                .Include(i => i.Tracker)
                .Include(i => i.Notification)
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take + 1)
                .ToListAsync();

            var hasMore = rows.Count > take;
            var items = rows.Take(take).ToList();

            return Result.Success(new InboxPageDto
            {
                Items = mapper.Map<List<InboxNotification>, List<InboxNotificationDto>>(items),
                UnreadCount = await query.CountAsync(i => i.ReadAt == null),
                HasMore = hasMore,
            });
        }

        public async Task<Result<int>> GetUnreadCount()
        {
            var userId = currentUserService.GetCurrentUser().Id;
            return Result.Success(await db.InboxNotifications.CountAsync(i => i.UserId == userId && i.ReadAt == null));
        }

        public async Task<Result> MarkRead(string id)
        {
            var userId = currentUserService.GetCurrentUser().Id;
            var updated = await db.InboxNotifications
                .Where(i => i.Id == id && i.UserId == userId && i.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.ReadAt, DateTime.UtcNow));

            // Already read is fine (idempotent); only a row that isn't the user's is a 404.
            if (updated > 0 || await ExistsForUser(id, userId))
                return Result.Success();

            return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("notification"));
        }

        public async Task<Result> MarkAllRead()
        {
            var userId = currentUserService.GetCurrentUser().Id;
            await db.InboxNotifications
                .Where(i => i.UserId == userId && i.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.ReadAt, DateTime.UtcNow));

            return Result.Success();
        }

        public async Task<Result> Delete(string id)
        {
            var userId = currentUserService.GetCurrentUser().Id;
            var deleted = await db.InboxNotifications
                .Where(i => i.Id == id && i.UserId == userId)
                .ExecuteDeleteAsync();

            return deleted == 0
                ? Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("notification"))
                : Result.Success();
        }

        public async Task CreateForTrackerMembersAsync(
            string trackerId, string? notificationId, string title, string body, string url, CancellationToken ct = default)
        {
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId, ct);

            if (tracker == null) return;

            var memberIds = tracker.ApplicationUserTrackers
                .Select(ut => ut.ApplicationUserId)
                .Append(tracker.OwnerId)
                .Distinct();

            foreach (var memberId in memberIds)
            {
                db.InboxNotifications.Add(new InboxNotification
                {
                    UserId = memberId,
                    TrackerId = trackerId,
                    NotificationId = notificationId,
                    Title = title,
                    Body = body,
                    Url = url,
                });
            }
        }

        private Task<bool> ExistsForUser(string id, string userId) =>
            db.InboxNotifications.AnyAsync(i => i.Id == id && i.UserId == userId);
    }
}
