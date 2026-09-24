using Operum.Model.Common;
using Operum.Model.DTOs.Widgets;
using Operum.Model.DTOs.Widgets.Requests;

namespace Operum.Service.Interfaces
{
    // Builds the Widget/EntriesWidget backing a single placement; called only from
    // DashboardService.CreateAndPlaceWidget/CreateAndPlaceEntriesWidget. There's no reuse
    // across dashboards, so nothing else needs to fetch, update, or delete one independently.
    public interface IWidgetsService
    {
        Task<Result<WidgetDto>> CreateWidget(CreateWidgetDto dto);
        Task<Result<EntriesWidgetDefinitionDto>> CreateEntriesWidget(CreateEntriesWidgetDto dto);
    }
}
