namespace Operum.Model.Constants
{
    public static class AnalyticDataTypePurposes
    {
        public const string Xaxis = "X-axis";
        public const string Yaxis = "Y-axis";
        public const string Value = "Value";

        public static readonly HashSet<string> All =
        [
            Xaxis, Yaxis, Value
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}
