using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class ComposedChartSeriesDto
    {
        // DashboardItemSource id, used as the chart series/dataKey.
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        // "line" | "bar" — see ComposedSeriesRenderTypes.
        public string RenderType { get; set; } = string.Empty;
        // Null falls back to the frontend's cycling palette.
        public string? Color { get; set; }
        // Line's XField or bar's NameField; lets the frontend format axis ticks/tooltips.
        public FieldDto XField { get; set; } = null!;
        public FieldDto ValueField { get; set; } = null!;
        public List<ComposedChartPointDto> Points { get; set; } = [];
    }
}
