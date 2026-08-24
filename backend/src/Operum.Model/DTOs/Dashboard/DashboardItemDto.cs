namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int Order { get; set; }
        // What the item renders, and where it sits on the dashboard grid.
        public string Type { get; set; } = string.Empty;
        public DashboardWidgetLayoutDto Layout { get; set; } = new();
        public string? Config { get; set; }
        // The single analytic definition every source below is calculated with.
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        // Combined charts only: whether the chart is restricted to x-axis values shared by
        // every source.
        public bool MatchedValuesOnly { get; set; }
        public List<DashboardItemSourceDto> Sources { get; set; } = [];
    }
}
