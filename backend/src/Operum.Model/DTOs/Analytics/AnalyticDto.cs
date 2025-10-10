using Operum.Model.Constants.Analytics;
using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Analytics
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = nameof(ResultType))]
    [JsonDerivedType(typeof(SingleValueAnalyticDto), AnalyticTypes.SingleValue)]
    [JsonDerivedType(typeof(LineChartAnalyticDto), AnalyticTypes.NumericChart)]
    [JsonDerivedType(typeof(ScatterPlotAnalyticDto), AnalyticTypes.ScatterPlot)]
    [JsonDerivedType(typeof(CalendarAnalyticDto), AnalyticTypes.Calendar)]
    public abstract class AnalyticDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string ResultType { get; set; } = string.Empty;
    }
}
