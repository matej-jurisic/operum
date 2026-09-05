using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Constants.Notifications;
using Operum.Model.Extensions;
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
            configuration.GetValue<int>("Notifications:EvalIntervalMinutes", 2));

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
            var inboxService = scope.ServiceProvider.GetRequiredService<IInboxService>();

            List<TrackerNotification> notifications;
            try
            {
                notifications = await db.TrackerNotifications
                    // The context runs no-tracking, which also skips identity resolution: two
                    // notifications on one tracker, or two filters over one field, would each
                    // materialise that row twice and conflict the moment the graph is attached.
                    // Tracking resolves them to a single instance and picks up the state
                    // changes below without an explicit Update.
                    .AsTracking()
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
                var title = $"{notification.Tracker.Name} - {notification.Name}";
                var url = $"/trackers/{notification.TrackerId}";

                try
                {
                    await inboxService.CreateForTrackerMembersAsync(notification.TrackerId, notification.Id, title, body, url, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to write inbox items for notification {Id}", notification.Id);
                }

                try
                {
                    await pushService.SendToTrackerUsersAsync(notification.TrackerId, title, body, url, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send push for notification {Id}", notification.Id);
                }
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save inbox items");
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
            var currentMatchIds = await ConditionEntryEvaluator.GetMatchingEntryIdsAsync(db, notification, ResolveUserTz(notification), ct);
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

                var fallback = newlyMatched.Count == 1
                    ? "1 new entry matches"
                    : $"{newlyMatched.Count} new entries match";
                var tokens = new Dictionary<string, string>
                {
                    ["count"] = newlyMatched.Count.ToString(),
                    ["tracker"] = notification.Tracker.Name,
                    ["notification"] = notification.Name,
                    ["fieldValueList"] = await BuildFieldValueListAsync(db, notification, newlyMatched, ct),
                };
                var body = NotificationMessageBuilder.Build(notification.MessageTemplate, fallback, tokens);
                pushQueue.Add((notification, body));
            }
        }

        private static async Task<string> BuildFieldValueListAsync(
            OperumContext db,
            TrackerNotification notification,
            List<string> newlyMatchedEntryIds,
            CancellationToken ct)
        {
            var displayFieldIds = notification.Condition.PurposeFields
                .Where(pf => pf.Purpose == NotificationPurposes.Display)
                .Select(pf => pf.FieldId)
                .ToList();

            if (displayFieldIds.Count == 0)
                return string.Empty;

            var entriesById = await db.Entries
                .Where(e => newlyMatchedEntryIds.Contains(e.Id))
                .Include(e => e.FieldValues.Where(fv => displayFieldIds.Contains(fv.FieldId)))
                    .ThenInclude(fv => fv.Field)
                .ToDictionaryAsync(e => e.Id, ct);

            // Preserve the order entries were matched in rather than whatever the query returned.
            var orderedEntries = newlyMatchedEntryIds
                .Select(id => entriesById.GetValueOrDefault(id))
                .Where(e => e != null)
                .Select(e => e!)
                .ToList();

            return NotificationFieldValueListBuilder.Build(orderedEntries, displayFieldIds);
        }

        private static async Task EvaluateAnalyticModeAsync(
            OperumContext db,
            TrackerNotification notification,
            DateTime nowUtc,
            List<(TrackerNotification, string)> pushQueue,
            CancellationToken ct)
        {
            var evaluation = await ConditionAnalyticEvaluator.EvaluateAsync(db, notification, ResolveUserTz(notification), ct);
            var wasTriggered = notification.IsTriggered;

            notification.IsTriggered = evaluation.ConditionMet;

            var isFrequency = notification.Event.EventType != NotificationEventType.Triggered;

            // Frequency: fire whenever condition is true on a due tick
            // Triggered: fire only on false→true edge
            var shouldFire = isFrequency ? evaluation.ConditionMet : (evaluation.ConditionMet && !wasTriggered);

            if (shouldFire)
            {
                notification.LastFiredAt = nowUtc;
                var tokens = new Dictionary<string, string>
                {
                    ["value"] = evaluation.Value ?? "",
                    ["tracker"] = notification.Tracker.Name,
                    ["notification"] = notification.Name,
                };
                var body = NotificationMessageBuilder.Build(notification.MessageTemplate, "Condition met", tokens);
                pushQueue.Add((notification, body));
            }
        }

        private static TimeZoneInfo ResolveUserTz(TrackerNotification notification)
        {
            return TimeZoneResolver.FromId(notification.Tracker?.Owner?.TimeZone);
        }
    }
}
