using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Fields;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics
{
    // One source after its own single-tracker calculation has run, ready to merge with its siblings.
    public sealed record MergeSource(
        string Key,
        string? Label,
        string TrackerName,
        string? TrackerColor,
        AnalyticDto Result);

    // Stitches independently-calculated per-source results into a single widget result.
    // Shared by DashboardService and AnalyticsService.
    public static class MultiSourceAnalyticMerger
    {
        // Presents a correlation source's Match/Value fields as X/Y so the line-chart pipeline
        // can produce the (match key, value) list MergeCorrelation joins on.
        public static Dictionary<string, Field> PairedAxisFieldMap(IReadOnlyDictionary<string, Field> byPurpose)
        {
            var map = new Dictionary<string, Field>();
            if (byPurpose.TryGetValue(AnalyticPurposes.Match, out var matchField))
                map[AnalyticPurposes.Xaxis] = matchField;
            if (byPurpose.TryGetValue(AnalyticPurposes.Value, out var valueField))
                map[AnalyticPurposes.Yaxis] = valueField;
            return map;
        }

        // Sources can differ in the kind of value on the x-axis; that's surfaced as a warning rather than rejected.
        public static ComposedChartAnalyticDto BuildComposed(IReadOnlyList<MergeSource> sources, bool matchedValuesOnly)
        {
            var composed = new ComposedChartAnalyticDto();

            foreach (var resolved in sources)
            {
                ComposedChartSeriesDto? series = resolved.Result switch
                {
                    // YField is null for a Count series; label and axis fall back to "Count".
                    LineChartAnalyticDto line => new ComposedChartSeriesDto
                    {
                        Key = resolved.Key,
                        Label = resolved.Label ?? $"{resolved.TrackerName}: {line.YField?.Name ?? "Count"}",
                        RenderType = ComposedSeriesRenderTypes.Line,
                        XField = line.XField,
                        ValueField = line.YField ?? new FieldDto { Name = "Count", Type = DataTypes.Number },
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
                    // Defensive only: the caller already rejects other result types once there's more than one source.
                    _ => null
                };

                if (series != null) composed.Series.Add(series);
            }

            composed.Name = string.Join(" - ", composed.Series.Select(s => s.Label));

            var hasMismatchedXTypes = composed.Series.Select(s => s.XField.Type).Distinct().Count() > 1;
            if (hasMismatchedXTypes)
                composed.Warnings.Add("Sources plot different kinds of value on the x-axis, alignment may be misleading.");

            if (matchedValuesOnly && composed.Series.Count > 1)
                KeepOnlyMatchedXValues(composed);

            return composed;
        }

        // Narrows every series to the x-axis values all of them share.
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

        // Merging calendars is just a union of dated events; when/what fields are taken from
        // the first source to format dates (every calendar "When" field is a date/datetime).
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

        // Joins two sources into one scatter plot on their shared match key (see
        // PairedAxisFieldMap); repeat entries for a key are averaged into one value.
        public static ScatterPlotAnalyticDto MergeCorrelation(IReadOnlyList<MergeSource> sources)
        {
            var result = new ScatterPlotAnalyticDto();
            if (sources.Count < 2)
                return result;

            var xSource = sources[0];
            var ySource = sources[1];

            // A missing axis means a field the calculation needs was deleted; leave XField/YField null.
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
