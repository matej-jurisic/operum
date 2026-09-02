using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Models;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Integrations
{
    /// <summary>
    /// Pulls every enabled connection on a timer. Structured like
    /// <c>NotificationEvaluatorService</c>: a PeriodicTimer, a fresh scope per tick, and
    /// failures contained per item so one bad connection cannot end the tick.
    /// <para>
    /// The unit of work is the connection, not the target: the executor fetches once per
    /// resource type and feeds every tracker linked to it from that one response, so N
    /// trackers on one account cost one call rather than N.
    /// </para>
    /// </summary>
    public class IntegrationSyncService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<IntegrationSyncService> logger) : BackgroundService
    {
        private TimeSpan Interval => TimeSpan.FromMinutes(
            configuration.GetValue("Integrations:SyncIntervalMinutes", 60));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SyncAllAsync(stoppingToken);
            }
        }

        private async Task SyncAllAsync(CancellationToken ct)
        {
            List<string> integrationIds;

            await using (var scope = services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

                try
                {
                    // Ids only: each connection is then synced in its own scope, so one
                    // connection's work -- and one connection's failure -- cannot touch
                    // another's change tracker.
                    integrationIds = await db.Integrations
                        .Where(i => i.IsEnabled
                            && i.Targets.Any(t => t.IsEnabled && t.Mode == IntegrationMode.Pull))
                        .Select(i => i.Id)
                        .ToListAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load integrations for syncing");
                    return;
                }
            }

            foreach (var integrationId in integrationIds)
            {
                if (ct.IsCancellationRequested)
                    return;

                await using var scope = services.CreateAsyncScope();
                var executor = scope.ServiceProvider.GetRequiredService<IIntegrationSyncExecutor>();

                try
                {
                    var result = await executor.SyncIntegrationAsync(integrationId, ct);

                    if (result.IsFailure)
                    {
                        // Already recorded on each target by the executor; logged here so an
                        // operator can see it without querying.
                        logger.LogWarning("Sync reported a problem for integration {IntegrationId}: {Messages}",
                            integrationId, string.Join("; ", result.Messages));
                        continue;
                    }

                    var written = result.Data;
                    if (written.Created + written.Updated + written.Deleted > 0)
                    {
                        logger.LogInformation(
                            "Synced integration {IntegrationId}: {Created} created, {Updated} updated, {Deleted} deleted, {Skipped} skipped",
                            integrationId, written.Created, written.Updated, written.Deleted, written.Skipped);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled failure syncing integration {IntegrationId}", integrationId);
                }
            }
        }
    }
}
