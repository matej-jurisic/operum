using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    // Value/Target are strings in ValueField's format; Progress is their ratio and can
    // exceed 1 once the target is met.
    public class GoalAnalyticDto : AnalyticDto
    {
        public string Value { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        // Null when there's no data, or the target isn't a positive number.
        public double? Progress { get; set; }
        public FieldDto ValueField { get; set; } = null!;
        // HigherIsBetter unless set up as a cap/budget.
        public string Direction { get; set; } = GoalDirections.HigherIsBetter;

        // Set only when followed by a date-bounded filter clause with ShowTrend on; see TrendCalculator.
        public TrendResultDto? Trend { get; set; }

        public GoalAnalyticDto()
        {
            ResultType = AnalyticTypes.Goal;
        }
    }
}
