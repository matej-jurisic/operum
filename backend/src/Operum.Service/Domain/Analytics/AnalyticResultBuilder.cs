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

            var result = builder.Build(request);

            // A builder always names its result from the definition (e.g. "Sum"), since it
            // has no idea whether the analytic was ever given a name of its own. Applying
            // the override here, once, keeps every builder ignorant of naming and covers
            // both places an Analytic is calculated: the tracker's own analytics page and a
            // dashboard widget copied from one.
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(request.Analytic.Name))
                result.Data.Name = request.Analytic.Name;

            return result;
        }
    }
}
