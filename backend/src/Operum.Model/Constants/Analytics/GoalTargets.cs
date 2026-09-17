using System.Globalization;
using Operum.Model.Constants.Fields;

namespace Operum.Model.Constants.Analytics
{
    // Target is stored as a raw string, read back as a magnitude by GoalAnalyticBuilder.
    public static class GoalTargets
    {
        // Sum/Average/Min/Max over a duration field produce a duration target; every other
        // code produces a plain number.
        private static readonly HashSet<string> DurationCarryingCodes =
            [AnalyticCodes.Sum, AnalyticCodes.Average, AnalyticCodes.Min, AnalyticCodes.Max];

        public static bool IsParseable(string? target) =>
            !string.IsNullOrWhiteSpace(target) &&
            (double.TryParse(target, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
             TimeSpan.TryParse(target, CultureInfo.InvariantCulture, out _));

        public static bool MatchesFieldType(string code, string? valueFieldType, string target) =>
            valueFieldType == DataTypes.TimeSpan && DurationCarryingCodes.Contains(code)
                ? TimeSpan.TryParse(target, CultureInfo.InvariantCulture, out _)
                : double.TryParse(target, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }
}
