using Operum.Model.Common;
using Operum.Model.DTOs.Widgets;
using Operum.Model.DTOs.Widgets.Requests;

namespace Operum.Service.Interfaces
{
    public interface IWidgetsService
    {
        Task<Result<List<WidgetDto>>> GetWidgets(string? trackerId);
        Task<Result<WidgetDto>> GetWidget(string widgetId);
        Task<Result<WidgetDto>> CreateWidget(CreateWidgetDto dto);
        Task<Result<WidgetDto>> UpdateWidget(string widgetId, UpdateWidgetDto dto);
        Task<Result> DeleteWidget(string widgetId);

        Task<Result<List<EntriesWidgetDefinitionDto>>> GetEntriesWidgets(string? trackerId);
        Task<Result<EntriesWidgetDefinitionDto>> GetEntriesWidget(string entriesWidgetId);
        Task<Result<EntriesWidgetDefinitionDto>> CreateEntriesWidget(CreateEntriesWidgetDto dto);
        Task<Result<EntriesWidgetDefinitionDto>> UpdateEntriesWidget(string entriesWidgetId, UpdateEntriesWidgetDto dto);
        Task<Result> DeleteEntriesWidget(string entriesWidgetId);
    }
}
