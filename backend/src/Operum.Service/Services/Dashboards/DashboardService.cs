using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Model.DTOs.Fields;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Dashboards
{
    public class DashboardService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper) : IDashboardService
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
                .ToListAsync();

            return Result.Success(dashboards.Select(MapToDto).ToList());
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
                        entriesConfigsByItemId.TryGetValue(item.Id, out var entriesConfig))
                    {
                        entriesWidget = await BuildEntriesWidget(entriesConfig, viewConfigsByItemId);
                    }

                    results.Add(MapToWidgetDto(item, null, quickAddTracker, viewWidget: viewWidget, entriesWidget: entriesWidget));
                    continue;
                }

                if (string.IsNullOrEmpty(item.ResultType) || string.IsNullOrEmpty(item.Code))
                    continue;

                var resolvedSources = new List<ResolvedSource>();

                foreach (var source in item.Sources.OrderBy(s => s.Order))
                {
                    // A linked source's filter comes from whatever its View widget is
                    // currently set to instead of its own (unset) ViewId — resolved from the
                    // dictionary above rather than a query per source.
                    var effectiveViewId = ResolveEffectiveViewId(source.ViewId, source.LinkedViewWidgetId, viewConfigsByItemId);

                    View? view = null;
                    if (!string.IsNullOrEmpty(effectiveViewId))
                    {
                        view = await db.Views
                            .Include(v => v.ViewQueries.OrderBy(vq => vq.Order)).ThenInclude(vq => vq.Query).ThenInclude(q => q.Field)
                            .FirstOrDefaultAsync(v => v.Id == effectiveViewId && v.TrackerId == source.TrackerId);
                    }

                    var entriesQuery = db.Entries
                        .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                        .Where(e => e.TrackerId == source.TrackerId);

                    if (view != null)
                    {
                        entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.ResolveFilters(view), currentUserService.GetCurrentUserTimeZone());
                        entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, ViewQueryBuilder.ResolveSorts(view));
                    }

                    var entries = await entriesQuery.ToListAsync();

                    var request = new AnalyticResultBuilderRequest
                    {
                        // A dashboard source has no Analytic row of its own, so the pipeline
                        // is fed a transient one built from the item's definition. The
                        // builders only read ResultType/Code/Id/Description.
                        Analytic = new Analytic
                        {
                            Id = source.Id,
                            TrackerId = source.TrackerId,
                            Code = item.Code,
                            ResultType = item.ResultType
                        },
                        Entries = entries,
                        FieldMap = BuildFieldMap(source)
                    };

                    // Always displayable, even when the source's field(s) are missing or a
                    // calculated field's formula is broken: an explanatory card beats the
                    // widget silently disappearing, which left no way to find and remove it.
                    var data = AnalyticResultBuilder.GetDisplayableAnalyticResult(request);
                    resolvedSources.Add(new ResolvedSource(source, source.Tracker.Name, source.Tracker.Color, data));
                }

                // Only possible if the item somehow ended up with no sources at all — every
                // source's own calculation now always resolves to something displayable.
                if (resolvedSources.Count == 0) continue;

                // A single source renders exactly as it always has; combining only kicks in
                // once there's more than one source to merge into a shared chart.
                var itemResult = resolvedSources.Count == 1
                    ? resolvedSources[0].Result
                    : BuildComposedResult(resolvedSources, item.MatchedValuesOnly);

                // A single source that was copied from a tracker's analytic carries that
                // analytic's name as its label, so the widget reads on the board the way it
                // did on the tracker instead of falling back to the definition's label.
                // Combined charts name themselves from their series.
                var singleSourceLabel = resolvedSources.Count == 1 ? resolvedSources[0].Source.Label : null;
                if (!string.IsNullOrWhiteSpace(singleSourceLabel))
                    itemResult.Name = singleSourceLabel;

                // Use dashboard item ID so frontend can reference it for layout/remove
                itemResult.Id = item.Id;
                itemResult.Order = item.Order;

                // A widget owned by exactly one tracker is colored like that tracker; one
                // combining more than one falls back to the dashboard's own color, applied
                // client-side (see TrackerColor on DashboardWidgetDto).
                var distinctTrackerIds = resolvedSources.Select(r => r.Source.TrackerId).Distinct().ToList();
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

            var dashboard = new Dashboard
            {
                Name = dto.Name,
                Color = dto.Color,
                Icon = dto.Icon,
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

        public async Task<Result<DashboardItemDto>> AddDashboardItem(string dashboardId, AddDashboardItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (dashboard.Items.Count >= DataLimits.MaxDashboardItemCount)
                return Result.Failure(ResultStatusCodes.Conflict, Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            if (dto.Sources.Count == 0 || dto.Sources.Count > DataLimits.MaxDashboardItemSourceCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("dashboard item sources", DataLimits.MaxDashboardItemSourceCount));

            if (!AnalyticDefinitionList.IsValidForType(dto.ResultType, dto.Code))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("code for this result type"));

            // Combining sources into one chart only has a merge path for line/bar results —
            // scatter/single-value/donut/calendar have no shared points shape to render
            // together. The definition is shared by every source, so this is one check for
            // the whole item rather than one per source.
            if (dto.Sources.Count > 1 && dto.ResultType != AnalyticTypes.LineChart && dto.ResultType != AnalyticTypes.BarChart)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("combining this analytic with another tracker"));

            var user = currentUserService.GetCurrentUser();
            var sources = new List<DashboardItemSource>();
            var sourceNames = new List<string>();

            foreach (var sourceDto in dto.Sources)
            {
                var tracker = await db.Trackers
                    .Include(t => t.ApplicationUserTrackers)
                    .FirstOrDefaultAsync(t => t.Id == sourceDto.TrackerId);

                var hasAccess = tracker != null &&
                    (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));

                if (tracker == null || !hasAccess)
                    return Result.Failure(ResultStatusCodes.Forbidden);

                if (!string.IsNullOrEmpty(sourceDto.ViewId))
                {
                    var exists = await db.Views.AnyAsync(v => v.Id == sourceDto.ViewId && v.TrackerId == sourceDto.TrackerId);
                    if (!exists)
                        return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
                }

                if (!string.IsNullOrEmpty(sourceDto.LinkedViewWidgetId) &&
                    !IsLinkableViewWidget(dashboard, sourceDto.LinkedViewWidgetId, sourceDto.TrackerId))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("view widget for this tracker"));

                var source = new DashboardItemSource
                {
                    Order = sources.Count,
                    Label = sourceDto.Label,
                    TrackerId = sourceDto.TrackerId,
                    ViewId = sourceDto.ViewId,
                    LinkedViewWidgetId = sourceDto.LinkedViewWidgetId
                };

                var fieldsResult = await BuildSourceFields(dto.ResultType, dto.Code, sourceDto, source);
                if (!fieldsResult.IsSuccess)
                    return Result.Failure(fieldsResult.StatusCode, fieldsResult.Messages);

                sourceNames.Add(fieldsResult.Data);
                sources.Add(source);
            }

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            // A new widget starts on its own row under everything already on the board, at
            // the size its chart type reads well at. The user moves it from there.
            var (width, height) = DashboardGrid.DefaultSizeFor(dto.ResultType);
            var nextRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Y + i.H) : 0;

            // Both grids are placed at once, so neither arrangement has a hole in it the
            // first time the board is opened on the other kind of screen. On a phone there
            // is no room to put anything beside anything else, so a new widget takes the
            // full width of the narrow grid and keeps the height its chart type wants.
            var nextMobileRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.MobileY + i.MobileH) : 0;

            var item = new DashboardItem
            {
                DashboardId = dashboardId,
                Order = nextOrder,
                Type = DashboardWidgetTypes.Analytic,
                X = 0,
                Y = nextRow,
                W = width,
                H = height,
                MobileX = 0,
                MobileY = nextMobileRow,
                MobileW = DashboardGrid.MobileColumns,
                MobileH = height,
                ResultType = dto.ResultType,
                Code = dto.Code,
                MatchedValuesOnly = dto.MatchedValuesOnly,
                Expandable = dto.Expandable,
                MobileExpandable = dto.MobileExpandable,
                Sources = sources
            };

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            var trackerIds = sources.Select(s => s.TrackerId).Distinct().ToList();
            var sourceFieldIds = sources.SelectMany(s => s.Fields).Select(sf => sf.FieldId).Distinct().ToList();

            var trackers = await db.Trackers.Where(t => trackerIds.Contains(t.Id)).ToListAsync();
            var fieldsById = await db.Fields
                .Where(f => sourceFieldIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

            return Result.Success(new DashboardItemDto
            {
                Id = item.Id,
                Order = item.Order,
                Type = item.Type,
                Layout = MapToLayoutDto(item),
                MobileLayout = MapToMobileLayoutDto(item),
                Config = item.Config,
                ResultType = item.ResultType,
                Code = item.Code,
                MatchedValuesOnly = item.MatchedValuesOnly,
                Sources = sources.Select((s, i) => new DashboardItemSourceDto
                {
                    Id = s.Id,
                    Name = sourceNames[i],
                    Fields = s.Fields.Select(sf => new DashboardItemSourceFieldDto
                    {
                        Purpose = sf.Purpose,
                        FieldId = sf.FieldId,
                        FieldName = fieldsById.TryGetValue(sf.FieldId, out var f) ? f.Name : string.Empty
                    }).ToList(),
                    TrackerId = s.TrackerId,
                    TrackerName = trackers.First(t => t.Id == s.TrackerId).Name,
                    ViewId = s.ViewId,
                    LinkedViewWidgetId = s.LinkedViewWidgetId,
                    Label = s.Label,
                    Order = s.Order
                }).ToList()
            });
        }

        // Copies a tracker's own analytic onto the board. The definition is duplicated rather
        // than referenced, so the widget keeps working (and keeps looking the way it did when
        // it was added) after the tracker's analytic is edited or deleted.
        public async Task<Result<DashboardItemDto>> AddDashboardItemFromAnalytic(string dashboardId, AddDashboardItemFromAnalyticDto dto)
        {
            var analytic = await db.Analytics
                .Include(a => a.AnalyticFields)
                .FirstOrDefaultAsync(a => a.Id == dto.AnalyticId);

            if (analytic == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("analytic"));

            // Everything else — access to the tracker, the field mapping, the board's item
            // limit — is settled by the normal add path, which this is only a shortcut into.
            return await AddDashboardItem(dashboardId, new AddDashboardItemDto
            {
                ResultType = analytic.ResultType,
                Code = analytic.Code,
                Expandable = dto.Expandable,
                MobileExpandable = dto.MobileExpandable,
                Sources =
                [
                    new DashboardItemSourceRequestDto
                    {
                        TrackerId = analytic.TrackerId,
                        // Left unset when the analytic was never named, so the widget falls
                        // back to the definition's own label the way any other item does.
                        Label = string.IsNullOrWhiteSpace(analytic.Name) ? null : analytic.Name,
                        ViewId = dto.ViewId,
                        LinkedViewWidgetId = dto.LinkedViewWidgetId,
                        AnalyticFields = analytic.AnalyticFields
                            .Select(f => new CreateAnalyticFieldDto { Purpose = f.Purpose, FieldId = f.FieldId })
                            .ToList()
                    }
                ]
            });
        }

        // A button that opens a tracker's quick-add entry dialog from the board. Unlike
        // AddDashboardItem this carries no analytic definition — access to the tracker is
        // the only thing worth checking before it is placed.
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

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            // Same placement rules as a chart: its own row under everything already on the
            // board, on both grids at once.
            var nextRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Y + i.H) : 0;
            var nextMobileRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.MobileY + i.MobileH) : 0;
            var (width, height) = DashboardGrid.QuickAddSize;

            var item = new DashboardItem
            {
                DashboardId = dashboardId,
                Order = nextOrder,
                Type = DashboardWidgetTypes.QuickAdd,
                Config = JsonSerializer.Serialize(new QuickAddWidgetConfigDto { TrackerId = dto.TrackerId }, ConfigJsonOptions),
                X = 0,
                Y = nextRow,
                W = width,
                H = height,
                MobileX = 0,
                MobileY = nextMobileRow,
                MobileW = DashboardGrid.MobileColumns,
                MobileH = height
            };

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(new DashboardItemDto
            {
                Id = item.Id,
                Order = item.Order,
                Type = item.Type,
                Layout = MapToLayoutDto(item),
                MobileLayout = MapToMobileLayoutDto(item),
                Config = item.Config,
                ResultType = item.ResultType,
                Code = item.Code,
                MatchedValuesOnly = item.MatchedValuesOnly,
                Sources = []
            });
        }

        // A dropdown over one tracker's views. Unlike AddDashboardItem this carries no
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

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            // Same placement rules as a chart: its own row under everything already on the
            // board, on both grids at once.
            var nextRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Y + i.H) : 0;
            var nextMobileRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.MobileY + i.MobileH) : 0;
            var (width, height) = DashboardGrid.ViewSize;

            var item = new DashboardItem
            {
                DashboardId = dashboardId,
                Order = nextOrder,
                Type = DashboardWidgetTypes.View,
                Config = JsonSerializer.Serialize(new ViewWidgetConfigDto { TrackerId = dto.TrackerId, ViewId = dto.ViewId }, ConfigJsonOptions),
                X = 0,
                Y = nextRow,
                W = width,
                H = height,
                MobileX = 0,
                MobileY = nextMobileRow,
                MobileW = DashboardGrid.MobileColumns,
                MobileH = height
            };

            db.DashboardItems.Add(item);
            await db.SaveChangesAsync();

            return Result.Success(new DashboardItemDto
            {
                Id = item.Id,
                Order = item.Order,
                Type = item.Type,
                Layout = MapToLayoutDto(item),
                MobileLayout = MapToMobileLayoutDto(item),
                Config = item.Config,
                ResultType = item.ResultType,
                Code = item.Code,
                MatchedValuesOnly = item.MatchedValuesOnly,
                Sources = []
            });
        }

        // A read-only table of one tracker's entries. Unlike AddDashboardItem this carries no
        // analytic definition either — access to the tracker, and that at most one of a fixed
        // view or a view widget to follow was supplied, is all that needs checking.
        public async Task<Result<DashboardItemDto>> AddEntriesItem(string dashboardId, AddDashboardEntriesItemDto dto)
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

            if (!string.IsNullOrEmpty(dto.LinkedViewWidgetId) &&
                !IsLinkableViewWidget(dashboard, dto.LinkedViewWidgetId, dto.TrackerId))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("view widget for this tracker"));

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            // Same placement rules as a chart: its own row under everything already on the
            // board, on both grids at once.
            var nextRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Y + i.H) : 0;
            var nextMobileRow = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.MobileY + i.MobileH) : 0;
            var (width, height) = DashboardGrid.EntriesSize;

            var item = new DashboardItem
            {
                DashboardId = dashboardId,
                Order = nextOrder,
                Type = DashboardWidgetTypes.Entries,
                Config = JsonSerializer.Serialize(new EntriesWidgetConfigDto
                {
                    TrackerId = dto.TrackerId,
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

            return Result.Success(new DashboardItemDto
            {
                Id = item.Id,
                Order = item.Order,
                Type = item.Type,
                Layout = MapToLayoutDto(item),
                MobileLayout = MapToMobileLayoutDto(item),
                Config = item.Config,
                ResultType = item.ResultType,
                Code = item.Code,
                MatchedValuesOnly = item.MatchedValuesOnly,
                Sources = []
            });
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

            return Result.Success(MapToPlainItemDto(item));
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

            return Result.Success(MapToPlainItemDto(item));
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

        private static DashboardItemDto MapToPlainItemDto(DashboardItem item) => new()
        {
            Id = item.Id,
            Order = item.Order,
            Type = item.Type,
            Layout = MapToLayoutDto(item),
            MobileLayout = MapToMobileLayoutDto(item),
            Config = item.Config,
            ResultType = item.ResultType,
            Code = item.Code,
            MatchedValuesOnly = item.MatchedValuesOnly,
            Sources = []
        };

        // Edits an analytic widget in place, but only where editing is the board's business:
        // what the widget is called, and which view each of its sources reads through. The
        // definition itself (result type, code, field mapping) is deliberately not editable —
        // changing that turns the widget into a different chart rather than the one that was
        // placed here, which is what adding a new one is for. Returns the whole board
        // recomputed, the same as SetViewWidgetSelection, since a changed view changes what
        // the chart draws.
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

                if (!string.IsNullOrEmpty(sourceDto.ViewId))
                {
                    var exists = await db.Views.AnyAsync(v => v.Id == sourceDto.ViewId && v.TrackerId == source.TrackerId);
                    if (!exists)
                        return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
                }

                if (!string.IsNullOrEmpty(sourceDto.LinkedViewWidgetId) &&
                    !IsLinkableViewWidget(dashboard, sourceDto.LinkedViewWidgetId, source.TrackerId))
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

        // Edits an Entries widget in place: only how it's filtered, and whether it collapses
        // to a button on each grid — the tracker it reads from is fixed at add time, the same
        // as an Analytic widget's definition is (see UpdateDashboardItem). Returns the whole
        // board recomputed, the same as SetViewWidgetSelection, since a changed view changes
        // what the table shows.
        public async Task<Result<List<DashboardWidgetDto>>> UpdateEntriesItem(string dashboardId, string itemId, UpdateDashboardEntriesItemDto dto)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId && i.Type == DashboardWidgetTypes.Entries);
            var config = item != null ? TryParseEntriesConfig(item.Config) : null;
            if (item == null || config == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("entries widget"));

            if (!string.IsNullOrEmpty(dto.ViewId))
            {
                var exists = await db.Views.AnyAsync(v => v.Id == dto.ViewId && v.TrackerId == config.TrackerId);
                if (!exists)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
            }

            if (!string.IsNullOrEmpty(dto.LinkedViewWidgetId) &&
                !IsLinkableViewWidget(dashboard, dto.LinkedViewWidgetId, config.TrackerId))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("view widget for this tracker"));

            config.ViewId = string.IsNullOrEmpty(dto.ViewId) ? null : dto.ViewId;
            config.LinkedViewWidgetId = string.IsNullOrEmpty(dto.LinkedViewWidgetId) ? null : dto.LinkedViewWidgetId;
            item.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
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

            return Result.Success(MapToPlainItemDto(item));
        }

        public async Task<Result> RemoveDashboardItem(string dashboardId, string itemId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var item = dashboard.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard item"));

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

        // Validates the field mapping a source supplies for the item's definition and, if it
        // holds up, fills source.Fields. Returns the display name for the source on success.
        private async Task<Result<string>> BuildSourceFields(string resultType, string code, DashboardItemSourceRequestDto dto, DashboardItemSource source)
        {
            var requiredPurposes = AnalyticDefinitionList.GetRequiredPurposes(resultType, code);
            var suppliedPurposes = dto.AnalyticFields.Select(f => f.Purpose).ToList();

            if (suppliedPurposes.Count != suppliedPurposes.Distinct().Count() ||
                !requiredPurposes.ToHashSet().SetEquals(suppliedPurposes))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Required($"a field for each of: {string.Join(", ", requiredPurposes)}"));

            var fieldNames = new List<string>();

            foreach (var analyticField in dto.AnalyticFields)
            {
                // Scoped to the source's tracker on purpose: a dashboard spans trackers, so
                // without this a caller could point a source at a field on some other tracker.
                var field = await db.Fields
                    .FirstOrDefaultAsync(f => f.Id == analyticField.FieldId && f.TrackerId == dto.TrackerId);

                if (field == null)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound($"field for purpose {analyticField.Purpose}"));

                if (!AnalyticDefinitionList.IsValidDataType(resultType, code, analyticField.Purpose, field.Type))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("data type for purpose"));

                source.Fields.Add(new DashboardItemSourceField
                {
                    Purpose = analyticField.Purpose,
                    FieldId = field.Id
                });
                fieldNames.Add(field.Name);
            }

            return Result.Success(AnalyticDefinitionList.GetDisplayName(resultType, code, fieldNames));
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

        // Purpose -> Field for the mappings that still resolve. Deleting a field cascades its
        // mapping away, so an incomplete map is possible here; the builder renders whatever
        // it can from what's left and GetDisplayableAnalyticResult falls back to an
        // explanatory placeholder otherwise, rather than the source disappearing.
        private static Dictionary<string, Field> BuildFieldMap(DashboardItemSource source) =>
            source.Fields
                .Where(f => f.Field != null)
                .ToDictionary(f => f.Purpose, f => f.Field);

        private static IQueryable<Dashboard> WithSourceGraph(IQueryable<Dashboard> query) => query
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.Tracker)
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.Fields)
                .ThenInclude(f => f.Field);

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
        // the item's result type and code, so the series are always produced the same way;
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
                    // Defensive only — AddDashboardItem already rejects any other result type
                    // once there's more than one source.
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
            Items = d.Items.OrderBy(i => i.Order).Select(i => new DashboardItemDto
            {
                Id = i.Id,
                Order = i.Order,
                Type = i.Type,
                Layout = MapToLayoutDto(i),
                MobileLayout = MapToMobileLayoutDto(i),
                Config = i.Config,
                ResultType = i.ResultType,
                Code = i.Code,
                MatchedValuesOnly = i.MatchedValuesOnly,
                Sources = i.Sources.OrderBy(s => s.Order).Select(s => MapSourceToDto(i, s)).ToList()
            }).ToList()
        };

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

        // Resolves everything an Entries widget's table needs to render: which tracker to
        // read from, the view it's currently filtered by, and the columns that view wants
        // shown, in its order. A view naming no columns (or no view at all) shows every
        // field, the same fallback the tracker page's own column picker uses.
        private async Task<EntriesWidgetDto?> BuildEntriesWidget(EntriesWidgetConfigDto config, Dictionary<string, ViewWidgetConfigDto> viewConfigsByItemId)
        {
            var tracker = await db.Trackers.FirstOrDefaultAsync(t => t.Id == config.TrackerId);
            if (tracker == null) return null;

            var effectiveViewId = ResolveEffectiveViewId(config.ViewId, config.LinkedViewWidgetId, viewConfigsByItemId);

            var columnFields = new List<Field>();
            if (!string.IsNullOrEmpty(effectiveViewId))
            {
                var view = await db.Views
                    .Include(v => v.ViewColumns.OrderBy(vc => vc.Order)).ThenInclude(vc => vc.Field)
                    .FirstOrDefaultAsync(v => v.Id == effectiveViewId && v.TrackerId == config.TrackerId);

                if (view != null && view.ViewColumns.Count > 0)
                    columnFields = view.ViewColumns.OrderBy(vc => vc.Order).Select(vc => vc.Field).ToList();
            }

            if (columnFields.Count == 0)
                columnFields = await db.Fields.Where(f => f.TrackerId == config.TrackerId).OrderBy(f => f.Order).ToListAsync();

            return new EntriesWidgetDto
            {
                TrackerId = tracker.Id,
                TrackerName = tracker.Name,
                Color = tracker.Color,
                Icon = tracker.Icon,
                ViewId = effectiveViewId,
                Columns = mapper.Map<List<Field>, List<FieldDto>>(columnFields)
            };
        }

        private static DashboardItemSourceDto MapSourceToDto(DashboardItem item, DashboardItemSource s)
        {
            var fields = s.Fields.Where(f => f.Field != null).ToList();

            return new DashboardItemSourceDto
            {
                Id = s.Id,
                Name = AnalyticDefinitionList.GetDisplayName(item.ResultType, item.Code, fields.Select(f => f.Field.Name)),
                Fields = fields
                    .Select(f => new DashboardItemSourceFieldDto { Purpose = f.Purpose, FieldId = f.FieldId, FieldName = f.Field.Name })
                    .ToList(),
                TrackerId = s.TrackerId,
                TrackerName = s.Tracker.Name,
                ViewId = s.ViewId,
                LinkedViewWidgetId = s.LinkedViewWidgetId,
                Label = s.Label,
                Order = s.Order
            };
        }
    }
}
