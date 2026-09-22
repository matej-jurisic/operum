namespace Operum.Service.Interfaces
{
    // The context-free half of correlation discovery: recomputes and upserts one user's
    // FieldCorrelationInsight rows. Shared by the manual "run now" endpoint and the
    // background tick, the same split IIntegrationSyncExecutor uses.
    public interface ICorrelationInsightEvaluator
    {
        Task RunForUserAsync(string userId, CancellationToken ct = default);
    }
}
