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
        Task<Result<List<DashboardWidgetDto>>> GetDashboardWidgets(string dashboardId);
        Task<Result<DashboardDto>> CreateDashboard(CreateDashboardDto dto);
        Task<Result<DashboardDto>> UpdateDashboard(string dashboardId, UpdateDashboardDto dto);
        Task<Result> DeleteDashboard(string dashboardId);
        Task<Result<DashboardItemDto>> AddDashboardItem(string dashboardId, AddDashboardItemDto dto);
        Task<Result<DashboardItemDto>> AddDashboardItemFromAnalytic(string dashboardId, AddDashboardItemFromAnalyticDto dto);
        Task<Result<DashboardItemDto>> AddQuickAddItem(string dashboardId, AddDashboardQuickAddItemDto dto);
        Task<Result<DashboardItemDto>> AddViewItem(string dashboardId, AddDashboardViewItemDto dto);
        Task<Result<DashboardItemDto>> AddEntriesItem(string dashboardId, AddDashboardEntriesItemDto dto);
        Task<Result<DashboardItemDto>> AddHeaderItem(string dashboardId, AddDashboardHeaderItemDto dto);
        Task<Result<DashboardItemDto>> AddDividerItem(string dashboardId);
        Task<Result<DashboardItemDto>> AddNoteItem(string dashboardId, AddDashboardNoteItemDto dto);
        Task<Result<List<DashboardWidgetDto>>> UpdateDashboardItem(string dashboardId, string itemId, UpdateDashboardItemDto dto);
        Task<Result<List<DashboardWidgetDto>>> SetViewWidgetSelection(string dashboardId, string itemId, SetViewWidgetSelectionDto dto);
        Task<Result<DashboardItemDto>> SetTextWidgetContent(string dashboardId, string itemId, SetTextWidgetContentDto dto);
        Task<Result> RemoveDashboardItem(string dashboardId, string itemId);
        Task<Result> UpdateDashboardLayout(string dashboardId, UpdateDashboardLayoutDto dto);
    }
}
