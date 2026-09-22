using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Domain.Constants;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;
using System.Text.RegularExpressions;

namespace Operum.Service.Services.Fields
{
    public class FieldsService(ICurrentUserService currentUserService, IMapper mapper, OperumContext db, ILogger<FieldsService> logger, IReferenceLabelService referenceLabelService) : IFieldsService
    {
        private static readonly Regex TokenPattern = new(@"\{([^}]+)\}", RegexOptions.Compiled);

        public async Task<Result<FieldDto>> CreateField(string trackerId, CreateFieldDto field)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));
            }

            var fieldCount = await db.Fields.Where(x => x.TrackerId == trackerId).CountAsync();
            if (fieldCount >= DataLimits.MaxFieldCount)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("fields", DataLimits.MaxFieldCount));
            }

            if (!DataTypes.IsValid(field.Type)) return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("field type"));

            if (field.IsCalculated)
            {
                var formulaError = await ValidateFormula(trackerId, field.Formula!, field.Name, null);
                if (formulaError != null)
                    return Result.Failure(ResultStatusCodes.BadRequest, formulaError);
                field.Required = false;
            }

            if (field.Type == DataTypes.Reference)
            {
                var refError = await ValidateReferenceConfig(field.ReferencedTrackerId, field.ReferencedDisplayFieldId, user.Id);
                if (refError != null)
                    return Result.Failure(ResultStatusCodes.BadRequest, refError);
                field.IsCalculated = false;
            }

            var defaultError = await ValidateDefaultValueConstant(trackerId, field.DefaultValueConstantId, field.Type);
            if (defaultError != null)
                return Result.Failure(ResultStatusCodes.BadRequest, defaultError);

            var visibilityError = await ValidateVisibilityCondition(trackerId, null, field.VisibilityFieldId, field.VisibilityOperator);
            if (visibilityError != null)
                return Result.Failure(ResultStatusCodes.BadRequest, visibilityError);

            var newField = mapper.Map<CreateFieldDto, Field>(field);

            if (newField.Type != DataTypes.Reference)
            {
                newField.ReferencedTrackerId = null;
                newField.ReferencedDisplayFieldId = null;
            }

            newField.TrackerId = trackerId;

            var maxOrder = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .MaxAsync(x => (int?)x.Order) ?? 0;
            newField.Order = maxOrder + 1;

            await db.Fields.AddAsync(newField);
            await db.SaveChangesAsync();

            var created = await GetField(trackerId, newField.Id);
            return Result.Success(created.Data);
        }

        public async Task<Result> DeleteField(string trackerId, string fieldId)
        {
            var user = currentUserService.GetCurrentUser();
            var field = await db.Fields
                .Include(x => x.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            var isOwner = field?.Tracker.OwnerId == user.Id;
            var userTracker = field?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (field == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("field"));
            }

            // Every dependent row (widget source field mappings, field values, view filters and
            // sorts) cascades away at the database level. A widget survives losing one field
            // mapping and falls back to a degraded render instead of disappearing (see
            // WidgetSourceField's FK config in OperumContext and AnalyticResultBuilder).
            await db.Fields.Where(x => x.Id == fieldId).ExecuteDeleteAsync();

            await ReorderFieldsAfterDeletion(trackerId, field.Order);

            return Result.Success();
        }

        public async Task<Result<FieldDto>> GetField(string trackerId, string fieldId)
        {
            var user = currentUserService.GetCurrentUser();
            var field = await db.Fields
                 .Include(x => x.Tracker)
                    .ThenInclude(x => x.ApplicationUserTrackers)
                 .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            var hasAccess = field != null && (field.Tracker.OwnerId == user.Id || field.Tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (field == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden, Messages.ItemNotFound("field"));
            }

            return Result.Success(mapper.Map<Field, FieldDto>(field));
        }

        public async Task<Result<List<FieldDto>>> GetFieldList(string trackerId)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                 .Include(x => x.ApplicationUserTrackers)
                 .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden, Messages.ItemNotFound("tracker"));
            }

            var fields = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.Order)
                .ToListAsync();

            return Result.Success(mapper.Map<List<Field>, List<FieldDto>>(fields));
        }

        public async Task<Result> ReorderFields(string trackerId, ReorderFieldsDto reorderFields)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);

            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));
            }

            var existingFields = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .Select(x => x.Id)
                .ToListAsync();

            var requestedFieldIds = reorderFields.FieldIds.ToHashSet();
            var existingFieldIds = existingFields.ToHashSet();

            if (!requestedFieldIds.SetEquals(existingFieldIds))
            {
                return Result.Failure(ResultStatusCodes.BadRequest);
            }

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < reorderFields.FieldIds.Count; i++)
                {
                    var fieldId = reorderFields.FieldIds[i];
                    var field = await db.Fields.FindAsync(fieldId);

                    if (field != null && field.TrackerId == trackerId)
                    {
                        field.Order = i + 1;
                        db.Fields.Update(field);
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Exception occurred while reordering fields.");
                return Result.Failure(ResultStatusCodes.Error);
            }
        }

        public async Task<Result<FieldDto>> UpdateField(string trackerId, string fieldId, UpdateFieldDto field)
        {
            var user = currentUserService.GetCurrentUser();
            var originalField = await db.Fields
                .Include(x => x.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            var isOwnerField = originalField?.Tracker.OwnerId == user.Id;
            var userTrackerField = originalField?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (originalField == null || (!isOwnerField && userTrackerField?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("field"));
            }

            if (!DataTypes.IsValid(field.Type)) return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("field type"));

            if (field.IsCalculated)
            {
                var formulaError = await ValidateFormula(trackerId, field.Formula!, field.Name, fieldId);
                if (formulaError != null)
                    return Result.Failure(ResultStatusCodes.BadRequest, formulaError);
                field.Required = false;
            }
            else
            {
                // Switching from calculated to manual clears formula
                field.Formula = null;
            }

            if (field.Type == DataTypes.Reference)
            {
                var refError = await ValidateReferenceConfig(field.ReferencedTrackerId, field.ReferencedDisplayFieldId, user.Id);
                if (refError != null)
                    return Result.Failure(ResultStatusCodes.BadRequest, refError);
                field.IsCalculated = false;
            }

            var defaultError = await ValidateDefaultValueConstant(trackerId, field.DefaultValueConstantId, field.Type);
            if (defaultError != null)
                return Result.Failure(ResultStatusCodes.BadRequest, defaultError);

            var visibilityError = await ValidateVisibilityCondition(trackerId, fieldId, field.VisibilityFieldId, field.VisibilityOperator);
            if (visibilityError != null)
                return Result.Failure(ResultStatusCodes.BadRequest, visibilityError);

            var wasReference = originalField.Type == DataTypes.Reference;
            var referenceTargetChanged = field.Type == DataTypes.Reference
                && (originalField.ReferencedTrackerId != field.ReferencedTrackerId
                    || originalField.ReferencedDisplayFieldId != field.ReferencedDisplayFieldId);

            mapper.Map(field, originalField, (s, d) =>
            {
                d.SelectOptions = s.SelectOptions != null
                    ? System.Text.Json.JsonSerializer.Serialize(s.SelectOptions)
                    : null;
                d.IsCalculated = s.IsCalculated;
                d.Formula = s.IsCalculated ? s.Formula : null;
                if (s.Type != DataTypes.Reference)
                {
                    d.ReferencedTrackerId = null;
                    d.ReferencedDisplayFieldId = null;
                }
            });
            db.Fields.Update(originalField);
            await db.SaveChangesAsync();

            if (wasReference && field.Type != DataTypes.Reference)
            {
                // The field is no longer a reference: drop every link and cached label.
                await db.FieldValues
                    .Where(fv => fv.FieldId == fieldId && (fv.ReferencedEntryId != null || fv.StringValue != null))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.ReferencedEntryId, (string?)null)
                        .SetProperty(x => x.StringValue, (string?)null));
            }
            else if (referenceTargetChanged)
            {
                await referenceLabelService.RefreshFieldReferences(fieldId);
            }

            var updatedField = await GetField(trackerId, fieldId);
            return Result.Success(updatedField.Data);
        }

        // A calculated or reference field's value would land in the new tracker meaning nothing.
        private static readonly HashSet<string> NonExtractableTypes = [DataTypes.Reference];

        // Grouping-key separator. A unit separator never appears in a formatted field value,
        // so it can't make two different combinations collide.
        private const char KeySeparator = (char)0x1F;

        public async Task<Result<ExtractFieldsResultDto>> ExtractFields(string trackerId, ExtractFieldsDto extract)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));
            }

            var allFields = await db.Fields.Where(f => f.TrackerId == trackerId).ToListAsync();
            var fieldsById = allFields.ToDictionary(f => f.Id);

            var selectedIds = extract.FieldIds.Distinct().ToList();
            if (selectedIds.Any(id => !fieldsById.ContainsKey(id)))
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("field"));
            }

            var selectedFields = selectedIds
                .Select(id => fieldsById[id])
                .OrderBy(f => f.Order)
                .ThenBy(f => f.Id)
                .ToList();

            if (selectedFields.Any(f => f.IsCalculated || NonExtractableTypes.Contains(f.Type)))
            {
                return Result.Failure(ResultStatusCodes.BadRequest, "Calculated and reference fields cannot be extracted.");
            }

            var selectedNames = selectedFields.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var blockingCalculated = allFields.FirstOrDefault(f =>
                f.IsCalculated
                && !string.IsNullOrWhiteSpace(f.Formula)
                && TokenNames(f.Formula!).Any(selectedNames.Contains));
            if (blockingCalculated != null)
            {
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"The calculated field '{blockingCalculated.Name}' uses a field you are extracting. Update its formula first.");
            }

            var trackerCount = await db.Trackers.CountAsync(x => x.OwnerId == user.Id && x.TrackerTypeId == null);
            if (trackerCount >= DataLimits.MaxTrackerCount)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("trackers", DataLimits.MaxTrackerCount));
            }

            // Every value of every selected field, keyed by entry. Filtering on FieldId alone is
            // enough since a selected field only ever belongs to this tracker.
            var selectedIdSet = selectedIds.ToHashSet();
            var valuesByEntry = (await db.FieldValues
                    .Where(fv => selectedIdSet.Contains(fv.FieldId))
                    .ToListAsync())
                .GroupBy(fv => fv.EntryId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(fv => fv.FieldId));

            var entryIds = await db.Entries
                .Where(e => e.TrackerId == trackerId)
                .Select(e => e.Id)
                .ToListAsync();

            // entryId -> grouping key ("" means every selected value was blank: no link, no row).
            var keyByEntry = new Dictionary<string, string>();
            // key -> the raw display values to seed that combination's new-tracker row.
            var rawByKey = new Dictionary<string, Dictionary<string, string?>>();

            foreach (var entryId in entryIds)
            {
                valuesByEntry.TryGetValue(entryId, out var fvByField);
                var raw = new Dictionary<string, string?>();
                var parts = new List<string>(selectedFields.Count);
                var allBlank = true;

                foreach (var field in selectedFields)
                {
                    string? value = null;
                    if (fvByField != null && fvByField.TryGetValue(field.Id, out var fv))
                    {
                        fv.Field = field;
                        value = fv.GetValueAsString();
                    }
                    raw[field.Id] = value;
                    var normalized = (value ?? string.Empty).Trim();
                    if (normalized.Length > 0) allBlank = false;
                    parts.Add(normalized.ToLowerInvariant());
                }

                if (allBlank)
                {
                    keyByEntry[entryId] = string.Empty;
                    continue;
                }

                var key = string.Join(KeySeparator, parts);
                keyByEntry[entryId] = key;
                rawByKey.TryAdd(key, raw);
            }

            if (rawByKey.Count > DataLimits.MaxEntryCount)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("entries", DataLimits.MaxEntryCount));
            }

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var newTracker = new Tracker
                {
                    Name = extract.NewTrackerName.Trim(),
                    OwnerId = user.Id,
                    Color = tracker.Color,
                    Icon = tracker.Icon,
                };
                await db.Trackers.AddAsync(newTracker);
                await db.SaveChangesAsync();
                var newTrackerId = newTracker.Id;
                var newTrackerName = newTracker.Name;

                var newFieldByOldId = new Dictionary<string, Field>();
                var order = 1;
                foreach (var field in selectedFields)
                {
                    var copy = new Field
                    {
                        Name = field.Name,
                        Description = field.Description,
                        Type = field.Type,
                        Required = field.Required,
                        Visible = field.Visible,
                        Order = order++,
                        SelectOptions = field.SelectOptions,
                        TrackerId = newTracker.Id,
                    };
                    newFieldByOldId[field.Id] = copy;
                    await db.Fields.AddAsync(copy);
                }
                await db.SaveChangesAsync();

                var newEntryByKey = new Dictionary<string, Entry>();
                foreach (var (key, raw) in rawByKey)
                {
                    var entry = new Entry { TrackerId = newTracker.Id, CreatedAt = DateTime.UtcNow };
                    await db.Entries.AddAsync(entry);

                    foreach (var field in selectedFields)
                    {
                        if (string.IsNullOrWhiteSpace(raw[field.Id])) continue;
                        var copy = newFieldByOldId[field.Id];
                        var fieldValue = new FieldValue { EntryId = entry.Id, FieldId = copy.Id };
                        fieldValue.SetFieldValue(copy, raw[field.Id]);
                        await db.FieldValues.AddAsync(fieldValue);
                    }

                    newEntryByKey[key] = entry;
                }
                await db.SaveChangesAsync();

                var referenceField = new Field
                {
                    Name = extract.ReferenceFieldName.Trim(),
                    Type = DataTypes.Reference,
                    Order = selectedFields.Min(f => f.Order),
                    TrackerId = trackerId,
                    ReferencedTrackerId = newTracker.Id,
                    ReferencedDisplayFieldId = extract.DisplayFieldId != null
                        && newFieldByOldId.TryGetValue(extract.DisplayFieldId, out var displayCopy)
                            ? displayCopy.Id
                            : null,
                };
                await db.Fields.AddAsync(referenceField);
                await db.SaveChangesAsync();

                var referenceValues = entryIds
                    .Where(id => keyByEntry[id].Length > 0)
                    .Select(id => new FieldValue
                    {
                        EntryId = id,
                        FieldId = referenceField.Id,
                        ReferencedEntryId = newEntryByKey[keyByEntry[id]].Id,
                    })
                    .ToList();
                await db.FieldValues.AddRangeAsync(referenceValues);
                await db.SaveChangesAsync();

                var referenceFieldId = referenceField.Id;

                // Everything above is persisted; clear the tracker so the closing renumber can
                // attach its own instances without an identity clash.
                db.ChangeTracker.Clear();

                await db.Fields.Where(f => selectedIdSet.Contains(f.Id)).ExecuteDeleteAsync();

                var remaining = await db.Fields
                    .AsTracking()
                    .Where(f => f.TrackerId == trackerId)
                    .OrderBy(f => f.Order)
                    .ThenBy(f => f.Id)
                    .ToListAsync();
                for (var i = 0; i < remaining.Count; i++)
                {
                    remaining[i].Order = i + 1;
                }
                await db.SaveChangesAsync();

                await referenceLabelService.RefreshFieldReferences(referenceFieldId);

                await transaction.CommitAsync();

                return Result.Success(new ExtractFieldsResultDto
                {
                    NewTrackerId = newTrackerId,
                    NewTrackerName = newTrackerName,
                    ReferenceFieldId = referenceFieldId,
                    ExtractedEntryCount = newEntryByKey.Count,
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Exception occurred while extracting fields from tracker {TrackerId}.", trackerId);
                return Result.Failure(ResultStatusCodes.Error);
            }
        }

        private async Task<string?> ValidateReferenceConfig(string? referencedTrackerId, string? displayFieldId, string userId)
        {
            if (string.IsNullOrEmpty(referencedTrackerId))
                return "A reference field needs a tracker to link to.";

            var target = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == referencedTrackerId);

            var canRead = target != null
                && (target.OwnerId == userId || target.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == userId));
            if (!canRead)
                return Messages.ItemNotFound("referenced tracker");

            if (!string.IsNullOrEmpty(displayFieldId))
            {
                var displayField = await db.Fields.FirstOrDefaultAsync(f => f.Id == displayFieldId);
                if (displayField == null || displayField.TrackerId != referencedTrackerId)
                    return "The display field must belong to the referenced tracker.";
                if (displayField.Type == DataTypes.Reference)
                    return "The display field cannot itself be a reference field.";
            }

            return null;
        }

        private async Task<string?> ValidateDefaultValueConstant(string trackerId, string? constantId, string fieldType)
        {
            if (string.IsNullOrEmpty(constantId))
                return null;

            var constant = await db.TrackerConstants.FirstOrDefaultAsync(c => c.Id == constantId && c.TrackerId == trackerId);
            if (constant == null)
                return Messages.ItemNotFound("constant");

            if (!DataTypes.AreCompatible(constant.Type, fieldType))
                return $"The linked constant's type ('{constant.Type}') is not compatible with this field's type ('{fieldType}').";

            return null;
        }

        private async Task<string?> ValidateVisibilityCondition(string trackerId, string? currentFieldId, string? visibilityFieldId, string? visibilityOperator)
        {
            if (string.IsNullOrEmpty(visibilityFieldId))
                return null;

            if (visibilityFieldId == currentFieldId)
                return "A field's visibility condition cannot reference itself.";

            var target = await db.Fields.FirstOrDefaultAsync(f => f.Id == visibilityFieldId && f.TrackerId == trackerId);
            if (target == null)
                return Messages.ItemNotFound("field");

            if (target.IsCalculated)
                return "A field's visibility condition cannot reference a calculated field.";

            if (string.IsNullOrEmpty(visibilityOperator) || !OperatorTypes.IsValid(visibilityOperator))
                return "Unknown operator for the visibility condition.";

            return null;
        }

        public async Task<Result<ResolveDefaultValuesResponseDto>> ResolveDefaultValues(string trackerId, ResolveDefaultValuesDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);

            var hasAccess = tracker != null &&
                (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id));
            if (tracker == null || !hasAccess)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("tracker"));

            var fields = await db.Fields.Where(f => f.TrackerId == trackerId).ToListAsync();

            var defaultBearingFields = fields
                .Where(f => f.DefaultValueConstantId != null || !string.IsNullOrEmpty(f.DefaultValue))
                .ToList();
            var visibilityBearingFields = fields
                .Where(f => f.VisibilityFieldId != null)
                .ToList();
            if (defaultBearingFields.Count == 0 && visibilityBearingFields.Count == 0)
                return Result.Success(new ResolveDefaultValuesResponseDto());

            var constantIds = defaultBearingFields
                .Where(f => f.DefaultValueConstantId != null)
                .Select(f => f.DefaultValueConstantId!)
                .Distinct()
                .ToList();

            var constants = constantIds.Count == 0
                ? new List<TrackerConstant>()
                : await db.TrackerConstants
                    .Include(c => c.Values)
                        .ThenInclude(v => v.Filters)
                    .Where(c => constantIds.Contains(c.Id))
                    .ToListAsync();
            var constantsById = constants.ToDictionary(c => c.Id, c => c);

            var fieldsById = fields.ToDictionary(f => f.Id, f => f);
            var fieldsByName = fields.ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

            var fieldValuesByFieldId = new Dictionary<string, FieldValue>();
            foreach (var (name, raw) in dto.FieldValues)
            {
                if (!fieldsByName.TryGetValue(name, out var field) || field.IsCalculated)
                    continue;

                var fv = new FieldValue { FieldId = field.Id };
                fv.SetFieldValue(field, raw);
                fieldValuesByFieldId[field.Id] = fv;
            }

            var tz = currentUserService.GetCurrentUserTimeZone();
            var response = new ResolveDefaultValuesResponseDto();

            foreach (var field in defaultBearingFields)
            {
                string? rawValue = null;
                if (field.DefaultValueConstantId != null && constantsById.TryGetValue(field.DefaultValueConstantId, out var constant))
                {
                    rawValue = ConstantValueResolver.ResolveRawValue(constant, fieldValuesByFieldId, fieldsById, tz);
                }
                else if (!string.IsNullOrEmpty(field.DefaultValue))
                {
                    rawValue = field.DefaultValue;
                }

                if (rawValue == null)
                    continue;

                if (field.Type == DataTypes.Date || field.Type == DataTypes.DateTime)
                {
                    var resolved = DynamicDateTokens.ResolveValue(rawValue, tz);
                    if (resolved == null)
                        continue;
                    rawValue = resolved.Value.ToString("O");
                }

                response.Defaults.Add(new ResolvedDefaultValueDto
                {
                    FieldId = field.Id,
                    FieldName = field.Name,
                    Value = rawValue,
                });
            }

            foreach (var field in visibilityBearingFields)
            {
                var visible = EntryFilterMatcher.Matches(
                    [new TrackerConstantValueFilter
                    {
                        FieldId = field.VisibilityFieldId!,
                        Operator = field.VisibilityOperator!,
                        Value = field.VisibilityValue,
                    }],
                    fieldValuesByFieldId,
                    fieldsById,
                    tz);

                response.Visibility.Add(new ResolvedFieldVisibilityDto
                {
                    FieldId = field.Id,
                    FieldName = field.Name,
                    Visible = visible,
                });
            }

            return Result.Success(response);
        }

        // Strip optional ".property" suffix (e.g. "Duration.hours" → "Duration")
        private static string TokenName(string token) =>
            token.Contains('.') ? token[..token.IndexOf('.')] : token;

        private static List<string> TokenNames(string formula) =>
            TokenPattern.Matches(formula).Select(m => TokenName(m.Groups[1].Value)).ToList();

        /// <param name="fieldId">Id of the field being updated, or null when creating one.</param>
        private async Task<string?> ValidateFormula(string trackerId, string formula, string fieldName, string? fieldId)
        {
            var tokens = TokenPattern.Matches(formula).Select(m => m.Groups[1].Value).ToList();
            if (tokens.Count == 0)
                return null;

            var fields = await db.Fields
                .Where(f => f.TrackerId == trackerId)
                .Select(f => new { f.Id, f.Name, f.IsCalculated, f.Formula })
                .ToListAsync();

            var constantNames = await db.TrackerConstants
                .Where(c => c.TrackerId == trackerId)
                .Select(c => c.Name)
                .ToListAsync();

            // The field being saved is excluded: referencing it would only be a circular reference.
            var validNames = new HashSet<string>(
                fields.Where(f => f.Id != fieldId).Select(f => f.Name).Concat(constantNames),
                StringComparer.OrdinalIgnoreCase);

            foreach (var token in tokens)
            {
                var name = TokenName(token);
                if (validNames.Contains(name))
                    continue;

                return name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)
                    ? "Formula cannot reference the field itself."
                    : $"Unknown token '{token}' in formula. Only fields and constants can be referenced.";
            }

            // Walk the chain of calculated fields this formula pulls in, using the pending formula
            // for the field being saved, and reject if it leads back to itself.
            var formulasByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields.Where(f => f.IsCalculated && !string.IsNullOrWhiteSpace(f.Formula) && f.Id != fieldId))
                formulasByName[field.Name] = field.Formula!;
            formulasByName[fieldName] = formula;

            if (HasCircularReference(fieldName, formulasByName))
                return "Formula creates a circular reference between calculated fields.";

            return null;
        }

        private static bool HasCircularReference(string startName, Dictionary<string, string> formulasByName)
        {
            var onPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var settled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool Visit(string name)
            {
                if (onPath.Contains(name)) return true;
                if (settled.Contains(name)) return false;
                if (!formulasByName.TryGetValue(name, out var formula))
                {
                    settled.Add(name);
                    return false;
                }

                onPath.Add(name);
                foreach (var referenced in TokenNames(formula))
                {
                    if (Visit(referenced))
                        return true;
                }
                onPath.Remove(name);
                settled.Add(name);
                return false;
            }

            return Visit(startName);
        }

        private async Task ReorderFieldsAfterDeletion(string trackerId, int deletedOrder)
        {
            var fieldsToUpdate = await db.Fields
                .Where(x => x.TrackerId == trackerId && x.Order > deletedOrder)
                .ToListAsync();

            foreach (var field in fieldsToUpdate)
            {
                field.Order -= 1;
                db.Fields.Update(field);
            }

            await db.SaveChangesAsync();
        }
    }
}