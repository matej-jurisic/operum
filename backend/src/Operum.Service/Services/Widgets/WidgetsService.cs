using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.DTOs.Widgets;
using Operum.Model.DTOs.Widgets.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Widgets
{
    // CRUD for the Widget Library: reusable chart (Widget) and Entries table
    // (EntriesWidget) definitions, owned by a user rather than a tracker or a dashboard.
    // Placing one on a board -- and rendering it there -- is DashboardService's job (see
    // DashboardService.BuildWidgets, rewired to read through these in Phase B3); this
    // service only manages the definitions themselves.
    public class WidgetsService(ICurrentUserService currentUserService, OperumContext db) : IWidgetsService
    {
        public async Task<Result<List<WidgetDto>>> GetWidgets(string? trackerId)
        {
            var user = currentUserService.GetCurrentUser();
            var query = WithSourceGraph(db.Widgets).Where(w => w.OwnerId == user.Id);

            if (!string.IsNullOrEmpty(trackerId))
                query = query.Where(w => w.Sources.Any(s => s.TrackerId == trackerId));

            var widgets = await query.ToListAsync();
            return Result.Success(widgets.Select(MapToDto).ToList());
        }

        public async Task<Result<WidgetDto>> GetWidget(string widgetId)
        {
            var widget = await GetOwnedWidget(widgetId);
            if (widget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("widget"));

            return Result.Success(MapToDto(widget));
        }

        public async Task<Result<WidgetDto>> CreateWidget(CreateWidgetDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            var count = await db.Widgets.CountAsync(w => w.OwnerId == user.Id) +
                await db.EntriesWidgets.CountAsync(w => w.OwnerId == user.Id);
            if (count >= DataLimits.MaxWidgetCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("widgets", DataLimits.MaxWidgetCount));

            if (dto.Sources.Count == 0 || dto.Sources.Count > DataLimits.MaxDashboardItemSourceCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("widget sources", DataLimits.MaxDashboardItemSourceCount));

            if (!AnalyticDefinitionList.IsValidForType(dto.ResultType, dto.Code, dto.Grouping))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("code for this result type"));

            // A Goal is a single-source Single Value calculation plus a target. The target is
            // required and has to be a number or an hh:mm:ss duration; whether it matches the
            // value field's type is settled once BuildSourceFields has resolved that field.
            var goalTarget = dto.ResultType == AnalyticTypes.Goal ? dto.GoalTarget?.Trim() : null;
            if (dto.ResultType == AnalyticTypes.Goal && !GoalTargets.IsParseable(goalTarget))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("goal target, which must be a number or an hh:mm:ss duration"));

            var goalDirection = dto.ResultType == AnalyticTypes.Goal && !string.IsNullOrEmpty(dto.GoalDirection)
                ? dto.GoalDirection
                : null;

            // Only some calculations combine more than one tracker: line/bar merge into a
            // Composed chart, a calendar unions its dated events, and a correlation scatter
            // pairs exactly two trackers on a shared match field.
            if (AnalyticTypes.RequiresPairedSources(dto.ResultType, dto.Code))
            {
                if (dto.Sources.Count != 2)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("source count for a correlation chart, which pairs exactly two trackers"));
            }
            else if (dto.Sources.Count > 1 && !AnalyticTypes.SupportsMultipleSources(dto.ResultType))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("combining this widget with another tracker"));

            var sources = new List<WidgetSource>();

            foreach (var sourceDto in dto.Sources)
            {
                var tracker = await db.Trackers
                    .Include(t => t.ApplicationUserTrackers)
                    .FirstOrDefaultAsync(t => t.Id == sourceDto.TrackerId);

                var hasAccess = tracker != null &&
                    (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));

                if (tracker == null || !hasAccess)
                    return Result.Failure(ResultStatusCodes.Forbidden);

                var source = new WidgetSource
                {
                    Order = sources.Count,
                    TrackerId = sourceDto.TrackerId
                };

                var fieldsResult = await BuildSourceFields(dto.ResultType, dto.Code, dto.Grouping, sourceDto, source);
                if (!fieldsResult.IsSuccess)
                    return Result.Failure(fieldsResult.StatusCode, fieldsResult.Messages);

                sources.Add(source);
            }

            // The target has to be readable as the same kind of magnitude the calculation
            // produces: a duration for Sum/Average/Min/Max over a duration field, a plain
            // number for everything else (the counts always come out as numbers).
            if (dto.ResultType == AnalyticTypes.Goal)
            {
                var valueFieldId = sources[0].Fields.FirstOrDefault(f => f.Purpose == AnalyticPurposes.Value)?.FieldId;
                var valueField = valueFieldId != null
                    ? await db.Fields.FirstOrDefaultAsync(f => f.Id == valueFieldId)
                    : null;

                if (!GoalTargets.MatchesFieldType(dto.Code, valueField?.Type, goalTarget!))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("goal target for this field's type"));
            }

            var widget = new Widget
            {
                Name = dto.Name?.Trim() ?? string.Empty,
                Description = dto.Description?.Trim() ?? string.Empty,
                ResultType = dto.ResultType,
                Code = dto.Code,
                Grouping = string.IsNullOrEmpty(dto.Grouping) ? null : dto.Grouping,
                MatchedValuesOnly = dto.MatchedValuesOnly,
                GoalTarget = goalTarget,
                GoalDirection = goalDirection,
                OwnerId = user.Id,
                Sources = sources
            };

            db.Widgets.Add(widget);
            await db.SaveChangesAsync();

            var saved = await GetOwnedWidget(widget.Id);
            return Result.Success(MapToDto(saved!));
        }

        public async Task<Result<WidgetDto>> UpdateWidget(string widgetId, UpdateWidgetDto dto)
        {
            var widget = await GetOwnedWidget(widgetId);
            if (widget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("widget"));

            widget.Name = dto.Name?.Trim() ?? string.Empty;
            widget.Description = dto.Description?.Trim() ?? string.Empty;

            // Only a Goal has a target, and only when the caller sends a new one. Its type
            // has to line up with the value field the same way it did at creation.
            if (widget.ResultType == AnalyticTypes.Goal && dto.GoalTarget != null)
            {
                var target = dto.GoalTarget.Trim();
                var valueFieldId = widget.Sources
                    .SelectMany(s => s.Fields)
                    .FirstOrDefault(f => f.Purpose == AnalyticPurposes.Value)?.FieldId;
                var valueField = valueFieldId != null
                    ? await db.Fields.FirstOrDefaultAsync(f => f.Id == valueFieldId)
                    : null;

                if (!GoalTargets.IsParseable(target) || !GoalTargets.MatchesFieldType(widget.Code, valueField?.Type, target))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("goal target for this field's type"));

                widget.GoalTarget = target;
            }

            if (widget.ResultType == AnalyticTypes.Goal && !string.IsNullOrEmpty(dto.GoalDirection))
                widget.GoalDirection = dto.GoalDirection;

            // The DbContext defaults to QueryTrackingBehavior.NoTracking (see
            // DatabaseConfiguration), so the mutation above is invisible to SaveChangesAsync
            // unless the entity is explicitly re-attached as Modified.
            db.Widgets.Update(widget);
            await db.SaveChangesAsync();

            return Result.Success(MapToDto(widget));
        }

        public async Task<Result> DeleteWidget(string widgetId)
        {
            var widget = await GetOwnedWidget(widgetId);
            if (widget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("widget"));

            // Cascades to every DashboardItem placing this widget (see OperumContext) -- it
            // disappears from every dashboard it was on, not just the library. The caller is
            // expected to have warned the user before getting here.
            db.Widgets.Remove(widget);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<List<EntriesWidgetDefinitionDto>>> GetEntriesWidgets(string? trackerId)
        {
            var user = currentUserService.GetCurrentUser();
            var query = db.EntriesWidgets.Include(w => w.Tracker).Where(w => w.OwnerId == user.Id);

            if (!string.IsNullOrEmpty(trackerId))
                query = query.Where(w => w.TrackerId == trackerId);

            var widgets = await query.ToListAsync();
            return Result.Success(widgets.Select(w => MapToDto(w)).ToList());
        }

        public async Task<Result<EntriesWidgetDefinitionDto>> GetEntriesWidget(string entriesWidgetId)
        {
            var widget = await GetOwnedEntriesWidget(entriesWidgetId);
            if (widget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("entries widget"));

            return Result.Success(MapToDto(widget));
        }

        public async Task<Result<EntriesWidgetDefinitionDto>> CreateEntriesWidget(CreateEntriesWidgetDto dto)
        {
            var user = currentUserService.GetCurrentUser();

            var count = await db.Widgets.CountAsync(w => w.OwnerId == user.Id) +
                await db.EntriesWidgets.CountAsync(w => w.OwnerId == user.Id);
            if (count >= DataLimits.MaxWidgetCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("widgets", DataLimits.MaxWidgetCount));

            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == dto.TrackerId);

            var hasAccess = tracker != null &&
                (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
                return Result.Failure(ResultStatusCodes.Forbidden);

            var widget = new EntriesWidget
            {
                Name = dto.Name?.Trim() ?? string.Empty,
                TrackerId = dto.TrackerId,
                OwnerId = user.Id
            };

            db.EntriesWidgets.Add(widget);
            await db.SaveChangesAsync();

            return Result.Success(MapToDto(widget, tracker.Name));
        }

        public async Task<Result<EntriesWidgetDefinitionDto>> UpdateEntriesWidget(string entriesWidgetId, UpdateEntriesWidgetDto dto)
        {
            var widget = await GetOwnedEntriesWidget(entriesWidgetId);
            if (widget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("entries widget"));

            widget.Name = dto.Name?.Trim() ?? string.Empty;
            db.EntriesWidgets.Update(widget);
            await db.SaveChangesAsync();

            return Result.Success(MapToDto(widget));
        }

        public async Task<Result> DeleteEntriesWidget(string entriesWidgetId)
        {
            var widget = await GetOwnedEntriesWidget(entriesWidgetId);
            if (widget == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("entries widget"));

            db.EntriesWidgets.Remove(widget);
            await db.SaveChangesAsync();

            return Result.Success();
        }

        // Validates the field mapping a source supplies against the widget's definition and,
        // if it holds up, fills source.Fields. Mirrors DashboardService.BuildSourceFields --
        // kept separate rather than shared because the two operate on different entity types
        // (WidgetSource vs DashboardItemSource) until Phase B3 unifies the placement path.
        private async Task<Result> BuildSourceFields(string resultType, string code, string? grouping, CreateWidgetSourceRequestDto dto, WidgetSource source)
        {
            var requiredPurposes = AnalyticDefinitionList.GetRequiredPurposes(resultType, code, grouping);
            var allowedPurposes = AnalyticDefinitionList.GetAllowedPurposes(resultType, code, grouping).ToHashSet();
            var suppliedPurposes = dto.Fields.Select(f => f.Purpose).ToList();

            if (suppliedPurposes.Count != suppliedPurposes.Distinct().Count() ||
                !suppliedPurposes.All(allowedPurposes.Contains) ||
                requiredPurposes.Any(p => !suppliedPurposes.Contains(p)))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Required($"a field for each of: {string.Join(", ", requiredPurposes)}"));

            foreach (var field in dto.Fields)
            {
                // Scoped to the source's tracker on purpose: a widget can span trackers, so
                // without this a caller could point a source at a field on some other tracker.
                var trackerField = await db.Fields
                    .FirstOrDefaultAsync(f => f.Id == field.FieldId && f.TrackerId == dto.TrackerId);

                if (trackerField == null)
                    return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound($"field for purpose {field.Purpose}"));

                if (!AnalyticDefinitionList.IsValidDataType(resultType, code, field.Purpose, trackerField.Type, grouping))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid("data type for purpose"));

                source.Fields.Add(new WidgetSourceField
                {
                    Purpose = field.Purpose,
                    FieldId = trackerField.Id
                });
            }

            return Result.Success();
        }

        private static IQueryable<Widget> WithSourceGraph(IQueryable<Widget> query) => query
            .Include(w => w.Sources).ThenInclude(s => s.Tracker)
            .Include(w => w.Sources).ThenInclude(s => s.Fields).ThenInclude(f => f.Field);

        private async Task<Widget?> GetOwnedWidget(string widgetId)
        {
            var user = currentUserService.GetCurrentUser();
            return await WithSourceGraph(db.Widgets)
                // Tracked explicitly the same way DashboardService.GetUserDashboard is: every
                // caller here either mutates + SaveChanges, or hands the graph to Remove(),
                // both of which need this tracked under the context-wide NoTracking default.
                .AsTracking()
                .FirstOrDefaultAsync(w => w.Id == widgetId && w.OwnerId == user.Id);
        }

        private async Task<EntriesWidget?> GetOwnedEntriesWidget(string entriesWidgetId)
        {
            var user = currentUserService.GetCurrentUser();
            return await db.EntriesWidgets
                .Include(w => w.Tracker)
                .AsTracking()
                .FirstOrDefaultAsync(w => w.Id == entriesWidgetId && w.OwnerId == user.Id);
        }

        // A widget named nothing falls back to its definition's own label (e.g. "Count"),
        // the same way a calculated AnalyticDto always ends up named -- see
        // AnalyticResultBuilder. No calculation is needed for this one, just the static
        // label lookup, so it happens here instead of waiting for a render pass.
        private static WidgetDto MapToDto(Widget w) => new()
        {
            Id = w.Id,
            Name = string.IsNullOrWhiteSpace(w.Name) ? AnalyticDefinitionList.GetLabel(w.ResultType, w.Code, w.Grouping) : w.Name,
            Description = w.Description,
            ResultType = w.ResultType,
            Code = w.Code,
            Grouping = w.Grouping,
            MatchedValuesOnly = w.MatchedValuesOnly,
            GoalTarget = w.GoalTarget,
            GoalDirection = w.GoalDirection,
            Sources = w.Sources.OrderBy(s => s.Order).Select(s => MapSourceToDto(w, s)).ToList()
        };

        private static WidgetSourceDto MapSourceToDto(Widget w, WidgetSource s)
        {
            var fields = s.Fields.Where(f => f.Field != null).ToList();

            return new WidgetSourceDto
            {
                Id = s.Id,
                Name = AnalyticDefinitionList.GetDisplayName(w.ResultType, w.Code, fields.Select(f => f.Field.Name), w.Grouping),
                Fields = fields
                    .Select(f => new WidgetSourceFieldDto { Purpose = f.Purpose, FieldId = f.FieldId, FieldName = f.Field.Name })
                    .ToList(),
                TrackerId = s.TrackerId,
                TrackerName = s.Tracker.Name,
                Order = s.Order
            };
        }

        // trackerNameOverride covers the just-created case: the new entity's Tracker nav
        // property isn't loaded (it was never queried with an Include), so the caller passes
        // the tracker it already looked up instead of the mapper re-querying for it.
        private static EntriesWidgetDefinitionDto MapToDto(EntriesWidget w, string? trackerNameOverride = null) => new()
        {
            Id = w.Id,
            Name = w.Name,
            TrackerId = w.TrackerId,
            TrackerName = trackerNameOverride ?? w.Tracker?.Name ?? string.Empty
        };
    }
}
