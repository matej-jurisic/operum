using Operum.Model.Constants;
using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Analytics
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "ResultType")]
    [JsonDerivedType(typeof(SingleValueAnalyticResult), AnalyticResultTypes.SingleValue)]
    [JsonDerivedType(typeof(NumericChartAnalyticResult), AnalyticResultTypes.NumericChart)]
    [JsonDerivedType(typeof(ScatterPlotAnalyticResult), AnalyticResultTypes.ScatterPlot)]
    [JsonDerivedType(typeof(CalendarAnalyticResult), AnalyticResultTypes.Calendar)]
    public abstract class AnalyticResultDto
    {
        public string AnalyticId { get; set; } = string.Empty;
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

    public class ScatterPlotAnalyticResult : AnalyticResultDto
    {
        public List<ScatterPointDto> Points { get; set; } = [];
        public string XFieldName { get; set; } = string.Empty;
        public string YFieldName { get; set; } = string.Empty;

        public string XFieldType { get; set; } = string.Empty;
        public string YFieldType { get; set; } = string.Empty;

        public ScatterPlotAnalyticResult()
        {
            ResultType = AnalyticResultTypes.ScatterPlot;
        }
    }

    public class CalendarAnalyticResult : AnalyticResultDto
    {
        public string DateFieldName { get; set; } = string.Empty;
        public string EventFieldName { get; set; } = string.Empty;
        public List<CalendarPointDto> Points { get; set; } = [];

        public CalendarAnalyticResult()
        {
            ResultType = AnalyticResultTypes.Calendar;
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
        public string? EntryId { get; set; }
        public DateTime? Date { get; set; }
        public string? Name { get; set; }
    }
}
