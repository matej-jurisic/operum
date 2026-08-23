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
        // The analytic definition behind a source, whether it came from a saved Analytic
        // or from the source's own ad hoc Code/ResultType/Fields.
        private sealed record SourceDefinition(
            string ResultType,
            string Code,
            Dictionary<string, Field> FieldMap,
            List<Field> Fields);

        // One source after its definition has been resolved and calculated, ready to be
        // returned as-is or merged with its siblings into a composed chart.
        private sealed record ResolvedSource(
            DashboardItemSource Source,
            string TrackerName,
            SourceDefinition Definition,
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
                var resolvedSources = new List<ResolvedSource>();

                foreach (var source in item.Sources.OrderBy(s => s.Order))
                {
                    var definition = ResolveDefinition(source);
                    if (definition == null) continue;

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
                        // An ad hoc source has no Analytic row, so the pipeline is fed a
                        // transient one built from the source's own definition. The builders
                        // only read ResultType/Code/Id/Description, all of which it carries.
                        Analytic = source.Analytic ?? new Analytic
                        {
                            Id = source.Id,
                            TrackerId = source.TrackerId,
                            Code = definition.Code,
                            ResultType = definition.ResultType
                        },
                        Entries = entries,
                        FieldMap = definition.FieldMap
                    };

                    var calculationResult = AnalyticResultBuilder.GetAnalyticResult(request);
                    if (calculationResult.IsSuccess)
                        resolvedSources.Add(new ResolvedSource(source, source.Tracker.Name, definition, calculationResult.Data));
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

            var user = currentUserService.GetCurrentUser();
            var sources = new List<DashboardItemSource>();
            var sourceNames = new List<string>();
            var resultTypes = new List<string>();
            var codes = new List<string>();

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

                if (sourceDto.IsAdHoc)
                {
                    var adHocResult = await BuildAdHocSource(sourceDto, source);
                    if (!adHocResult.IsSuccess)
                        return Result.Failure(adHocResult.StatusCode, adHocResult.Messages);

                    resultTypes.Add(sourceDto.ResultType!);
                    codes.Add(sourceDto.Code!);
                    sourceNames.Add(adHocResult.Data);
                }
                else
                {
                    var analytic = await db.Analytics
                        .Include(a => a.AnalyticFields).ThenInclude(af => af.Field)
                        .FirstOrDefaultAsync(a => a.Id == sourceDto.AnalyticId && a.TrackerId == sourceDto.TrackerId);
                    if (analytic == null)
                        return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("analytic"));

                    source.AnalyticId = analytic.Id;
                    resultTypes.Add(analytic.ResultType);
                    codes.Add(analytic.Code);
                    sourceNames.Add(GetAnalyticName(analytic));
                }

                sources.Add(source);
            }

            // Combining sources into one chart only has a merge path for line/bar results —
            // scatter/single-value/donut/calendar have no shared points shape to render together.
            if (sources.Count > 1 && resultTypes.Any(t => t != AnalyticTypes.LineChart && t != AnalyticTypes.BarChart))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("combining this analytic with another tracker"));

            var nextOrder = dashboard.Items.Count > 0 ? dashboard.Items.Max(i => i.Order) + 1 : 0;

            var item = new DashboardItem
            {
                DashboardId = dashboardId,
                Order = nextOrder,
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
                Sources = sources.Select((s, i) => new DashboardItemSourceDto
                {
                    Id = s.Id,
                    AnalyticId = s.AnalyticId,
                    AnalyticName = sourceNames[i],
                    ResultType = resultTypes[i],
                    Code = codes[i],
                    IsAdHoc = s.IsAdHoc,
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

        // Validates an inline analytic definition and, if it holds up, fills the ad hoc
        // parts of `source`. Returns the display name for the definition on success.
        private async Task<Result<string>> BuildAdHocSource(DashboardItemSourceRequestDto dto, DashboardItemSource source)
        {
            var resultType = dto.ResultType!;
            var code = dto.Code!;

            if (!AnalyticDefinitionList.IsValidForType(resultType, code))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("code for this result type"));

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

            source.ResultType = resultType;
            source.Code = code;

            return Result.Success(AnalyticDefinitionList.GetDisplayName(resultType, code, fieldNames));
        }

        // Reads the definition off whichever half of the source carries it. Returns null
        // when the source is unusable (analytic deleted, or an ad hoc row with no code),
        // in which case the caller skips it rather than failing the whole dashboard.
        private static SourceDefinition? ResolveDefinition(DashboardItemSource source)
        {
            if (source.Analytic != null)
            {
                var fields = source.Analytic.AnalyticFields.Where(af => af.Field != null).ToList();
                return new SourceDefinition(
                    source.Analytic.ResultType,
                    source.Analytic.Code,
                    fields.ToDictionary(af => af.Purpose, af => af.Field),
                    fields.Select(af => af.Field).ToList());
            }

            if (string.IsNullOrEmpty(source.ResultType) || string.IsNullOrEmpty(source.Code))
                return null;

            var sourceFields = source.Fields.Where(f => f.Field != null).ToList();
            return new SourceDefinition(
                source.ResultType,
                source.Code,
                sourceFields.ToDictionary(f => f.Purpose, f => f.Field),
                sourceFields.Select(f => f.Field).ToList());
        }

        private static string GetAnalyticName(Analytic analytic) =>
            AnalyticDefinitionList.GetDisplayName(
                analytic.ResultType,
                analytic.Code,
                analytic.AnalyticFields.Where(af => af.Field != null).Select(af => af.Field.Name));

        private static IQueryable<Dashboard> WithSourceGraph(IQueryable<Dashboard> query) => query
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.Tracker)
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.Analytic!)
                .ThenInclude(a => a.AnalyticFields).ThenInclude(af => af.Field)
            .Include(d => d.Items).ThenInclude(i => i.Sources).ThenInclude(s => s.Fields)
                .ThenInclude(f => f.Field);

        private async Task<Dashboard?> GetUserDashboard(string dashboardId)
        {
            var user = currentUserService.GetCurrentUser();
            return await WithSourceGraph(db.Dashboards)
                // The DbContext defaults to QueryTrackingBehavior.NoTracking (see
                // DatabaseConfiguration), which also skips identity resolution: if the same
                // Tracker/Analytic is referenced by more than one source in this graph, each
                // reference materializes as a separate CLR instance. Every caller here either
                // mutates and calls SaveChanges (Update/Reorder), or hands the graph to
                // Remove() (Delete), both of which need this tracked and identity-resolved,
                // the latter otherwise throws when it tries to attach two same-key instances.
                .AsTracking()
                .FirstOrDefaultAsync(d => d.Id == dashboardId && d.UserId == user.Id);
        }

        private static List<string> ParseViewIds(string? viewIds) =>
            string.IsNullOrEmpty(viewIds)
                ? []
                : viewIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Merges 2+ per-source results (each computed independently by the same
        // single-tracker pipeline as always) into one multi-series chart. Mismatched
        // ResultType/Code across sources is allowed but surfaced as a warning rather than
        // rejected — see AddDashboardItem for the one hard constraint (line/bar only).
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

            var distinctResultTypes = resolvedSources.Select(s => s.Definition.ResultType).Distinct().Count();
            if (distinctResultTypes > 1)
                composed.Warnings.Add("This chart mixes line and bar sources, axes may not align as expected.");

            var hasMismatchedCodes = resolvedSources
                .GroupBy(s => s.Definition.ResultType)
                .Any(g => g.Select(s => s.Definition.Code).Distinct().Count() > 1);
            if (hasMismatchedCodes)
                composed.Warnings.Add("Sources use different time buckets or aggregations, axis alignment may be misleading.");

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
                Sources = i.Sources.OrderBy(s => s.Order).Select(MapSourceToDto).ToList()
            }).ToList()
        };

        private static DashboardItemSourceDto MapSourceToDto(DashboardItemSource s)
        {
            var definition = ResolveDefinition(s);

            return new DashboardItemSourceDto
            {
                Id = s.Id,
                AnalyticId = s.AnalyticId,
                AnalyticName = definition == null
                    ? string.Empty
                    : AnalyticDefinitionList.GetDisplayName(definition.ResultType, definition.Code, definition.Fields.Select(f => f.Name)),
                ResultType = definition?.ResultType ?? string.Empty,
                Code = definition?.Code ?? string.Empty,
                IsAdHoc = s.IsAdHoc,
                Fields = definition == null
                    ? []
                    : (s.Analytic != null
                        ? s.Analytic.AnalyticFields.Where(af => af.Field != null)
                            .Select(af => new DashboardItemSourceFieldDto { Purpose = af.Purpose, FieldId = af.FieldId, FieldName = af.Field.Name })
                        : s.Fields.Where(f => f.Field != null)
                            .Select(f => new DashboardItemSourceFieldDto { Purpose = f.Purpose, FieldId = f.FieldId, FieldName = f.Field.Name })
                      ).ToList(),
                TrackerId = s.TrackerId,
                TrackerName = s.Tracker.Name,
                ViewIds = ParseViewIds(s.ViewIds),
                Label = s.Label,
                Order = s.Order
            };
        }
    }
}
