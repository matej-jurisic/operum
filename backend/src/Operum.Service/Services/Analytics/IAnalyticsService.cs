using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;

namespace Operum.Service.Services.Analytics
{
    public interface IAnalyticsService
    {
        Task<ServiceResponse<AnalyticDto>> GetAnalytic(string analyticId);
        Task<ServiceResponse<AnalyticDto>> CreateAnalytic(CreateAnalyticRequestDto createAnalytic);
        Task<ServiceResponse> DeleteAnalytic(string analyticId);
        Task<ServiceResponse<List<AnalyticDto>>> GetAnalyticList();
    }
}
