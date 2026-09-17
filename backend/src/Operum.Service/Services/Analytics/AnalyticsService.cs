using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Analytics
{
    public class AnalyticsService(ICurrentUserService currentUserService, OperumContext db) : IAnalyticsService
    {
        public Result<AnalyticConfigDto> GetAnalyticConfig()
        {
            var config = new AnalyticConfigDto
            {
                ResultTypes = [.. AnalyticDefinitionList.ByResultType.Select(rt => new AnalyticConfigType
                {
                    Name = rt.Key,
                    WidgetOnly = rt.Value.WidgetOnly,
                    GroupingPurpose = rt.Value.GroupingPurpose,
                    Codes = [.. rt.Value.Codes.Select(code => new AnalyticConfigCode
                    {
                        Code = code.Key,
                        // A grouping type's display name is composed client-side; this is just the bare aggregation label.
                        Name = rt.Value.UsesGrouping
                            ? AnalyticDefinitionList.GetAggregationLabel(code.Key)
                            : string.IsNullOrEmpty(code.Value.Label) ? code.Key : code.Value.Label,
                        Purposes = [.. code.Value.AllowedDataTypes
                            .Select(p => new AnalyticConfigPurpose
                            {
                                Name = p.Key,
                                AllowedDataTypes = [.. p.Value],
                                Optional = AnalyticDefinitionList.IsOptionalPurpose(rt.Key, code.Key, p.Key)
                            })]
                    })],
                    Groupings = [.. rt.Value.Groupings.Select(g => new AnalyticConfigGrouping
                    {
                        Grouping = g.Key,
                        Name = g.Value.Label,
                        AllowedDataTypes = [.. g.Value.AllowedAxisTypes],
                        AllowedCodes = [.. g.Value.AllowedCodes]
                    })]
                })]
            };

            return Result.Success(config);
        }

        // Mirrors DashboardService.BuildWidgets but persists nothing; field mapping comes
        // straight off the request.
        public async Task<Result<AnalyticDto>> Evaluate(EvaluateWidgetDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            // Rewrites a legacy fused Line/Bar code (from before grouping and aggregation were split) to the current pair.
            var (grouping, code) = LegacyLineBarCodes.Resolve(dto.Grouping, dto.Code);

            if (!AnalyticDefinitionList.IsValidForType(dto.ResultType, code, grouping))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("code for this result type"));

            // A Goal's target only exists on a saved Widget; there's nothing to evaluate ad hoc.
            if (dto.ResultType == AnalyticTypes.Goal)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("evaluating a goal without a saved target"));

            var isPaired = AnalyticTypes.RequiresPairedSources(dto.ResultType, code);

            // Gated the same way as WidgetsService.CreateWidget.
            if (isPaired)
            {
                if (dto.Sources.Count != 2)
                    return Result.Failure(ResultStatusCodes.BadRequest,
                        Messages.Invalid("source count for a correlation chart, which pairs exactly two trackers"));
            }
            else if (dto.Sources.Count > 1 && !AnalyticTypes.SupportsMultipleSources(dto.ResultType))
                return Result.Failure(ResultStatusCodes.BadRequest,
                    Messages.NotAllowed("combining this calculation with another tracker"));

            var tz = currentUserService.GetCurrentUserTimeZone();
            var mergeSources = new List<MergeSource>();

            for (var i = 0; i < dto.Sources.Count; i++)
            {
                var src = dto.Sources[i];

                var tracker = await db.Trackers
                    .Include(t => t.ApplicationUserTrackers)
                    .FirstOrDefaultAsync(t => t.Id == src.TrackerId);

                var hasAccess = tracker != null &&
                    (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));

                if (tracker == null || !hasAccess)
                    return Result.Failure(ResultStatusCodes.Forbidden);

                var trackerFields = await db.Fields
                    .Where(f => f.TrackerId == src.TrackerId)
                    .ToDictionaryAsync(f => f.Id);

                var fieldMapResult = BuildFieldMap(dto.ResultType, code, grouping, src, trackerFields);
                if (!fieldMapResult.IsSuccess)
                    return Result.Failure(fieldMapResult.StatusCode, fieldMapResult.Messages);

                var entriesResult = await BuildSourceEntries(src, trackerFields, tz);
                if (!entriesResult.IsSuccess)
                    return Result.Failure(entriesResult.StatusCode, entriesResult.Messages);

                // A correlation source has no calculation of its own: each side is the (match
                // key -> value) list a raw-values line chart produces, for MergeCorrelation to join.
                var request = new AnalyticResultBuilderRequest
                {
                    Analytic = new Analytic
                    {
                        Id = $"explore-{i}",
                        Code = isPaired ? AnalyticCodes.RawValues : code,
                        Grouping = isPaired ? AnalyticGroupings.None : grouping,
                        ResultType = isPaired ? AnalyticTypes.LineChart : dto.ResultType
                    },
                    Entries = entriesResult.Data,
                    FieldMap = isPaired
                        ? MultiSourceAnalyticMerger.PairedAxisFieldMap(fieldMapResult.Data)
                        : fieldMapResult.Data
                };

                var data = AnalyticResultBuilder.GetDisplayableAnalyticResult(request);
                mergeSources.Add(new MergeSource(i.ToString(), null, tracker.Name, tracker.Color, data));
            }

            if (mergeSources.Count == 1)
                return Result.Success(mergeSources[0].Result);

            AnalyticDto merged = isPaired
                ? MultiSourceAnalyticMerger.MergeCorrelation(mergeSources)
                : dto.ResultType == AnalyticTypes.Calendar
                    ? MultiSourceAnalyticMerger.MergeCalendars(mergeSources)
                    : MultiSourceAnalyticMerger.BuildComposed(mergeSources, dto.MatchedValuesOnly);

            return Result.Success(merged);
        }

        // Validates one source's purpose -> field mapping the same way WidgetsService.BuildSourceFields does.
        private static Result<Dictionary<string, Field>> BuildFieldMap(
            string resultType, string code, string? grouping, EvaluateSourceDto src, IReadOnlyDictionary<string, Field> trackerFields)
        {
            var requiredPurposes = AnalyticDefinitionList.GetRequiredPurposes(resultType, code, grouping);
            var allowedPurposes = AnalyticDefinitionList.GetAllowedPurposes(resultType, code, grouping).ToHashSet();
            var suppliedPurposes = src.Fields.Select(f => f.Purpose).ToList();

            if (suppliedPurposes.Count != suppliedPurposes.Distinct().Count() ||
                !suppliedPurposes.All(allowedPurposes.Contains) ||
                requiredPurposes.Any(p => !suppliedPurposes.Contains(p)))
                return Result.Failure(ResultStatusCodes.BadRequest,
                    Messages.Required($"a field for each of: {string.Join(", ", requiredPurposes)}"));

            var map = new Dictionary<string, Field>();

            foreach (var field in src.Fields)
            {
                if (!trackerFields.TryGetValue(field.FieldId, out var trackerField))
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound($"field for purpose {field.Purpose}"));

                if (!AnalyticDefinitionList.IsValidDataType(resultType, code, field.Purpose, trackerField.Type, grouping))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("data type for purpose"));

                map[field.Purpose] = trackerField;
            }

            return Result.Success(map);
        }

        private async Task<Result<List<Entry>>> BuildSourceEntries(
            EvaluateSourceDto src, IReadOnlyDictionary<string, Field> trackerFields, TimeZoneInfo tz)
        {
            var entriesQuery = db.Entries
                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == src.TrackerId);

            if (!string.IsNullOrEmpty(src.ViewId))
            {
                var view = await db.Views
                    .Include(v => v.ViewQueries.OrderBy(vq => vq.Order)).ThenInclude(vq => vq.Query)
                    .Include(v => v.ViewQueries).ThenInclude(vq => vq.Field)
                    .FirstOrDefaultAsync(v => v.Id == src.ViewId && v.TrackerId == src.TrackerId);

                if (view == null)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));

                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.ResolveFilters(view), tz);
                entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, ViewQueryBuilder.ResolveSorts(view));
            }

            var inlineFilters = ResolveInlineFilters(src.Filters, trackerFields);
            if (inlineFilters.Count > 0)
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, inlineFilters, tz);

            return Result.Success(await entriesQuery.ToListAsync());
        }

        // A blank value is only kept for the equality operators ("is empty" / "has a value").
        private static List<ResolvedClause> ResolveInlineFilters(
            List<EvaluateFilterClauseDto> filters, IReadOnlyDictionary<string, Field> trackerFields)
        {
            var resolved = new List<ResolvedClause>();

            foreach (var filter in filters)
            {
                if (!trackerFields.TryGetValue(filter.FieldId, out var field))
                    continue;

                if (string.IsNullOrEmpty(filter.Value) &&
                    filter.Operator != OperatorTypes.EqualsOperator &&
                    filter.Operator != OperatorTypes.NotEquals)
                    continue;

                resolved.Add(new ResolvedClause(field.Id, field.Type, filter.Operator, filter.Value, false));
            }

            return resolved;
        }
    }
}
