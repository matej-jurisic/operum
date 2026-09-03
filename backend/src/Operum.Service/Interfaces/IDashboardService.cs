using Operum.Model.Common;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.DTOs.Dashboard.Requests;

namespace Operum.Service.Interfaces
{
    public interface IDashboardService
    {
        Task<Result<List<DashboardDto>>> GetDashboards();
        Task<Result> ReorderDashboards(ReorderDashboardsDto dto);
        Task<Result<DashboardDto>> GetDashboard(string dashboardId);
        Task<Result<List<DashboardWidgetDto>>> GetDashboardWidgets(string dashboardId);
        Task<Result<DashboardDto>> CreateDashboard(CreateDashboardDto dto);
        Task<Result<DashboardDto>> UpdateDashboard(string dashboardId, UpdateDashboardDto dto);
        Task<Result> DeleteDashboard(string dashboardId);
        Task<Result<DashboardItemDto>> CreateAndPlaceWidget(string dashboardId, CreateAndPlaceWidgetDto dto);
        Task<Result<DashboardItemDto>> PlaceWidget(string dashboardId, PlaceWidgetDto dto);
        Task<Result<DashboardItemDto>> AddQuickAddItem(string dashboardId, AddDashboardQuickAddItemDto dto);
        Task<Result<DashboardItemDto>> AddFilterItem(string dashboardId, SaveFilterItemDto dto);
        Task<Result<DashboardItemDto>> CreateAndPlaceEntriesWidget(string dashboardId, CreateAndPlaceEntriesWidgetDto dto);
        Task<Result<DashboardItemDto>> PlaceEntriesWidget(string dashboardId, PlaceEntriesWidgetDto dto);
        Task<Result<DashboardItemDto>> AddHeaderItem(string dashboardId, AddDashboardHeaderItemDto dto);
        Task<Result<DashboardItemDto>> AddDividerItem(string dashboardId);
        Task<Result<DashboardItemDto>> AddContainerItem(string dashboardId);
        Task<Result<DashboardItemDto>> AddNoteItem(string dashboardId, AddDashboardNoteItemDto dto);
        Task<Result<List<DashboardWidgetDto>>> UpdateDashboardItem(string dashboardId, string itemId, UpdateDashboardItemDto dto);
        Task<Result<List<DashboardWidgetDto>>> UpdateFilterItem(string dashboardId, string itemId, SaveFilterItemDto dto);
        Task<Result<List<DashboardWidgetDto>>> SetFilterValues(string dashboardId, string itemId, SetFilterValuesDto dto);
        Task<Result<List<DashboardWidgetDto>>> UpdateEntriesItem(string dashboardId, string itemId, UpdateDashboardEntriesItemDto dto);
        Task<Result<DashboardItemDto>> SetTextWidgetContent(string dashboardId, string itemId, SetTextWidgetContentDto dto);
        Task<Result> RemoveDashboardItem(string dashboardId, string itemId);
        Task<Result> UpdateDashboardLayout(string dashboardId, UpdateDashboardLayoutDto dto);

        Task<Result<List<DashboardViewDto>>> GetDashboardViews(string dashboardId);
        Task<Result<DashboardViewDto>> AddDashboardView(string dashboardId, SaveDashboardViewDto dto);
        Task<Result<DashboardViewDto>> UpdateDashboardView(string dashboardId, string viewId, SaveDashboardViewDto dto);
        Task<Result> DeleteDashboardView(string dashboardId, string viewId);
        Task<Result> ReorderDashboardViews(string dashboardId, ReorderDashboardViewsDto dto);
    }
}
