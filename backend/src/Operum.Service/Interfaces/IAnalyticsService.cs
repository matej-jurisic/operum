using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Interfaces
{
    public interface IAnalyticsService
    {
        public Result<AnalyticConfigDto> GetAnalyticConfig();
    }
}
