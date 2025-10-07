using Operum.Model.Constants;
using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Analytics
{
    public class TrackerAnalyticsResultDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public List<AnalyticResultDto> Analytics { get; set; } = [];
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "ResultType")]
    [JsonDerivedType(typeof(SingleValueAnalyticResult), AnalyticResultTypes.SingleValue)]
    [JsonDerivedType(typeof(NumericChartAnalyticResult), AnalyticResultTypes.NumericChart)]
    [JsonDerivedType(typeof(ScatterChartAnalyticResult), AnalyticResultTypes.ScatterChart)]
    [JsonDerivedType(typeof(CalendarEventsAnalyticResult), AnalyticResultTypes.CalendarEvents)]
    public abstract class AnalyticResultDto
    {
        public string AnalyticId { get; set; } = string.Empty;
        public string TrackerAnalyticId { get; set; } = string.Empty;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string ResultType { get; set; } = string.Empty;
    }

    public class SingleValueAnalyticResult : AnalyticResultDto
    {
        public string Value { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? EntryId { get; set; }

        public SingleValueAnalyticResult()
        {
            ResultType = AnalyticResultTypes.SingleValue;
        }
    }

    public class NumericChartAnalyticResult : AnalyticResultDto
    {
        public List<ChartPointDto> Points { get; set; } = [];
        public string XFieldName { get; set; } = string.Empty;
        public string YFieldName { get; set; } = string.Empty;
        public string YFieldType { get; set; } = string.Empty;

        public NumericChartAnalyticResult()
        {
            ResultType = AnalyticResultTypes.NumericChart;
        }
    }

    public class ScatterChartAnalyticResult : AnalyticResultDto
    {
        public List<ScatterPointDto> Points { get; set; } = [];
        public string XFieldName { get; set; } = string.Empty;
        public string YFieldName { get; set; } = string.Empty;

        public string XFieldType { get; set; } = string.Empty;
        public string YFieldType { get; set; } = string.Empty;

        public ScatterChartAnalyticResult()
        {
            ResultType = AnalyticResultTypes.ScatterChart;
        }
    }

    public class CalendarEventsAnalyticResult : AnalyticResultDto
    {
        public string DateFieldName { get; set; } = string.Empty;
        public string EventFieldName { get; set; }  = string.Empty;
        public List<CalendarPointDto> Points { get; set; } = [];

        public CalendarEventsAnalyticResult()
        {
            ResultType = AnalyticResultTypes.CalendarEvents;
        }
    }
    
    public class ChartPointDto
    {
        public string? X { get; set; }
        public double? Y { get; set; }
    }

    public class ScatterPointDto
    {
        public double? X { get; set; }
        public double? Y { get; set; }
    }

    public class CalendarPointDto
    {
        public DateTime? Date { get; set; }
        public string? Name { get; set; }
    }
}
