using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries;
using Operum.Model.Models;

namespace Operum.Service.Domain.Queries
{
    // Queries are pooled per owner: two clauses with identical kind/type/operator/value/direction share one row.
    public static class QueryPool
    {
        public static async Task<Query> GetOrCreate(OperumContext db, string ownerId, ClauseDto clause)
        {
            var kind = clause.Kind;
            var op = kind == QueryKinds.Filter ? clause.Operator : null;
            var value = kind == QueryKinds.Filter ? clause.Value : null;
            var descending = kind == QueryKinds.Sort && clause.Descending;

            bool Matches(Query q) =>
                q.OwnerId == ownerId && q.Kind == kind && q.DataType == clause.DataType &&
                q.Operator == op && q.Value == value && q.Descending == descending;

            // Must also check not-yet-saved additions, in case a single save attaches the same clause twice.
            var local = db.Queries.Local.FirstOrDefault(Matches);
            if (local != null)
                return local;

            var existing = await db.Queries.FirstOrDefaultAsync(q =>
                q.OwnerId == ownerId && q.Kind == kind && q.DataType == clause.DataType &&
                q.Operator == op && q.Value == value && q.Descending == descending);
            if (existing != null)
                return existing;

            var created = new Query
            {
                OwnerId = ownerId,
                Kind = kind,
                DataType = clause.DataType,
                Operator = op,
                Value = value,
                Descending = descending,
            };
            db.Queries.Add(created);
            return created;
        }
    }
}
