using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class ComposedChartSeriesDto
    {
        // Stable id (the DashboardItemSource id) used as the chart series/dataKey.
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        // "line" | "bar" — see ComposedSeriesRenderTypes.
        public string RenderType { get; set; } = string.Empty;
        // The field each point's X came from (line's XField, or bar's NameField) — needed
        // so the frontend can format axis ticks/tooltips (dates, bools, etc.) instead of
        // rendering the raw point value.
        public FieldDto XField { get; set; } = null!;
        public FieldDto ValueField { get; set; } = null!;
        public List<ComposedChartPointDto> Points { get; set; } = [];
    }
}
