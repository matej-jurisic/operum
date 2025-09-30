namespace Operum.Model.Constants
{
    public static class AnalyticCodes
    {
        public const string NumberCount = "NumberCount";
        public const string NumberMin = "NumberMin";
        public const string NumberMax = "NumberMax";
        public const string NumberAverage = "NumberAverage";
        public const string NumberSum = "NumberSum";
        public const string NumberStdDev = "NumberStdDev";

        public const string TimespanCount = "TimespanCount";
        public const string TimespanMin = "TimespanMin";
        public const string TimespanMax = "TimespanMax";
        public const string TimespanAverage = "TimespanAverage";
        public const string TimespanSum = "TimespanSum";

        public const string DateCount = "DateCount";
        public const string DateMin = "DateMin";
        public const string DateMax = "DateMax";

        public const string DatetimeCount = "DatetimeCount";
        public const string DatetimeMin = "DatetimeMin";
        public const string DatetimeMax = "DatetimeMax";

        public const string StringCount = "StringCount";

        public const string BoolCount = "BoolCount";
        public const string BoolTrueCount = "BoolTrueCount";
        public const string BoolFalseCount = "BoolFalseCount";
        public const string BoolTruePercentage = "BoolTruePercentage";

        public const string DateNumberLineChart = "DateNumberLineChart";
        public const string DatetimeNumberLineChart = "DatetimeNumberLineChart";
        public const string DateTimespanLineChart = "DateTimespanLineChart";
        public const string DatetimeTimespanLineChart = "DatetimeTimespanLineChart";
        public const string DateBoolTimeChart = "DateBoolTimeChart";
        public const string DatetimeBoolLineChart = "DatetimeBoolLineChart";
        public const string StringNumberLineChart = "StringNumberLineChart";
        public const string StringBoolLineChart = "StringBoolLineChart";


        public static readonly HashSet<string> All =
        [
            NumberCount, NumberMin, NumberMax, NumberAverage, NumberSum, NumberStdDev,
            TimespanCount, TimespanMin, TimespanMax, TimespanAverage, TimespanSum,
            DateCount, DateMin, DateMax,
            DatetimeCount, DatetimeMin, DatetimeMax,
            StringCount,
            BoolCount, BoolTrueCount, BoolFalseCount, BoolTruePercentage,
            DateNumberLineChart, DatetimeNumberLineChart, DateTimespanLineChart, DatetimeTimespanLineChart, DateBoolTimeChart, DatetimeBoolLineChart, StringNumberLineChart, StringBoolLineChart
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}
