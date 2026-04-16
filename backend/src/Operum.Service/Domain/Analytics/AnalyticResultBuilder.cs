using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Service.Domain.Analytics.Builders;

namespace Operum.Service.Domain.Analytics
{
    public static class AnalyticResultBuilder
    {
        private static readonly Dictionary<string, IAnalyticResultBuilder> _builders;

        static AnalyticResultBuilder()
        {
            var builders = new IAnalyticResultBuilder[]
            {
                new SingleValueAnalyticBuilder(),
                new LineChartAnalyticBuilder(),
                new ScatterChartAnalyticBuilder(),
                new CalendarAnalyticBuilder(),
                new DonutChartAnalyticBuilder(),
                new BarChartAnalyticBuilder()
            };

            _builders = builders.ToDictionary(b => b.SupportedType);
        }

        public static Result<AnalyticDto> GetAnalyticResult(
            AnalyticResultBuilderRequest request)
        {
            if (!_builders.TryGetValue(request.Analytic.ResultType, out var builder))
                return Result.Success((AnalyticDto)new SingleValueAnalyticDto()
                {
                    Value = "This analytic is not supported.",
                    Name = request.Analytic.Code + " " + request.Analytic.ResultType,
                });

            return builder.Build(request);
        }
    }
}
