using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Views;
using System.Text.Json;

namespace Operum.Service.Domain.Notifications
{
    public static class ConditionAnalyticEvaluator
    {
        public static async Task<bool> EvaluateAsync(
            OperumContext db,
            TrackerNotification notification,
            CancellationToken ct = default)
        {
            var condition = notification.Condition;
            if (string.IsNullOrEmpty(condition.AnalyticCode)) return false;

            var viewIds = string.IsNullOrEmpty(notification.ViewIds)
                ? (List<string>)[]
                : JsonSerializer.Deserialize<List<string>>(notification.ViewIds) ?? [];

            var views = viewIds.Count > 0
                ? await db.Views
                    .Include(v => v.Filters).ThenInclude(f => f.Field)
                    .Where(v => viewIds.Contains(v.Id))
                    .ToListAsync(ct)
                : (List<View>)[];

            var entriesQuery = db.Entries
                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == notification.TrackerId);

            if (views.Count > 0)
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.MergeViewFilters(views));

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
                return false;

            // All condition filters must match (AND semantics)
            return condition.Filters.All(f =>
                NotificationConditionEvaluator.Evaluate(svDto.Value, f.Operator, f.Value ?? string.Empty));
        }
    }
}
