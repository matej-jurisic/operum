using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardWidgetLayoutDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        // Analytic/Entries widgets only: whether this grid draws the widget as a small
        // button that opens the real thing in a modal instead of inline.
        public bool Expandable { get; set; }
    }

    // The tracker summary a QuickAdd widget's button needs — resolved server-side from
    // Config's trackerId so the client can render the button immediately instead of
    // fetching the tracker itself once the card mounts.
    public class QuickAddTrackerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
    }

    // One option a view selector widget's dropdown can be set to -- a DashboardView on the
    // same board, resolved to just its id and name for the card to render.
    public class ViewSelectorOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // What a DashboardWidgetTypes.ViewSelector widget's dropdown needs -- resolved
    // server-side from Config the same way QuickAddTrackerDto is: the DashboardViews it
    // offers (id + name), and which one Config currently names.
    public class ViewSelectorWidgetDto
    {
        public List<ViewSelectorOptionDto> Options { get; set; } = [];
        public string? SelectedId { get; set; }
    }

    // What a DashboardWidgetTypes.Entries widget's table needs — resolved server-side the
    // same way QuickAddTrackerDto is: the tracker it reads from, the fixed view it is
    // filtered by, and the columns that view wants shown, in its order. A view naming none
    // shows every field, the same fallback the tracker page uses.
    public class EntriesWidgetDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public string? ViewId { get; set; }
        public List<FieldDto> Columns { get; set; } = [];
    }

    // One item of a dashboard as the client renders it: where it sits on each of the two
    // grids, what kind of widget it is, and the payload that kind needs. An analytic widget
    // carries the chart calculated for it; a QuickAdd widget carries the tracker its button
    // opens instead; a View widget carries its dropdown's tracker, options and selection; an
    // Entries widget carries its table's tracker, columns and current view.
    public class DashboardWidgetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        // Placement on the wide grid, in DashboardGrid.Columns columns.
        public DashboardWidgetLayoutDto Layout { get; set; } = new();
        // Placement on the narrow grid, in DashboardGrid.MobileColumns columns. The client
        // picks between the two by its own width, and only writes back the one it rendered.
        public DashboardWidgetLayoutDto MobileLayout { get; set; } = new();
        public string? Config { get; set; }
        public AnalyticDto? Analytic { get; set; }
        public QuickAddTrackerDto? QuickAddTracker { get; set; }
        public ViewSelectorWidgetDto? ViewSelector { get; set; }
        public EntriesWidgetDto? EntriesWidget { get; set; }
        // The color of the single tracker every source of this widget reads from. Null when
        // the widget has no single owning tracker — a combined chart spanning more than one —
        // so the client falls back to the dashboard's own color instead.
        public string? TrackerColor { get; set; }
    }
}
