namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int Order { get; set; }
        // What the item renders, and where it sits on each of the dashboard's two grids.
        public string Type { get; set; } = string.Empty;
        public DashboardWidgetLayoutDto Layout { get; set; } = new();
        public DashboardWidgetLayoutDto MobileLayout { get; set; } = new();
        public string? Config { get; set; }
        // What this widget is called on the board: an Analytic widget's own name (or its
        // calculation's default label), or an Entries widget's name. Empty for the kinds
        // that have no name (View/QuickAdd/Header/Divider/Note). Only used so a form
        // elsewhere can label this item without a second fetch — see ViewWidgetForm.
        public string Name { get; set; } = string.Empty;
        // Every tracker this widget reads from: one for an Entries widget, one or more for
        // an Analytic widget (a composed chart spans several), empty for the rest. Lets a
        // caller find which widgets a View selector for a given tracker could link.
        public List<string> TrackerIds { get; set; } = [];
        // The single analytic definition every source below is calculated with.
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        // Combined charts only: whether the chart is restricted to x-axis values shared by
        // every source.
        public bool MatchedValuesOnly { get; set; }
        public List<DashboardItemSourceDto> Sources { get; set; } = [];
    }
}
