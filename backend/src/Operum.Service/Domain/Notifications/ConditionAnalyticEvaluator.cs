using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Views;

namespace Operum.Service.Domain.Notifications
{
    /// <summary>Whether the analytic's single value currently satisfies the condition, and that value itself (for the <c>{value}</c> push-body token).</summary>
    public record AnalyticEvaluationResult(bool ConditionMet, string? Value);

    public static class ConditionAnalyticEvaluator
    {
        public static async Task<AnalyticEvaluationResult> EvaluateAsync(
            OperumContext db,
            TrackerNotification notification,
            TimeZoneInfo tz,
            CancellationToken ct = default)
        {
            var condition = notification.Condition;
            if (string.IsNullOrEmpty(condition.AnalyticCode)) return new AnalyticEvaluationResult(false, null);

            var view = !string.IsNullOrEmpty(notification.ViewId)
                ? await db.Views
                    .Include(v => v.ViewQueries.OrderBy(vq => vq.Order)).ThenInclude(vq => vq.Query)
                    .Include(v => v.ViewQueries).ThenInclude(vq => vq.Field)
                    .FirstOrDefaultAsync(v => v.Id == notification.ViewId, ct)
                : null;

            var entriesQuery = db.Entries
                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == notification.TrackerId);

            if (view != null)
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.ResolveFilters(view), tz);

            var entries = await entriesQuery.ToListAsync(ct);

            var fieldMap = condition.PurposeFields.ToDictionary(pf => pf.Purpose, pf => pf.Field);
            var analytic = new Analytic
            {
                Code = condition.AnalyticCode,
                ResultType = condition.AnalyticResultType ?? "Single Value"
            };

            var result = AnalyticResultBuilder.GetAnalyticResult(new AnalyticResultBuilderRequest
            {
                Analytic = analytic,
                Entries = entries,
                FieldMap = fieldMap
            });

            if (!result.IsSuccess || result.Data is not SingleValueAnalyticDto svDto)
                return new AnalyticEvaluationResult(false, null);

            // All condition filters must match (AND semantics)
            var conditionMet = condition.Filters.All(f =>
                NotificationConditionEvaluator.Evaluate(svDto.Value, f.Operator, f.Value ?? string.Empty, tz));

            return new AnalyticEvaluationResult(conditionMet, svDto.Value);
        }
    }
}
