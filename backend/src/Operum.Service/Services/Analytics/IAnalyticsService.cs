using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;

namespace Operum.Service.Services.Analytics
{
    public interface IAnalyticsService
    {
        Task<Result<AnalyticDto>> GetAnalytic(string analyticId);
        Task<Result> CreateAnalytic(CreateAnalyticRequestDto createAnalytic);
        Task<Result> DeleteAnalytic(string analyticId);
        Task<Result<List<AnalyticDto>>> GetAnalyticList();
        Task<Result<List<AnalyticDto>>> GetPublicAnalyticList();
    }
}
