namespace Operum.Model.Constants
{
    public static class AnalyticResultTypes
    {
        public const string SingleValue = "SingleValue";
        public const string NumericChart = "NumericChart";
        public const string ScatterPlot = "ScatterPlot";
        public const string Calendar = "Calendar";

        public static readonly HashSet<string> All =
        [
            SingleValue, NumericChart, ScatterPlot, Calendar
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }


    public static class AnalyticCodes
    {
        public const string Count = "Count";
        public const string Min = "Min";
        public const string Max = "Max";
        public const string Average = "Average";
        public const string Sum = "Sum";
        public const string StdDev = "Standard Deviation";

        public const string TrueCount = "True Count";
        public const string FalseCount = "False Count";
        public const string TruePercentage = "True Percentage";

        public const string AggregatedLineChart = "Aggregated LineChart";
        public const string CumulativeLineChart = "Cumulative LineChart";
        public const string LineChart = "LineChart";

        public const string ScatterPlot = "ScatterPlot";
        public const string Calendar = "Calendar";

        public static readonly HashSet<string> All =
        [
            Count, Min, Max, Average, Sum, StdDev,
            TrueCount, FalseCount, TruePercentage,
            AggregatedLineChart, CumulativeLineChart, LineChart,
            Calendar, ScatterPlot
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }


    public static class AnalyticDataTypePurposes
    {
        public const string Xaxis = "X-axis";
        public const string Yaxis = "Y-axis";
        public const string Value = "Value";
        public const string What = "What";
        public const string When = "When";

        public static readonly HashSet<string> All =
        [
            Xaxis, Yaxis, Value, When, What
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }


    /// <summary>
    /// Holds rules for a specific Analytic Code:
    ///  - Allowed DataTypes for each Purpose
    /// </summary>
    public class AnalyticCodeDefinition
    {
        /// <summary>
        /// Purpose → Allowed DataTypes
        /// </summary>
        public Dictionary<string, HashSet<string>> AllowedDataTypes { get; init; } = new();
    }


    /// <summary>
    /// Groups all codes and purposes for a given AnalyticResultType
    /// </summary>
    public class AnalyticDefinition
    {
        public HashSet<string> Purposes { get; init; } = [];
        public Dictionary<string, AnalyticCodeDefinition> Codes { get; init; } = new();
    }


    public static class AnalyticDefinitions
    {
        public static readonly Dictionary<string, AnalyticDefinition> ByResultType =
            new()
            {
                [AnalyticResultTypes.SingleValue] = new AnalyticDefinition
                {
                    Purposes = [AnalyticDataTypePurposes.Value],
                    Codes = new()
                    {
                        [AnalyticCodes.Count] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [.. DataTypes.All]
                            }
                        },
                        [AnalyticCodes.Min] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan, DataTypes.Date, DataTypes.DateTime]
                            }
                        },
                        [AnalyticCodes.Max] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan, DataTypes.Date, DataTypes.DateTime]
                            }
                        },
                        [AnalyticCodes.Average] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.Sum] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.StdDev] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.TrueCount] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [DataTypes.Bool]
                            }
                        },
                        [AnalyticCodes.FalseCount] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [DataTypes.Bool]
                            }
                        },
                        [AnalyticCodes.TruePercentage] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Value] = [DataTypes.Bool]
                            }
                        }
                    }
                },

                [AnalyticResultTypes.NumericChart] = new AnalyticDefinition
                {
                    Purposes = [AnalyticDataTypePurposes.Xaxis, AnalyticDataTypePurposes.Yaxis],
                    Codes = new()
                    {
                        [AnalyticCodes.AggregatedLineChart] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticDataTypePurposes.Yaxis] = [.. DataTypes.All]
                            }
                        },
                        [AnalyticCodes.CumulativeLineChart] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticDataTypePurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.LineChart] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticDataTypePurposes.Yaxis] = [.. DataTypes.All]
                            }
                        }
                    }
                },

                [AnalyticResultTypes.ScatterPlot] = new AnalyticDefinition
                {
                    Purposes = [AnalyticDataTypePurposes.Xaxis, AnalyticDataTypePurposes.Yaxis],
                    Codes = new()
                    {
                        [AnalyticCodes.ScatterPlot] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.Xaxis] = [DataTypes.Number, DataTypes.TimeSpan],
                                [AnalyticDataTypePurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        }
                    }
                },

                [AnalyticResultTypes.Calendar] = new AnalyticDefinition
                {
                    Purposes = [AnalyticDataTypePurposes.When, AnalyticDataTypePurposes.What],
                    Codes = new()
                    {
                        [AnalyticCodes.Calendar] = new AnalyticCodeDefinition
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticDataTypePurposes.When] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticDataTypePurposes.What] = [.. DataTypes.All]
                            }
                        }
                    }
                }
            };


        public static bool IsValidForType(string resultType, string code) =>
            ByResultType.TryGetValue(resultType, out var def) && def.Codes.ContainsKey(code);

        public static bool SupportsPurpose(string resultType, string purpose) =>
            ByResultType.TryGetValue(resultType, out var def) && def.Purposes.Contains(purpose);

        public static bool IsValidDataType(string resultType, string code, string purpose, string dataType) =>
            ByResultType.TryGetValue(resultType, out var def) &&
            def.Codes.TryGetValue(code, out var codeDef) &&
            codeDef.AllowedDataTypes.TryGetValue(purpose, out var allowed) &&
            allowed.Contains(dataType);
    }
}
