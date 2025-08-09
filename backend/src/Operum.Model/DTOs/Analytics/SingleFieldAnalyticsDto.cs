namespace Operum.Model.DTOs.Analytics
{
    public class SingleFieldAnalyticsDto
    {
        // Common
        public int? Count { get; set; }

        // For Numbers
        public double? Average { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? StdDev { get; set; }
        public double? Sum { get; set; }

        // For DateTime
        public DateTime? MinDateTime { get; set; }
        public DateTime? MaxDateTime { get; set; }

        // For Date
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }

        // For TimeSpan
        public TimeSpan? MinTimeSpan { get; set; }
        public TimeSpan? MaxTimeSpan { get; set; }
        public TimeSpan? AverageTimeSpan { get; set; }

        // For Bool
        public int? TrueCount { get; set; }
        public int? FalseCount { get; set; }
        public double? TruePercentage { get; set; }
    }
}
