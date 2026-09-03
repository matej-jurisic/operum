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
                    Codes = [.. rt.Value.Codes.Select(code => new AnalyticConfigCode
                    {
                        Code = code.Key,
                        Name = string.IsNullOrEmpty(code.Value.Label) ? code.Key : code.Value.Label,
                        Purposes = [.. code.Value.AllowedDataTypes
                            .Select(p => new AnalyticConfigPurpose
                            {
                                Name = p.Key,
                                AllowedDataTypes = [.. p.Value]
                            })]
                    })]
                })]
            };

            return Result.Success(config);
        }

        // Mirrors the placement pipeline in DashboardService.BuildWidgets, minus the shared
        // Widget/DashboardItemSource entities: nothing is saved, the field mapping comes
        // straight off the request, and the base filter/sort is an optional saved view with
        // any number of inline clauses ANDed on top.
        public async Task<Result<AnalyticDto>> Evaluate(EvaluateWidgetDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            if (!AnalyticDefinitionList.IsValidForType(dto.ResultType, dto.Code))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("code for this result type"));

            // A correlation pairs two trackers, one per axis; there's nothing for it to
            // evaluate against a single tracker (the Explore page's only shape).
            if (AnalyticTypes.RequiresPairedSources(dto.ResultType, dto.Code))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("evaluating a correlation chart here"));

            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == dto.TrackerId);

            var hasAccess = tracker != null &&
                (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
                return Result.Failure(ResultStatusCodes.Forbidden);

            var trackerFields = await db.Fields
                .Where(f => f.TrackerId == dto.TrackerId)
                .ToDictionaryAsync(f => f.Id);

            var fieldMapResult = BuildFieldMap(dto, trackerFields);
            if (!fieldMapResult.IsSuccess)
                return Result.Failure(fieldMapResult.StatusCode, fieldMapResult.Messages);

            var tz = currentUserService.GetCurrentUserTimeZone();

            var entriesQuery = db.Entries
                .Include(e => e.FieldValues).ThenInclude(fv => fv.Field)
                .Where(e => e.TrackerId == dto.TrackerId);

            if (!string.IsNullOrEmpty(dto.ViewId))
            {
                var view = await db.Views
                    .Include(v => v.ViewQueries.OrderBy(vq => vq.Order)).ThenInclude(vq => vq.Query)
                    .Include(v => v.ViewQueries).ThenInclude(vq => vq.Field)
                    .FirstOrDefaultAsync(v => v.Id == dto.ViewId && v.TrackerId == dto.TrackerId);

                if (view == null)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("view"));

                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, ViewQueryBuilder.ResolveFilters(view), tz);
                entriesQuery = ViewQueryBuilder.ApplyViewSorting(entriesQuery, ViewQueryBuilder.ResolveSorts(view));
            }

            var inlineFilters = ResolveInlineFilters(dto.Filters, trackerFields);
            if (inlineFilters.Count > 0)
                entriesQuery = ViewQueryBuilder.ApplyViewFilters(entriesQuery, inlineFilters, tz);

            var entries = await entriesQuery.ToListAsync();

            var request = new AnalyticResultBuilderRequest
            {
                // No persisted Analytic -- the pipeline only reads Id/Code/ResultType off a
                // transient one, same as DashboardService does for a placement.
                Analytic = new Analytic
                {
                    Id = "explore",
                    Code = dto.Code,
                    ResultType = dto.ResultType
                },
                Entries = entries,
                FieldMap = fieldMapResult.Data
            };

            return Result.Success(AnalyticResultBuilder.GetDisplayableAnalyticResult(request));
        }

        // Validates the request's purpose -> field mapping the same way
        // WidgetsService.BuildSourceFields does: the supplied purposes must be exactly the
        // set the code requires, each field must belong to the tracker, and its data type
        // must be one the code allows for that purpose.
        private static Result<Dictionary<string, Field>> BuildFieldMap(
            EvaluateWidgetDto dto, IReadOnlyDictionary<string, Field> trackerFields)
        {
            var requiredPurposes = AnalyticDefinitionList.GetRequiredPurposes(dto.ResultType, dto.Code);
            var suppliedPurposes = dto.Fields.Select(f => f.Purpose).ToList();

            if (suppliedPurposes.Count != suppliedPurposes.Distinct().Count() ||
                !requiredPurposes.ToHashSet().SetEquals(suppliedPurposes))
                return Result.Failure(ResultStatusCodes.BadRequest,
                    Messages.Required($"a field for each of: {string.Join(", ", requiredPurposes)}"));

            var map = new Dictionary<string, Field>();

            foreach (var field in dto.Fields)
            {
                if (!trackerFields.TryGetValue(field.FieldId, out var trackerField))
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound($"field for purpose {field.Purpose}"));

                if (!AnalyticDefinitionList.IsValidDataType(dto.ResultType, dto.Code, field.Purpose, trackerField.Type))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("data type for purpose"));

                map[field.Purpose] = trackerField;
            }

            return Result.Success(map);
        }

        // Inline clauses resolved to the field they run against. A clause whose field is
        // unknown is dropped; a blank value is only kept for the equality operators
        // ("is empty" / "has a value"), matching DashboardService.ResolveFilterClauses.
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
