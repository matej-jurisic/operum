namespace Operum.Model.Constants
{
    public static class AnalyticCodes
    {
        public const string NumberCount = "NumberCount";
        public const string DateCount = "DateCount";
        public const string DatetimeCount = "DatetimeCount";
        public const string StringCount = "StringCount";
        public const string BoolCount = "BoolCount";
        public const string TimespanCount = "TimespanCount";

        public static readonly HashSet<string> All =
        [
            NumberCount, DateCount, DatetimeCount, StringCount, BoolCount, TimespanCount
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}
