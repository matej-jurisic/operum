using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Services.Analytics
{
    public interface IAnalyticsService
    {
        public Task<ServiceResponse<SingleFieldAnalyticsDto>> GetSingleFieldAnalytics(string trackerId, string fieldId);
    }
}
