using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Converters;
using System.Globalization;

namespace Operum.Service.Domain.Notifications
{
    public static class NotificationConditionEvaluator
    {
        public static bool Evaluate(string? actualStr, string op, string thresholdStr, TimeZoneInfo tz)
        {
            if (actualStr is null) return false;

            if (double.TryParse(actualStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var actual) &&
                double.TryParse(thresholdStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var threshold))
            {
                return op switch
                {
                    OperatorTypes.EqualsOperator => Math.Abs(actual - threshold) < 1e-9,
                    OperatorTypes.NotEquals => Math.Abs(actual - threshold) >= 1e-9,
                    OperatorTypes.GreaterThan => actual > threshold,
                    OperatorTypes.GreaterThanOrEqual => actual >= threshold,
                    OperatorTypes.LessThan => actual < threshold,
                    OperatorTypes.LessThanOrEqual => actual <= threshold,
                    _ => false
                };
            }

            // threshold may be a dynamic token, e.g. "start_of_month"
            var thresholdDate = DynamicDateTokens.IsValid(thresholdStr)
                ? DynamicDateTokens.Resolve(thresholdStr, tz)
                : DataFormatters.StringToDateTime(thresholdStr);
            var actualDate = DataFormatters.StringToDateTime(actualStr);

            if (thresholdDate.HasValue && actualDate.HasValue)
            {
                return op switch
                {
                    OperatorTypes.EqualsOperator => actualDate.Value == thresholdDate.Value,
                    OperatorTypes.NotEquals => actualDate.Value != thresholdDate.Value,
                    OperatorTypes.GreaterThan => actualDate.Value > thresholdDate.Value,
                    OperatorTypes.GreaterThanOrEqual => actualDate.Value >= thresholdDate.Value,
                    OperatorTypes.LessThan => actualDate.Value < thresholdDate.Value,
                    OperatorTypes.LessThanOrEqual => actualDate.Value <= thresholdDate.Value,
                    _ => false
                };
            }

            return op switch
            {
                OperatorTypes.EqualsOperator => string.Equals(actualStr, thresholdStr, StringComparison.OrdinalIgnoreCase),
                OperatorTypes.NotEquals => !string.Equals(actualStr, thresholdStr, StringComparison.OrdinalIgnoreCase),
                OperatorTypes.Contains => actualStr.Contains(thresholdStr, StringComparison.OrdinalIgnoreCase),
                OperatorTypes.StartsWith => actualStr.StartsWith(thresholdStr, StringComparison.OrdinalIgnoreCase),
                OperatorTypes.EndsWith => actualStr.EndsWith(thresholdStr, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
