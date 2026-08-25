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

        // For the two places an analytic is rendered for a person to look at (a tracker's own
        // analytics page and a dashboard widget) rather than evaluated for a notification: a
        // failure here almost always means a field the analytic depends on is missing or
        // broken — e.g. deleting a field doesn't clean up a *different* calculated field's
        // formula that still refers to it by name, so that field quietly stops producing
        // values. GetAnalyticResult already turns a bad request into an explanatory
        // single-value card; this also catches a genuine calculation failure (no data,
        // unsupported code) and an unexpected exception the same way, so a broken analytic
        // still shows up — and can be edited or deleted — instead of silently vanishing from
        // the page. Notification evaluation deliberately keeps using GetAnalyticResult
        // directly: there, IsSuccess == false correctly means "don't fire".
        public static AnalyticDto GetDisplayableAnalyticResult(AnalyticResultBuilderRequest request)
        {
            try
            {
                var result = GetAnalyticResult(request);
                if (result.IsSuccess)
                    return result.Data;

                return Fallback(request, result.Messages.FirstOrDefault());
            }
            catch (Exception ex)
            {
                return Fallback(request, $"Could not calculate this analytic: {ex.Message}");
            }
        }

        private static AnalyticDto Fallback(AnalyticResultBuilderRequest request, string? message) =>
            new SingleValueAnalyticDto
            {
                Id = request.Analytic.Id,
                Name = string.IsNullOrWhiteSpace(request.Analytic.Name) ? "" : request.Analytic.Name,
                Description = request.Analytic.Description,
                Value = message ?? "Error"
            };
    }
}
