using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;
using System.Text.RegularExpressions;

namespace Operum.Service.Services.Fields
{
    public class FieldsService(ICurrentUserService currentUserService, IMapper mapper, OperumContext db, ILogger<FieldsService> logger) : IFieldsService
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
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            var fieldCount = await db.Fields.Where(x => x.TrackerId == trackerId).CountAsync();
            if (fieldCount >= DataLimits.MaxFieldCount)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("fields", DataLimits.MaxFieldCount));
            }

            if (!DataTypes.IsValid(field.Type)) return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("field type"));

            if (field.IsCalculated)
            {
                var formulaError = await ValidateFormula(trackerId, field.Formula!);
                if (formulaError != null)
                    return Result.Failure(ResultStatusCodes.BadRequest, formulaError);
                field.Required = false;
            }

            var newField = mapper.Map<CreateFieldDto, Field>(field);

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
                .Include(x => x.AnalyticFields)
                    .ThenInclude(af => af.Analytic)
                .Include(x => x.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            var isOwner = field?.Tracker.OwnerId == user.Id;
            var userTracker = field?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (field == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            var fieldAnalytics = field.AnalyticFields.Select(x => x.AnalyticId);

            db.Analytics.RemoveRange(field.AnalyticFields.Select(x => x.Analytic));

            db.Fields.Remove(field);
            await db.SaveChangesAsync();

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
                return Result.Failure(ResultStatusCodes.Forbidden);
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
                return Result.Failure(ResultStatusCodes.Forbidden);
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
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            var existingFields = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .Select(x => x.Id)
                .ToListAsync();

            var requestedFieldIds = reorderFields.FieldIds.ToHashSet();
            var existingFieldIds = existingFields.ToHashSet();

            // Check if all requested field IDs exist and belong to tracker
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
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            if (!DataTypes.IsValid(field.Type)) return Result.Failure(ResultStatusCodes.BadRequest, Messages.NotAllowed("field type"));

            if (field.IsCalculated)
            {
                var formulaError = await ValidateFormula(trackerId, field.Formula!);
                if (formulaError != null)
                    return Result.Failure(ResultStatusCodes.BadRequest, formulaError);
                field.Required = false;
            }
            else
            {
                // Switching from calculated to manual clears formula
                field.Formula = null;
            }

            mapper.Map(field, originalField, (s, d) =>
            {
                d.SelectOptions = s.SelectOptions != null
                    ? System.Text.Json.JsonSerializer.Serialize(s.SelectOptions)
                    : null;
                d.IsCalculated = s.IsCalculated;
                d.Formula = s.IsCalculated ? s.Formula : null;
            });
            db.Fields.Update(originalField);
            await db.SaveChangesAsync();

            var updatedField = await GetField(trackerId, fieldId);
            return Result.Success(updatedField.Data);
        }

        private async Task<string?> ValidateFormula(string trackerId, string formula)
        {
            var tokens = TokenPattern.Matches(formula).Select(m => m.Groups[1].Value).ToList();
            if (tokens.Count == 0)
                return null;

            var manualFieldNames = await db.Fields
                .Where(f => f.TrackerId == trackerId && !f.IsCalculated)
                .Select(f => f.Name)
                .ToListAsync();

            var constantNames = await db.TrackerConstants
                .Where(c => c.TrackerId == trackerId)
                .Select(c => c.Name)
                .ToListAsync();

            var validNames = new HashSet<string>(
                manualFieldNames.Concat(constantNames),
                StringComparer.OrdinalIgnoreCase);

            foreach (var token in tokens)
            {
                // Strip optional ".property" suffix (e.g. "Duration.hours" → "Duration")
                var name = token.Contains('.') ? token[..token.IndexOf('.')] : token;
                if (!validNames.Contains(name))
                    return $"Unknown token '{token}' in formula. Only manual fields and constants can be referenced.";
            }

            return null;
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