using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class SingleValueAnalyticDto : AnalyticDto
    {
        public string Value { get; set; } = string.Empty;
        public string? EntryId { get; set; }
        public FieldDto ValueField { get; set; } = null!;

        // Min/Max with a Display field only.
        public string? SecondaryValue { get; set; }
        public FieldDto? SecondaryValueField { get; set; }

        // Set only when followed by a date-bounded filter clause with ShowTrend on; see TrendCalculator.
        public TrendResultDto? Trend { get; set; }

        public SingleValueAnalyticDto()
        {
            ResultType = AnalyticTypes.SingleValue;
        }
    }
}
