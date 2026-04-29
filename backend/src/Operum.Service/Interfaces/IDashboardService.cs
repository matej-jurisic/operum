using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.DTOs.Dashboard.Requests;

namespace Operum.Service.Interfaces
{
    public interface IDashboardService
    {
        Task<Result<List<DashboardDto>>> GetDashboards();
        Task<Result<DashboardDto>> GetDashboard(string dashboardId);
        Task<Result<List<AnalyticDto>>> GetDashboardAnalytics(string dashboardId);
        Task<Result<DashboardDto>> CreateDashboard(CreateDashboardDto dto);
        Task<Result> DeleteDashboard(string dashboardId);
        Task<Result<DashboardItemDto>> AddDashboardItem(string dashboardId, AddDashboardItemDto dto);
        Task<Result> RemoveDashboardItem(string dashboardId, string itemId);
        Task<Result> ReorderDashboardItems(string dashboardId, List<string> orderedItemIds);
    }
}
