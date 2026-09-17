using Operum.Model.Constants.Analytics;
using Operum.Service.Domain.Analytics.Calculators;

namespace Operum.Service.Domain.Analytics
{
    // Shared by SingleValueAnalyticBuilder and TrendCalculator, so a trend's sparkline and
    // previous-period comparison run the same calculation as the widget itself.
    public static class SingleValueCalculators
    {
        private static readonly Dictionary<string, ISingleValueCalculator> ByCode = new()
        {
            [AnalyticCodes.Count] = new CountCalculator(),
            [AnalyticCodes.TrueCount] = new TrueCountCalculator(),
            [AnalyticCodes.FalseCount] = new FalseCountCalculator(),
            [AnalyticCodes.TruePercentage] = new TruePercentageCalculator(),
            [AnalyticCodes.Min] = new MinCalculator(),
            [AnalyticCodes.Max] = new MaxCalculator(),
            [AnalyticCodes.Average] = new AverageCalculator(),
            [AnalyticCodes.Sum] = new SumCalculator(),
            [AnalyticCodes.StdDev] = new StdDevCalculator(),
            [AnalyticCodes.CountDistinct] = new CountDistinctCalculator(),
            [AnalyticCodes.MostCommon] = new MostCommonCalculator(),
            [AnalyticCodes.LeastCommon] = new LeastCommonCalculator()
        };

        public static ISingleValueCalculator? Get(string code) => ByCode.GetValueOrDefault(code);
    }
}
