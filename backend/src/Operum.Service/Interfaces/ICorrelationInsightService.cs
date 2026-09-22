using Operum.Model.Common;
using Operum.Model.DTOs.CorrelationInsights;

namespace Operum.Service.Interfaces
{
    public interface ICorrelationInsightService
    {
        Task<Result<List<CorrelationInsightDto>>> GetInsights();

        // Recomputes the signed-in user's insights synchronously and returns the refreshed list.
        Task<Result<List<CorrelationInsightDto>>> RunNow(CancellationToken ct = default);

        Task<Result> Dismiss(string insightId);
    }
}
