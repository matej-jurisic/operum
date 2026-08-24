using Microsoft.EntityFrameworkCore;
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
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Dashboards
{
    public class DashboardService(ICurrentUserService currentUserService, OperumContext db) : IDashboardService
    {
        // One source after it has been calculated, ready to be returned as-is or merged
        // with its siblings into a composed chart.
        private sealed record ResolvedSource(
            DashboardItemSource Source,
            string TrackerName,
            AnalyticDto Result);

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

        public async Task<Result<List<AnalyticDto>>> GetDashboardAnalytics(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            var items = dashboard.Items.OrderBy(i => i.Order).ToList();
            var results = new List<AnalyticDto>();

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.ResultType) || string.IsNullOrEmpty(item.Code))
                    continue;

                var resolvedSources = new List<ResolvedSource>();

                foreach (var source in item.Sources.OrderBy(s => s.Order))
                {
                    var viewIds = ParseViewIds(source.ViewIds);
                    var views = new List<View>();

                    foreach (var viewId in viewIds)
                    {
                        var view = await db.Views
                            .Include(v => v.Filters).ThenInclude(f => f.Field)
                            .Include(v => v.Sorts).ThenInclude(s => s.Field)
                            .FirstOrDefaultAsync(v => v.Id == viewId && v.TrackerId == source.TrackerId);
                        if (view != null) views.Add(view);
                    }

                    var entriesQuery = db.Entries
                        .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                        .Where(e => e.TrackerId == source.TrackerId);

                    if (views.Count > 0)
                    {
                        entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.MergeViewFilters(views), currentUserService.GetCurrentUserTimeZone());
                        entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, ViewQueryBuilder.MergeViewSorts(views));
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

                    var calculationResult = AnalyticResultBuilder.GetAnalyticResult(request);
                    if (calculationResult.IsSuccess)
                        resolvedSources.Add(new ResolvedSource(source, source.Tracker.Name, calculationResult.Data));
                }

                if (resolvedSources.Count == 0) continue;

                // A single source renders exactly as it always has; combining only kicks in
                // once there's more than one source to merge into a shared chart.
                var itemResult = resolvedSources.Count == 1
                    ? resolvedSources[0].Result
                    : BuildComposedResult(resolvedSources);

                // Use dashboard item ID so frontend can reference it for reorder/remove
                itemResult.Id = item.Id;
                itemResult.Order = item.Order;
                results.Add(itemResult);
            }

            return Result.Success(results);
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

                var viewIds = sourceDto.ViewIds.Where(v => !string.IsNullOrEmpty(v)).ToList();
                foreach (var viewId in viewIds)
                {
                    var exists = await db.Views.AnyAsync(v => v.Id == viewId && v.TrackerId == sourceDto.TrackerId);
                    if (!exists)
                        return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));
                }

                var source = new DashboardItemSource
                {
                    Order = sources.Count,
                    Label = sourceDto.Label,
                    TrackerId = sourceDto.TrackerId,
                    ViewIds = viewIds.Count > 0 ? string.Join(",", viewIds) : null
                };

                var fieldsResult = await BuildSourceFields(dto.ResultType, dto.Code, sourceDto, source);
                if (!fieldsResult.IsSuccess)
                    return Result.Failure(fieldsResult.StatusCode, fieldsResult.Messages);

                sourceNames.Add(fieldsResult.Data);
                sources.Add(source);
            }

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            var item = new DashboardItem
            {
                DashboardId = dashboardId,
                Order = nextOrder,
                ResultType = dto.ResultType,
                Code = dto.Code,
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
                ResultType = item.ResultType,
                Code = item.Code,
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
                    ViewIds = ParseViewIds(s.ViewIds),
                    Label = s.Label,
                    Order = s.Order
                }).ToList()
            });
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

        public async Task<Result> ReorderDashboardItems(string dashboardId, List<string> orderedItemIds)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            for (int i = 0; i < orderedItemIds.Count; i++)
            {
                var item = dashboard.Items.FirstOrDefault(x => x.Id == orderedItemIds[i]);
                if (item != null) item.Order = i;
            }

            await db.SaveChangesAsync();
            return Result.Success();
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

        // Purpose -> Field for the mappings that still resolve. Deleting a field cascades its
        // mapping away, so an incomplete map is possible here; the calculation then fails for
        // that source and the caller skips it rather than failing the whole dashboard.
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

        private static List<string> ParseViewIds(string? viewIds) =>
            string.IsNullOrEmpty(viewIds)
                ? []
                : viewIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Merges 2+ per-source results (each computed independently by the same
        // single-tracker pipeline as always) into one multi-series chart. Every source shares
        // the item's result type and code, so the series are always produced the same way;
        // what they can still differ in is the kind of value on the x-axis, which is surfaced
        // as a warning rather than rejected.
        private static ComposedChartAnalyticDto BuildComposedResult(List<ResolvedSource> resolvedSources)
        {
            var composed = new ComposedChartAnalyticDto
            {
                Name = "Combined chart"
            };

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
                        Points = line.Points.Select(p => new ComposedChartPointDto { X = p.X, Y = p.Y }).ToList()
                    },
                    BarChartAnalyticDto bar => new ComposedChartSeriesDto
                    {
                        Key = source.Id,
                        Label = source.Label ?? $"{resolved.TrackerName}: {bar.ValueField?.Name ?? "Count"}",
                        RenderType = ComposedSeriesRenderTypes.Bar,
                        XField = bar.NameField,
                        ValueField = bar.ValueField ?? new FieldDto { Name = "Count", Type = DataTypes.Number },
                        Points = bar.Points.Select(p => new ComposedChartPointDto { X = p.Name, Y = p.Value }).ToList()
                    },
                    // Defensive only — AddDashboardItem already rejects any other result type
                    // once there's more than one source.
                    _ => null
                };

                if (series != null) composed.Series.Add(series);
            }

            var hasMismatchedXTypes = composed.Series.Select(s => s.XField.Type).Distinct().Count() > 1;
            if (hasMismatchedXTypes)
                composed.Warnings.Add("Sources plot different kinds of value on the x-axis, alignment may be misleading.");

            return composed;
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
                ResultType = i.ResultType,
                Code = i.Code,
                Sources = i.Sources.OrderBy(s => s.Order).Select(s => MapSourceToDto(i, s)).ToList()
            }).ToList()
        };

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
                ViewIds = ParseViewIds(s.ViewIds),
                Label = s.Label,
                Order = s.Order
            };
        }
    }
}
