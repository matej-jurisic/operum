namespace Operum.Model.Constants.Notifications
{
    public static class ComparisonOperators
    {
        public const string GreaterThan = ">";
        public const string LessThan = "<";
        public const string GreaterThanOrEqual = ">=";
        public const string LessThanOrEqual = "<=";
        public const string Equal = "==";
        public const string NotEqual = "!=";

        public static readonly HashSet<string> All =
        [
            GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, Equal, NotEqual
        ];

        public static bool IsValid(string op) => All.Contains(op);

        public static bool Evaluate(double actual, string op, double threshold) => op switch
        {
            ">"  => actual > threshold,
            "<"  => actual < threshold,
            ">=" => actual >= threshold,
            "<=" => actual <= threshold,
            "==" => actual == threshold,
            "!=" => actual != threshold,
            _    => false
        };
    }
}
