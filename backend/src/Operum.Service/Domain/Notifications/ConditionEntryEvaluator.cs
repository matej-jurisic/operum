using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.Models;
using Operum.Service.Domain.Views;

namespace Operum.Service.Domain.Notifications
{
    public static class ConditionEntryEvaluator
    {
        public static async Task<List<string>> GetMatchingEntryIdsAsync(
            OperumContext db,
            TrackerNotification notification,
            TimeZoneInfo tz,
            CancellationToken ct = default)
        {
            var condition = notification.Condition;

            var view = !string.IsNullOrEmpty(notification.ViewId)
                ? await db.Views
                    .Include(v => v.ViewQueries.OrderBy(vq => vq.Order)).ThenInclude(vq => vq.Query).ThenInclude(q => q.Field)
                    .FirstOrDefaultAsync(v => v.Id == notification.ViewId, ct)
                : null;

            var entriesQuery = db.Entries
                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == notification.TrackerId);

            if (view != null)
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.ResolveFilters(view), tz);

            // Project condition filters to filter Queries so the view filter builder can be reused
            var conditionFilters = condition.Filters
                .Where(f => f.FieldId != null && f.Field != null)
                .Select(f => new Query
                {
                    Kind = QueryKinds.Filter,
                    FieldId = f.FieldId!,
                    Operator = f.Operator,
                    Value = f.Value,
                    Field = f.Field!
                })
                .ToList();

            if (conditionFilters.Count > 0)
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, conditionFilters, tz);

            return await entriesQuery.Select(e => e.Id).ToListAsync(ct);
        }
    }
}
