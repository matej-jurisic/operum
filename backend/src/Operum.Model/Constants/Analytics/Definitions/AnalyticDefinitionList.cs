using Operum.Model.Constants.Fields;

namespace Operum.Model.Constants.Analytics.Definitions
{
    public static class AnalyticDefinitionList
    {
        public static readonly Dictionary<string, AnalyticDefinition> ByResultType =
            new()
            {
                [AnalyticTypes.SingleValue] = new AnalyticDefinition
                {
                    Purposes = [AnalyticPurposes.Value],
                    Codes = new()
                    {
                        [AnalyticCodes.Count] = new AnalyticPurposeDataTypes
                        {
                            Label = "Count",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [.. DataTypes.All]
                            }
                        },
                        [AnalyticCodes.Min] = new AnalyticPurposeDataTypes
                        {
                            Label = "Minimum",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan, DataTypes.Date, DataTypes.DateTime]
                            }
                        },
                        [AnalyticCodes.Max] = new AnalyticPurposeDataTypes
                        {
                            Label = "Maximum",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan, DataTypes.Date, DataTypes.DateTime]
                            }
                        },
                        [AnalyticCodes.Average] = new AnalyticPurposeDataTypes
                        {
                            Label = "Average",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.Sum] = new AnalyticPurposeDataTypes
                        {
                            Label = "Sum",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.StdDev] = new AnalyticPurposeDataTypes
                        {
                            Label = "Std. Deviation",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.TrueCount] = new AnalyticPurposeDataTypes
                        {
                            Label = "Yes Count",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Bool]
                            }
                        },
                        [AnalyticCodes.FalseCount] = new AnalyticPurposeDataTypes
                        {
                            Label = "No Count",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Bool]
                            }
                        },
                        [AnalyticCodes.TruePercentage] = new AnalyticPurposeDataTypes
                        {
                            Label = "Yes Percentage",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Bool]
                            }
                        },
                        [AnalyticCodes.CountDistinct] = new AnalyticPurposeDataTypes
                        {
                            Label = "Unique Count",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [.. DataTypes.All]
                            }
                        },
                        [AnalyticCodes.MostCommon] = new AnalyticPurposeDataTypes
                        {
                            Label = "Most Common",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [.. DataTypes.All]
                            }
                        },
                        [AnalyticCodes.LeastCommon] = new AnalyticPurposeDataTypes
                        {
                            Label = "Least Common",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [.. DataTypes.All]
                            }
                        }
                    }
                },

                [AnalyticTypes.LineChart] = new AnalyticDefinition
                {
                    Purposes = [AnalyticPurposes.Xaxis, AnalyticPurposes.Yaxis],
                    Codes = new()
                    {
                        [AnalyticCodes.AggregatedSumLineChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Sum by Category",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.CumulativeLineChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Cumulative Sum",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.LineChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Raw Values",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.DailyLineChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Daily Totals",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.WeeklyLineChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Weekly Totals",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.MonthlyLineChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Monthly Totals",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.YearlyLineChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Yearly Totals",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        }
                    }
                },

                [AnalyticTypes.BarChart] = new AnalyticDefinition
                {
                    Purposes = [AnalyticPurposes.Name, AnalyticPurposes.Value],
                    Codes = new()
                    {
                        [AnalyticCodes.CountBarChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Count per Category",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [.. DataTypes.All]
                            }
                        },
                        [AnalyticCodes.SumBarChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Sum per Category",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [.. DataTypes.All],
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.AverageBarChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Average per Category",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [.. DataTypes.All],
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.DailyBarChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Daily Totals",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.WeeklyBarChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Weekly Totals",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.MonthlyBarChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Monthly Totals",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.YearlyBarChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Yearly Totals",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        }
                    }
                },

                [AnalyticTypes.ScatterChart] = new AnalyticDefinition
                {
                    Purposes = [AnalyticPurposes.Xaxis, AnalyticPurposes.Yaxis, AnalyticPurposes.Match, AnalyticPurposes.Value],
                    Codes = new()
                    {
                        [AnalyticCodes.ScatterChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Scatter Plot",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [DataTypes.Number, DataTypes.TimeSpan],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        // Two sources, one per axis: each tracker maps a Match field (the
                        // join key, e.g. the day) and a Value field, and a point pairs the
                        // first source's value (x) with the second's (y) for every match
                        // key they share. See MultiSourceAnalyticMerger.MergeCorrelation.
                        [AnalyticCodes.CorrelationScatter] = new AnalyticPurposeDataTypes
                        {
                            Label = "Correlation",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Match] = [DataTypes.Date, DataTypes.DateTime, DataTypes.String, DataTypes.Number],
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        }
                    }
                },

                [AnalyticTypes.Calendar] = new AnalyticDefinition
                {
                    Purposes = [AnalyticPurposes.When, AnalyticPurposes.What],
                    Codes = new()
                    {
                        [AnalyticCodes.Calendar] = new AnalyticPurposeDataTypes
                        {
                            Label = "Calendar",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.When] = [DataTypes.Date, DataTypes.DateTime],
                                [AnalyticPurposes.What] = [.. DataTypes.All]
                            }
                        }
                    }
                },

                [AnalyticTypes.Donut] = new AnalyticDefinition
                {
                    Purposes = [AnalyticPurposes.Name, AnalyticPurposes.Value],
                    Codes = new()
                    {
                        [AnalyticCodes.DonutChart] = new AnalyticPurposeDataTypes
                        {
                            Label = "Sum per Category",
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [.. DataTypes.All],
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
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

        // The purposes a given code needs mapped to a field. AllowedDataTypes is keyed by
        // purpose and only lists the ones that code actually uses, so its keys are exactly
        // the required set (e.g. "Count per Category" needs Name but not Value).
        public static IReadOnlyCollection<string> GetRequiredPurposes(string resultType, string code) =>
            ByResultType.TryGetValue(resultType, out var def) &&
            def.Codes.TryGetValue(code, out var codeDef)
                ? codeDef.AllowedDataTypes.Keys
                : [];

        // The human-readable name for an analytic, e.g. "Line Chart · Monthly Totals: Day,
        // Amount". Leads with the chart type because a calculation label alone doesn't
        // always identify the analytic: Bar and Donut both call their per-category sum "Sum
        // per Category", and "Single Value · Average" reads very differently from "Line
        // Chart · Average" once it's sitting in a list next to other widgets. Skipped when
        // it would just repeat the calculation (e.g. Calendar's only code is also called
        // "Calendar"). Shared by tracker analytic summaries and dashboard sources so a saved
        // and an ad hoc analytic with the same definition read identically.
        public static string GetDisplayName(string resultType, string code, IEnumerable<string> fieldNames)
        {
            var label = GetLabel(resultType, code);
            var names = fieldNames.Where(n => !string.IsNullOrEmpty(n)).ToList();
            var calculation = names.Count > 0 ? $"{label}: {string.Join(", ", names)}" : label;
            return label == resultType ? calculation : $"{resultType} · {calculation}";
        }

        public static string GetLabel(string resultType, string code) =>
            ByResultType.TryGetValue(resultType, out var def) &&
            def.Codes.TryGetValue(code, out var codeDef) &&
            !string.IsNullOrEmpty(codeDef.Label)
                ? codeDef.Label
                : code;
    }
}
