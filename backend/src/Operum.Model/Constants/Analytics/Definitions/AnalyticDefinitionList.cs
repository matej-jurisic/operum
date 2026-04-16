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
                        }
                    }
                },

                [AnalyticTypes.ScatterChart] = new AnalyticDefinition
                {
                    Purposes = [AnalyticPurposes.Xaxis, AnalyticPurposes.Yaxis],
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

        public static string GetLabel(string resultType, string code) =>
            ByResultType.TryGetValue(resultType, out var def) &&
            def.Codes.TryGetValue(code, out var codeDef) &&
            !string.IsNullOrEmpty(codeDef.Label)
                ? codeDef.Label
                : code;
    }
}
