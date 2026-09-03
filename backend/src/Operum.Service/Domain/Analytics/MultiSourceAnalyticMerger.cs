using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Fields;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics
{
    // One source after its own single-tracker calculation has run, ready to be merged with
    // its siblings into a combined chart. Decoupled from any persistence shape: a dashboard
    // placement builds one of these per DashboardItemSource, the Explore page one per
    // request source.
    public sealed record MergeSource(
        string Key,
        string? Label,
        string TrackerName,
        string? TrackerColor,
        AnalyticDto Result);

    // The merge paths for combining 2+ tracker sources into a single widget result. Each
    // source is calculated independently by the ordinary single-tracker pipeline; these
    // methods only stitch the per-source results together. Shared by DashboardService
    // (saved widgets on a board) and AnalyticsService (the Explore page).
    public static class MultiSourceAnalyticMerger
    {
        // A correlation source's Match/Value fields, presented to the line-chart pipeline as
        // its X/Y axes: the raw-values line chart it then produces is the (match key, value)
        // list MergeCorrelation joins on. A mapping that lost its field (deleted) is
        // dropped, leaving the line result without that axis and the merge with nothing to
        // pair -- handled the same way as any other missing analytic field.
        public static Dictionary<string, Field> PairedAxisFieldMap(IReadOnlyDictionary<string, Field> byPurpose)
        {
            var map = new Dictionary<string, Field>();
            if (byPurpose.TryGetValue(AnalyticPurposes.Match, out var matchField))
                map[AnalyticPurposes.Xaxis] = matchField;
            if (byPurpose.TryGetValue(AnalyticPurposes.Value, out var valueField))
                map[AnalyticPurposes.Yaxis] = valueField;
            return map;
        }

        // Merges 2+ per-source results (each computed independently by the same
        // single-tracker pipeline as always) into one multi-series chart. Every source shares
        // the widget's result type and code, so the series are always produced the same way;
        // what they can still differ in is the kind of value on the x-axis, which is surfaced
        // as a warning rather than rejected.
        public static ComposedChartAnalyticDto BuildComposed(IReadOnlyList<MergeSource> sources, bool matchedValuesOnly)
        {
            var composed = new ComposedChartAnalyticDto();

            foreach (var resolved in sources)
            {
                ComposedChartSeriesDto? series = resolved.Result switch
                {
                    LineChartAnalyticDto line => new ComposedChartSeriesDto
                    {
                        Key = resolved.Key,
                        Label = resolved.Label ?? $"{resolved.TrackerName}: {line.YField.Name}",
                        RenderType = ComposedSeriesRenderTypes.Line,
                        XField = line.XField,
                        ValueField = line.YField,
                        Points = line.Points.Select(p => new ComposedChartPointDto { X = p.X, Y = p.Y }).ToList(),
                        Color = resolved.TrackerColor
                    },
                    BarChartAnalyticDto bar => new ComposedChartSeriesDto
                    {
                        Key = resolved.Key,
                        Label = resolved.Label ?? $"{resolved.TrackerName}: {bar.ValueField?.Name ?? "Count"}",
                        RenderType = ComposedSeriesRenderTypes.Bar,
                        XField = bar.NameField,
                        ValueField = bar.ValueField ?? new FieldDto { Name = "Count", Type = DataTypes.Number },
                        Points = bar.Points.Select(p => new ComposedChartPointDto { X = p.Name, Y = p.Value }).ToList(),
                        Color = resolved.TrackerColor
                    },
                    // Defensive only -- the caller already rejects any other result type once
                    // there's more than one source.
                    _ => null
                };

                if (series != null) composed.Series.Add(series);
            }

            // No name of its own: the chart is titled from its series, in the same order
            // they're plotted, so renaming a source's series also renames the widget.
            composed.Name = string.Join(" - ", composed.Series.Select(s => s.Label));

            var hasMismatchedXTypes = composed.Series.Select(s => s.XField.Type).Distinct().Count() > 1;
            if (hasMismatchedXTypes)
                composed.Warnings.Add("Sources plot different kinds of value on the x-axis, alignment may be misleading.");

            if (matchedValuesOnly && composed.Series.Count > 1)
                KeepOnlyMatchedXValues(composed);

            return composed;
        }

        // Narrows every series to the x-axis values all of them have a point for, so the
        // chart compares the sources over the same range instead of letting each one run on
        // wherever the others have no data. Series whose x-axis buckets never line up (a
        // different field type, or simply no overlapping period) end up empty, which is worth
        // saying out loud rather than rendering as a blank chart.
        private static void KeepOnlyMatchedXValues(ComposedChartAnalyticDto composed)
        {
            var shared = composed.Series
                .Select(s => s.Points.Select(p => p.X ?? string.Empty).ToHashSet())
                .Aggregate((a, b) => { a.IntersectWith(b); return a; });

            foreach (var series in composed.Series)
                series.Points = series.Points.Where(p => shared.Contains(p.X ?? string.Empty)).ToList();

            if (shared.Count == 0)
                composed.Warnings.Add("No x-axis value appears in every source, so nothing is left to show with matched values only.");
        }

        // A calendar has no shared axis to reconcile: merging trackers is just a union of
        // their dated events. Each point keeps the colour of the tracker it came from and a
        // source name (the placement's label override, else the tracker's own name) so the
        // card can tell the sources apart. The when/what fields are taken from the first
        // source purely to format event dates in the card (every calendar "When" field is a
        // date or datetime).
        public static CalendarAnalyticDto MergeCalendars(IReadOnlyList<MergeSource> sources)
        {
            var calendars = sources
                .Where(r => r.Result is CalendarAnalyticDto)
                .Select(r => (Resolved: r, Calendar: (CalendarAnalyticDto)r.Result))
                .ToList();

            var merged = new CalendarAnalyticDto();

            var first = calendars.FirstOrDefault(c => c.Calendar.WhenField != null && c.Calendar.WhatField != null);
            if (first.Calendar != null)
            {
                merged.WhenField = first.Calendar.WhenField;
                merged.WhatField = first.Calendar.WhatField;
            }

            merged.Points = calendars
                .SelectMany(c => c.Calendar.Points.Select(p => new CalendarPointDto
                {
                    EntryId = p.EntryId,
                    Date = p.Date,
                    Name = p.Name,
                    TrackerName = string.IsNullOrWhiteSpace(c.Resolved.Label)
                        ? c.Resolved.TrackerName
                        : c.Resolved.Label,
                    Color = c.Resolved.TrackerColor
                }))
                .ToList();

            return merged;
        }

        // Joins two sources into one scatter plot: source A's value is the x of each point,
        // source B's the y, paired on every match key both sources have. Each side arrives
        // as a raw-values line chart (see PairedAxisFieldMap) -- X is the match key, Y the
        // value -- so the join is just an intersection of their keys. Repeat entries for a
        // key are averaged into the one value that key contributes.
        public static ScatterPlotAnalyticDto MergeCorrelation(IReadOnlyList<MergeSource> sources)
        {
            var result = new ScatterPlotAnalyticDto();
            if (sources.Count < 2)
                return result;

            var xSource = sources[0];
            var ySource = sources[1];

            // A non-line result, or a line result missing an axis, means a field the
            // calculation needs was deleted: nothing can be paired, and the card shows its
            // missing-fields state (XField/YField left null).
            if (xSource.Result is not LineChartAnalyticDto xLine || ySource.Result is not LineChartAnalyticDto yLine)
                return result;

            if (xLine.YField is null || yLine.YField is null)
                return result;

            result.XField = AxisField(xLine.YField, xSource);
            result.YField = AxisField(yLine.YField, ySource);
            result.Name = $"{result.XField.Name} vs {result.YField.Name}";

            var xByKey = AverageByMatchKey(xLine.Points);
            var yByKey = AverageByMatchKey(yLine.Points);

            result.Points = xByKey.Keys
                .Where(yByKey.ContainsKey)
                .OrderBy(k => k, StringComparer.Ordinal)
                .Select(k => new ScatterChartPointDto { X = xByKey[k], Y = yByKey[k] })
                .ToList();

            if (xLine.YField.Type != yLine.YField.Type)
                result.Warnings.Add("The two trackers measure different kinds of value, so the axes aren't directly comparable.");

            if (result.Points.Count == 0)
                result.Warnings.Add("The two trackers share no match value, so there's nothing to pair up.");

            return result;
        }

        private static Dictionary<string, double> AverageByMatchKey(List<LineChartPointDto> points) =>
            points
                .Where(p => p.X != null && p.Y.HasValue)
                .GroupBy(p => p.X!)
                .ToDictionary(g => g.Key, g => g.Average(p => p.Y!.Value));

        // The scatter axis for a correlation source: the value field's own type (so ticks
        // and the tooltip format it right), named for the tracker it came from unless the
        // source was given a label of its own.
        private static FieldDto AxisField(FieldDto valueField, MergeSource source) => new()
        {
            Id = valueField.Id,
            Type = valueField.Type,
            Required = valueField.Required,
            Description = valueField.Description,
            Name = string.IsNullOrWhiteSpace(source.Label)
                ? $"{source.TrackerName}: {valueField.Name}"
                : source.Label
        };
    }
}
