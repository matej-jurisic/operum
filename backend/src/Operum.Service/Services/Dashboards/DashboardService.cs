using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Model.DTOs.Entries;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Queries;
using Operum.Model.DTOs.Widgets;
using Operum.Model.DTOs.Widgets.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Queries;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Dashboards
{
    public partial class DashboardService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper, IWidgetsService widgetsService) : IDashboardService
    {
        private sealed record ResolvedSource(
            DashboardItemSource Source,
            string TrackerName,
            string? TrackerColor,
            AnalyticDto Result);

        private static MergeSource ToMergeSource(ResolvedSource r) =>
            new(r.Source.Id, r.Source.Label, r.TrackerName, r.TrackerColor, r.Result);

        // Two widgets reading the same tracker through the same view and the same follow filters load it once between them. Lives for one BuildWidgets call only.
        private sealed class EntrySetCache(OperumContext db, TimeZoneInfo tz)
        {
            private readonly Dictionary<string, List<Entry>> _sets = [];

            // The returned list is shared, not copied: callers only ever enumerate it.
            public async Task<List<Entry>> Get(
                string trackerId,
                View? view,
                List<ResolvedClause> filters,
                List<ResolvedClause> sorts,
                int? limit = null)
            {
                var key = KeyFor(trackerId, view?.Id, filters, sorts, limit);
                if (_sets.TryGetValue(key, out var cached))
                    return cached;

                var query = db.Entries
                    .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                    .Where(e => e.TrackerId == trackerId);

                if (view != null)
                {
                    query = ViewQueryBuilder.ApplyViewFilters(query, ViewQueryBuilder.ResolveFilters(view), tz);
                    query = ViewQueryBuilder.ApplyViewSorting(query, ViewQueryBuilder.ResolveSorts(view));
                }

                // Every filter widget a source follows narrows it further, ANDed on top of the fixed view above.
                if (filters.Count > 0)
                    query = ViewQueryBuilder.ApplyViewFilters(query, filters, tz);
                if (sorts.Count > 0)
                    query = ViewQueryBuilder.ApplyViewSorting(query, sorts);

                if (limit != null)
                    query = query.Take(limit.Value);

                var entries = await query.ToListAsync();
                _sets[key] = entries;
                return entries;
            }

            // The view is keyed by id rather than by its resolved clauses, so two views that filter alike each load their own set: a missed hit, never a wrong one. Filter clauses are ANDed, so the signature sorts them; sorts are applied in order, so it does not.
            private static string KeyFor(
                string trackerId,
                string? viewId,
                List<ResolvedClause> filters,
                List<ResolvedClause> sorts,
                int? limit)
            {
                static string Part(ResolvedClause c) =>
                    $"{c.FieldId}~{c.FieldType}~{c.Operator}~{c.Value}~{c.Descending}";

                return string.Join("\u0000", [
                    trackerId,
                    viewId ?? string.Empty,
                    string.Join(";", filters.Select(Part).Order()),
                    string.Join(";", sorts.Select(Part)),
                    limit?.ToString() ?? string.Empty
                ]);
            }
        }

        // Must match the controller's own camelCase JSON convention since Config is written by hand.
        private static readonly JsonSerializerOptions ConfigJsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<Result<List<DashboardDto>>> GetDashboards()
        {
            var user = currentUserService.GetCurrentUser();
            var dashboards = await WithSourceGraph(db.Dashboards)
                .Where(d => d.UserId == user.Id)
                .OrderBy(d => d.Order)
                .ThenBy(d => d.Name)
                .ToListAsync();

            return Result.Success(dashboards.Select(MapToDto).ToList());
        }

        // All-or-nothing: the payload must name exactly the user's own dashboards.
        public async Task<Result> ReorderDashboards(ReorderDashboardsDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var dashboards = await db.Dashboards
                    .AsTracking()
                    .Where(d => d.UserId == user.Id)
                    .ToListAsync();

                if (!dto.DashboardIds.ToHashSet().SetEquals(dashboards.Select(d => d.Id).ToHashSet()))
                    return Result.Failure(ResultStatusCodes.BadRequest);

                var byId = dashboards.ToDictionary(d => d.Id);
                for (int i = 0; i < dto.DashboardIds.Count; i++)
                    byId[dto.DashboardIds[i]].Order = i;

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Result.Success();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Result.Failure(ResultStatusCodes.Error);
            }
        }

        public async Task<Result<DashboardDto>> GetDashboard(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            return Result.Success(MapToDto(dashboard));
        }

        public async Task<Result<List<DashboardWidgetDto>>> GetDashboardWidgets(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            return Result.Success(await BuildWidgets(dashboard));
        }

        // `onlyItemIds` narrows what is calculated, and so what comes back, to the items a write actually changed; null calculates the whole board. Either way the preamble below reads every filter widget on the board, since a widget's filters come from the ones it follows, wherever those sit.
        private async Task<List<DashboardWidgetDto>> BuildWidgets(Dashboard dashboard, IReadOnlySet<string>? onlyItemIds = null)
        {
            var boardItems = dashboard.Items.OrderBy(i => i.Order).ToList();
            var items = onlyItemIds == null
                ? boardItems
                : boardItems.Where(i => onlyItemIds.Contains(i.Id)).ToList();
            var results = new List<DashboardWidgetDto>();
            var tz = currentUserService.GetCurrentUserTimeZone();

            // Shared by every source calculated below, so a board of charts over one tracker
            // reads that tracker once rather than once per chart.
            var entryCache = new EntrySetCache(db, tz);

            // Resolved up front in one query so the client gets name/color/icon inline.
            var quickAddTrackerIds = items
                .Where(i => i.Type == DashboardWidgetTypes.QuickAdd)
                .Select(i => TryParseQuickAddConfig(i.Config)?.TrackerId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            var quickAddTrackers = quickAddTrackerIds.Count > 0
                ? await db.Trackers
                    .Where(t => quickAddTrackerIds.Contains(t.Id))
                    .ToDictionaryAsync(t => t.Id, t => new QuickAddTrackerDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Color = t.Color,
                        Icon = t.Icon
                    })
                : new Dictionary<string, QuickAddTrackerDto>();

            var entriesConfigsByItemId = items
                .Where(i => i.Type == DashboardWidgetTypes.Entries)
                .Select(i => (ItemId: i.Id, Config: TryParseEntriesConfig(i.Config)))
                .Where(x => x.Config != null)
                .ToDictionary(x => x.ItemId, x => x.Config!);

            // Every filter widget on the board, parsed once, plus every DashboardView and the pooled clause behind each of its queries.
            var filterConfigsByItemId = boardItems
                .Where(i => i.Type == DashboardWidgetTypes.Filter)
                .Select(i => (ItemId: i.Id, Config: TryParseFilterConfig(i.Config)))
                .Where(x => x.Config != null)
                .ToDictionary(x => x.ItemId, x => x.Config!);

            var filterQueryIds = filterConfigsByItemId.Values
                .SelectMany(c => c.Slots.Select(s => s.QueryId))
                .Distinct()
                .ToList();

            var filterQueriesById = filterQueryIds.Count > 0
                ? await db.Queries.Where(q => filterQueryIds.Contains(q.Id)).ToDictionaryAsync(q => q.Id)
                : new Dictionary<string, Query>();

            // Keyed by slot id, and also by pooled query id so a target saved before the slot
            // model still resolves. Lets a goal's conditional target compare against a date clause.
            var filterClauseDataTypes = new Dictionary<string, string>();
            foreach (var cfg in filterConfigsByItemId.Values)
                foreach (var slot in cfg.Slots)
                    if (filterQueriesById.TryGetValue(slot.QueryId, out var slotQuery))
                    {
                        filterClauseDataTypes[slot.SlotId] = slotQuery.DataType;
                        filterClauseDataTypes.TryAdd(slot.QueryId, slotQuery.DataType);
                    }

            // Only read to offer a filter widget its presets, so a build that renders no
            // filter widget skips them.
            var dashboardViewsById = items.Any(i => i.Type == DashboardWidgetTypes.Filter)
                ? (await db.DashboardViews
                        .Where(dv => dv.DashboardId == dashboard.Id)
                        .Include(dv => dv.DashboardViewQueries.OrderBy(q => q.Order)).ThenInclude(q => q.Query)
                        .OrderBy(dv => dv.Order)
                        .ToListAsync())
                    .ToDictionary(dv => dv.Id)
                : [];

            // Loaded up front: ApplyViewFilters needs the field's Type.
            var selectorFieldIds = filterConfigsByItemId.Values
                .SelectMany(c => c.Links)
                .SelectMany(l => l.FieldByQuery.Values)
                .Distinct()
                .ToList();

            var selectorFieldsById = selectorFieldIds.Count > 0
                ? await db.Fields.Where(f => selectorFieldIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id)
                : new Dictionary<string, Field>();

            // The fixed view every analytic source reads through, loaded in one query rather
            // than one per source inside the loop below.
            var sourceViewIds = items
                .SelectMany(i => i.Sources)
                .Select(s => s.ViewId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            var sourceViewsById = sourceViewIds.Count > 0
                ? await db.Views
                    .Include(v => v.ViewQueries.OrderBy(vq => vq.Order)).ThenInclude(vq => vq.Query)
                    .Include(v => v.ViewQueries).ThenInclude(vq => vq.Field)
                    .Where(v => sourceViewIds.Contains(v.Id))
                    .ToDictionaryAsync(v => v.Id)
                : [];

            // Every Entries table's tracker fields, the same way — the table picks its
            // columns out of them (see BuildEntriesWidget).
            var entriesTrackerIds = items
                .Where(i => i.Type == DashboardWidgetTypes.Entries && i.EntriesWidget != null)
                .Select(i => i.EntriesWidget!.TrackerId)
                .Distinct()
                .ToList();

            var entriesFieldsByTrackerId = entriesTrackerIds.Count > 0
                ? (await db.Fields
                        .Where(f => entriesTrackerIds.Contains(f.TrackerId))
                        .OrderBy(f => f.Order)
                        .ToListAsync())
                    .GroupBy(f => f.TrackerId)
                    .ToDictionary(g => g.Key, g => g.ToList())
                : [];

            foreach (var item in items)
            {
                if (item.Type != DashboardWidgetTypes.Analytic)
                {
                    QuickAddTrackerDto? quickAddTracker = null;
                    if (item.Type == DashboardWidgetTypes.QuickAdd)
                    {
                        var trackerId = TryParseQuickAddConfig(item.Config)?.TrackerId;
                        if (trackerId != null)
                            quickAddTrackers.TryGetValue(trackerId, out quickAddTracker);
                    }

                    FilterWidgetDto? filter = null;
                    if (item.Type == DashboardWidgetTypes.Filter &&
                        filterConfigsByItemId.TryGetValue(item.Id, out var filterConfig))
                    {
                        var slotClauses = filterConfig.Slots
                            .Where(s => filterQueriesById.ContainsKey(s.QueryId))
                            .Select(s => (Slot: s, Query: filterQueriesById[s.QueryId]))
                            .Where(x => x.Query.Kind == QueryKinds.Filter)
                            .ToList();

                        filter = new FilterWidgetDto
                        {
                            Clauses = slotClauses
                                .Select(x => new FilterClauseDto
                                {
                                    SlotId = x.Slot.SlotId,
                                    Kind = x.Query.Kind,
                                    DataType = x.Query.DataType,
                                    Operator = x.Query.Operator,
                                    Value = filterConfig.ValueBySlot.GetValueOrDefault(x.Slot.SlotId)
                                })
                                .ToList(),
                            // Only presets whose clause shape still matches the widget's are offered.
                            Presets = filterConfig.PresetIds
                                .Distinct()
                                .Where(dashboardViewsById.ContainsKey)
                                .Select(id => (Id: id, Values: PresetValuesForShape(dashboardViewsById[id], slotClauses.Select(x => x.Query).ToList())))
                                .Where(p => p.Values != null)
                                .Select(p => new FilterPresetOptionDto
                                {
                                    Id = p.Id,
                                    Name = dashboardViewsById[p.Id].Name,
                                    Values = p.Values!
                                })
                                .ToList()
                        };
                    }

                    EntriesWidgetDto? entriesWidget = null;
                    if (item.Type == DashboardWidgetTypes.Entries &&
                        item.EntriesWidget != null &&
                        entriesConfigsByItemId.TryGetValue(item.Id, out var entriesConfig))
                    {
                        entriesWidget = await BuildEntriesWidget(
                            item.Id, item.EntriesWidget, entriesConfig,
                            filterConfigsByItemId.Values,
                            filterQueriesById, selectorFieldsById,
                            entriesFieldsByTrackerId.GetValueOrDefault(item.EntriesWidget.TrackerId, []),
                            entryCache);
                    }

                    results.Add(MapToWidgetDto(item, null, quickAddTracker,
                        entriesWidget: entriesWidget, filter: filter));
                    continue;
                }

                // An orphaned or not-yet-migrated row has no shared definition to render.
                if (item.Widget == null) continue;

                // A goal's target is the Widget's default unless a conditional row matches
                // the board's currently-set filter values. A goal is always single-source.
                var goalTarget = item.Widget.ResultType != AnalyticTypes.Goal
                    ? item.Widget.GoalTarget
                    : ResolveGoalTarget(
                        item.Widget.GoalTarget,
                        ParseGoalConditionalTargets(item.GoalConditionalTargets),
                        ConnectedFilterValues(item.Id, filterConfigsByItemId.Values),
                        filterClauseDataTypes,
                        tz);

                var resolvedSources = new List<ResolvedSource>();

                foreach (var source in item.Sources.OrderBy(s => s.Order))
                {
                    var widgetSource = source.WidgetSource;
                    if (widgetSource == null) continue;

                    // A view that has been deleted out from under the source, or that no
                    // longer belongs to the tracker the source reads, is ignored.
                    View? view = null;
                    if (!string.IsNullOrEmpty(source.ViewId) &&
                        sourceViewsById.TryGetValue(source.ViewId, out var sourceView) &&
                        sourceView.TrackerId == widgetSource.TrackerId)
                        view = sourceView;

                    // A clause left blank is skipped rather than filtering on nothing.
                    var (followFilters, followSorts) = ResolveFilterClauses(
                        item.Id, widgetSource.TrackerId, filterConfigsByItemId.Values,
                        filterQueriesById, selectorFieldsById);

                    var entries = await entryCache.Get(
                        widgetSource.TrackerId, view, followFilters, followSorts);

                    // A correlation scatter has no per-source calculation: each source is a
                    // raw-values line chart's (match key -> value) pairs, joined by MergeCorrelation.
                    var isPaired = AnalyticTypes.RequiresPairedSources(item.Widget.ResultType, item.Widget.Code);
                    var fieldMap = BuildFieldMap(widgetSource);

                    var request = new AnalyticResultBuilderRequest
                    {
                        // A placement has no Analytic row of its own; the pipeline is fed a transient one.
                        Analytic = new Analytic
                        {
                            Id = source.Id,
                            Code = isPaired ? AnalyticCodes.RawValues : item.Widget.Code,
                            Grouping = isPaired ? AnalyticGroupings.None : item.Widget.Grouping,
                            ResultType = isPaired ? AnalyticTypes.LineChart : item.Widget.ResultType,
                            // Goal widgets only; ignored by every other builder.
                            GoalTarget = goalTarget,
                            GoalDirection = item.Widget.GoalDirection
                        },
                        Entries = entries,
                        FieldMap = isPaired
                            ? MultiSourceAnalyticMerger.PairedAxisFieldMap(fieldMap)
                            : fieldMap
                    };

                    var data = AnalyticResultBuilder.GetDisplayableAnalyticResult(request);

                    if (item.ShowTrend && !isPaired &&
                        (item.Widget.ResultType == AnalyticTypes.SingleValue || item.Widget.ResultType == AnalyticTypes.Goal) &&
                        fieldMap.TryGetValue(AnalyticPurposes.Value, out var valueField))
                    {
                        var dateRange = TrendCalculator.TryResolveDateRange(followFilters, tz);
                        if (dateRange != null)
                        {
                            var (dateFieldId, dateFieldType, start, end) = dateRange.Value;
                            var points = TrendCalculator.BuildSparkline(entries, dateFieldId, start, end, item.Widget.Code, valueField);

                            var previousFilters = TrendCalculator.WithPreviousRange(followFilters, dateFieldId, dateFieldType, start, end);
                            var previousQuery = db.Entries
                                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                                .Where(e => e.TrackerId == widgetSource.TrackerId);
                            if (view != null)
                                previousQuery = ViewQueryBuilder.ApplyViewFilters(previousQuery, ViewQueryBuilder.ResolveFilters(view), tz);
                            previousQuery = ViewQueryBuilder.ApplyViewFilters(previousQuery, previousFilters, tz);
                            var previousEntries = await previousQuery.ToListAsync();
                            var previousValue = TrendCalculator.CalculatePreviousValue(previousEntries, item.Widget.Code, valueField);

                            var trend = new TrendResultDto { Points = points, PreviousValue = previousValue };
                            if (data is SingleValueAnalyticDto singleValueData)
                                singleValueData.Trend = trend;
                            else if (data is GoalAnalyticDto goalData)
                                goalData.Trend = trend;
                        }
                    }

                    resolvedSources.Add(new ResolvedSource(source, widgetSource.Tracker.Name, widgetSource.Tracker.Color, data));
                }

                if (resolvedSources.Count == 0) continue;

                var mergeSources = resolvedSources.Select(ToMergeSource).ToList();
                var itemResult = resolvedSources.Count == 1
                    ? resolvedSources[0].Result
                    : AnalyticTypes.RequiresPairedSources(item.Widget.ResultType, item.Widget.Code)
                        ? MultiSourceAnalyticMerger.MergeCorrelation(mergeSources)
                        : item.Widget.ResultType == AnalyticTypes.Calendar
                            ? MultiSourceAnalyticMerger.MergeCalendars(mergeSources)
                            : MultiSourceAnalyticMerger.BuildComposed(mergeSources, item.Widget.MatchedValuesOnly);

                // Label precedence: single-source label override, then the widget's own name, then the calculation's default.
                var singleSourceLabel = resolvedSources.Count == 1 ? resolvedSources[0].Source.Label : null;
                if (!string.IsNullOrWhiteSpace(singleSourceLabel))
                    itemResult.Name = singleSourceLabel;
                else if (!string.IsNullOrWhiteSpace(item.Widget.Name))
                    itemResult.Name = item.Widget.Name;

                itemResult.Id = item.Id;
                itemResult.Order = item.Order;

                // Y-axis anchoring and a calendar's start month are placement choices, stamped on here rather than threaded through the analytic pipeline.
                if (itemResult is LineChartAnalyticDto lineResult)
                    lineResult.YAxisFromZero = item.YAxisFromZero;
                else if (itemResult is ComposedChartAnalyticDto composedResult)
                    composedResult.YAxisFromZero = item.YAxisFromZero;
                else if (itemResult is CalendarAnalyticDto calendarResult)
                    calendarResult.StartMonth = item.CalendarStartMonth;

                // A widget combining more than one tracker falls back to the dashboard's own color client-side.
                var distinctTrackerIds = resolvedSources.Select(r => r.Source.WidgetSource!.TrackerId).Distinct().ToList();
                var trackerColor = distinctTrackerIds.Count == 1 ? resolvedSources[0].TrackerColor : null;

                results.Add(MapToWidgetDto(item, itemResult, trackerColor: trackerColor, honorColorOverride: distinctTrackerIds.Count == 1));
            }

            return results;
        }

        public async Task<Result<DashboardDto>> CreateDashboard(CreateDashboardDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            var count = await db.Dashboards.CountAsync(d => d.UserId == user.Id);
            if (count >= DataLimits.MaxDashboardCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("dashboards", DataLimits.MaxDashboardCount));

            var maxOrder = await db.Dashboards
                .Where(d => d.UserId == user.Id)
                .Select(d => (int?)d.Order)
                .MaxAsync() ?? -1;

            var dashboard = new Dashboard
            {
                Name = dto.Name,
                Color = dto.Color,
                Icon = dto.Icon,
                Order = maxOrder + 1,
                UserId = user.Id
            };

            db.Dashboards.Add(dashboard);
            await db.SaveChangesAsync();

            return Result.Success(MapToDto(dashboard));
        }

        public async Task<Result<DashboardDto>> UpdateDashboard(string dashboardId, UpdateDashboardDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            dashboard.Name = dto.Name;
            dashboard.Color = dto.Color;
            dashboard.Icon = dto.Icon;

            await db.SaveChangesAsync();

            return Result.Success(MapToDto(dashboard));
        }

        public async Task<Result> DeleteDashboard(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            db.Dashboards.Remove(dashboard);
            await db.SaveChangesAsync();
            return Result.Success();
        }

        // Builds this placement's backing Widget via WidgetsService and places it in the same
        // call. Board capacity is checked first so a request that won't fit doesn't spend a slot.
        public async Task<Result<DashboardItemDto>> CreateAndPlaceWidget(string dashboardId, CreateAndPlaceWidgetDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var createResult = await widgetsService.CreateWidget(new CreateWidgetDto
            {
                Name = dto.Name,
                Description = dto.Description,
                ResultType = dto.ResultType,
                Code = dto.Code,
                Grouping = dto.Grouping,
                MatchedValuesOnly = dto.MatchedValuesOnly,
                GoalTarget = dto.GoalTarget,
                GoalDirection = dto.GoalDirection,
                Sources = dto.Sources.Select(s => new CreateWidgetSourceRequestDto
                {
                    TrackerId = s.TrackerId,
                    Fields = s.AnalyticFields
                }).ToList()
            });

            if (!createResult.IsSuccess)
                return Result.Failure(createResult.StatusCode, createResult.Messages);

            var widget = await db.Widgets
                .Include(w => w.Sources).ThenInclude(s => s.Tracker)
                .Include(w => w.Sources).ThenInclude(s => s.Fields).ThenInclude(f => f.Field)
                .FirstAsync(w => w.Id == createResult.Data.Id);

            // The two lists share the order they were built in (WidgetsService assigns WidgetSource.Order from the same enumeration).
            var overrides = dto.Sources.Zip(widget.Sources.OrderBy(s => s.Order), (input, saved) => new PlaceWidgetSourceOverrideDto
            {
                WidgetSourceId = saved.Id,
                Label = input.Label,
                ViewId = input.ViewId
            }).ToList();

            return await PlaceWidgetOnDashboard(dashboard, widget, new PlaceWidgetDto
            {
                WidgetId = widget.Id,
                DisplayMode = dto.DisplayMode,
                MobileDisplayMode = dto.MobileDisplayMode,
                YAxisFromZero = dto.YAxisFromZero,
                Color = dto.Color,
                ShowTrend = dto.ShowTrend,
                CalendarStartMonth = dto.CalendarStartMonth,
                SourceOverrides = overrides
            });
        }

        // Inserts one DashboardItem + one DashboardItemSource per WidgetSource, referencing
        // the Widget that CreateAndPlaceWidget just built for this placement alone.
        private async Task<Result<DashboardItemDto>> PlaceWidgetOnDashboard(Dashboard dashboard, Widget widget, PlaceWidgetDto dto)
        {
            var widgetSourceIds = widget.Sources.Select(s => s.Id).ToHashSet();
            var overridesBySourceId = dto.SourceOverrides.ToDictionary(o => o.WidgetSourceId);

            if (!overridesBySourceId.Keys.All(widgetSourceIds.Contains))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("source id for this widget"));

            foreach (var over in dto.SourceOverrides)
            {
                var widgetSource = widget.Sources.First(s => s.Id == over.WidgetSourceId);

                if (!string.IsNullOrEmpty(over.ViewId))
                {
                    var exists = await db.Views.AnyAsync(v => v.Id == over.ViewId && v.TrackerId == widgetSource.TrackerId);
                    if (!exists)
                        return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
                }
            }

            var item = BuildAnalyticItem(dashboard, widget, dto, overridesBySourceId);
            var sources = item.Sources;

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            var sourceDtos = sources.Select(s =>
            {
                var widgetSource = widget.Sources.First(ws => ws.Id == s.WidgetSourceId);
                var fields = widgetSource.Fields.Where(f => f.Field != null).ToList();

                return new DashboardItemSourceDto
                {
                    Id = s.Id,
                    Name = AnalyticDefinitionList.GetDisplayName(widget.ResultType, widget.Code, fields.Select(f => f.Field.Name), widget.Grouping),
                    Fields = fields.Select(f => new DashboardItemSourceFieldDto { Purpose = f.Purpose, FieldId = f.FieldId, FieldName = f.Field.Name }).ToList(),
                    TrackerId = widgetSource.TrackerId,
                    TrackerName = widgetSource.Tracker.Name,
                    ViewId = s.ViewId,
                    Label = s.Label,
                    Order = s.Order
                };
            }).ToList();

            return Result.Success(new DashboardItemDto
            {
                Id = item.Id,
                Order = item.Order,
                Type = item.Type,
                Layout = MapToLayoutDto(item),
                MobileLayout = MapToMobileLayoutDto(item),
                Config = item.Config,
                ResultType = widget.ResultType,
                Code = widget.Code,
                Grouping = widget.Grouping,
                MatchedValuesOnly = widget.MatchedValuesOnly,
                YAxisFromZero = item.YAxisFromZero,
                Color = item.Color,
                ShowTrend = item.ShowTrend,
                CalendarStartMonth = item.CalendarStartMonth,
                Sources = sourceDtos
            });
        }

        // Both grids are placed at once so neither has a hole the first time the board opens on the other screen size.
        private static DashboardItem BuildAnalyticItem(
            Dashboard dashboard,
            Widget widget,
            PlaceWidgetDto dto,
            IReadOnlyDictionary<string, PlaceWidgetSourceOverrideDto> overridesBySourceId)
        {
            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            var (width, height) = DashboardGrid.DefaultSizeFor(widget.ResultType);
            var nextRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Y + i.H) : 0;
            var nextMobileRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.MobileY + i.MobileH) : 0;

            var sources = widget.Sources.OrderBy(s => s.Order).Select(widgetSource =>
            {
                overridesBySourceId.TryGetValue(widgetSource.Id, out var over);
                return new DashboardItemSource
                {
                    Order = widgetSource.Order,
                    WidgetSourceId = widgetSource.Id,
                    Label = over?.Label,
                    ViewId = over?.ViewId
                };
            }).ToList();

            return new DashboardItem
            {
                DashboardId = dashboard.Id,
                Order = nextOrder,
                Type = DashboardWidgetTypes.Analytic,
                WidgetId = widget.Id,
                X = 0,
                Y = nextRow,
                W = width,
                H = height,
                MobileX = 0,
                MobileY = nextMobileRow,
                MobileW = DashboardGrid.MobileColumns,
                MobileH = height,
                DisplayMode = dto.DisplayMode,
                MobileDisplayMode = dto.MobileDisplayMode,
                YAxisFromZero = dto.YAxisFromZero,
                Color = string.IsNullOrEmpty(dto.Color) ? null : dto.Color,
                ShowTrend = dto.ShowTrend,
                CalendarStartMonth = string.IsNullOrEmpty(dto.CalendarStartMonth) ? null : dto.CalendarStartMonth,
                Sources = sources
            };
        }

        public async Task<Result<DashboardItemDto>> AddQuickAddItem(string dashboardId, AddDashboardQuickAddItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == dto.TrackerId);

            var hasAccess = tracker != null &&
                (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
                return Result.Failure(ResultStatusCodes.Forbidden);

            var item = BuildLayoutItem(dashboard, dashboardId, DashboardWidgetTypes.QuickAdd, DashboardGrid.QuickAddSize,
                JsonSerializer.Serialize(new QuickAddWidgetConfigDto { TrackerId = dto.TrackerId }, ConfigJsonOptions));

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        // Only Analytic and Entries widgets read a tracker, so only they can follow a filter.
        private static DashboardItem? FollowerTarget(Dashboard dashboard, string itemId)
        {
            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId);

            return item != null &&
                (item.Type == DashboardWidgetTypes.Analytic || item.Type == DashboardWidgetTypes.Entries)
                    ? item
                    : null;
        }

        private static bool LinkResolves(Dashboard dashboard, WidgetLinkDto link)
        {
            var target = FollowerTarget(dashboard, link.ItemId);
            return target != null && ResolveItemTrackerIds(target).Contains(link.TrackerId);
        }

        private async Task<Result> ValidateFollowerLinks(
            Dashboard dashboard,
            List<WidgetLinkDto> links,
            IReadOnlyDictionary<string, Query> clauseQueriesBySlot,
            string label)
        {
            var seenLinks = new HashSet<string>();
            var fieldIds = links.SelectMany(l => l.FieldByQuery.Values).Distinct().ToList();
            var fields = fieldIds.Count > 0
                ? await db.Fields.Where(f => fieldIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id)
                : new Dictionary<string, Field>();

            foreach (var link in links)
            {
                if (!seenLinks.Add($"{link.ItemId}|{link.TrackerId}"))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"duplicate {label} link"));

                var target = FollowerTarget(dashboard, link.ItemId);
                if (target == null)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("widget to link"));

                if (!ResolveItemTrackerIds(target).Contains(link.TrackerId))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("tracker for this widget"));

                foreach (var (slotId, fieldId) in link.FieldByQuery)
                {
                    if (!clauseQueriesBySlot.TryGetValue(slotId, out var query))
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"clause for this {label}"));

                    if (!fields.TryGetValue(fieldId, out var field) ||
                        field.TrackerId != link.TrackerId ||
                        !DataTypes.AreCompatible(query.DataType, field.Type))
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"field mapping for this {label}"));
                }
            }

            return Result.Success();
        }

        // ----- Filter widget (clause set typed on the board, with matching-shape presets) -----

        public async Task<Result<DashboardItemDto>> AddFilterItem(string dashboardId, SaveFilterItemDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var built = await BuildFilterConfig(dashboard, user.Id, dto);
            if (!built.IsSuccess)
                return Result.Failure(built.StatusCode, built.Messages);

            var config = JsonSerializer.Serialize(built.Data, ConfigJsonOptions);

            var item = BuildLayoutItem(dashboard, dashboardId, DashboardWidgetTypes.Filter, DashboardGrid.FilterSize, config);

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        // Returns the widget and its followers, the ones it had and the ones it has now, since a changed clause changes what every follower draws and a dropped link unfilters what used to follow it.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateFilterItem(string dashboardId, string itemId, SaveFilterItemDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Filter);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("filter widget"));

            var previous = TryParseFilterConfig(item.Config);

            // A previously-valid link can go stale (its follower or tracker left the board);
            // drop only those, since a genuinely new invalid link should still fail below.
            if (previous != null)
            {
                var carried = previous.Links.Select(l => $"{l.ItemId}|{l.TrackerId}").ToHashSet();
                dto.Links = dto.Links
                    .Where(l => LinkResolves(dashboard, l) || !carried.Contains($"{l.ItemId}|{l.TrackerId}"))
                    .ToList();
            }

            var built = await BuildFilterConfig(dashboard, user.Id, dto, previous?.Slots);
            if (!built.IsSuccess)
                return Result.Failure(built.StatusCode, built.Messages);

            // The edit form only carries clause shape; carry ValueBySlot across for every slot
            // that survived (BuildFilterConfig keeps a slot's id when its shape is unchanged).
            if (previous != null)
            {
                var surviving = built.Data!.Slots.Select(s => s.SlotId).ToHashSet();
                foreach (var (slotId, value) in previous.ValueBySlot)
                    if (!string.IsNullOrEmpty(value) && surviving.Contains(slotId))
                        built.Data!.ValueBySlot[slotId] = value;
            }

            item.Config = JsonSerializer.Serialize(built.Data, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard, FilterScope(item.Id, previous, built.Data)));
        }

        // Persists the values onto the item's Config, so every future load starts from them. Returns the widget and every follower its links name.
        public async Task<Result<List<DashboardWidgetDto>>> SetFilterValues(string dashboardId, string itemId, SetFilterValuesDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Filter);
            var config = item != null ? TryParseFilterConfig(item.Config) : null;
            if (item == null || config == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("filter widget"));

            var slotQueryIds = config.Slots.Select(s => s.QueryId).Distinct().ToList();
            var queriesById = await db.Queries
                .Where(q => slotQueryIds.Contains(q.Id))
                .ToDictionaryAsync(q => q.Id);

            var valueCheck = ValidateFilterValues(config.Slots, queriesById, dto.Values);
            if (!valueCheck.IsSuccess)
                return Result.Failure(valueCheck.StatusCode, valueCheck.Messages);

            config.ValueBySlot = NormalizeValues(dto.Values);
            item.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard, FilterScope(item.Id, config)));
        }

        // A preset's clauses (data type, operator, in order) must match the widget's clauses exactly.
        private async Task<Result<FilterWidgetConfigDto>> BuildFilterConfig(
            Dashboard dashboard, string ownerId, SaveFilterItemDto dto,
            IReadOnlyList<FilterClauseSlotDto>? existingSlots = null)
        {
            var resolved = await ResolveDashboardViewClauses(ownerId, dto.Clauses);
            if (resolved.IsFailure)
                return Result.Failure(resolved.StatusCode, resolved.Messages);

            var queries = resolved.Data!;

            // A surviving clause (matched by pooled query) keeps its slot id, so values and
            // follower field maps keyed off it ride through the edit; a reshaped clause gets a fresh id.
            var reusable = (existingSlots ?? [])
                .GroupBy(s => s.QueryId)
                .ToDictionary(g => g.Key, g => new Queue<string>(g.Select(s => s.SlotId)));

            var slots = new List<FilterClauseSlotDto>(queries.Count);
            foreach (var query in queries)
            {
                var slotId = reusable.TryGetValue(query.Id, out var pool) && pool.Count > 0
                    ? pool.Dequeue()
                    : Guid.NewGuid().ToString();
                slots.Add(new FilterClauseSlotDto { SlotId = slotId, QueryId = query.Id });
            }

            var queriesBySlot = slots
                .Select((slot, i) => (slot.SlotId, Query: queries[i]))
                .ToDictionary(x => x.SlotId, x => x.Query);

            // FieldByQuery arrives keyed by clause index; rewrite to slot id so two clauses of the same shape stay distinct.
            var mappedLinks = new List<WidgetLinkDto>();
            foreach (var link in dto.Links)
            {
                var fieldBySlot = new Dictionary<string, string>();
                foreach (var (key, fieldId) in link.FieldByQuery)
                {
                    if (!int.TryParse(key, out var index) || index < 0 || index >= slots.Count)
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("clause for this filter widget"));
                    fieldBySlot[slots[index].SlotId] = fieldId;
                }
                mappedLinks.Add(new WidgetLinkDto
                {
                    ItemId = link.ItemId,
                    TrackerId = link.TrackerId,
                    FieldByQuery = fieldBySlot
                });
            }

            var linkCheck = await ValidateFollowerLinks(dashboard, mappedLinks, queriesBySlot, "filter widget");
            if (linkCheck.IsFailure)
                return Result.Failure(linkCheck.StatusCode, linkCheck.Messages);

            var valueBySlot = new Dictionary<string, string?>();
            for (var i = 0; i < slots.Count; i++)
            {
                var value = dto.Clauses[i].Value;
                if (!string.IsNullOrEmpty(value))
                    valueBySlot[slots[i].SlotId] = value;
            }

            var presetIds = dto.PresetIds.Distinct().ToList();
            if (presetIds.Count > 0)
            {
                var presetViews = await db.DashboardViews
                    .Where(dv => dv.DashboardId == dashboard.Id && presetIds.Contains(dv.Id))
                    .Include(dv => dv.DashboardViewQueries).ThenInclude(q => q.Query)
                    .ToListAsync();

                if (presetViews.Count != presetIds.Count)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("preset"));

                if (presetViews.Any(v => PresetValuesForShape(v, queries) == null))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("preset for this filter widget's clauses"));
            }

            return Result.Success(new FilterWidgetConfigDto
            {
                Slots = slots,
                ValueBySlot = valueBySlot,
                Links = mappedLinks,
                PresetIds = presetIds
            });
        }

        private static Result ValidateFilterValues(
            IReadOnlyList<FilterClauseSlotDto> slots,
            IReadOnlyDictionary<string, Query> queriesById,
            Dictionary<string, string?> values)
        {
            var filterQueryBySlot = slots
                .Where(s => queriesById.TryGetValue(s.QueryId, out var q) && q.Kind == QueryKinds.Filter)
                .ToDictionary(s => s.SlotId, s => queriesById[s.QueryId]);

            foreach (var (slotId, value) in values)
            {
                if (!filterQueryBySlot.TryGetValue(slotId, out var query))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("clause for this filter widget"));

                if (string.IsNullOrEmpty(value))
                    continue;

                var check = QueryBuilder.ValidateClause(QueryKinds.Filter, query.DataType, query.Operator, value, false);
                if (check.IsFailure)
                    return Result.Failure(check.StatusCode, check.Messages);
            }

            return Result.Success();
        }

        private static Dictionary<string, string?> NormalizeValues(Dictionary<string, string?> values) =>
            values
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

        // ----- DashboardView (named clause set) CRUD -----

        public async Task<Result<List<DashboardViewDto>>> GetDashboardViews(string dashboardId)
        {
            if (!await UserOwnsDashboard(dashboardId))
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var views = await db.DashboardViews
                .Where(dv => dv.DashboardId == dashboardId)
                .Include(dv => dv.DashboardViewQueries.OrderBy(q => q.Order)).ThenInclude(q => q.Query)
                .OrderBy(dv => dv.Order)
                .ToListAsync();

            return Result.Success(views.Select(MapDashboardViewToDto).ToList());
        }

        public async Task<Result<DashboardViewDto>> AddDashboardView(string dashboardId, SaveDashboardViewDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            if (!await UserOwnsDashboard(dashboardId))
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var count = await db.DashboardViews.CountAsync(dv => dv.DashboardId == dashboardId);
            if (count >= DataLimits.MaxDashboardViewCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("dashboard views", DataLimits.MaxDashboardViewCount));

            var clauses = await ResolveDashboardViewClauses(user.Id, dto.Clauses);
            if (clauses.IsFailure)
                return Result.Failure(clauses.StatusCode, clauses.Messages);

            var maxOrder = await db.DashboardViews.Where(dv => dv.DashboardId == dashboardId)
                .Select(dv => (int?)dv.Order).MaxAsync() ?? -1;

            var view = new DashboardView { DashboardId = dashboardId, Name = dto.Name, Order = maxOrder + 1 };
            db.DashboardViews.Add(view);
            for (int i = 0; i < clauses.Data!.Count; i++)
                db.DashboardViewQueries.Add(new DashboardViewQuery { DashboardViewId = view.Id, QueryId = clauses.Data[i].Id, Order = i });

            await db.SaveChangesAsync();
            return await GetDashboardView(dashboardId, view.Id);
        }

        public async Task<Result<DashboardViewDto>> UpdateDashboardView(string dashboardId, string viewId, SaveDashboardViewDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            if (!await UserOwnsDashboard(dashboardId))
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var view = await db.DashboardViews.FirstOrDefaultAsync(dv => dv.Id == viewId && dv.DashboardId == dashboardId);
            if (view == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard view"));

            var clauses = await ResolveDashboardViewClauses(user.Id, dto.Clauses);
            if (clauses.IsFailure)
                return Result.Failure(clauses.StatusCode, clauses.Messages);

            view.Name = dto.Name;
            await db.DashboardViewQueries.Where(q => q.DashboardViewId == viewId).ExecuteDeleteAsync();
            for (int i = 0; i < clauses.Data!.Count; i++)
                db.DashboardViewQueries.Add(new DashboardViewQuery { DashboardViewId = viewId, QueryId = clauses.Data[i].Id, Order = i });

            await db.SaveChangesAsync();
            return await GetDashboardView(dashboardId, viewId);
        }

        public async Task<Result> DeleteDashboardView(string dashboardId, string viewId)
        {
            if (!await UserOwnsDashboard(dashboardId))
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var deleted = await db.DashboardViews
                .Where(dv => dv.Id == viewId && dv.DashboardId == dashboardId)
                .ExecuteDeleteAsync();

            return deleted > 0
                ? Result.Success()
                : Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard view"));
        }

        public async Task<Result> ReorderDashboardViews(string dashboardId, ReorderDashboardViewsDto dto)
        {
            if (!await UserOwnsDashboard(dashboardId))
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var views = await db.DashboardViews.AsTracking().Where(dv => dv.DashboardId == dashboardId).ToListAsync();
            if (!dto.DashboardViewIds.ToHashSet().SetEquals(views.Select(v => v.Id).ToHashSet()))
                return Result.Failure(ResultStatusCodes.BadRequest);

            var byId = views.ToDictionary(v => v.Id);
            for (int i = 0; i < dto.DashboardViewIds.Count; i++)
                byId[dto.DashboardViewIds[i]].Order = i;

            await db.SaveChangesAsync();
            return Result.Success();
        }

        private async Task<Result<DashboardViewDto>> GetDashboardView(string dashboardId, string viewId)
        {
            var view = await db.DashboardViews
                .Where(dv => dv.Id == viewId && dv.DashboardId == dashboardId)
                .Include(dv => dv.DashboardViewQueries.OrderBy(q => q.Order)).ThenInclude(q => q.Query)
                .FirstOrDefaultAsync();

            return view == null
                ? Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard view"))
                : Result.Success(MapDashboardViewToDto(view));
        }

        private async Task<Result<List<Query>>> ResolveDashboardViewClauses(string ownerId, List<ClauseDto> clauses)
        {
            var resolved = new List<Query>();
            var filterCount = 0;
            var sortCount = 0;

            foreach (var clause in clauses)
            {
                var validation = QueryBuilder.ValidateClause(clause);
                if (validation.IsFailure)
                    return Result.Failure(validation.StatusCode, validation.Messages);

                if (clause.Kind == QueryKinds.Sort) sortCount++; else filterCount++;
                resolved.Add(await QueryPool.GetOrCreate(db, ownerId, clause));
            }

            if (filterCount > DataLimits.MaxFilters)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("filters", DataLimits.MaxFilters));
            if (sortCount > DataLimits.MaxSorts)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("sorts", DataLimits.MaxSorts));

            var addedToPool = db.ChangeTracker.Entries<Query>().Count(e => e.State == EntityState.Added);
            var existingPool = await db.Queries.CountAsync(q => q.OwnerId == ownerId);
            if (existingPool + addedToPool > DataLimits.MaxQueryCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("queries", DataLimits.MaxQueryCount));

            return Result.Success(resolved);
        }

        private static DashboardViewDto MapDashboardViewToDto(DashboardView view) => new()
        {
            Id = view.Id,
            Name = view.Name,
            Order = view.Order,
            Clauses = view.DashboardViewQueries
                .OrderBy(q => q.Order)
                .Select(q => new DashboardViewClauseDto
                {
                    QueryId = q.QueryId,
                    Kind = q.Query.Kind,
                    DataType = q.Query.DataType,
                    Operator = q.Query.Operator,
                    Value = q.Query.Value,
                    Descending = q.Query.Descending
                })
                .ToList()
        };

        private async Task<bool> UserOwnsDashboard(string dashboardId)
        {
            var user = currentUserService.GetCurrentUser();
            return await db.Dashboards.AnyAsync(d => d.Id == dashboardId && d.UserId == user.Id);
        }

        // The Entries equivalent of CreateAndPlaceWidget.
        public async Task<Result<DashboardItemDto>> CreateAndPlaceEntriesWidget(string dashboardId, CreateAndPlaceEntriesWidgetDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var createResult = await widgetsService.CreateEntriesWidget(new CreateEntriesWidgetDto { TrackerId = dto.TrackerId, Name = dto.Name });
            if (!createResult.IsSuccess)
                return Result.Failure(createResult.StatusCode, createResult.Messages);

            var entriesWidget = await db.EntriesWidgets.FirstAsync(w => w.Id == createResult.Data.Id);

            return await PlaceEntriesWidgetOnDashboard(dashboard, entriesWidget, new PlaceEntriesWidgetDto
            {
                EntriesWidgetId = entriesWidget.Id,
                ColumnFieldIds = dto.ColumnFieldIds,
                DisplayMode = dto.DisplayMode,
                MobileDisplayMode = dto.MobileDisplayMode
            });
        }

        // Inserts one Entries DashboardItem referencing the EntriesWidget that
        // CreateAndPlaceEntriesWidget just built for this placement alone.
        private async Task<Result<DashboardItemDto>> PlaceEntriesWidgetOnDashboard(Dashboard dashboard, EntriesWidget entriesWidget, PlaceEntriesWidgetDto dto)
        {
            var columns = await ResolveEntriesColumns(entriesWidget.TrackerId, dto.ColumnFieldIds);
            if (columns.IsFailure)
                return Result.Failure(columns.StatusCode, columns.Messages);

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;
            var nextRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Y + i.H) : 0;
            var nextMobileRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.MobileY + i.MobileH) : 0;
            var (width, height) = DashboardGrid.EntriesSize;

            var item = new DashboardItem
            {
                DashboardId = dashboard.Id,
                Order = nextOrder,
                Type = DashboardWidgetTypes.Entries,
                EntriesWidgetId = entriesWidget.Id,
                Config = JsonSerializer.Serialize(new EntriesWidgetConfigDto
                {
                    ColumnFieldIds = columns.Data!
                }, ConfigJsonOptions),
                X = 0,
                Y = nextRow,
                W = width,
                H = height,
                MobileX = 0,
                MobileY = nextMobileRow,
                MobileW = DashboardGrid.MobileColumns,
                MobileH = height,
                DisplayMode = dto.DisplayMode,
                MobileDisplayMode = dto.MobileDisplayMode
            };

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        public async Task<Result<DashboardItemDto>> AddHeaderItem(string dashboardId, AddDashboardHeaderItemDto dto)
        {
            return await AddTextItem(dashboardId, DashboardWidgetTypes.Header, DashboardGrid.HeaderSize, dto.Text);
        }

        public async Task<Result<DashboardItemDto>> AddNoteItem(string dashboardId, AddDashboardNoteItemDto dto)
        {
            return await AddTextItem(dashboardId, DashboardWidgetTypes.Note, DashboardGrid.NoteSize, dto.Text);
        }

        public async Task<Result<DashboardItemDto>> AddDividerItem(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var item = BuildLayoutItem(dashboard, dashboardId, DashboardWidgetTypes.Divider, DashboardGrid.DividerSize, config: null);

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        public async Task<Result<DashboardItemDto>> AddContainerItem(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var item = BuildLayoutItem(dashboard, dashboardId, DashboardWidgetTypes.Container, DashboardGrid.ContainerSize, config: null);

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        public async Task<Result<DashboardItemDto>> AddTabsContainerItem(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var config = JsonSerializer.Serialize(new TabsContainerConfigDto
            {
                Tabs = [new TabDefDto { Id = Guid.NewGuid().ToString(), Name = "Tab 1" }]
            }, ConfigJsonOptions);

            var item = BuildLayoutItem(dashboard, dashboardId, DashboardWidgetTypes.TabsContainer, DashboardGrid.TabsContainerSize, config);

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        // A tab dropped from the list has its children repointed to the first surviving tab.
        public async Task<Result<List<DashboardWidgetDto>>> SaveTabsContainer(string dashboardId, string itemId, SaveTabsContainerDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.TabsContainer);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tabs container"));

            if (dto.Tabs.Count is < 1 or > DataLimits.MaxDashboardTabCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("tab count"));

            var existingIds = (TryParseTabsContainerConfig(item.Config)?.Tabs ?? []).Select(t => t.Id).ToHashSet();

            // A matching id is a rename in place; anything else gets a fresh id.
            var claimed = new HashSet<string>();
            var tabs = dto.Tabs.Select(t =>
            {
                var keep = !string.IsNullOrEmpty(t.Id) && existingIds.Contains(t.Id) && claimed.Add(t.Id);
                return new TabDefDto
                {
                    Id = keep ? t.Id! : Guid.NewGuid().ToString(),
                    Name = t.Name.Trim()
                };
            }).ToList();

            if (tabs.Any(t => string.IsNullOrEmpty(t.Name) || t.Name.Length > DataLimits.MaxTabNameLength))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("tab name"));

            var survivingIds = tabs.Select(t => t.Id).ToHashSet();
            var fallbackTabId = tabs[0].Id;
            foreach (var child in dashboard.Items.Where(i => i.ParentItemId == item.Id))
                if (child.ParentTabId == null || !survivingIds.Contains(child.ParentTabId))
                    child.ParentTabId = fallbackTabId;

            var title = dto.Title?.Trim();
            item.Config = JsonSerializer.Serialize(new TabsContainerConfigDto
            {
                Title = string.IsNullOrEmpty(title) ? null : title,
                Tabs = tabs
            }, ConfigJsonOptions);

            await db.SaveChangesAsync();

            // The panel and its children: a child can have been moved to another tab just
            // above, and the client has no way to work out which ones from the tab list alone.
            // The rest of the board is untouched by a tab rename.
            var scope = new HashSet<string> { item.Id };
            foreach (var child in dashboard.Items.Where(i => i.ParentItemId == item.Id))
                scope.Add(child.Id);

            return Result.Success(await BuildWidgets(dashboard, scope));
        }

        private async Task<Result<DashboardItemDto>> AddTextItem(string dashboardId, string type, (int Width, int Height) size, string text)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var config = JsonSerializer.Serialize(new TextWidgetConfigDto { Text = text }, ConfigJsonOptions);
            var item = BuildLayoutItem(dashboard, dashboardId, type, size, config);

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        private static DashboardItem BuildLayoutItem(Dashboard dashboard, string dashboardId, string type, (int Width, int Height) size, string? config)
        {
            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;
            var nextRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Y + i.H) : 0;
            var nextMobileRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.MobileY + i.MobileH) : 0;
            var (width, height) = size;

            return new DashboardItem
            {
                DashboardId = dashboardId,
                Order = nextOrder,
                Type = type,
                Config = config,
                X = 0,
                Y = nextRow,
                W = width,
                H = height,
                MobileX = 0,
                MobileY = nextMobileRow,
                MobileW = DashboardGrid.MobileColumns,
                MobileH = height
            };
        }

        // Most fields here are placement-level (display mode, color, per-source label/view),
        // but Name/GoalTarget/GoalDirection/MatchedValuesOnly live on the backing Widget --
        // fine to edit directly since it's this one placement's own, never shared.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateDashboardItem(string dashboardId, string itemId, UpdateDashboardItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Analytic);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("analytic widget"));

            // All-or-nothing: the payload must name every source exactly once, or an omitted
            // one could mean "cleared" rather than "left alone".
            var suppliedIds = dto.Sources.Select(s => s.SourceId).ToList();
            if (suppliedIds.Count != suppliedIds.Distinct().Count() ||
                !item.Sources.Select(s => s.Id).ToHashSet().SetEquals(suppliedIds))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Required("every source of this widget, exactly once"));

            foreach (var sourceDto in dto.Sources)
            {
                var source = item.Sources.First(s => s.Id == sourceDto.SourceId);
                var trackerId = source.WidgetSource!.TrackerId;

                if (!string.IsNullOrEmpty(sourceDto.ViewId))
                {
                    var exists = await db.Views.AnyAsync(v => v.Id == sourceDto.ViewId && v.TrackerId == trackerId);
                    if (!exists)
                        return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
                }
            }

            foreach (var sourceDto in dto.Sources)
            {
                var source = item.Sources.First(s => s.Id == sourceDto.SourceId);

                source.Label = string.IsNullOrWhiteSpace(sourceDto.Label) ? null : sourceDto.Label.Trim();
                source.ViewId = string.IsNullOrEmpty(sourceDto.ViewId) ? null : sourceDto.ViewId;
            }

            // These live on the shared Widget rather than this placement, mirroring
            // WidgetsService.UpdateWidget -- editing them here touches every board the widget
            // is placed on, same as editing its result type or sources would if that were allowed.
            item.Widget!.Name = dto.Name?.Trim() ?? string.Empty;
            item.Widget.MatchedValuesOnly = dto.MatchedValuesOnly;

            if (item.Widget.ResultType == AnalyticTypes.Goal)
            {
                if (!string.IsNullOrEmpty(dto.GoalTarget))
                {
                    var target = dto.GoalTarget.Trim();
                    var valueFieldType = item.Sources
                        .SelectMany(s => s.WidgetSource?.Fields ?? [])
                        .FirstOrDefault(f => f.Purpose == AnalyticPurposes.Value)?.Field?.Type;

                    if (!GoalTargets.IsParseable(target) || !GoalTargets.MatchesFieldType(item.Widget.Code, valueFieldType, target))
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("goal target for this field's type"));

                    item.Widget.GoalTarget = target;
                }

                if (!string.IsNullOrEmpty(dto.GoalDirection))
                    item.Widget.GoalDirection = dto.GoalDirection;
            }

            var targets = ValidateGoalConditionalTargets(dashboard, item, dto.GoalConditionalTargets);
            if (targets.IsFailure)
                return Result.Failure(targets.StatusCode, targets.Messages);

            var conditionalTargetsJson = targets.Data;

            item.DisplayMode = dto.DisplayMode;
            item.MobileDisplayMode = dto.MobileDisplayMode;
            item.YAxisFromZero = dto.YAxisFromZero;
            item.GoalConditionalTargets = conditionalTargetsJson;
            item.Color = string.IsNullOrEmpty(dto.Color) ? null : dto.Color;
            item.ShowTrend = dto.ShowTrend;
            item.CalendarStartMonth = string.IsNullOrEmpty(dto.CalendarStartMonth) ? null : dto.CalendarStartMonth;

            await db.SaveChangesAsync();

            // Only this placement changed, so only this placement is recalculated -- nothing
            // else on the board reads its sources, its axis or its targets.
            return Result.Success(await BuildWidgets(dashboard, new HashSet<string> { item.Id }));
        }

        // Null when there are no rows to keep: only a Goal placement has conditional targets, and an empty list means the default always applies.
        private static Result<string?> ValidateGoalConditionalTargets(
            Dashboard dashboard,
            DashboardItem item,
            List<GoalConditionalTargetDto> rows)
        {
            if (item.Widget?.ResultType != AnalyticTypes.Goal || rows.Count == 0)
                return Result.Success<string?>(null);

            var filterConfigs = dashboard.Items
                .Where(i => i.Type == DashboardWidgetTypes.Filter)
                .Select(i => TryParseFilterConfig(i.Config))
                .Where(c => c != null)
                .Select(c => c!)
                .ToList();

            var connectedKeys = ConnectedFilterValues(item.Id, filterConfigs).Keys.ToHashSet();

            var valueFieldType = item.Sources
                .SelectMany(s => s.WidgetSource?.Fields ?? [])
                .FirstOrDefault(f => f.Purpose == AnalyticPurposes.Value)?.Field?.Type;

            foreach (var row in rows)
            {
                if (row.Conditions.Count == 0)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Required("a condition for every conditional target"));

                if (!row.Conditions.Keys.All(connectedKeys.Contains))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("condition for a filter this widget doesn't follow"));

                var target = row.Target?.Trim() ?? string.Empty;
                if (!GoalTargets.IsParseable(target) || !GoalTargets.MatchesFieldType(item.Widget.Code, valueFieldType, target))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("conditional target for this field's type"));

                row.Target = target;
            }

            return Result.Success<string?>(JsonSerializer.Serialize(rows, ConfigJsonOptions));
        }

        // Only which columns an Entries widget shows and whether it collapses to a button: the tracker it reads from stays as placed. Returns this one table recomputed.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateEntriesItem(string dashboardId, string itemId, UpdateDashboardEntriesItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Entries);
            if (item?.EntriesWidget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("entries widget"));

            var columns = await ResolveEntriesColumns(item.EntriesWidget.TrackerId, dto.ColumnFieldIds);
            if (columns.IsFailure)
                return Result.Failure(columns.StatusCode, columns.Messages);

            // Lives on the shared EntriesWidget, not this placement -- see UpdateDashboardItem.
            item.EntriesWidget.Name = dto.Name?.Trim() ?? string.Empty;

            item.Config = JsonSerializer.Serialize(new EntriesWidgetConfigDto
            {
                ColumnFieldIds = columns.Data!
            }, ConfigJsonOptions);
            item.DisplayMode = dto.DisplayMode;
            item.MobileDisplayMode = dto.MobileDisplayMode;

            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard, new HashSet<string> { item.Id }));
        }

        // Nothing else on the board depends on this widget's Config, so only the changed item is returned.
        public async Task<Result<DashboardItemDto>> SetTextWidgetContent(string dashboardId, string itemId, SetTextWidgetContentDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i =>
                i.Id == itemId && (i.Type == DashboardWidgetTypes.Header
                    || i.Type == DashboardWidgetTypes.Note
                    || i.Type == DashboardWidgetTypes.Container));
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("text widget"));

            var maxLength = item.Type == DashboardWidgetTypes.Note
                ? DataLimits.MaxNoteTextLength
                : DataLimits.MaxHeaderTextLength;

            if (dto.Text.Length > maxLength)
                return Result.Failure(ResultStatusCodes.BadRequest, $"Text cannot exceed {maxLength} characters.");

            item.Config = JsonSerializer.Serialize(new TextWidgetConfigDto { Text = dto.Text }, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        public async Task<Result> RemoveDashboardItem(string dashboardId, string itemId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard item"));

            // A container's children move onto the board rather than being deleted with it.
            if (DashboardWidgetTypes.IsContainer(item.Type))
            {
                foreach (var child in dashboard.Items.Where(i => i.ParentItemId == item.Id).ToList())
                {
                    child.ParentItemId = null;
                    child.ParentTabId = null;
                    child.Y += item.Y;
                    child.X = Math.Min(child.X, Math.Max(0, DashboardGrid.Columns - child.W));
                }
            }

            // A filter widget names followers by id inside its own Config, where no foreign
            // key can clean up after a delete; drop dangling links here instead.
            foreach (var filterItem in dashboard.Items.Where(i => i.Type == DashboardWidgetTypes.Filter))
            {
                var filterConfig = TryParseFilterConfig(filterItem.Config);
                if (filterConfig == null || filterConfig.Links.RemoveAll(l => l.ItemId == item.Id) == 0)
                    continue;

                filterItem.Config = JsonSerializer.Serialize(filterConfig, ConfigJsonOptions);
            }

            // A Widget/EntriesWidget belongs exclusively to the one placement that created it
            // (there's no reuse across dashboards), so it must go with the item or it leaks forever.
            if (item.Widget != null) db.Widgets.Remove(item.Widget);
            if (item.EntriesWidget != null) db.EntriesWidgets.Remove(item.EntriesWidget);
            db.DashboardItems.Remove(item);
            await db.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> UpdateDashboardLayout(string dashboardId, UpdateDashboardLayoutDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            // A container can never itself be nested, so it is not a candidate parent for one.
            var containerIds = dashboard.Items
                .Where(i => DashboardWidgetTypes.IsContainer(i.Type))
                .Select(i => i.Id)
                .ToHashSet();

            var tabOrderByContainer = dashboard.Items
                .Where(i => i.Type == DashboardWidgetTypes.TabsContainer)
                .ToDictionary(
                    i => i.Id,
                    i => (TryParseTabsContainerConfig(i.Config)?.Tabs ?? []).Select(t => t.Id).ToList());

            foreach (var placement in dto.Items)
            {
                var item = dashboard.Items.FirstOrDefault(x => x.Id == placement.ItemId);
                if (item == null) continue;

                // The narrow grid flattens containers away; parent is only set from the wide grid.
                if (dto.Variant == DashboardLayoutVariants.Desktop)
                {
                    var wantsParent = placement.ParentItemId;
                    var parentOk =
                        wantsParent != null
                        && wantsParent != item.Id
                        && !DashboardWidgetTypes.IsContainer(item.Type)
                        && containerIds.Contains(wantsParent);

                    item.ParentItemId = parentOk ? wantsParent : null;

                    // Falls back to the first tab so the widget stays in the panel rather than vanishing to the board.
                    if (parentOk && tabOrderByContainer.TryGetValue(wantsParent!, out var tabIds) && tabIds.Count > 0)
                        item.ParentTabId = placement.ParentTabId != null && tabIds.Contains(placement.ParentTabId) ? placement.ParentTabId : tabIds[0];
                    else
                        item.ParentTabId = null;
                }

                ApplyPlacement(item, dto.Variant, placement.X, placement.Y, placement.W, placement.H);
            }

            // Order still decides reading order for a client without the grid. Only the
            // desktop layout gets a say, to avoid the two grids fighting over it.
            if (dto.Variant == DashboardLayoutVariants.Desktop)
                RecomputeItemOrder(dashboard, tabOrderByContainer);

            await db.SaveChangesAsync();
            return Result.Success();
        }

        // Reading order, top-left to bottom-right, derived from the desktop placement: a
        // child follows its container, and inside one, its tab's position in the tab list.
        private static void RecomputeItemOrder(
            Dashboard dashboard,
            IReadOnlyDictionary<string, List<string>> tabOrderByContainer)
        {
            int TabRank(DashboardItem c) =>
                c.ParentItemId != null
                && tabOrderByContainer.TryGetValue(c.ParentItemId, out var tabIds)
                && c.ParentTabId != null
                    ? tabIds.IndexOf(c.ParentTabId)
                    : 0;

            var childrenByParent = dashboard.Items
                .Where(i => i.ParentItemId != null)
                .GroupBy(i => i.ParentItemId!)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(TabRank).ThenBy(c => c.Y).ThenBy(c => c.X).ToList());

            var order = 0;
            foreach (var item in dashboard.Items
                .Where(i => i.ParentItemId == null)
                .OrderBy(i => i.Y).ThenBy(i => i.X))
            {
                item.Order = order++;
                if (childrenByParent.TryGetValue(item.Id, out var children))
                    foreach (var child in children)
                        child.Order = order++;
            }
        }

        // Out-of-bounds values are clamped rather than rejected, so one bad placement doesn't fail the whole board save.
        private static void ApplyPlacement(DashboardItem item, string variant, int x, int y, int w, int h)
        {
            var columns = DashboardGrid.ColumnsFor(variant);
            var width = Math.Clamp(w, DashboardGrid.MinWidthFor(variant), columns);
            var height = Math.Clamp(h, DashboardGrid.MinHeight, DashboardGrid.MaxHeight);

            if (variant == DashboardLayoutVariants.Mobile)
            {
                item.MobileW = width;
                item.MobileH = height;
                item.MobileX = Math.Clamp(x, 0, columns - width);
                item.MobileY = Math.Max(y, 0);
                return;
            }

            item.W = width;
            item.H = height;
            item.X = Math.Clamp(x, 0, columns - width);
            item.Y = Math.Max(y, 0);
        }

        // Returns the preset's value per clause in the widget's clause order, or null if the shapes no longer match.
        private static List<string?>? PresetValuesForShape(DashboardView view, IReadOnlyList<Query> widgetClauses)
        {
            var presetClauses = view.DashboardViewQueries
                .OrderBy(q => q.Order)
                .Select(q => q.Query)
                .Where(q => q.Kind == QueryKinds.Filter)
                .ToList();

            if (presetClauses.Count != widgetClauses.Count)
                return null;

            var values = new List<string?>();
            for (var i = 0; i < widgetClauses.Count; i++)
            {
                if (presetClauses[i].DataType != widgetClauses[i].DataType ||
                    presetClauses[i].Operator != widgetClauses[i].Operator)
                    return null;
                values.Add(presetClauses[i].Value);
            }

            return values;
        }

        // What a write to one filter widget has to recalculate. Pass both the config as it was and as it now is, so a widget that just stopped following it comes back unfiltered too.
        private static HashSet<string> FilterScope(string itemId, params FilterWidgetConfigDto?[] configs)
        {
            var scope = new HashSet<string> { itemId };

            foreach (var config in configs)
                foreach (var link in config?.Links ?? [])
                    scope.Add(link.ItemId);

            return scope;
        }

        // A blank filter value is dropped unless its operator reads a blank as "is empty" / "has a value" on its own.
        private static (List<ResolvedClause> Filters, List<ResolvedClause> Sorts) ResolveFilterClauses(
            string itemId,
            string trackerId,
            IEnumerable<FilterWidgetConfigDto> filterConfigs,
            IReadOnlyDictionary<string, Query> filterQueriesById,
            IReadOnlyDictionary<string, Field> selectorFieldsById)
        {
            var filters = new List<ResolvedClause>();
            var sorts = new List<ResolvedClause>();

            foreach (var config in filterConfigs)
            {
                var link = config.Links.FirstOrDefault(l =>
                    l.ItemId == itemId && l.TrackerId == trackerId);
                if (link == null) continue;

                foreach (var slot in config.Slots)
                {
                    if (!filterQueriesById.TryGetValue(slot.QueryId, out var query))
                        continue;

                    if (!link.FieldByQuery.TryGetValue(slot.SlotId, out var fieldId) ||
                        !selectorFieldsById.TryGetValue(fieldId, out var field))
                        continue;

                    if (query.Kind == QueryKinds.Sort)
                    {
                        sorts.Add(new ResolvedClause(field.Id, field.Type, null, null, query.Descending));
                        continue;
                    }

                    var value = config.ValueBySlot.GetValueOrDefault(slot.SlotId);

                    if (string.IsNullOrEmpty(value) &&
                        query.Operator != OperatorTypes.EqualsOperator &&
                        query.Operator != OperatorTypes.NotEquals)
                        continue;

                    filters.Add(new ResolvedClause(field.Id, field.Type, query.Operator, value, false));
                }
            }

            return (filters, sorts);
        }

        // Empty in, empty out; the renderer reads an empty list as "every field".
        private async Task<Result<List<string>>> ResolveEntriesColumns(string trackerId, List<string> columnFieldIds)
        {
            if (columnFieldIds.Count == 0)
                return Result.Success(new List<string>());

            var trackerFieldIds = (await db.Fields
                    .Where(f => f.TrackerId == trackerId)
                    .Select(f => f.Id)
                    .ToListAsync())
                .ToHashSet();

            var resolved = new List<string>();
            foreach (var fieldId in columnFieldIds)
            {
                if (!trackerFieldIds.Contains(fieldId))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("column field"));
                if (!resolved.Contains(fieldId))
                    resolved.Add(fieldId);
            }

            if (resolved.Count > DataLimits.MaxColumns)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("columns", DataLimits.MaxColumns));

            return Result.Success(resolved);
        }

        // Deleting a field cascades its mapping away, so an incomplete map is possible here.
        private static Dictionary<string, Field> BuildFieldMap(WidgetSource source) =>
            source.Fields
                .Where(f => f.Field != null)
                .ToDictionary(f => f.Purpose, f => f.Field);

        private static IQueryable<Dashboard> WithSourceGraph(IQueryable<Dashboard> query) => query
            .Include(d => d.Items).ThenInclude(i => i.Widget)
            .Include(d => d.Items).ThenInclude(i => i.EntriesWidget).ThenInclude(w => w!.Tracker)
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.WidgetSource).ThenInclude(ws => ws!.Tracker)
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.WidgetSource).ThenInclude(ws => ws!.Fields).ThenInclude(f => f.Field);

        private async Task<Dashboard?> GetUserDashboard(string dashboardId)
        {
            var user = currentUserService.GetCurrentUser();
            return await WithSourceGraph(db.Dashboards)
                // Must be tracked and identity-resolved: Remove() throws if the same Tracker,
                // referenced by more than one source, materializes as separate CLR instances.
                .AsTracking()
                .FirstOrDefaultAsync(d => d.Id == dashboardId && d.UserId == user.Id);
        }

        private static DashboardDto MapToDto(Dashboard d) => new()
        {
            Id = d.Id,
            Name = d.Name,
            Color = d.Color,
            Icon = d.Icon,
            Items = d.Items.OrderBy(i => i.Order).Select(MapToItemDto).ToList()
        };

        private static DashboardItemDto MapToItemDto(DashboardItem item) => new()
        {
            Id = item.Id,
            Order = item.Order,
            Type = item.Type,
            ParentItemId = item.ParentItemId,
            ParentTabId = item.ParentTabId,
            Layout = MapToLayoutDto(item),
            MobileLayout = MapToMobileLayoutDto(item),
            Config = item.Config,
            Name = ResolveItemName(item),
            RawName = ResolveRawName(item),
            TrackerIds = ResolveItemTrackerIds(item),
            ResultType = item.Widget?.ResultType ?? string.Empty,
            Code = item.Widget?.Code ?? string.Empty,
            Grouping = item.Widget?.Grouping,
            MatchedValuesOnly = item.Widget?.MatchedValuesOnly ?? false,
            YAxisFromZero = item.YAxisFromZero,
            GoalConditionalTargets = ParseGoalConditionalTargets(item.GoalConditionalTargets),
            Color = item.Color,
            ShowTrend = item.ShowTrend,
            CalendarStartMonth = item.CalendarStartMonth,
            GoalDirection = item.Widget?.GoalDirection,
            GoalTarget = item.Widget?.GoalTarget,
            Sources = item.Sources.OrderBy(s => s.Order).Select(s => MapSourceToDto(item, s)).ToList()
        };

        private static string? ResolveRawName(DashboardItem item)
        {
            var raw = item.Type == DashboardWidgetTypes.Entries ? item.EntriesWidget?.Name : item.Widget?.Name;
            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }

        private static string ResolveItemName(DashboardItem item)
        {
            if (item.Type == DashboardWidgetTypes.Entries)
                return item.EntriesWidget?.Name ?? string.Empty;

            if (item.Widget == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(item.Widget.Name))
                return item.Widget.Name;

            var firstSource = item.Sources.OrderBy(s => s.Order).FirstOrDefault();
            var fieldNames = firstSource?.WidgetSource?.Fields
                .Where(f => f.Field != null)
                .Select(f => f.Field.Name) ?? [];

            return AnalyticDefinitionList.GetDisplayName(item.Widget.ResultType, item.Widget.Code, fieldNames, item.Widget.Grouping);
        }

        private static List<string> ResolveItemTrackerIds(DashboardItem item)
        {
            if (item.Type == DashboardWidgetTypes.Entries)
                return item.EntriesWidget != null ? [item.EntriesWidget.TrackerId] : [];

            return item.Sources
                .Where(s => s.WidgetSource != null)
                .Select(s => s.WidgetSource!.TrackerId)
                .Distinct()
                .ToList();
        }

        private static DashboardWidgetLayoutDto MapToLayoutDto(DashboardItem i) => new()
        {
            X = i.X,
            Y = i.Y,
            W = i.W,
            H = i.H,
            DisplayMode = i.DisplayMode
        };

        private static DashboardWidgetLayoutDto MapToMobileLayoutDto(DashboardItem i) => new()
        {
            X = i.MobileX,
            Y = i.MobileY,
            W = i.MobileW,
            H = i.MobileH,
            DisplayMode = i.MobileDisplayMode
        };

        private static DashboardWidgetDto MapToWidgetDto(
            DashboardItem item,
            AnalyticDto? analytic,
            QuickAddTrackerDto? quickAddTracker = null,
            string? trackerColor = null,
            EntriesWidgetDto? entriesWidget = null,
            FilterWidgetDto? filter = null,
            bool honorColorOverride = true) => new()
        {
            Id = item.Id,
            Type = item.Type,
            ParentItemId = item.ParentItemId,
            ParentTabId = item.ParentTabId,
            Layout = MapToLayoutDto(item),
            MobileLayout = MapToMobileLayoutDto(item),
            Config = item.Config,
            Analytic = analytic,
            QuickAddTracker = quickAddTracker,
            Filter = filter,
            EntriesWidget = entriesWidget,
            TrackerColor = trackerColor,
            Color = honorColorOverride ? item.Color : null
        };

        private static QuickAddWidgetConfigDto? TryParseQuickAddConfig(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                return JsonSerializer.Deserialize<QuickAddWidgetConfigDto>(config, ConfigJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static FilterWidgetConfigDto? TryParseFilterConfig(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                var parsed = JsonSerializer.Deserialize<FilterWidgetConfigDto>(config, ConfigJsonOptions);
                if (parsed == null)
                    return null;
                if (parsed.Slots.Count > 0)
                    return parsed;

                // Pre-slot config: fold the parallel QueryIds list into slots.
                var legacy = JsonSerializer.Deserialize<LegacyFilterConfig>(config, ConfigJsonOptions);
                return legacy?.QueryIds is { Count: > 0 }
                    ? FromLegacyFilterConfig(legacy)
                    : parsed;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private sealed class LegacyFilterConfig
        {
            public List<string>? QueryIds { get; set; }
            public Dictionary<string, string?>? ValueByQuery { get; set; }
            public List<WidgetLinkDto>? Links { get; set; }
            public List<string>? PresetIds { get; set; }
        }

        // A unique clause keeps its pooled query id as slot id so existing references still
        // resolve; only genuinely duplicated clauses get a suffixed id.
        private static FilterWidgetConfigDto FromLegacyFilterConfig(LegacyFilterConfig legacy)
        {
            var queryIds = legacy.QueryIds!;
            var duplicated = queryIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

            var slots = queryIds
                .Select((id, i) => new FilterClauseSlotDto
                {
                    SlotId = duplicated.Contains(id) ? $"{id}~{i}" : id,
                    QueryId = id
                })
                .ToList();

            var slotByQuery = new Dictionary<string, string>();
            foreach (var slot in slots)
                slotByQuery.TryAdd(slot.QueryId, slot.SlotId);

            Dictionary<string, TValue> Remap<TValue>(IEnumerable<KeyValuePair<string, TValue>>? map) =>
                (map ?? [])
                    .Where(kv => slotByQuery.ContainsKey(kv.Key))
                    .ToDictionary(kv => slotByQuery[kv.Key], kv => kv.Value);

            return new FilterWidgetConfigDto
            {
                Slots = slots,
                ValueBySlot = Remap(legacy.ValueByQuery),
                Links = (legacy.Links ?? [])
                    .Select(l => new WidgetLinkDto
                    {
                        ItemId = l.ItemId,
                        TrackerId = l.TrackerId,
                        FieldByQuery = Remap(l.FieldByQuery)
                    })
                    .ToList(),
                PresetIds = legacy.PresetIds ?? []
            };
        }

        private static TabsContainerConfigDto? TryParseTabsContainerConfig(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                return JsonSerializer.Deserialize<TabsContainerConfigDto>(config, ConfigJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static List<GoalConditionalTargetDto> ParseGoalConditionalTargets(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return [];

            try
            {
                return JsonSerializer.Deserialize<List<GoalConditionalTargetDto>>(json, ConfigJsonOptions) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        // A clause the placement doesn't follow isn't in here at all, so a conditional target
        // can never match on a filter it was never connected to.
        private static Dictionary<string, string?> ConnectedFilterValues(
            string itemId, IEnumerable<FilterWidgetConfigDto> filterConfigs)
        {
            var values = new Dictionary<string, string?>();
            foreach (var config in filterConfigs)
            {
                var followed = config.Links
                    .Where(l => l.ItemId == itemId)
                    .SelectMany(l => l.FieldByQuery.Keys)
                    .ToHashSet();

                foreach (var slot in config.Slots)
                {
                    if (!followed.Contains(slot.SlotId)) continue;
                    var value = config.ValueBySlot.GetValueOrDefault(slot.SlotId);
                    values[slot.SlotId] = value;
                    values.TryAdd(slot.QueryId, value);
                }
            }
            return values;
        }

        // A row with no conditions, or one naming a clause this placement no longer follows, never matches.
        private static string? ResolveGoalTarget(
            string? defaultTarget,
            List<GoalConditionalTargetDto> conditionalTargets,
            IReadOnlyDictionary<string, string?> connectedValues,
            IReadOnlyDictionary<string, string> clauseDataTypes,
            TimeZoneInfo tz)
        {
            foreach (var row in conditionalTargets)
            {
                if (row.Conditions.Count == 0)
                    continue;

                var matches = row.Conditions.All(condition =>
                    connectedValues.TryGetValue(condition.Key, out var current) &&
                    ConditionValueMatches(
                        current, condition.Value, clauseDataTypes.GetValueOrDefault(condition.Key), tz));

                if (matches)
                    return row.Target;
            }

            return defaultTarget;
        }

        // For a date/datetime clause, matches when both sides resolve to the same instant (or
        // same calendar day for a date), so "start of month" matches a literal first-of-month value.
        private static bool ConditionValueMatches(string? current, string? expected, string? dataType, TimeZoneInfo tz)
        {
            current ??= string.Empty;
            expected ??= string.Empty;

            if (string.Equals(current, expected, StringComparison.Ordinal))
                return true;

            if (dataType != DataTypes.Date && dataType != DataTypes.DateTime)
                return false;

            var currentInstant = DynamicDateTokens.ResolveValue(current, tz);
            var expectedInstant = DynamicDateTokens.ResolveValue(expected, tz);
            if (currentInstant is null || expectedInstant is null)
                return false;

            if (dataType == DataTypes.DateTime)
                return currentInstant.Value == expectedInstant.Value;

            return TimeZoneInfo.ConvertTimeFromUtc(currentInstant.Value, tz).Date
                == TimeZoneInfo.ConvertTimeFromUtc(expectedInstant.Value, tz).Date;
        }

        private static EntriesWidgetConfigDto? TryParseEntriesConfig(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                // Legacy placements stored { "viewId": "..." }; that key deserializes away,
                // leaving an empty column list, which renders as "every field".
                return JsonSerializer.Deserialize<EntriesWidgetConfigDto>(config, ConfigJsonOptions)
                    ?? new EntriesWidgetConfigDto();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private const int EntriesWidgetRowLimit = 25;

        private async Task<EntriesWidgetDto> BuildEntriesWidget(
            string itemId,
            EntriesWidget entriesWidget,
            EntriesWidgetConfigDto config,
            IEnumerable<FilterWidgetConfigDto> filterConfigs,
            IReadOnlyDictionary<string, Query> filterQueriesById,
            IReadOnlyDictionary<string, Field> selectorFieldsById,
            List<Field> trackerFields,
            EntrySetCache entryCache)
        {
            // Skips any field the tracker has since lost; falls back to every field when none resolve.
            var fieldsById = trackerFields.ToDictionary(f => f.Id);
            var columnFields = config.ColumnFieldIds
                .Where(fieldsById.ContainsKey)
                .Select(id => fieldsById[id])
                .ToList();
            if (columnFields.Count == 0)
                columnFields = trackerFields;

            var (followFilters, followSorts) = ResolveFilterClauses(
                itemId, entriesWidget.TrackerId, filterConfigs, filterQueriesById, selectorFieldsById);

            // Capped, so this shares an entry set only with another table filtered the same
            // way -- never with a chart, which reads the tracker uncapped.
            var entries = await entryCache.Get(
                entriesWidget.TrackerId, null, followFilters, followSorts, EntriesWidgetRowLimit);

            return new EntriesWidgetDto
            {
                RawName = string.IsNullOrWhiteSpace(entriesWidget.Name) ? null : entriesWidget.Name,
                TrackerId = entriesWidget.TrackerId,
                TrackerName = entriesWidget.Tracker.Name,
                Color = entriesWidget.Tracker.Color,
                Icon = entriesWidget.Tracker.Icon,
                Columns = mapper.Map<List<Field>, List<FieldDto>>(columnFields),
                Entries = mapper.Map<List<Entry>, List<EntryDto>>(entries)
            };
        }

        private static DashboardItemSourceDto MapSourceToDto(DashboardItem item, DashboardItemSource s)
        {
            var widgetSource = s.WidgetSource;
            if (widgetSource == null || item.Widget == null)
                return new DashboardItemSourceDto { Id = s.Id, Label = s.Label, ViewId = s.ViewId, Order = s.Order };

            var fields = widgetSource.Fields.Where(f => f.Field != null).ToList();

            return new DashboardItemSourceDto
            {
                Id = s.Id,
                Name = AnalyticDefinitionList.GetDisplayName(item.Widget.ResultType, item.Widget.Code, fields.Select(f => f.Field.Name), item.Widget.Grouping),
                Fields = fields
                    .Select(f => new DashboardItemSourceFieldDto { Purpose = f.Purpose, FieldId = f.FieldId, FieldName = f.Field.Name })
                    .ToList(),
                TrackerId = widgetSource.TrackerId,
                TrackerName = widgetSource.Tracker.Name,
                ViewId = s.ViewId,
                Label = s.Label,
                Order = s.Order
            };
        }
    }
}
