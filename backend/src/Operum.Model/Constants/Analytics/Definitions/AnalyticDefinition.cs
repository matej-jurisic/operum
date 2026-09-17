namespace Operum.Model.Constants.Analytics.Definitions
{
    public class AnalyticDefinition
    {
        public HashSet<string> Purposes { get; init; } = [];
        public Dictionary<string, AnalyticPurposeDataTypes> Codes { get; init; } = [];

        // True only for types that require a saved Widget (e.g. Goal needs a target stored
        // on the Widget); excluded from ad hoc Explore/notification use.
        public bool WidgetOnly { get; init; }

        // Line/Bar only: calculation is a (Grouping, Code) pair instead of a bare Code.
        public Dictionary<string, AnalyticGrouping> Groupings { get; init; } = [];

        // Line/Bar only: the purpose the grouping buckets (X-axis for a line, Name for a bar).
        public string GroupingPurpose { get; init; } = string.Empty;

        // Line/Bar only: "value" or "category", used to compose labels.
        public string AxisNoun { get; init; } = string.Empty;

        public bool UsesGrouping => Groupings.Count > 0;
    }

    public class AnalyticGrouping
    {
        public string Label { get; init; } = string.Empty;

        public HashSet<string> AllowedAxisTypes { get; init; } = [];

        public HashSet<string> AllowedCodes { get; init; } = [];
    }
}
