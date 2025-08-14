using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Analytics
{
    public class FieldAnalyticsDto
    {
        public string FieldName { get; set; } = string.Empty;
        // Common
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Count { get; set; }

        // For Numbers
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Average { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Min { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Max { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? StdDev { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Sum { get; set; }

        // For DateTime
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? MinDateTime { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? MaxDateTime { get; set; }

        // For Date
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? MinDate { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? MaxDate { get; set; }

        // For TimeSpan
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TimeSpan? MinTimeSpan { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TimeSpan? MaxTimeSpan { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TimeSpan? AverageTimeSpan { get; set; }

        // For Bool
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TrueCount { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FalseCount { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? TruePercentage { get; set; }
    }
}
