namespace Operum.Model.Constants
{
    public static class OperatorTypes
    {
        public const string EqualsOperator = "Equals";
        public const string NotEquals = "NotEquals";
        public const string GreaterThan = "GreaterThan";
        public const string LessThan = "LessThan";
        public const string GreaterThanOrEqual = "GreaterThanOrEqual";
        public const string LessThanOrEqual = "LessThanOrEqual";
        public const string Contains = "Contains";
        public const string StartsWith = "StartsWith";
        public const string EndsWith = "EndsWith";

        public static readonly HashSet<string> All =
        [
            EqualsOperator, NotEquals, GreaterThan, GreaterThanOrEqual, LessThanOrEqual, LessThan, Contains, StartsWith, EndsWith
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}
