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
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Widgets;
using Operum.Model.DTOs.Widgets.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
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
        // board and needs it recomputed from the same in-memory graph (SetViewWidgetSelection)
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

            // View widgets keyed by their own item id — both to resolve their own dropdown
            // (tracker + its views) and to let an analytic source below resolve what its
            // LinkedViewWidgetId currently points at without a query per source.
            var viewConfigsByItemId = items
                .Where(i => i.Type == DashboardWidgetTypes.View)
                .Select(i => (ItemId: i.Id, Config: TryParseViewConfig(i.Config)))
                .Where(x => x.Config != null)
                .ToDictionary(x => x.ItemId, x => x.Config!);

            var viewTrackerIds = viewConfigsByItemId.Values.Select(c => c.TrackerId).Distinct().ToList();

            var viewTrackers = viewTrackerIds.Count > 0
                ? await db.Trackers.Where(t => viewTrackerIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id)
                : new Dictionary<string, Tracker>();

            var viewsByTracker = viewTrackerIds.Count > 0
                ? (await db.Views.Where(v => viewTrackerIds.Contains(v.TrackerId)).OrderBy(v => v.Order).ToListAsync())
                    .GroupBy(v => v.TrackerId)
                    .ToDictionary(g => g.Key, g => g.ToList())
                : new Dictionary<string, List<View>>();

            // Entries widgets keyed by their own item id, resolved into everything their
            // table needs below — see BuildEntriesWidget.
            var entriesConfigsByItemId = items
                .Where(i => i.Type == DashboardWidgetTypes.Entries)
                .Select(i => (ItemId: i.Id, Config: TryParseEntriesConfig(i.Config)))
                .Where(x => x.Config != null)
                .ToDictionary(x => x.ItemId, x => x.Config!);

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

                    ViewWidgetDto? viewWidget = null;
                    if (item.Type == DashboardWidgetTypes.View &&
                        viewConfigsByItemId.TryGetValue(item.Id, out var viewConfig) &&
                        viewTrackers.TryGetValue(viewConfig.TrackerId, out var viewTracker))
                    {
                        viewsByTracker.TryGetValue(viewConfig.TrackerId, out var trackerViews);
                        viewWidget = new ViewWidgetDto
                        {
                            TrackerId = viewTracker.Id,
                            TrackerName = viewTracker.Name,
                            Color = viewTracker.Color,
                            Icon = viewTracker.Icon,
                            ViewId = viewConfig.ViewId,
                            Views = (trackerViews ?? []).Select(v => new ViewOptionDto { Id = v.Id, Name = v.Name }).ToList()
                        };
                    }

                    EntriesWidgetDto? entriesWidget = null;
                    if (item.Type == DashboardWidgetTypes.Entries &&
                        item.EntriesWidget != null &&
                        entriesConfigsByItemId.TryGetValue(item.Id, out var entriesConfig))
                    {
                        entriesWidget = await BuildEntriesWidget(item.EntriesWidget, entriesConfig, viewConfigsByItemId);
                    }

                    results.Add(MapToWidgetDto(item, null, quickAddTracker, viewWidget: viewWidget, entriesWidget: entriesWidget));
                    continue;
                }

                // No shared definition to render -- an orphaned or not-yet-migrated row.
                if (item.Widget == null) continue;

                var resolvedSources = new List<ResolvedSource>();

                foreach (var source in item.Sources.OrderBy(s => s.Order))
                {
                    var widgetSource = source.WidgetSource;
                    if (widgetSource == null) continue;

                    // A linked source's filter comes from whatever its View widget is
                    // currently set to instead of its own (unset) ViewId — resolved from the
                    // dictionary above rather than a query per source.
                    var effectiveViewId = ResolveEffectiveViewId(source.ViewId, source.LinkedViewWidgetId, viewConfigsByItemId);

                    View? view = null;
                    if (!string.IsNullOrEmpty(effectiveViewId))
                    {
                        view = await db.Views
                            .Include(v => v.ViewQueries.OrderBy(vq => vq.Order)).ThenInclude(vq => vq.Query).ThenInclude(q => q.Field)
                            .FirstOrDefaultAsync(v => v.Id == effectiveViewId && v.TrackerId == widgetSource.TrackerId);
                    }

                    var entriesQuery = db.Entries
                        .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                        .Where(e => e.TrackerId == widgetSource.TrackerId);

                    if (view != null)
                    {
                        entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.ResolveFilters(view), currentUserService.GetCurrentUserTimeZone());
                        entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, ViewQueryBuilder.ResolveSorts(view));
                    }

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
                ViewId = input.ViewId,
                LinkedViewWidgetId = input.LinkedViewWidgetId
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

                if (!string.IsNullOrEmpty(over.LinkedViewWidgetId) &&
                    !IsLinkableViewWidget(dashboard, over.LinkedViewWidgetId, widgetSource.TrackerId))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("view widget for this tracker"));
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
                    ViewId = over?.ViewId,
                    LinkedViewWidgetId = over?.LinkedViewWidgetId
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
                    LinkedViewWidgetId = s.LinkedViewWidgetId,
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

        // A dropdown over one tracker's views. Unlike a chart widget this carries no
        // analytic definition either — access to the tracker, and that the starting
        // selection (if any) is actually one of its views, is all that needs checking.
        public async Task<Result<DashboardItemDto>> AddViewItem(string dashboardId, AddDashboardViewItemDto dto)
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

            if (!string.IsNullOrEmpty(dto.ViewId))
            {
                var exists = await db.Views.AnyAsync(v => v.Id == dto.ViewId && v.TrackerId == dto.TrackerId);
                if (!exists)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
            }

            var linkTargetIds = dto.LinkedItemIds.Distinct().ToList();
            if (linkTargetIds.Any(id => !IsLinkableTargetItem(dashboard, id, dto.TrackerId)))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("widget to link"));

            var item = BuildLayoutItem(dashboard, dashboardId, DashboardWidgetTypes.View, DashboardGrid.ViewSize,
                JsonSerializer.Serialize(new ViewWidgetConfigDto { TrackerId = dto.TrackerId, ViewId = dto.ViewId }, ConfigJsonOptions));

            db.DashboardItems.Add(item);
            // Saved first so the item has the Id the linked sources point at.
            await db.SaveChangesAsync();

            if (linkTargetIds.Count > 0)
            {
                ApplyViewWidgetLinks(dashboard, item.Id, dto.TrackerId, linkTargetIds, clearUnlisted: false);
                await db.SaveChangesAsync();
            }

            return Result.Success(MapToItemDto(item));
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
                ViewId = dto.ViewId,
                LinkedViewWidgetId = dto.LinkedViewWidgetId,
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
            if (!string.IsNullOrEmpty(dto.ViewId))
            {
                var exists = await db.Views.AnyAsync(v => v.Id == dto.ViewId && v.TrackerId == entriesWidget.TrackerId);
                if (!exists)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
            }

            if (!string.IsNullOrEmpty(dto.LinkedViewWidgetId) &&
                !IsLinkableViewWidget(dashboard, dto.LinkedViewWidgetId, entriesWidget.TrackerId))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("view widget for this tracker"));

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
                    ViewId = dto.ViewId,
                    LinkedViewWidgetId = dto.LinkedViewWidgetId
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
        // SetViewWidgetSelection, since a changed view changes what the chart draws.
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

                if (!string.IsNullOrEmpty(sourceDto.LinkedViewWidgetId) &&
                    !IsLinkableViewWidget(dashboard, sourceDto.LinkedViewWidgetId, trackerId))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("view widget for this tracker"));
            }

            foreach (var sourceDto in dto.Sources)
            {
                var source = item.Sources.First(s => s.Id == sourceDto.SourceId);

                // A name of nothing but whitespace is no name: stored as none at all, so the
                // widget falls back to the definition's own label rather than showing a blank
                // title.
                source.Label = string.IsNullOrWhiteSpace(sourceDto.Label) ? null : sourceDto.Label.Trim();
                source.ViewId = string.IsNullOrEmpty(sourceDto.ViewId) ? null : sourceDto.ViewId;
                source.LinkedViewWidgetId = string.IsNullOrEmpty(sourceDto.LinkedViewWidgetId) ? null : sourceDto.LinkedViewWidgetId;
            }

            item.Expandable = dto.Expandable;
            item.MobileExpandable = dto.MobileExpandable;

            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Changes what a View widget's dropdown is currently set to and persists it onto the
        // item's own Config, so it's what every future load starts from — not just this
        // session. Returns the whole board recomputed, the same as GetDashboardWidgets,
        // since every source linked to this widget needs to be re-filtered by it.
        public async Task<Result<List<DashboardWidgetDto>>> SetViewWidgetSelection(string dashboardId, string itemId, SetViewWidgetSelectionDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.View);
            var config = item != null ? TryParseViewConfig(item.Config) : null;
            if (item == null || config == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view widget"));

            if (!string.IsNullOrEmpty(dto.ViewId))
            {
                var exists = await db.Views.AnyAsync(v => v.Id == dto.ViewId && v.TrackerId == config.TrackerId);
                if (!exists)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
            }

            config.ViewId = dto.ViewId;
            item.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Edits a View widget in place: its starting/current selection, and the full set of
        // widgets on the board that follow it — the same thing the following widgets' own
        // forms can already set from their side, gathered here so a selector can be wired up
        // without opening each one. The payload is the whole set: a widget dropped from
        // LinkedItemIds is unlinked from this selector (a fixed view, or a link to a
        // different selector, is left alone). Returns the whole board recomputed, the same as
        // SetViewWidgetSelection.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateViewItem(string dashboardId, string itemId, UpdateDashboardViewItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.View);
            var config = item != null ? TryParseViewConfig(item.Config) : null;
            if (item == null || config == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view widget"));

            if (!string.IsNullOrEmpty(dto.ViewId))
            {
                var exists = await db.Views.AnyAsync(v => v.Id == dto.ViewId && v.TrackerId == config.TrackerId);
                if (!exists)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
            }

            var linkTargetIds = dto.LinkedItemIds.Distinct().ToList();
            if (linkTargetIds.Any(id => !IsLinkableTargetItem(dashboard, id, config.TrackerId)))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("widget to link"));

            ApplyViewWidgetLinks(dashboard, item.Id, config.TrackerId, linkTargetIds, clearUnlisted: true);

            config.ViewId = dto.ViewId;
            item.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // Edits an Entries widget's placement in place: only how it's filtered, and whether
        // it collapses to a button on each grid — the tracker it reads from lives on the
        // EntriesWidget and is fixed the same way an Analytic widget's definition is (see
        // UpdateDashboardItem). Returns the whole board recomputed, the same as
        // SetViewWidgetSelection, since a changed view changes what the table shows.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateEntriesItem(string dashboardId, string itemId, UpdateDashboardEntriesItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Entries);
            if (item?.EntriesWidget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("entries widget"));

            var trackerId = item.EntriesWidget.TrackerId;

            if (!string.IsNullOrEmpty(dto.ViewId))
            {
                var exists = await db.Views.AnyAsync(v => v.Id == dto.ViewId && v.TrackerId == trackerId);
                if (!exists)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
            }

            if (!string.IsNullOrEmpty(dto.LinkedViewWidgetId) &&
                !IsLinkableViewWidget(dashboard, dto.LinkedViewWidgetId, trackerId))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("view widget for this tracker"));

            item.Config = JsonSerializer.Serialize(new EntriesWidgetConfigDto
            {
                ViewId = string.IsNullOrEmpty(dto.ViewId) ? null : dto.ViewId,
                LinkedViewWidgetId = string.IsNullOrEmpty(dto.LinkedViewWidgetId) ? null : dto.LinkedViewWidgetId
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

        // A source may only follow a View widget that is on this same board and built for the
        // same tracker the source reads from, otherwise the widget's selection would name a
        // view that never applies to it. Looked up in the graph already loaded rather than
        // queried, since the widget has to be on this dashboard to begin with.
        private static bool IsLinkableViewWidget(Dashboard dashboard, string viewWidgetId, string trackerId)
        {
            var viewWidgetItem = dashboard.Items.FirstOrDefault(i => i.Id == viewWidgetId && i.Type == DashboardWidgetTypes.View);
            var viewWidgetConfig = viewWidgetItem != null ? TryParseViewConfig(viewWidgetItem.Config) : null;

            return viewWidgetConfig != null && viewWidgetConfig.TrackerId == trackerId;
        }

        // The reverse of IsLinkableViewWidget: whether `itemId` names an Analytic or Entries
        // widget on this board that a View selector for `trackerId` could point at — i.e. one
        // that actually reads from that tracker. What the add/edit View forms are choosing
        // from, checked again here since the request is the client's word for it.
        private static bool IsLinkableTargetItem(Dashboard dashboard, string itemId, string trackerId)
        {
            var target = dashboard.Items.FirstOrDefault(i => i.Id == itemId);
            if (target == null) return false;

            return ResolveItemTrackerIds(target).Contains(trackerId) &&
                target.Type is DashboardWidgetTypes.Analytic or DashboardWidgetTypes.Entries;
        }

        // Points every source that reads from `trackerId`, on each item named in
        // `linkedItemIds`, at the View widget `viewItemId` — dropping any fixed view it had.
        // With `clearUnlisted` (the edit path), a matching source on any *other* board item
        // that still follows this selector is unlinked, so the payload stands for the whole
        // set; the add path only ever adds. A source following a different selector, or
        // fixed to its own view, is never touched unless its item is in `linkedItemIds`.
        private static void ApplyViewWidgetLinks(
            Dashboard dashboard, string viewItemId, string trackerId,
            IReadOnlyCollection<string> linkedItemIds, bool clearUnlisted)
        {
            foreach (var target in dashboard.Items)
            {
                if (!ResolveItemTrackerIds(target).Contains(trackerId))
                    continue;

                var desired = linkedItemIds.Contains(target.Id);
                if (!desired && !clearUnlisted)
                    continue;

                if (target.Type == DashboardWidgetTypes.Analytic)
                {
                    foreach (var source in target.Sources.Where(s => s.WidgetSource?.TrackerId == trackerId))
                    {
                        if (desired)
                        {
                            source.ViewId = null;
                            source.LinkedViewWidgetId = viewItemId;
                        }
                        else if (source.LinkedViewWidgetId == viewItemId)
                        {
                            source.LinkedViewWidgetId = null;
                        }
                    }
                }
                else if (target.Type == DashboardWidgetTypes.Entries)
                {
                    var config = TryParseEntriesConfig(target.Config) ?? new EntriesWidgetConfigDto();

                    if (desired)
                    {
                        config.ViewId = null;
                        config.LinkedViewWidgetId = viewItemId;
                    }
                    else if (config.LinkedViewWidgetId == viewItemId)
                    {
                        config.LinkedViewWidgetId = null;
                    }
                    else
                    {
                        continue;
                    }

                    target.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
                }
            }
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
            ViewWidgetDto? viewWidget = null,
            EntriesWidgetDto? entriesWidget = null) => new()
        {
            Id = item.Id,
            Type = item.Type,
            Layout = MapToLayoutDto(item),
            MobileLayout = MapToMobileLayoutDto(item),
            Config = item.Config,
            Analytic = analytic,
            QuickAddTracker = quickAddTracker,
            ViewWidget = viewWidget,
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

        private static ViewWidgetConfigDto? TryParseViewConfig(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ViewWidgetConfigDto>(config, ConfigJsonOptions);
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
                return JsonSerializer.Deserialize<EntriesWidgetConfigDto>(config, ConfigJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // A source's filter is either its own ViewId, or (once that's empty) whatever a
        // linked View widget on the same board is currently set to — resolved from the
        // dictionary BuildWidgets already built rather than a query per caller. Shared by an
        // analytic source and an Entries widget, the only two things that carry this duality.
        private static string? ResolveEffectiveViewId(string? viewId, string? linkedViewWidgetId, Dictionary<string, ViewWidgetConfigDto> viewConfigsByItemId)
        {
            if (!string.IsNullOrEmpty(linkedViewWidgetId) &&
                viewConfigsByItemId.TryGetValue(linkedViewWidgetId, out var linkedConfig))
            {
                return linkedConfig.ViewId;
            }

            return viewId;
        }

        // Resolves everything an Entries widget's table needs to render: the view it's
        // currently filtered by, and the columns that view wants shown, in its order. A
        // view naming no columns (or no view at all) shows every field, the same fallback
        // the tracker page's own column picker uses. Which tracker to read from is the
        // EntriesWidget's own -- fixed at creation, not part of the placement's Config.
        private async Task<EntriesWidgetDto> BuildEntriesWidget(EntriesWidget entriesWidget, EntriesWidgetConfigDto config, Dictionary<string, ViewWidgetConfigDto> viewConfigsByItemId)
        {
            var effectiveViewId = ResolveEffectiveViewId(config.ViewId, config.LinkedViewWidgetId, viewConfigsByItemId);

            var columnFields = new List<Field>();
            if (!string.IsNullOrEmpty(effectiveViewId))
            {
                var view = await db.Views
                    .Include(v => v.ViewColumns.OrderBy(vc => vc.Order)).ThenInclude(vc => vc.Field)
                    .FirstOrDefaultAsync(v => v.Id == effectiveViewId && v.TrackerId == entriesWidget.TrackerId);

                if (view != null && view.ViewColumns.Count > 0)
                    columnFields = view.ViewColumns.OrderBy(vc => vc.Order).Select(vc => vc.Field).ToList();
            }

            if (columnFields.Count == 0)
                columnFields = await db.Fields.Where(f => f.TrackerId == entriesWidget.TrackerId).OrderBy(f => f.Order).ToListAsync();

            return new EntriesWidgetDto
            {
                TrackerId = entriesWidget.TrackerId,
                TrackerName = entriesWidget.Tracker.Name,
                Color = entriesWidget.Tracker.Color,
                Icon = entriesWidget.Tracker.Icon,
                ViewId = effectiveViewId,
                Columns = mapper.Map<List<Field>, List<FieldDto>>(columnFields)
            };
        }

        private static DashboardItemSourceDto MapSourceToDto(DashboardItem item, DashboardItemSource s)
        {
            var widgetSource = s.WidgetSource;
            if (widgetSource == null || item.Widget == null)
                return new DashboardItemSourceDto { Id = s.Id, Label = s.Label, ViewId = s.ViewId, LinkedViewWidgetId = s.LinkedViewWidgetId, Order = s.Order };

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
                LinkedViewWidgetId = s.LinkedViewWidgetId,
                Label = s.Label,
                Order = s.Order
            };
        }
    }
}
