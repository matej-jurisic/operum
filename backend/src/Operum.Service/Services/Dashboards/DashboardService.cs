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
    public class DashboardService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper, IWidgetsService widgetsService) : IDashboardService
    {
        // One source after it has been calculated, ready to be returned as-is or merged
        // with its siblings into a composed chart.
        private sealed record ResolvedSource(
            DashboardItemSource Source,
            string TrackerName,
            string? TrackerColor,
            AnalyticDto Result);

        // Config is written by hand rather than through the controller's own JSON
        // formatting, so it has to pick the same camelCase convention itself.
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

        // All-or-nothing, like reordering a tracker's fields: the payload must name exactly
        // the user's own dashboards, and Order is reassigned from its position in the list.
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

        // Shared by the plain read (GetDashboardWidgets) and anything that just wrote to the
        // board and needs it recomputed from the same in-memory graph (SetFilterValues)
        // — every source is recalculated on every call today regardless of what changed, so
        // there is nothing to reconcile between the two.
        private async Task<List<DashboardWidgetDto>> BuildWidgets(Dashboard dashboard)
        {
            var items = dashboard.Items.OrderBy(i => i.Order).ToList();
            var results = new List<DashboardWidgetDto>();

            // QuickAdd widgets only carry a trackerId in Config; resolve every one of them
            // up front in a single query so the client gets the button's name/color/icon
            // inline instead of each card fetching its own tracker after mounting.
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

            // Entries widgets keyed by their own item id, resolved into everything their
            // table needs below — see BuildEntriesWidget.
            var entriesConfigsByItemId = items
                .Where(i => i.Type == DashboardWidgetTypes.Entries)
                .Select(i => (ItemId: i.Id, Config: TryParseEntriesConfig(i.Config)))
                .Where(x => x.Config != null)
                .ToDictionary(x => x.ItemId, x => x.Config!);

            // Filter widgets parsed once, plus every DashboardView on this board and the
            // pooled clause behind each of its queries, so the analytic loop can layer a
            // filter widget's typed clauses on top of whatever fixed view a source reads
            // through, resolved against the field each link maps a clause to, and the read
            // below can expose each widget's matching-shape presets with their values.
            var filterConfigsByItemId = items
                .Where(i => i.Type == DashboardWidgetTypes.Filter)
                .Select(i => (ItemId: i.Id, Config: TryParseFilterConfig(i.Config)))
                .Where(x => x.Config != null)
                .ToDictionary(x => x.ItemId, x => x.Config!);

            // The pooled clause behind every filter widget's Config.QueryIds, so the loop
            // below can render its inputs and layer it onto the widgets it follows.
            var filterQueryIds = filterConfigsByItemId.Values
                .SelectMany(c => c.QueryIds)
                .Distinct()
                .ToList();

            var filterQueriesById = filterQueryIds.Count > 0
                ? await db.Queries.Where(q => filterQueryIds.Contains(q.Id)).ToDictionaryAsync(q => q.Id)
                : new Dictionary<string, Query>();

            var dashboardViewsById = (await db.DashboardViews
                    .Where(dv => dv.DashboardId == dashboard.Id)
                    .Include(dv => dv.DashboardViewQueries.OrderBy(q => q.Order)).ThenInclude(q => q.Query)
                    .OrderBy(dv => dv.Order)
                    .ToListAsync())
                .ToDictionary(dv => dv.Id);

            // Every tracker field a filter widget's clause link maps to, loaded up front —
            // ApplyViewFilters needs the field's Type to know how to filter on it.
            var selectorFieldIds = filterConfigsByItemId.Values
                .SelectMany(c => c.Links)
                .SelectMany(l => l.FieldByQuery.Values)
                .Distinct()
                .ToList();

            var selectorFieldsById = selectorFieldIds.Count > 0
                ? await db.Fields.Where(f => selectorFieldIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id)
                : new Dictionary<string, Field>();

            foreach (var item in items)
            {
                // A widget that isn't an analytic renders from its own Config alone, so it
                // never reaches the calculation below.
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
                        var widgetClauses = filterConfig.QueryIds
                            .Distinct()
                            .Where(filterQueriesById.ContainsKey)
                            .Select(id => filterQueriesById[id])
                            .Where(q => q.Kind == QueryKinds.Filter)
                            .ToList();

                        filter = new FilterWidgetDto
                        {
                            Clauses = widgetClauses
                                .Select(q => new FilterClauseDto
                                {
                                    QueryId = q.Id,
                                    Kind = q.Kind,
                                    DataType = q.DataType,
                                    Operator = q.Operator,
                                    Value = filterConfig.ValueByQuery.GetValueOrDefault(q.Id)
                                })
                                .ToList(),
                            // Only presets whose clause shape still matches the widget's are
                            // offered; each carries its value per clause in the widget's own
                            // clause order, so the card just fills those inputs.
                            Presets = filterConfig.PresetIds
                                .Distinct()
                                .Where(dashboardViewsById.ContainsKey)
                                .Select(id => (Id: id, Values: PresetValuesForShape(dashboardViewsById[id], widgetClauses)))
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
                            currentUserService.GetCurrentUserTimeZone());
                    }

                    results.Add(MapToWidgetDto(item, null, quickAddTracker,
                        entriesWidget: entriesWidget, filter: filter));
                    continue;
                }

                // No shared definition to render -- an orphaned or not-yet-migrated row.
                if (item.Widget == null) continue;

                var resolvedSources = new List<ResolvedSource>();

                foreach (var source in item.Sources.OrderBy(s => s.Order))
                {
                    var widgetSource = source.WidgetSource;
                    if (widgetSource == null) continue;

                    var tz = currentUserService.GetCurrentUserTimeZone();

                    View? view = null;
                    if (!string.IsNullOrEmpty(source.ViewId))
                    {
                        view = await db.Views
                            .Include(v => v.ViewQueries.OrderBy(vq => vq.Order)).ThenInclude(vq => vq.Query)
                            .Include(v => v.ViewQueries).ThenInclude(vq => vq.Field)
                            .FirstOrDefaultAsync(v => v.Id == source.ViewId && v.TrackerId == widgetSource.TrackerId);
                    }

                    var entriesQuery = db.Entries
                        .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                        .Where(e => e.TrackerId == widgetSource.TrackerId);

                    if (view != null)
                    {
                        entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.ResolveFilters(view), tz);
                        entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, ViewQueryBuilder.ResolveSorts(view));
                    }

                    // Every filter widget this widget follows narrows it further, ANDed on
                    // top of the fixed view above, using the values typed on the board -- a
                    // clause left blank is skipped rather than filtering on nothing.
                    var (followFilters, followSorts) = ResolveFilterClauses(
                        item.Id, widgetSource.TrackerId, filterConfigsByItemId.Values,
                        filterQueriesById, selectorFieldsById);

                    if (followFilters.Count > 0)
                        entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, followFilters, tz);
                    if (followSorts.Count > 0)
                        entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, followSorts);

                    var entries = await entriesQuery.ToListAsync();

                    // A correlation scatter has no per-source calculation of its own: each
                    // source is just a list of (match key -> value) pairs, which is exactly
                    // what a raw-values line chart produces. Compute it as one here and let
                    // MergeCorrelationResults join the two sides.
                    var isPaired = AnalyticTypes.RequiresPairedSources(item.Widget.ResultType, item.Widget.Code);

                    var request = new AnalyticResultBuilderRequest
                    {
                        // A placement has no Analytic row of its own, so the pipeline is fed
                        // a transient one built from the shared widget's definition. The
                        // builders only read ResultType/Code/Id/Description.
                        Analytic = new Analytic
                        {
                            Id = source.Id,
                            Code = isPaired ? AnalyticCodes.LineChart : item.Widget.Code,
                            ResultType = isPaired ? AnalyticTypes.LineChart : item.Widget.ResultType
                        },
                        Entries = entries,
                        FieldMap = isPaired ? PairedSourceFieldMap(widgetSource) : BuildFieldMap(widgetSource)
                    };

                    // Always displayable, even when the source's field(s) are missing or a
                    // calculated field's formula is broken: an explanatory card beats the
                    // widget silently disappearing, which left no way to find and remove it.
                    var data = AnalyticResultBuilder.GetDisplayableAnalyticResult(request);
                    resolvedSources.Add(new ResolvedSource(source, widgetSource.Tracker.Name, widgetSource.Tracker.Color, data));
                }

                // Only possible if every source's WidgetSource has gone missing somehow —
                // every source's own calculation now always resolves to something
                // displayable otherwise.
                if (resolvedSources.Count == 0) continue;

                // A single source renders exactly as it always has; combining only kicks in
                // once there's more than one source to merge into a shared chart.
                var itemResult = resolvedSources.Count == 1
                    ? resolvedSources[0].Result
                    : AnalyticTypes.RequiresPairedSources(item.Widget.ResultType, item.Widget.Code)
                        ? MergeCorrelationResults(resolvedSources)
                        : item.Widget.ResultType == AnalyticTypes.Calendar
                            ? MergeCalendarResults(resolvedSources)
                            : BuildComposedResult(resolvedSources, item.Widget.MatchedValuesOnly);

                // A single source placed with a label override reads on the board under that
                // name; otherwise the widget's own name -- editable from the Library, shared
                // by every placement -- wins over the calculation's own default label. A
                // widget that was never named falls all the way through to that default,
                // same as before.
                var singleSourceLabel = resolvedSources.Count == 1 ? resolvedSources[0].Source.Label : null;
                if (!string.IsNullOrWhiteSpace(singleSourceLabel))
                    itemResult.Name = singleSourceLabel;
                else if (!string.IsNullOrWhiteSpace(item.Widget.Name))
                    itemResult.Name = item.Widget.Name;

                // Use dashboard item ID so frontend can reference it for layout/remove
                itemResult.Id = item.Id;
                itemResult.Order = item.Order;

                // The y-axis anchoring is a placement choice, not part of the calculation,
                // so it is stamped onto the result here rather than threaded through the
                // analytic pipeline (which also serves tracker-level saved analytics).
                if (itemResult is LineChartAnalyticDto lineResult)
                    lineResult.YAxisFromZero = item.YAxisFromZero;
                else if (itemResult is ComposedChartAnalyticDto composedResult)
                    composedResult.YAxisFromZero = item.YAxisFromZero;

                // A widget owned by exactly one tracker is colored like that tracker; one
                // combining more than one falls back to the dashboard's own color, applied
                // client-side (see TrackerColor on DashboardWidgetDto).
                var distinctTrackerIds = resolvedSources.Select(r => r.Source.WidgetSource!.TrackerId).Distinct().ToList();
                var trackerColor = distinctTrackerIds.Count == 1 ? resolvedSources[0].TrackerColor : null;

                results.Add(MapToWidgetDto(item, itemResult, trackerColor: trackerColor));
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

        // Defines a new Widget Library chart (via WidgetsService, so it gets exactly the
        // same validation and reuse-ability as one built from the Library directly) and
        // places it on this board in the same call. The board's own capacity is checked
        // first, so a request that was never going to fit here doesn't spend a widget-count
        // slot in the Library on the way to failing.
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
                MatchedValuesOnly = dto.MatchedValuesOnly,
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

            // The two lists share the order they were built in (WidgetsService assigns
            // WidgetSource.Order from the same enumeration), so a source's placement
            // overrides line up with the definition it was submitted alongside.
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
                SourceOverrides = overrides
            });
        }

        // Places an existing Widget Library chart onto this board by reference: no copy is
        // made, so editing the widget afterwards -- from the Library, or from any other
        // dashboard placing it -- changes what this placement draws too.
        public async Task<Result<DashboardItemDto>> PlaceWidget(string dashboardId, PlaceWidgetDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var user = currentUserService.GetCurrentUser();
            var widget = await db.Widgets
                .Include(w => w.Sources).ThenInclude(s => s.Tracker)
                .Include(w => w.Sources).ThenInclude(s => s.Fields).ThenInclude(f => f.Field)
                .FirstOrDefaultAsync(w => w.Id == dto.WidgetId && w.OwnerId == user.Id);

            if (widget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("widget"));

            return await PlaceWidgetOnDashboard(dashboard, widget, dto);
        }

        // Shared by CreateAndPlaceWidget and PlaceWidget once each has settled on which
        // Widget is being placed: validates the placement-only overrides and inserts one
        // DashboardItem + one DashboardItemSource per WidgetSource, carrying nothing but a
        // reference back to the shared definition.
        private async Task<Result<DashboardItemDto>> PlaceWidgetOnDashboard(Dashboard dashboard, Widget widget, PlaceWidgetDto dto)
        {
            var widgetSourceIds = widget.Sources.Select(s => s.Id).ToHashSet();
            var overridesBySourceId = dto.SourceOverrides.ToDictionary(o => o.WidgetSourceId);

            // Every override must actually name one of this widget's sources — otherwise
            // the caller has stale data (the widget changed since it was picked) or the
            // wrong widget id entirely.
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

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            // A new widget starts on its own row under everything already on the board, at
            // the size its chart type reads well at. The user moves it from there.
            var (width, height) = DashboardGrid.DefaultSizeFor(widget.ResultType);
            var nextRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Y + i.H) : 0;

            // Both grids are placed at once, so neither arrangement has a hole in it the
            // first time the board is opened on the other kind of screen. On a phone there
            // is no room to put anything beside anything else, so a new widget takes the
            // full width of the narrow grid and keeps the height its chart type wants.
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

            var item = new DashboardItem
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
                Sources = sources
            };

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            var sourceDtos = sources.Select(s =>
            {
                var widgetSource = widget.Sources.First(ws => ws.Id == s.WidgetSourceId);
                var fields = widgetSource.Fields.Where(f => f.Field != null).ToList();

                return new DashboardItemSourceDto
                {
                    Id = s.Id,
                    Name = AnalyticDefinitionList.GetDisplayName(widget.ResultType, widget.Code, fields.Select(f => f.Field.Name)),
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
                MatchedValuesOnly = widget.MatchedValuesOnly,
                YAxisFromZero = item.YAxisFromZero,
                Sources = sourceDtos
            });
        }

        // A button that opens a tracker's quick-add entry dialog from the board. Unlike a
        // chart widget this carries no analytic definition — access to the tracker is the
        // only thing worth checking before it is placed.
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

        // Checks a Filter widget's follower links: every link names an Analytic/Entries
        // widget on this board, a tracker it reads from, and — for every clause it maps — a
        // real field of that tracker whose data type the clause allows. `label` is folded
        // into the error messages so the failures read naturally.
        private async Task<Result> ValidateFollowerLinks(
            Dashboard dashboard,
            List<WidgetLinkDto> links,
            IReadOnlyDictionary<string, Query> queriesById,
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

                var target = dashboard.Items.FirstOrDefault(i => i.Id == link.ItemId);
                if (target == null ||
                    (target.Type != DashboardWidgetTypes.Analytic && target.Type != DashboardWidgetTypes.Entries))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("widget to link"));

                if (!ResolveItemTrackerIds(target).Contains(link.TrackerId))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("tracker for this widget"));

                foreach (var (queryId, fieldId) in link.FieldByQuery)
                {
                    if (!queriesById.TryGetValue(queryId, out var query))
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

        // The widget owns a set of filter clauses outright (pooled into Query rows here),
        // each carrying the value it starts out filtering on, and offers the board's
        // DashboardViews whose clause shape matches as presets. Everything worth checking is
        // in BuildFilterConfig.
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

        // Edits a filter widget in place: its clauses, its presets and the full set of
        // widgets that follow it. Values the clauses are currently set to are preserved
        // across the edit (see below). The whole board comes back recomputed, since a
        // changed clause changes what every follower draws.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateFilterItem(string dashboardId, string itemId, SaveFilterItemDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Filter);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("filter widget"));

            var built = await BuildFilterConfig(dashboard, user.Id, dto);
            if (!built.IsSuccess)
                return Result.Failure(built.StatusCode, built.Messages);

            // The edit form only carries clause shape, never the values the clauses are
            // currently set to -- those are typed on the board (SetFilterValues) and live
            // only in ValueByQuery. Carry them across for every clause that survived the
            // edit: an unchanged clause shape pools to the same Query id, so a value whose
            // id still appears in the rebuilt config is still valid for that clause.
            var previous = TryParseFilterConfig(item.Config);
            if (previous != null)
            {
                var surviving = built.Data!.QueryIds.ToHashSet();
                foreach (var (queryId, value) in previous.ValueByQuery)
                    if (!string.IsNullOrEmpty(value) && surviving.Contains(queryId))
                        built.Data!.ValueByQuery[queryId] = value;
            }

            item.Config = JsonSerializer.Serialize(built.Data, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Changes the values a filter widget's clauses are currently set to and persists
        // them onto the item's Config, so they're what every future load starts from. Returns
        // the whole board recomputed, since every widget the filter links re-filters by it.
        public async Task<Result<List<DashboardWidgetDto>>> SetFilterValues(string dashboardId, string itemId, SetFilterValuesDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Filter);
            var config = item != null ? TryParseFilterConfig(item.Config) : null;
            if (item == null || config == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("filter widget"));

            var queries = await db.Queries
                .Where(q => config.QueryIds.Contains(q.Id))
                .ToListAsync();

            var valueCheck = ValidateFilterValues(queries, dto.Values);
            if (!valueCheck.IsSuccess)
                return Result.Failure(valueCheck.StatusCode, valueCheck.Messages);

            config.ValueByQuery = NormalizeValues(dto.Values);
            item.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Resolves a SaveFilterItemDto into the Config the widget stores.
        //
        // Clauses: the sent clauses pooled into Query rows (ResolveDashboardViewClauses
        // validates them and enforces the filter/query limits), each link's index-keyed
        // FieldByQuery rewritten to those pooled ids and checked against its follower, and
        // the starting value per clause.
        //
        // Presets: every id must be a DashboardView on this board whose filter clauses, in
        // order, are the same (data type, operator) list as the widget's clauses -- a preset
        // is just a named set of values for this exact clause set.
        private async Task<Result<FilterWidgetConfigDto>> BuildFilterConfig(Dashboard dashboard, string ownerId, SaveFilterItemDto dto)
        {
            var resolved = await ResolveDashboardViewClauses(ownerId, dto.Clauses);
            if (resolved.IsFailure)
                return Result.Failure(resolved.StatusCode, resolved.Messages);

            // QueryIds runs parallel to dto.Clauses; a link's FieldByQuery keys are clause
            // indices into that list, rewritten here to the pooled id each clause resolved to.
            var queryIds = resolved.Data!.Select(q => q.Id).ToList();
            var queriesById = resolved.Data!.DistinctBy(q => q.Id).ToDictionary(q => q.Id);

            var mappedLinks = new List<WidgetLinkDto>();
            foreach (var link in dto.Links)
            {
                var fieldByQuery = new Dictionary<string, string>();
                foreach (var (key, fieldId) in link.FieldByQuery)
                {
                    if (!int.TryParse(key, out var index) || index < 0 || index >= queryIds.Count)
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("clause for this filter widget"));
                    fieldByQuery[queryIds[index]] = fieldId;
                }
                mappedLinks.Add(new WidgetLinkDto
                {
                    ItemId = link.ItemId,
                    TrackerId = link.TrackerId,
                    FieldByQuery = fieldByQuery
                });
            }

            var linkCheck = await ValidateFollowerLinks(dashboard, mappedLinks, queriesById, "filter widget");
            if (linkCheck.IsFailure)
                return Result.Failure(linkCheck.StatusCode, linkCheck.Messages);

            var valueByQuery = new Dictionary<string, string?>();
            for (var i = 0; i < queryIds.Count; i++)
            {
                var value = dto.Clauses[i].Value;
                if (!string.IsNullOrEmpty(value))
                    valueByQuery[queryIds[i]] = value;
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

                // A preset may only be offered if its clause shape still matches this
                // widget's exactly -- it is a value set for this clause set, nothing else.
                if (presetViews.Any(v => PresetValuesForShape(v, resolved.Data!) == null))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("preset for this filter widget's clauses"));
            }

            return Result.Success(new FilterWidgetConfigDto
            {
                QueryIds = queryIds,
                ValueByQuery = valueByQuery,
                Links = mappedLinks,
                PresetIds = presetIds
            });
        }

        // Every key names a filter clause the widget holds; every non-empty value parses for
        // that clause's operator and data type (the same check the clause editor runs).
        private static Result ValidateFilterValues(IEnumerable<Query> queries, Dictionary<string, string?> values)
        {
            var filtersById = queries
                .Where(q => q.Kind == QueryKinds.Filter)
                .DistinctBy(q => q.Id)
                .ToDictionary(q => q.Id);

            foreach (var (queryId, value) in values)
            {
                if (!filtersById.TryGetValue(queryId, out var query))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("clause for this filter widget"));

                if (string.IsNullOrEmpty(value))
                    continue;

                var check = QueryBuilder.ValidateClause(QueryKinds.Filter, query.DataType, query.Operator, value, false);
                if (check.IsFailure)
                    return Result.Failure(check.StatusCode, check.Messages);
            }

            return Result.Success();
        }

        // Drops blank entries so an unset clause is simply absent from Config rather than
        // stored as an empty string.
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

        // Validates every clause and resolves it to a pooled Query (created unsaved if new).
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

        // Defines a new Widget Library Entries table and places it on this board in the
        // same call, the Entries equivalent of CreateAndPlaceWidget.
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

        // Places an existing Widget Library Entries table onto this board by reference —
        // the Entries equivalent of PlaceWidget.
        public async Task<Result<DashboardItemDto>> PlaceEntriesWidget(string dashboardId, PlaceEntriesWidgetDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var user = currentUserService.GetCurrentUser();
            var entriesWidget = await db.EntriesWidgets.FirstOrDefaultAsync(w => w.Id == dto.EntriesWidgetId && w.OwnerId == user.Id);
            if (entriesWidget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("entries widget"));

            return await PlaceEntriesWidgetOnDashboard(dashboard, entriesWidget, dto);
        }

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

        // A short line of text read as a section title. Unlike every other Add*Item this
        // carries no tracker at all — there is nothing to check but the board's own item
        // limit before it is placed.
        public async Task<Result<DashboardItemDto>> AddHeaderItem(string dashboardId, AddDashboardHeaderItemDto dto)
        {
            return await AddTextItem(dashboardId, DashboardWidgetTypes.Header, DashboardGrid.HeaderSize, dto.Text);
        }

        // A free-form block of text. Same shape as AddHeaderItem, just a different type and
        // a card-sized footprint instead of a full row.
        public async Task<Result<DashboardItemDto>> AddNoteItem(string dashboardId, AddDashboardNoteItemDto dto)
        {
            return await AddTextItem(dashboardId, DashboardWidgetTypes.Note, DashboardGrid.NoteSize, dto.Text);
        }

        // A bare visual rule. Carries no Config at all — there is nothing about it to
        // configure — so it needs even less than AddTextItem checks for.
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

        // An empty panel to arrange other widgets inside. Starts with no Config -- its
        // content is whichever items are later dropped into it, and its title is set later
        // through SetTextWidgetContent -- so it needs nothing but the board's own item limit
        // checked before it is placed.
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

        // Shared by AddHeaderItem and AddNoteItem: both are nothing but a tracker-less
        // widget holding one string of Config, placed the same way a QuickAdd or View
        // widget is — its own row under everything already on the board, on both grids at
        // once.
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

        // The placement rules every non-analytic widget shares: its own row under
        // everything already on the board, at the size its kind reads well at, on both
        // grids at once.
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

        // Edits an analytic widget's placement in place, but only where editing is the
        // board's business: what this placement is called, and which view it reads
        // through. The shared definition (result type, code, field mapping) lives on the
        // Widget instead and isn't editable here — changing that is what the Widget
        // Library is for. Returns the whole board recomputed, the same as
        // SetFilterValues, since a changed view changes what the chart draws.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateDashboardItem(string dashboardId, string itemId, UpdateDashboardItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Analytic);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("analytic widget"));

            // All-or-nothing, the way reordering a tracker's fields is: the payload stands
            // for the whole widget, so it has to name every source exactly once before a
            // label or a view left out of it can mean "cleared" rather than "left alone".
            var suppliedIds = dto.Sources.Select(s => s.SourceId).ToList();
            if (suppliedIds.Count != suppliedIds.Distinct().Count() ||
                !item.Sources.Select(s => s.Id).ToHashSet().SetEquals(suppliedIds))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Required("every source of this widget, exactly once"));

            // Validated in full before anything is written, so a rejected edit never leaves
            // the widget half changed.
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

                // A name of nothing but whitespace is no name: stored as none at all, so the
                // widget falls back to the definition's own label rather than showing a blank
                // title.
                source.Label = string.IsNullOrWhiteSpace(sourceDto.Label) ? null : sourceDto.Label.Trim();
                source.ViewId = string.IsNullOrEmpty(sourceDto.ViewId) ? null : sourceDto.ViewId;
            }

            item.DisplayMode = dto.DisplayMode;
            item.MobileDisplayMode = dto.MobileDisplayMode;
            item.YAxisFromZero = dto.YAxisFromZero;

            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Edits an Entries widget's placement in place: only which columns it shows and
        // whether it collapses to a button on each grid — the tracker it reads from lives on
        // the EntriesWidget and is fixed the same way an Analytic widget's definition is (see
        // UpdateDashboardItem), and how it's filtered comes only from the filter widgets
        // it follows. Returns the whole board recomputed, the same as SetFilterValues,
        // since a changed column set changes what the table shows.
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

            item.Config = JsonSerializer.Serialize(new EntriesWidgetConfigDto
            {
                ColumnFieldIds = columns.Data!
            }, ConfigJsonOptions);
            item.DisplayMode = dto.DisplayMode;
            item.MobileDisplayMode = dto.MobileDisplayMode;

            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Changes what a Header or Note widget's text reads, or a Container's title,
        // persisted the same way a View widget's selection is. Unlike that one, nothing else
        // on the board ever depends on this widget's Config, so there's no need to recompute
        // the whole board back — the one item that changed is all the caller needs.
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

            // These widgets share this endpoint but not their length cap — a note gets a
            // paragraph's worth of room; a header and a container title both stay short.
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
            // Their placement was relative to the container's own sub-grid (same column
            // count as the board), so offsetting the row by where the container sat drops
            // them roughly where they were; the client's compactor tidies the rest on the
            // next arrange.
            if (item.Type == DashboardWidgetTypes.Container)
            {
                foreach (var child in dashboard.Items.Where(i => i.ParentItemId == item.Id).ToList())
                {
                    child.ParentItemId = null;
                    child.Y += item.Y;
                    child.X = Math.Min(child.X, Math.Max(0, DashboardGrid.Columns - child.W));
                }
            }

            // Removes only this placement. The shared Widget/EntriesWidget it referenced --
            // if any -- is untouched and keeps rendering on every other dashboard it's
            // placed on; deleting the definition itself is the Widget Library's job.
            db.DashboardItems.Remove(item);
            await db.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> UpdateDashboardLayout(string dashboardId, UpdateDashboardLayoutDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            // Which items are containers others may be dropped into. A container can never
            // itself be nested, so it is not a candidate parent for one.
            var containerIds = dashboard.Items
                .Where(i => i.Type == DashboardWidgetTypes.Container)
                .Select(i => i.Id)
                .ToHashSet();

            foreach (var placement in dto.Items)
            {
                var item = dashboard.Items.FirstOrDefault(x => x.Id == placement.ItemId);
                if (item == null) continue;

                // The narrow grid flattens containers away, so a placement made there never
                // changes what an item's parent is on the wide grid.
                if (dto.Variant == DashboardLayoutVariants.Desktop)
                {
                    var wantsParent = placement.ParentItemId;
                    item.ParentItemId =
                        wantsParent != null
                        && wantsParent != item.Id
                        && item.Type != DashboardWidgetTypes.Container
                        && containerIds.Contains(wantsParent)
                            ? wantsParent
                            : null;
                }

                ApplyPlacement(item, dto.Variant, placement.X, placement.Y, placement.W, placement.H);
            }

            // Order no longer decides where an item sits, but it still decides which widget
            // a client without the grid reads first, so keep it as the board's reading
            // order instead of letting it drift away from what the user arranged.
            //
            // Only the wide grid gets a say in it. The two arrangements can disagree about
            // what comes first, and letting whichever screen was used last rewrite the order
            // would make it flip back and forth; the desktop board is the one that has the
            // room to express an order in the first place. A container's children follow it
            // in reading order, each block sorted top-left to bottom-right.
            if (dto.Variant == DashboardLayoutVariants.Desktop)
            {
                var childrenByParent = dashboard.Items
                    .Where(i => i.ParentItemId != null)
                    .GroupBy(i => i.ParentItemId!)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(c => c.Y).ThenBy(c => c.X).ToList());

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

            await db.SaveChangesAsync();
            return Result.Success();
        }

        // Clamps a placement to the grid the client is told to render. Out-of-bounds values
        // are pulled back in rather than rejected: refusing would throw away the rest of
        // the board's arrangement over one widget the client placed badly.
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

        // Whether a preset (DashboardView) still fits a filter widget's clause set: its
        // filter clauses, in order, must be the same (data type, operator) list as the
        // widget's. Returns the preset's value per clause in the widget's clause order when
        // it fits, so the card can drop those straight into its value inputs -- or null
        // when the shapes have drifted apart and the preset should no longer be offered.
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

        // The clauses every filter widget contributes to one widget's (item, tracker) pair.
        // The widget owns its clause set (Config.QueryIds) and the filter value is the one
        // typed on the board (Config.ValueByQuery). A filter whose value is blank is dropped
        // entirely -- the filter just hasn't been set yet -- unless its operator is one that
        // reads a blank as "is empty" / "has a value" on its own.
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

                foreach (var queryId in config.QueryIds.Distinct())
                {
                    if (!filterQueriesById.TryGetValue(queryId, out var query))
                        continue;

                    if (!link.FieldByQuery.TryGetValue(queryId, out var fieldId) ||
                        !selectorFieldsById.TryGetValue(fieldId, out var field))
                        continue;

                    if (query.Kind == QueryKinds.Sort)
                    {
                        sorts.Add(new ResolvedClause(field.Id, field.Type, null, null, query.Descending));
                        continue;
                    }

                    var value = config.ValueByQuery.GetValueOrDefault(queryId);

                    // A blank value only means something for the two equality operators
                    // ("is empty" / "has a value"); for anything else it means the filter
                    // is unset, so leave the clause off rather than filter on nothing.
                    if (string.IsNullOrEmpty(value) &&
                        query.Operator != OperatorTypes.EqualsOperator &&
                        query.Operator != OperatorTypes.NotEquals)
                        continue;

                    filters.Add(new ResolvedClause(field.Id, field.Type, query.Operator, value, false));
                }
            }

            return (filters, sorts);
        }

        // Validates a placement's chosen columns against its tracker: every id must be one of
        // the tracker's fields, duplicates collapse keeping first-seen order, and the list is
        // capped like a view's own columns. Empty in, empty out -- which the renderer reads
        // as "every field".
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

        // Purpose -> Field for the mappings that still resolve. Deleting a field cascades its
        // mapping away, so an incomplete map is possible here; the builder renders whatever
        // it can from what's left and GetDisplayableAnalyticResult falls back to an
        // explanatory placeholder otherwise, rather than the source disappearing.
        private static Dictionary<string, Field> BuildFieldMap(WidgetSource source) =>
            source.Fields
                .Where(f => f.Field != null)
                .ToDictionary(f => f.Purpose, f => f.Field);

        // A correlation source's Match/Value fields, presented to the line-chart pipeline as
        // its X/Y axes: the raw-values line chart it then produces is the (match key, value)
        // list MergeCorrelationResults joins on. A mapping that lost its field (deleted) is
        // dropped, leaving the line result without that axis and the merge with nothing to
        // pair -- handled the same way as any other missing analytic field.
        private static Dictionary<string, Field> PairedSourceFieldMap(WidgetSource source)
        {
            var byPurpose = BuildFieldMap(source);
            var map = new Dictionary<string, Field>();
            if (byPurpose.TryGetValue(AnalyticPurposes.Match, out var matchField))
                map[AnalyticPurposes.Xaxis] = matchField;
            if (byPurpose.TryGetValue(AnalyticPurposes.Value, out var valueField))
                map[AnalyticPurposes.Yaxis] = valueField;
            return map;
        }

        private static IQueryable<Dashboard> WithSourceGraph(IQueryable<Dashboard> query) => query
            .Include(d => d.Items).ThenInclude(i => i.Widget)
            // Include's lambda receives the navigation's own (nullable) CLR type -- it never
            // actually dereferences a null instance, Include just walks the expression tree.
            .Include(d => d.Items).ThenInclude(i => i.EntriesWidget).ThenInclude(w => w!.Tracker)
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.WidgetSource).ThenInclude(ws => ws!.Tracker)
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.WidgetSource).ThenInclude(ws => ws!.Fields).ThenInclude(f => f.Field);

        private async Task<Dashboard?> GetUserDashboard(string dashboardId)
        {
            var user = currentUserService.GetCurrentUser();
            return await WithSourceGraph(db.Dashboards)
                // The DbContext defaults to QueryTrackingBehavior.NoTracking (see
                // DatabaseConfiguration), which also skips identity resolution: if the same
                // Tracker is referenced by more than one source in this graph, each reference
                // materializes as a separate CLR instance. Every caller here either mutates
                // and calls SaveChanges (Update/Reorder), or hands the graph to Remove()
                // (Delete), both of which need this tracked and identity-resolved, the latter
                // otherwise throws when it tries to attach two same-key instances.
                .AsTracking()
                .FirstOrDefaultAsync(d => d.Id == dashboardId && d.UserId == user.Id);
        }

        // Merges 2+ per-source results (each computed independently by the same
        // single-tracker pipeline as always) into one multi-series chart. Every source shares
        // the widget's result type and code, so the series are always produced the same way;
        // what they can still differ in is the kind of value on the x-axis, which is surfaced
        // as a warning rather than rejected.
        private static ComposedChartAnalyticDto BuildComposedResult(List<ResolvedSource> resolvedSources, bool matchedValuesOnly)
        {
            var composed = new ComposedChartAnalyticDto();

            foreach (var resolved in resolvedSources)
            {
                var source = resolved.Source;
                ComposedChartSeriesDto? series = resolved.Result switch
                {
                    LineChartAnalyticDto line => new ComposedChartSeriesDto
                    {
                        Key = source.Id,
                        Label = source.Label ?? $"{resolved.TrackerName}: {line.YField.Name}",
                        RenderType = ComposedSeriesRenderTypes.Line,
                        XField = line.XField,
                        ValueField = line.YField,
                        Points = line.Points.Select(p => new ComposedChartPointDto { X = p.X, Y = p.Y }).ToList(),
                        Color = resolved.TrackerColor
                    },
                    BarChartAnalyticDto bar => new ComposedChartSeriesDto
                    {
                        Key = source.Id,
                        Label = source.Label ?? $"{resolved.TrackerName}: {bar.ValueField?.Name ?? "Count"}",
                        RenderType = ComposedSeriesRenderTypes.Bar,
                        XField = bar.NameField,
                        ValueField = bar.ValueField ?? new FieldDto { Name = "Count", Type = DataTypes.Number },
                        Points = bar.Points.Select(p => new ComposedChartPointDto { X = p.Name, Y = p.Value }).ToList(),
                        Color = resolved.TrackerColor
                    },
                    // Defensive only — WidgetsService.CreateWidget already rejects any other
                    // result type once there's more than one source.
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
        private static CalendarAnalyticDto MergeCalendarResults(List<ResolvedSource> resolvedSources)
        {
            var calendars = resolvedSources
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
                    TrackerName = string.IsNullOrWhiteSpace(c.Resolved.Source.Label)
                        ? c.Resolved.TrackerName
                        : c.Resolved.Source.Label,
                    Color = c.Resolved.TrackerColor
                }))
                .ToList();

            return merged;
        }

        // Joins two sources into one scatter plot: source A's value is the x of each point,
        // source B's the y, paired on every match key both sources have. Each side arrives
        // as a raw-values line chart (see PairedSourceFieldMap) -- X is the match key, Y the
        // value -- so the join is just an intersection of their keys. Repeat entries for a
        // key are averaged into the one value that key contributes.
        private static ScatterPlotAnalyticDto MergeCorrelationResults(List<ResolvedSource> resolvedSources)
        {
            var result = new ScatterPlotAnalyticDto();
            if (resolvedSources.Count < 2)
                return result;

            var xSource = resolvedSources[0];
            var ySource = resolvedSources[1];

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
        // placement gave the source a label of its own.
        private static FieldDto AxisField(FieldDto valueField, ResolvedSource source) => new()
        {
            Id = valueField.Id,
            Type = valueField.Type,
            Required = valueField.Required,
            Description = valueField.Description,
            Name = string.IsNullOrWhiteSpace(source.Source.Label)
                ? $"{source.TrackerName}: {valueField.Name}"
                : source.Source.Label
        };

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
            Layout = MapToLayoutDto(item),
            MobileLayout = MapToMobileLayoutDto(item),
            Config = item.Config,
            Name = ResolveItemName(item),
            TrackerIds = ResolveItemTrackerIds(item),
            ResultType = item.Widget?.ResultType ?? string.Empty,
            Code = item.Widget?.Code ?? string.Empty,
            MatchedValuesOnly = item.Widget?.MatchedValuesOnly ?? false,
            YAxisFromZero = item.YAxisFromZero,
            Sources = item.Sources.OrderBy(s => s.Order).Select(s => MapSourceToDto(item, s)).ToList()
        };

        // What a form elsewhere labels this item by. An Entries widget carries its own name;
        // an Analytic widget's placement name wins over its calculation's default label, and
        // an unnamed one falls through to that label the same way the board itself renders
        // it (see BuildWidgets). Everything else has no name to show.
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

            return AnalyticDefinitionList.GetDisplayName(item.Widget.ResultType, item.Widget.Code, fieldNames);
        }

        // Every tracker this item reads from — one for an Entries widget, the distinct set
        // across its sources for an Analytic widget, none for the kinds that read no tracker.
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
            FilterWidgetDto? filter = null) => new()
        {
            Id = item.Id,
            Type = item.Type,
            ParentItemId = item.ParentItemId,
            Layout = MapToLayoutDto(item),
            MobileLayout = MapToMobileLayoutDto(item),
            Config = item.Config,
            Analytic = analytic,
            QuickAddTracker = quickAddTracker,
            Filter = filter,
            EntriesWidget = entriesWidget,
            TrackerColor = trackerColor
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
                return JsonSerializer.Deserialize<FilterWidgetConfigDto>(config, ConfigJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static EntriesWidgetConfigDto? TryParseEntriesConfig(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                // Legacy placements stored { "viewId": "..." } here; that key is gone now and
                // deserializes away, leaving an empty column list -- which renders as "every
                // field", exactly the fallback those widgets had before.
                return JsonSerializer.Deserialize<EntriesWidgetConfigDto>(config, ConfigJsonOptions)
                    ?? new EntriesWidgetConfigDto();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // A board widget is a window onto a tracker's recent activity, not its own paginated
        // table -- this many rows is plenty without the card growing pagination of its own.
        private const int EntriesWidgetRowLimit = 25;

        // Resolves everything an Entries widget's table needs to render: the columns to show
        // in order (Config's ColumnFieldIds, or every field when it names none), and the rows
        // themselves -- filtered and sorted by whatever view selectors this placement follows,
        // then capped. Unlike the analytic pipeline this returns the entries directly rather
        // than an aggregate, so the card renders them without a fetch of its own. Which
        // tracker to read from is the EntriesWidget's own -- fixed at creation.
        private async Task<EntriesWidgetDto> BuildEntriesWidget(
            string itemId,
            EntriesWidget entriesWidget,
            EntriesWidgetConfigDto config,
            IEnumerable<FilterWidgetConfigDto> filterConfigs,
            IReadOnlyDictionary<string, Query> filterQueriesById,
            IReadOnlyDictionary<string, Field> selectorFieldsById,
            TimeZoneInfo tz)
        {
            var trackerFields = await db.Fields
                .Where(f => f.TrackerId == entriesWidget.TrackerId)
                .OrderBy(f => f.Order)
                .ToListAsync();

            // The chosen columns in the order Config stores them, skipping any the tracker
            // has since lost so a deleted field never breaks the table. Falls back to every
            // field when Config names none, or when none of the ones it names still resolve.
            var fieldsById = trackerFields.ToDictionary(f => f.Id);
            var columnFields = config.ColumnFieldIds
                .Where(fieldsById.ContainsKey)
                .Select(id => fieldsById[id])
                .ToList();
            if (columnFields.Count == 0)
                columnFields = trackerFields;

            var entriesQuery = db.Entries
                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == entriesWidget.TrackerId);

            var (followFilters, followSorts) = ResolveFilterClauses(
                itemId, entriesWidget.TrackerId, filterConfigs, filterQueriesById, selectorFieldsById);

            entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, followFilters, tz);
            entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, followSorts);

            var entries = await entriesQuery.Take(EntriesWidgetRowLimit).ToListAsync();

            return new EntriesWidgetDto
            {
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
                Name = AnalyticDefinitionList.GetDisplayName(item.Widget.ResultType, item.Widget.Code, fields.Select(f => f.Field.Name)),
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
