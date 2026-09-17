namespace Operum.Model.Constants.Analytics.Definitions
{
    public class AnalyticPurposeDataTypes
    {
        public string Label { get; init; } = string.Empty;
        public Dictionary<string, HashSet<string>> AllowedDataTypes { get; init; } = [];

        // Purposes a caller may leave unmapped; every other key of AllowedDataTypes is required.
        public HashSet<string> OptionalPurposes { get; init; } = [];
    }
}
