namespace Operum.Model.DTOs.Analytics
{
    public class SingleFieldNumericAnalyticsDto
    {
        public int? Count { get; set; }
        public double? Average { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? StdDev { get; set; }
    }
}
