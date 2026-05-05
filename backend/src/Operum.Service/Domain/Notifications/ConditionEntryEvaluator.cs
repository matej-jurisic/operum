using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Models;
using Operum.Service.Domain.Views;
using System.Text.Json;

namespace Operum.Service.Domain.Notifications
{
    public static class ConditionEntryEvaluator
    {
        public static async Task<List<string>> GetMatchingEntryIdsAsync(
            OperumContext db,
            TrackerNotification notification,
            CancellationToken ct = default)
        {
            var condition = notification.Condition;

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

            // Project condition filters to ViewFilter instances for reuse
            var conditionFilters = condition.Filters
                .Where(f => f.FieldId != null && f.Field != null)
                .Select(f => new ViewFilter
                {
                    FieldId = f.FieldId!,
                    Operator = f.Operator,
                    Value = f.Value,
                    Field = f.Field!
                })
                .ToList();

            if (conditionFilters.Count > 0)
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, conditionFilters);

            return await entriesQuery.Select(e => e.Id).ToListAsync(ct);
        }
    }
}
