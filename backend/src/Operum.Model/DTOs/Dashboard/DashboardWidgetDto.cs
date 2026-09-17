using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Entries;
using Operum.Model.DTOs.Fields;
using Operum.Model.Enums;

namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardWidgetLayoutDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        // Analytic/Entries widgets only.
        public DashboardItemDisplayMode DisplayMode { get; set; }
    }

    // Resolved server-side from Config's trackerId so the client can render the button
    // immediately instead of fetching the tracker once the card mounts.
    public class QuickAddTrackerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
    }

    // Null Value means the clause is unset and not applied. SlotId is the widget-local id
    // SetFilterValues writes back under, and that a goal's conditional target names.
    public class FilterClauseDto
    {
        public string SlotId { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Operator { get; set; }
        public string? Value { get; set; }
    }

    // A DashboardView whose clause shape matches the widget's; Values are in the widget's own clause order.
    public class FilterPresetOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string?> Values { get; set; } = [];
    }

    public class FilterWidgetDto
    {
        public List<FilterClauseDto> Clauses { get; set; } = [];
        public List<FilterPresetOptionDto> Presets { get; set; } = [];
    }

    // Rows are already filtered/sorted by whatever filter widgets this placement follows,
    // capped to the most recent handful; the card does not fetch its own rows.
    public class EntriesWidgetDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public List<FieldDto> Columns { get; set; } = [];
        public List<EntryDto> Entries { get; set; } = [];
    }

    public class DashboardWidgetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        // Null on the board itself, and always null on the narrow grid (containers are
        // flattened there).
        public string? ParentItemId { get; set; }
        // Set when the parent is a TabsContainer; only the active tab's widgets are drawn.
        public string? ParentTabId { get; set; }
        // In DashboardGrid.Columns columns.
        public DashboardWidgetLayoutDto Layout { get; set; } = new();
        // In DashboardGrid.MobileColumns columns; the client writes back only the one it rendered.
        public DashboardWidgetLayoutDto MobileLayout { get; set; } = new();
        public string? Config { get; set; }
        public AnalyticDto? Analytic { get; set; }
        public QuickAddTrackerDto? QuickAddTracker { get; set; }
        public FilterWidgetDto? Filter { get; set; }
        public EntriesWidgetDto? EntriesWidget { get; set; }
        // Null when the widget spans more than one tracker (a combined chart); client then
        // falls back to the dashboard's own color.
        public string? TrackerColor { get; set; }
        // Beats TrackerColor and the dashboard color; only set for a single-tracker widget,
        // see DashboardService.BuildWidgets.
        public string? Color { get; set; }
    }
}
