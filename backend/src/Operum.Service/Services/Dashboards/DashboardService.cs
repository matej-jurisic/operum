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
        // board and needs it recomputed from the same in-memory graph (SetViewSelectorSelection)
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

            // View selector widgets parsed once, plus every DashboardView on this board and
            // the pooled clause behind each of its queries, so the analytic loop can layer
            // the selected option's clauses on top of whatever fixed view a source reads
            // through -- resolved against the field each link maps the clause to.
            var selectorConfigsByItemId = items
                .Where(i => i.Type == DashboardWidgetTypes.ViewSelector)
                .Select(i => (ItemId: i.Id, Config: TryParseViewSelectorConfig(i.Config)))
                .Where(x => x.Config != null)
                .ToDictionary(x => x.ItemId, x => x.Config!);

            var dashboardViewsById = (await db.DashboardViews
                    .Where(dv => dv.DashboardId == dashboard.Id)
                    .Include(dv => dv.DashboardViewQueries.OrderBy(q => q.Order)).ThenInclude(q => q.Query)
                    .OrderBy(dv => dv.Order)
                    .ToListAsync())
                .ToDictionary(dv => dv.Id);

            // Every tracker field a selector link maps to, loaded up front — ApplyViewFilters
            // needs the field's Type to know how to filter on it.
            var selectorFieldIds = selectorConfigsByItemId.Values
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

                    ViewSelectorWidgetDto? viewSelector = null;
                    if (item.Type == DashboardWidgetTypes.ViewSelector &&
                        selectorConfigsByItemId.TryGetValue(item.Id, out var selectorConfig))
                    {
                        viewSelector = new ViewSelectorWidgetDto
                        {
                            SelectedId = selectorConfig.SelectedId,
                            Options = selectorConfig.OptionIds
                                .Where(dashboardViewsById.ContainsKey)
                                .Select(id => new ViewSelectorOptionDto { Id = id, Name = dashboardViewsById[id].Name })
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
                            selectorConfigsByItemId.Values, dashboardViewsById, selectorFieldsById,
                            currentUserService.GetCurrentUserTimeZone());
                    }

                    results.Add(MapToWidgetDto(item, null, quickAddTracker, viewSelector: viewSelector, entriesWidget: entriesWidget));
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

                    // Every view selector this widget follows narrows it further, ANDed on top
                    // of the fixed view above: the selected option's clauses, each run against
                    // the field the link maps that clause to on this source's tracker.
                    var (selFilters, selSorts) = ResolveSelectorClauses(
                        item.Id, widgetSource.TrackerId, selectorConfigsByItemId.Values,
                        dashboardViewsById, selectorFieldsById);

                    if (selFilters.Count > 0)
                        entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, selFilters, tz);
                    if (selSorts.Count > 0)
                        entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, selSorts);

                    var entries = await entriesQuery.ToListAsync();

                    var request = new AnalyticResultBuilderRequest
                    {
                        // A placement has no Analytic row of its own, so the pipeline is fed
                        // a transient one built from the shared widget's definition. The
                        // builders only read ResultType/Code/Id/Description.
                        Analytic = new Analytic
                        {
                            Id = source.Id,
                            Code = item.Widget.Code,
                            ResultType = item.Widget.ResultType
                        },
                        Entries = entries,
                        FieldMap = BuildFieldMap(widgetSource)
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
                Expandable = dto.Expandable,
                MobileExpandable = dto.MobileExpandable,
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
                Expandable = dto.Expandable,
                MobileExpandable = dto.MobileExpandable,
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

        // A dropdown over a set of the board's DashboardViews. Carries no analytic definition
        // — just Config: the option ids, the current selection, and per following Analytic
        // widget the field each clause runs against. Everything worth checking is in
        // ValidateViewSelectorConfig.
        public async Task<Result<DashboardItemDto>> AddViewSelectorItem(string dashboardId, SaveViewSelectorItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            var validation = await ValidateViewSelectorConfig(dashboard, dto);
            if (!validation.IsSuccess)
                return Result.Failure(validation.StatusCode, validation.Messages);

            var config = JsonSerializer.Serialize(new ViewSelectorWidgetConfigDto
            {
                OptionIds = dto.OptionIds,
                SelectedId = dto.SelectedId,
                Links = dto.Links
            }, ConfigJsonOptions);

            var item = BuildLayoutItem(dashboard, dashboardId, DashboardWidgetTypes.ViewSelector, DashboardGrid.ViewSelectorSize, config);

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(MapToItemDto(item));
        }

        // Edits a view selector in place: its options, its current selection, and the full
        // set of widgets that follow it with their field maps. The payload stands for the
        // whole widget, and the whole board comes back recomputed since a changed selection
        // or map changes what every follower draws.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateViewSelectorItem(string dashboardId, string itemId, SaveViewSelectorItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.ViewSelector);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view selector"));

            var validation = await ValidateViewSelectorConfig(dashboard, dto);
            if (!validation.IsSuccess)
                return Result.Failure(validation.StatusCode, validation.Messages);

            item.Config = JsonSerializer.Serialize(new ViewSelectorWidgetConfigDto
            {
                OptionIds = dto.OptionIds,
                SelectedId = dto.SelectedId,
                Links = dto.Links
            }, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Changes what a view selector's dropdown is currently set to and persists it onto
        // the item's Config, so it's what every future load starts from. Returns the whole
        // board recomputed, since every widget the selector links re-filters by it.
        public async Task<Result<List<DashboardWidgetDto>>> SetViewSelectorSelection(string dashboardId, string itemId, SetViewSelectorSelectionDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.ViewSelector);
            var config = item != null ? TryParseViewSelectorConfig(item.Config) : null;
            if (item == null || config == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view selector"));

            if (!string.IsNullOrEmpty(dto.SelectedId) && !config.OptionIds.Contains(dto.SelectedId))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("selected option"));

            config.SelectedId = dto.SelectedId;
            item.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Shared by AddViewSelectorItem and UpdateViewSelectorItem: every option is a
        // DashboardView on this board, the selection is one of the options (or none), and
        // every link names an Analytic widget on this board, a tracker it reads from, and a
        // real field of that tracker of the right data type for every clause across the
        // selected options.
        private async Task<Result> ValidateViewSelectorConfig(Dashboard dashboard, SaveViewSelectorItemDto dto)
        {
            var optionIds = dto.OptionIds.Distinct().ToList();
            if (optionIds.Count == 0)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Required("option"));

            var dashboardViews = await db.DashboardViews
                .Where(dv => dv.DashboardId == dashboard.Id && optionIds.Contains(dv.Id))
                .Include(dv => dv.DashboardViewQueries).ThenInclude(q => q.Query)
                .ToListAsync();

            if (dashboardViews.Count != optionIds.Count)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("view selector option"));

            if (!string.IsNullOrEmpty(dto.SelectedId) && !optionIds.Contains(dto.SelectedId))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("selected option"));

            var queriesById = dashboardViews
                .SelectMany(dv => dv.DashboardViewQueries.Select(q => q.Query))
                .DistinctBy(q => q.Id)
                .ToDictionary(q => q.Id);

            var seenLinks = new HashSet<string>();
            var fieldIds = dto.Links.SelectMany(l => l.FieldByQuery.Values).Distinct().ToList();
            var fields = fieldIds.Count > 0
                ? await db.Fields.Where(f => fieldIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id)
                : new Dictionary<string, Field>();

            foreach (var link in dto.Links)
            {
                if (!seenLinks.Add($"{link.ItemId}|{link.TrackerId}"))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("duplicate view selector link"));

                var target = dashboard.Items.FirstOrDefault(i => i.Id == link.ItemId);
                if (target == null ||
                    (target.Type != DashboardWidgetTypes.Analytic && target.Type != DashboardWidgetTypes.Entries))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("widget to link"));

                if (!ResolveItemTrackerIds(target).Contains(link.TrackerId))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("tracker for this widget"));

                foreach (var (queryId, fieldId) in link.FieldByQuery)
                {
                    if (!queriesById.TryGetValue(queryId, out var query))
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("clause for this view selector"));

                    if (!fields.TryGetValue(fieldId, out var field) ||
                        field.TrackerId != link.TrackerId ||
                        !DataTypes.AreCompatible(query.DataType, field.Type))
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("field mapping for this view selector"));
                }
            }

            return Result.Success();
        }

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
                Expandable = dto.Expandable,
                MobileExpandable = dto.MobileExpandable
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
                Expandable = dto.Expandable,
                MobileExpandable = dto.MobileExpandable
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
        // SetViewSelectorSelection, since a changed view changes what the chart draws.
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

            item.Expandable = dto.Expandable;
            item.MobileExpandable = dto.MobileExpandable;

            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Edits an Entries widget's placement in place: only which columns it shows and
        // whether it collapses to a button on each grid — the tracker it reads from lives on
        // the EntriesWidget and is fixed the same way an Analytic widget's definition is (see
        // UpdateDashboardItem), and how it's filtered comes only from the view selectors it
        // follows. Returns the whole board recomputed, the same as SetViewSelectorSelection,
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
            item.Expandable = dto.Expandable;
            item.MobileExpandable = dto.MobileExpandable;

            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Changes what a Header or Note widget's text reads, persisted the same way a View
        // widget's selection is. Unlike that one, nothing else on the board ever depends on
        // this widget's Config, so there's no need to recompute the whole board back — the
        // one item that changed is all the caller needs.
        public async Task<Result<DashboardItemDto>> SetTextWidgetContent(string dashboardId, string itemId, SetTextWidgetContentDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i =>
                i.Id == itemId && (i.Type == DashboardWidgetTypes.Header || i.Type == DashboardWidgetTypes.Note));
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("text widget"));

            // The two widgets share this endpoint but not their length cap — a header stays
            // short, a note gets a paragraph's worth of room.
            var maxLength = item.Type == DashboardWidgetTypes.Header
                ? DataLimits.MaxHeaderTextLength
                : DataLimits.MaxNoteTextLength;

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

            foreach (var placement in dto.Items)
            {
                var item = dashboard.Items.FirstOrDefault(x => x.Id == placement.ItemId);
                if (item == null) continue;

                ApplyPlacement(item, dto.Variant, placement.X, placement.Y, placement.W, placement.H);
            }

            // Order no longer decides where an item sits, but it still decides which widget
            // a client without the grid reads first, so keep it as the board's reading
            // order instead of letting it drift away from what the user arranged.
            //
            // Only the wide grid gets a say in it. The two arrangements can disagree about
            // what comes first, and letting whichever screen was used last rewrite the order
            // would make it flip back and forth; the desktop board is the one that has the
            // room to express an order in the first place.
            if (dto.Variant == DashboardLayoutVariants.Desktop)
            {
                var order = 0;
                foreach (var item in dashboard.Items.OrderBy(i => i.Y).ThenBy(i => i.X))
                    item.Order = order++;
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

        // The clauses every view selector on the board contributes to one widget's
        // (item, tracker) pair: for each selector currently set to an option, the option's
        // DashboardView clauses run against the field the matching link maps each one to.
        // Shared by the analytic source loop and BuildEntriesWidget so both narrow the same
        // way. Filters all AND together; sorts keep the order they resolve in. The caller
        // hands the results to ViewQueryBuilder.ApplyViewFilters / ApplyViewSorting.
        private static (List<ResolvedClause> Filters, List<ResolvedClause> Sorts) ResolveSelectorClauses(
            string itemId,
            string trackerId,
            IEnumerable<ViewSelectorWidgetConfigDto> selectorConfigs,
            IReadOnlyDictionary<string, DashboardView> dashboardViewsById,
            IReadOnlyDictionary<string, Field> selectorFieldsById)
        {
            var filters = new List<ResolvedClause>();
            var sorts = new List<ResolvedClause>();

            foreach (var selectorConfig in selectorConfigs)
            {
                if (string.IsNullOrEmpty(selectorConfig.SelectedId) ||
                    !dashboardViewsById.TryGetValue(selectorConfig.SelectedId, out var selectedView))
                    continue;

                var link = selectorConfig.Links.FirstOrDefault(l =>
                    l.ItemId == itemId && l.TrackerId == trackerId);
                if (link == null) continue;

                foreach (var dvq in selectedView.DashboardViewQueries.OrderBy(q => q.Order))
                {
                    if (!link.FieldByQuery.TryGetValue(dvq.QueryId, out var fieldId) ||
                        !selectorFieldsById.TryGetValue(fieldId, out var field))
                        continue;

                    if (dvq.Query.Kind == QueryKinds.Sort)
                        sorts.Add(new ResolvedClause(field.Id, field.Type, null, null, dvq.Query.Descending));
                    else
                        filters.Add(new ResolvedClause(field.Id, field.Type, dvq.Query.Operator, dvq.Query.Value, false));
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
            Layout = MapToLayoutDto(item),
            MobileLayout = MapToMobileLayoutDto(item),
            Config = item.Config,
            Name = ResolveItemName(item),
            TrackerIds = ResolveItemTrackerIds(item),
            ResultType = item.Widget?.ResultType ?? string.Empty,
            Code = item.Widget?.Code ?? string.Empty,
            MatchedValuesOnly = item.Widget?.MatchedValuesOnly ?? false,
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
            Expandable = i.Expandable
        };

        private static DashboardWidgetLayoutDto MapToMobileLayoutDto(DashboardItem i) => new()
        {
            X = i.MobileX,
            Y = i.MobileY,
            W = i.MobileW,
            H = i.MobileH,
            Expandable = i.MobileExpandable
        };

        private static DashboardWidgetDto MapToWidgetDto(
            DashboardItem item,
            AnalyticDto? analytic,
            QuickAddTrackerDto? quickAddTracker = null,
            string? trackerColor = null,
            ViewSelectorWidgetDto? viewSelector = null,
            EntriesWidgetDto? entriesWidget = null) => new()
        {
            Id = item.Id,
            Type = item.Type,
            Layout = MapToLayoutDto(item),
            MobileLayout = MapToMobileLayoutDto(item),
            Config = item.Config,
            Analytic = analytic,
            QuickAddTracker = quickAddTracker,
            ViewSelector = viewSelector,
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

        private static ViewSelectorWidgetConfigDto? TryParseViewSelectorConfig(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ViewSelectorWidgetConfigDto>(config, ConfigJsonOptions);
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
            IEnumerable<ViewSelectorWidgetConfigDto> selectorConfigs,
            IReadOnlyDictionary<string, DashboardView> dashboardViewsById,
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

            var (selFilters, selSorts) = ResolveSelectorClauses(
                itemId, entriesWidget.TrackerId, selectorConfigs, dashboardViewsById, selectorFieldsById);

            entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, selFilters, tz);
            entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, selSorts);

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
