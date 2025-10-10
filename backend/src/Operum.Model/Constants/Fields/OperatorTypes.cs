namespace Operum.Model.Constants
{
    public static class OperatorTypes
    {
        public const string EqualsOperator = "Equals";
        public const string NotEquals = "Not Equals";
        public const string GreaterThan = "Greater Than";
        public const string LessThan = "Less Than";
        public const string GreaterThanOrEqual = "Greater Than Or Equal";
        public const string LessThanOrEqual = "Less Than Or Equal";
        public const string Contains = "Contains";
        public const string StartsWith = "Starts With";
        public const string EndsWith = "Ends With";

        public static readonly HashSet<string> All =
        [
            EqualsOperator, NotEquals, GreaterThan, GreaterThanOrEqual, LessThanOrEqual, LessThan, Contains, StartsWith, EndsWith
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}
