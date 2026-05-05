using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Models;
using Operum.Service.Domain.Notifications;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Notifications
{
    public class NotificationEvaluatorService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<NotificationEvaluatorService> logger) : BackgroundService
    {
        private TimeSpan Interval => TimeSpan.FromMinutes(
            configuration.GetValue<int>("Notifications:EvalIntervalMinutes", 10));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await EvaluateAllAsync(stoppingToken);
            }
        }

        private async Task EvaluateAllAsync(CancellationToken ct)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var pushService = scope.ServiceProvider.GetRequiredService<IWebPushService>();

            List<TrackerNotification> notifications;
            try
            {
                notifications = await db.TrackerNotifications
                    .Where(n => n.IsEnabled)
                    .Include(n => n.Tracker)
                        .ThenInclude(t => t.Owner)
                    .Include(n => n.Event)
                    .Include(n => n.Condition)
                        .ThenInclude(c => c.Filters)
                            .ThenInclude(f => f.Field)
                    .Include(n => n.Condition)
                        .ThenInclude(c => c.PurposeFields)
                            .ThenInclude(pf => pf.Field)
                    .Include(n => n.TriggeredEntries)
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load notifications for evaluation");
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var pushQueue = new List<(TrackerNotification Notification, string Body)>();

            foreach (var notification in notifications)
            {
                try
                {
                    await EvaluateNotificationAsync(db, notification, nowUtc, pushQueue, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to evaluate notification {Id}", notification.Id);
                }
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save notification states");
                return;
            }

            foreach (var (notification, body) in pushQueue)
            {
                try
                {
                    var title = $"{notification.Tracker.Name} — {notification.Name}";
                    var url = $"/trackers/{notification.TrackerId}";
                    await pushService.SendToTrackerUsersAsync(notification.TrackerId, title, body, url, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send push for notification {Id}", notification.Id);
                }
            }
        }

        private async Task EvaluateNotificationAsync(
            OperumContext db,
            TrackerNotification notification,
            DateTime nowUtc,
            List<(TrackerNotification, string)> pushQueue,
            CancellationToken ct)
        {
            var userTz = ResolveUserTz(notification);

            var isDue = NotificationScheduleResolver.IsDue(
                notification.Event, nowUtc, notification.LastEvaluatedAt, userTz);

            if (!isDue)
                return;

            notification.LastEvaluatedAt = nowUtc;
            db.TrackerNotifications.Update(notification);

            if (notification.Condition.ValueMode == NotificationValueMode.Entry)
            {
                await EvaluateEntryModeAsync(db, notification, nowUtc, pushQueue, ct);
            }
            else
            {
                await EvaluateAnalyticModeAsync(db, notification, nowUtc, pushQueue, ct);
            }
        }

        private static async Task EvaluateEntryModeAsync(
            OperumContext db,
            TrackerNotification notification,
            DateTime nowUtc,
            List<(TrackerNotification, string)> pushQueue,
            CancellationToken ct)
        {
            var currentMatchIds = await ConditionEntryEvaluator.GetMatchingEntryIdsAsync(db, notification, ct);
            var currentMatchSet = currentMatchIds.ToHashSet();

            var existingTriggered = notification.TriggeredEntries
                .Select(t => t.EntryId)
                .ToHashSet();

            var newlyMatched = currentMatchSet.Except(existingTriggered).ToList();
            var dropped = existingTriggered.Except(currentMatchSet).ToList();

            // Add triggered entries for newly matched
            foreach (var entryId in newlyMatched)
            {
                db.NotificationTriggeredEntries.Add(new NotificationTriggeredEntry
                {
                    NotificationId = notification.Id,
                    EntryId = entryId,
                    TriggeredAt = nowUtc
                });
            }

            // Remove triggered entries that no longer match (re-fireable on next entry)
            if (dropped.Count > 0)
            {
                await db.NotificationTriggeredEntries
                    .Where(t => t.NotificationId == notification.Id && dropped.Contains(t.EntryId))
                    .ExecuteDeleteAsync(ct);
            }

            if (newlyMatched.Count > 0)
            {
                notification.LastFiredAt = nowUtc;
                db.TrackerNotifications.Update(notification);

                var body = newlyMatched.Count == 1
                    ? "1 new entry matches"
                    : $"{newlyMatched.Count} new entries match";
                pushQueue.Add((notification, body));
            }
        }

        private static async Task EvaluateAnalyticModeAsync(
            OperumContext db,
            TrackerNotification notification,
            DateTime nowUtc,
            List<(TrackerNotification, string)> pushQueue,
            CancellationToken ct)
        {
            var conditionMet = await ConditionAnalyticEvaluator.EvaluateAsync(db, notification, ct);
            var wasTriggered = notification.IsTriggered;

            notification.IsTriggered = conditionMet;
            db.TrackerNotifications.Update(notification);

            var isFrequency = notification.Event.EventType != NotificationEventType.Triggered;

            // Frequency: fire whenever condition is true on a due tick
            // Triggered: fire only on false→true edge
            var shouldFire = isFrequency ? conditionMet : (conditionMet && !wasTriggered);

            if (shouldFire)
            {
                notification.LastFiredAt = nowUtc;
                db.TrackerNotifications.Update(notification);
                pushQueue.Add((notification, "Condition met"));
            }
        }

        private static TimeZoneInfo ResolveUserTz(TrackerNotification notification)
        {
            var tzId = notification.Tracker?.Owner?.TimeZone;
            if (string.IsNullOrEmpty(tzId))
                return TimeZoneInfo.Utc;

            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch { return TimeZoneInfo.Utc; }
        }
    }
}
