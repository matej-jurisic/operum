using Operum.Model.DTOs.Analytics;

namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardWidgetLayoutDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
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

    // One view a View widget's dropdown can be set to.
    public class ViewOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // What a DashboardWidgetTypes.View widget's dropdown needs — resolved server-side from
    // Config the same way QuickAddTrackerDto is, plus the tracker's own views and which one
    // Config currently names. ViewId is the persisted, current selection: it changes (and is
    // saved) whenever the dropdown does, so every DashboardItemSource with a matching
    // LinkedViewWidgetId is resolved against it.
    public class ViewWidgetDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public string? ViewId { get; set; }
        public List<ViewOptionDto> Views { get; set; } = [];
    }

    // One item of a dashboard as the client renders it: where it sits on each of the two
    // grids, what kind of widget it is, and the payload that kind needs. An analytic widget
    // carries the chart calculated for it; a QuickAdd widget carries the tracker its button
    // opens instead; a View widget carries its dropdown's tracker, options and selection.
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
        public ViewWidgetDto? ViewWidget { get; set; }
        // The color of the single tracker every source of this widget reads from. Null when
        // the widget has no single owning tracker — a combined chart spanning more than one —
        // so the client falls back to the dashboard's own color instead.
        public string? TrackerColor { get; set; }
    }
}
