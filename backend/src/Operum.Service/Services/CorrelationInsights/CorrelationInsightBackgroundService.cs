using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.CorrelationInsights
{
    // Daily tick over every user who owns a tracker. The manual "run now" endpoint drives the
    // same ICorrelationInsightEvaluator, so this only exists to keep insights fresh for users
    // who never ask for a refresh themselves.
    public class CorrelationInsightBackgroundService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<CorrelationInsightBackgroundService> logger) : BackgroundService
    {
        private TimeSpan Interval => TimeSpan.FromHours(
            configuration.GetValue("CorrelationInsights:IntervalHours", 24));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAllAsync(stoppingToken);
            }
        }

        private async Task RunAllAsync(CancellationToken ct)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var evaluator = scope.ServiceProvider.GetRequiredService<ICorrelationInsightEvaluator>();

            List<string> userIds;
            try
            {
                userIds = await db.Trackers.Select(t => t.OwnerId).Distinct().ToListAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load tracker owners for correlation insight discovery");
                return;
            }

            foreach (var userId in userIds)
            {
                try
                {
                    await evaluator.RunForUserAsync(userId, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to discover correlation insights for user {UserId}", userId);
                }
            }
        }
    }
}
