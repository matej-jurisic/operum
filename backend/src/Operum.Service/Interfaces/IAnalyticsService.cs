using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;

namespace Operum.Service.Interfaces
{
    public interface IAnalyticsService
    {
        public Result<AnalyticConfigDto> GetAnalyticConfig();

        // Calculates a chart definition once against live data without persisting anything --
        // the Explore page. Always returns a displayable result (an explanatory card on a
        // bad mapping) unless access or the request shape itself is wrong.
        Task<Result<AnalyticDto>> Evaluate(EvaluateWidgetDto dto);
    }
}
