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
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [.. DataTypes.All]
                            }
                        },
                        [AnalyticCodes.Min] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan, DataTypes.Date, DataTypes.DateTime]
                            }
                        },
                        [AnalyticCodes.Max] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan, DataTypes.Date, DataTypes.DateTime]
                            }
                        },
                        [AnalyticCodes.Average] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.Sum] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.StdDev] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.TrueCount] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Bool]
                            }
                        },
                        [AnalyticCodes.FalseCount] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Bool]
                            }
                        },
                        [AnalyticCodes.TruePercentage] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Value] = [DataTypes.Bool]
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
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.CumulativeLineChart] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
                            }
                        },
                        [AnalyticCodes.LineChart] = new AnalyticPurposeDataTypes
                        {
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Xaxis] = [.. DataTypes.All],
                                [AnalyticPurposes.Yaxis] = [DataTypes.Number, DataTypes.TimeSpan]
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
                            AllowedDataTypes = new()
                            {
                                [AnalyticPurposes.Name] = [DataTypes.String],
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
    }
}
