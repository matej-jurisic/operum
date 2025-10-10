using Operum.Model.Common;
using Operum.Model.Constants.Analytics;

namespace Operum.Service.Interfaces
{
    public interface IAnalyticsService
    {
        public Result<AnalyticConfig> GetAnalyticConfig();
    }
}
