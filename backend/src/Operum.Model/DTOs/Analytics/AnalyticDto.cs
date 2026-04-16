using Operum.Model.Constants.Analytics;
using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Analytics
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = nameof(ResultType))]
    [JsonDerivedType(typeof(SingleValueAnalyticDto), AnalyticTypes.SingleValue)]
    [JsonDerivedType(typeof(LineChartAnalyticDto), AnalyticTypes.LineChart)]
    [JsonDerivedType(typeof(ScatterPlotAnalyticDto), AnalyticTypes.ScatterChart)]
    [JsonDerivedType(typeof(CalendarAnalyticDto), AnalyticTypes.Calendar)]
    [JsonDerivedType(typeof(DonutChartAnalyticDto), AnalyticTypes.Donut)]
    [JsonDerivedType(typeof(BarChartAnalyticDto), AnalyticTypes.BarChart)]
    public abstract class AnalyticDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string ResultType { get; set; } = string.Empty;
        public int? Order { get; set; }
    }
}
