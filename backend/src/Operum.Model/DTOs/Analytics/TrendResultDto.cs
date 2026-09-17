namespace Operum.Model.DTOs.Analytics
{
    // Y is a plain double, unwrapped from whatever format (duration, count, ...) the widget's value carries.
    public class TrendPointDto
    {
        public string X { get; set; } = string.Empty;
        public double Y { get; set; }
    }

    // Attached to a SingleValue/Goal result only when its placement follows a date-bounded
    // filter clause; see TrendCalculator.
    public class TrendResultDto
    {
        public List<TrendPointDto> Points { get; set; } = [];
        // The same Code run over the immediately preceding, equal-length period. Null if empty.
        public string? PreviousValue { get; set; }
    }
}
