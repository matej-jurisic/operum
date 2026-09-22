using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Operum.Model;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.CorrelationInsights
{
    public class CorrelationInsightEvaluator(OperumContext db, IConfiguration configuration) : ICorrelationInsightEvaluator
    {
        private int MinSampleSize => configuration.GetValue("CorrelationInsights:MinSampleSize", 14);
        private double MinCoefficient => configuration.GetValue("CorrelationInsights:MinCoefficient", 0.5);

        // A tracker needs one of these to be a join key; both are treated as calendar days.
        private static readonly HashSet<string> MatchableTypes = [DataTypes.Date, DataTypes.DateTime];

        // Matches CorrelationScatter's allowed Value purpose types (AnalyticDefinitionList).
        private static readonly HashSet<string> ValueTypes = [DataTypes.Number, DataTypes.TimeSpan];

        public async Task RunForUserAsync(string userId, CancellationToken ct = default)
        {
            var trackers = await db.Trackers
                .Include(t => t.Fields)
                .Include(t => t.ApplicationUserTrackers)
                .Where(t => t.OwnerId == userId || t.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == userId))
                .ToListAsync(ct);

            // Sorted so tracker A/B assignment is stable across runs regardless of query
            // order -- otherwise a rerun could swap sides and look like a different pair to
            // the (ValueFieldAId, ValueFieldBId) upsert key in ReplaceInsights.
            var candidates = trackers
                .Select(t => (Tracker: t, MatchField: t.Fields
                    .Where(f => MatchableTypes.Contains(f.Type))
                    .OrderBy(f => f.Order)
                    .FirstOrDefault()))
                .Where(c => c.MatchField != null)
                .OrderBy(c => c.Tracker.Id, StringComparer.Ordinal)
                .ToList();

            var found = new List<FieldCorrelationInsight>();
            var entriesByTracker = new Dictionary<string, List<Entry>>();

            for (var i = 0; i < candidates.Count; i++)
            {
                var (trackerA, matchFieldA) = candidates[i];
                var valueFieldsA = trackerA.Fields.Where(f => ValueTypes.Contains(f.Type)).ToList();
                if (valueFieldsA.Count == 0)
                    continue;

                for (var j = i + 1; j < candidates.Count; j++)
                {
                    var (trackerB, matchFieldB) = candidates[j];
                    var valueFieldsB = trackerB.Fields.Where(f => ValueTypes.Contains(f.Type)).ToList();
                    if (valueFieldsB.Count == 0)
                        continue;

                    var entriesA = await EntriesFor(trackerA.Id, entriesByTracker, ct);
                    var entriesB = await EntriesFor(trackerB.Id, entriesByTracker, ct);
                    if (entriesA.Count == 0 || entriesB.Count == 0)
                        continue;

                    foreach (var valueFieldA in valueFieldsA)
                    {
                        var sourceA = BuildSource("a", trackerA, entriesA, matchFieldA!, valueFieldA);

                        foreach (var valueFieldB in valueFieldsB)
                        {
                            var sourceB = BuildSource("b", trackerB, entriesB, matchFieldB!, valueFieldB);

                            var scatter = MultiSourceAnalyticMerger.MergeCorrelation([sourceA, sourceB]);
                            if (scatter.Points.Count < MinSampleSize)
                                continue;

                            var r = Pearson(scatter.Points);
                            if (r is null || Math.Abs(r.Value) < MinCoefficient)
                                continue;

                            found.Add(new FieldCorrelationInsight
                            {
                                UserId = userId,
                                TrackerAId = trackerA.Id,
                                TrackerBId = trackerB.Id,
                                MatchFieldAId = matchFieldA!.Id,
                                MatchFieldBId = matchFieldB!.Id,
                                ValueFieldAId = valueFieldA.Id,
                                ValueFieldBId = valueFieldB.Id,
                                Coefficient = r.Value,
                                SampleSize = scatter.Points.Count,
                                ComputedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            await ReplaceInsights(userId, found, ct);
        }

        private async Task<List<Entry>> EntriesFor(
            string trackerId, Dictionary<string, List<Entry>> cache, CancellationToken ct)
        {
            if (cache.TryGetValue(trackerId, out var cached))
                return cached;

            var entries = await db.Entries
                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == trackerId)
                .ToListAsync(ct);

            cache[trackerId] = entries;
            return entries;
        }

        private static MergeSource BuildSource(
            string key, Tracker tracker, List<Entry> entries, Field matchField, Field valueField)
        {
            var fieldMap = MultiSourceAnalyticMerger.PairedAxisFieldMap(new Dictionary<string, Field>
            {
                [AnalyticPurposes.Match] = matchField,
                [AnalyticPurposes.Value] = valueField
            });

            var request = new AnalyticResultBuilderRequest
            {
                Analytic = new Analytic
                {
                    Id = $"correlation-{key}",
                    Code = AnalyticCodes.RawValues,
                    Grouping = AnalyticGroupings.None,
                    ResultType = AnalyticTypes.LineChart
                },
                Entries = entries,
                FieldMap = fieldMap
            };

            var data = AnalyticResultBuilder.GetDisplayableAnalyticResult(request);
            return new MergeSource(key, null, tracker.Name, tracker.Color, data);
        }

        private static double? Pearson(IReadOnlyList<ScatterChartPointDto> points)
        {
            if (points.Count < 2)
                return null;

            var xMean = points.Average(p => p.X!.Value);
            var yMean = points.Average(p => p.Y!.Value);

            double cov = 0, xVar = 0, yVar = 0;
            foreach (var p in points)
            {
                var dx = p.X!.Value - xMean;
                var dy = p.Y!.Value - yMean;
                cov += dx * dy;
                xVar += dx * dx;
                yVar += dy * dy;
            }

            if (xVar == 0 || yVar == 0)
                return null;

            return cov / Math.Sqrt(xVar * yVar);
        }

        // Upserts by (ValueFieldAId, ValueFieldBId): an existing row keeps its Dismissed
        // state on a recompute, so a user's "not interested" survives a rerun until the
        // pair itself stops qualifying and is dropped.
        private async Task ReplaceInsights(string userId, List<FieldCorrelationInsight> found, CancellationToken ct)
        {
            var existing = await db.FieldCorrelationInsights
                .Where(i => i.UserId == userId)
                .ToListAsync(ct);

            var existingByKey = existing.ToDictionary(i => (i.ValueFieldAId, i.ValueFieldBId));
            var foundByKey = found.ToDictionary(i => (i.ValueFieldAId, i.ValueFieldBId));

            foreach (var stale in existing.Where(i => !foundByKey.ContainsKey((i.ValueFieldAId, i.ValueFieldBId))))
                db.FieldCorrelationInsights.Remove(stale);

            foreach (var candidate in found)
            {
                if (existingByKey.TryGetValue((candidate.ValueFieldAId, candidate.ValueFieldBId), out var current))
                {
                    current.Coefficient = candidate.Coefficient;
                    current.SampleSize = candidate.SampleSize;
                    current.ComputedAt = candidate.ComputedAt;
                    current.MatchFieldAId = candidate.MatchFieldAId;
                    current.MatchFieldBId = candidate.MatchFieldBId;
                }
                else
                {
                    db.FieldCorrelationInsights.Add(candidate);
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
