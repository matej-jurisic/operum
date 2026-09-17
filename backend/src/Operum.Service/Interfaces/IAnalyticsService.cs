using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;

namespace Operum.Service.Interfaces
{
    public interface IAnalyticsService
    {
        public Result<AnalyticConfigDto> GetAnalyticConfig();

        // Evaluates a chart definition once against live data without persisting it (the Explore page).
        Task<Result<AnalyticDto>> Evaluate(EvaluateWidgetDto dto);
    }
}
