namespace Operum.Model.Constants.Analytics.Definitions
{
    public class AnalyticPurposeDataTypes
    {
        public string Label { get; init; } = string.Empty;
        public Dictionary<string, HashSet<string>> AllowedDataTypes { get; init; } = [];
    }
}
