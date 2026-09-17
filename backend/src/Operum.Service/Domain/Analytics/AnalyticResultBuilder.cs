using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
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
                new GoalAnalyticBuilder(),
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

            // Name override applied here once so builders don't need to know about it.
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(request.Analytic.Name))
                result.Data.Name = request.Analytic.Name;

            return result;
        }

        // Notification evaluation must keep calling GetAnalyticResult directly instead of this
        // method: there, IsSuccess == false correctly means "don't fire".
        public static AnalyticDto GetDisplayableAnalyticResult(AnalyticResultBuilderRequest request)
        {
            try
            {
                var result = GetAnalyticResult(request);
                if (result.IsSuccess)
                    return result.Data;

                // NotFound is an expected empty state; show compact "N/A" instead of the full message.
                if (result.StatusCode == ResultStatusCodes.NotFound)
                    return Fallback(request, "N/A");

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
