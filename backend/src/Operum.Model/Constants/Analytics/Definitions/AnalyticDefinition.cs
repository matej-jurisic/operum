namespace Operum.Model.Constants.Analytics.Definitions
{
    public class AnalyticDefinition
    {
        public HashSet<string> Purposes { get; init; } = [];
        public Dictionary<string, AnalyticPurposeDataTypes> Codes { get; init; } = [];
    }
}
