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

    // One item of a dashboard as the client renders it: where it sits on each of the two
    // grids, what kind of widget it is, and the payload that kind needs. An analytic widget
    // carries the chart calculated for it; a QuickAdd widget carries the tracker its button
    // opens instead.
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
    }
}
