namespace Operum.Model.Constants
{
    public static class AnalyticDataTypePurposes
    {
        public const string Xaxis = "X-axis";
        public const string Yaxis = "Y-axis";
        public const string Value = "Value";
        public const string Datetime = "Datetime";
        public const string Name = "Name";

        public static readonly HashSet<string> All =
        [
            Xaxis, Yaxis, Value, Name, Datetime, Name
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}
