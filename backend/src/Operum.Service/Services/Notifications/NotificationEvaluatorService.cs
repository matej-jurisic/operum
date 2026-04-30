using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Notifications;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;
using System.Text.Json;

namespace Operum.Service.Services.Notifications
{
    public class NotificationEvaluatorService(IServiceProvider services, IConfiguration configuration, ILogger<NotificationEvaluatorService> logger) : BackgroundService
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
                    .Include(n => n.Condition)
                        .ThenInclude(c => c.ConditionFields)
                            .ThenInclude(cf => cf.Field)
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load notifications for evaluation");
                return;
            }

            var toNotify = new List<TrackerNotification>();

            foreach (var notification in notifications)
            {
                try
                {
                    var wasTriggered = notification.IsTriggered;
                    var conditionMet = await ComputeConditionAsync(db, notification, ct);

                    notification.IsTriggered = conditionMet;
                    logger.LogDebug("Eval Result: Notification {Id} conditionMet = {Result}", notification.Id, conditionMet);
                    if (conditionMet && !wasTriggered)
                    {
                        logger.LogInformation("Triggering Notification {Id}: Condition met (True) and was previously False. Adding to queue.", notification.Id);
                        notification.LastTriggeredAt = DateTime.UtcNow;
                        toNotify.Add(notification);
                    }
                    else if (conditionMet && wasTriggered)
                    {
                        logger.LogDebug("Notification {Id} still meeting condition, but was already triggered. Skipping.", notification.Id);
                    }

                    db.TrackerNotifications.Update(notification);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to evaluate notification {Id}", notification.Id);
                }
            }

            try
            {
                int entriesSaved = await db.SaveChangesAsync(ct);
                logger.LogInformation("Database sync complete. {Count} changes written to database.", entriesSaved);

                if (entriesSaved == 0)
                {
                    logger.LogWarning("SaveChangesAsync reported 0 changes. The database was NOT updated.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save notification states");
            }

            foreach (var notification in toNotify)
            {
                try
                {
                    var targetUrl = $"/trackers/{notification.TrackerId}";
                    var title = notification.Tracker.Name + " - " + notification.Name;
                    var body = $"{string.Join(", ", notification.Condition.ConditionFields.Select(x => x.Field.Name))} - {notification.Condition.Code} {notification.Condition.Operator} {notification.Condition.Value}";
                    await pushService.SendToTrackerUsersAsync(notification.TrackerId, title, body, targetUrl, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send push for notification {Id}", notification.Id);
                }
            }
        }

        private async Task<bool> ComputeConditionAsync(OperumContext db, TrackerNotification notification, CancellationToken ct)
        {
            var condition = notification.Condition;

            var viewIds = string.IsNullOrEmpty(notification.ViewIds)
                ? []
                : JsonSerializer.Deserialize<List<string>>(notification.ViewIds) ?? [];

            var views = viewIds.Count > 0
                ? await db.Views
                    .Include(v => v.Filters).ThenInclude(f => f.Field)
                    .Where(v => viewIds.Contains(v.Id))
                    .ToListAsync(ct)
                : [];

            var entriesQuery = db.Entries
                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == notification.TrackerId);

            if (views.Count > 0)
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.MergeViewFilters(views));

            var entries = await entriesQuery.ToListAsync(ct);

            var fieldMap = condition.ConditionFields.ToDictionary(cf => cf.Purpose, cf => cf.Field);

            var analytic = new Analytic { Code = condition.Code, ResultType = condition.ResultType };
            var result = AnalyticResultBuilder.GetAnalyticResult(new AnalyticResultBuilderRequest
            {
                Analytic = analytic,
                Entries = entries,
                FieldMap = fieldMap
            });

            if (!result.IsSuccess || result.Data is not SingleValueAnalyticDto svDto)
                return false;

            return NotificationConditionEvaluator.Evaluate(svDto.Value, condition.Operator, condition.Value);
        }
    }
}
