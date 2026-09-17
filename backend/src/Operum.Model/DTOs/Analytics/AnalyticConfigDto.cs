namespace Operum.Model.DTOs.Analytics
{
    public class AnalyticConfigDto
    {
        public List<AnalyticConfigType> ResultTypes { get; set; } = [];
    }

    public class AnalyticConfigType
    {
        public string Name { get; set; } = default!;
        // Only offered when building a saved widget (e.g. Goal), never Explore/notifications.
        public bool WidgetOnly { get; set; }
        public List<AnalyticConfigCode> Codes { get; set; } = [];

        // Line/Bar only: calculation is a (Grouping, Code) pair.
        public List<AnalyticConfigGrouping> Groupings { get; set; } = [];
        public string GroupingPurpose { get; set; } = string.Empty;
    }

    public class AnalyticConfigCode
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public List<AnalyticConfigPurpose> Purposes { get; set; } = [];
    }

    public class AnalyticConfigGrouping
    {
        public string Grouping { get; set; } = default!;
        public string Name { get; set; } = default!;
        public List<string> AllowedDataTypes { get; set; } = [];
        public List<string> AllowedCodes { get; set; } = [];
    }

    public class AnalyticConfigPurpose
    {
        public string Name { get; set; } = default!;
        public List<string> AllowedDataTypes { get; set; } = [];
        // The calculation runs fine without this purpose (e.g. Min/Max's Display field).
        public bool Optional { get; set; }
    }
}
