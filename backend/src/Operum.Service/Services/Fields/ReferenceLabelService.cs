using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Constants.Fields;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using System.Globalization;

namespace Operum.Service.Services.Fields
{
    public class ReferenceLabelService(OperumContext db) : IReferenceLabelService
    {
        public async Task ResolveEntryReferences(string entryId, List<FieldValue> currentFieldValues, List<Field> allFields)
        {
            var referenceFields = allFields
                .Where(f => f.Type == DataTypes.Reference)
                .ToDictionary(f => f.Id);
            if (referenceFields.Count == 0)
                return;

            var toResolve = currentFieldValues
                .Where(fv => referenceFields.ContainsKey(fv.FieldId) && fv.ReferencedEntryId != null)
                .ToList();
            if (toResolve.Count == 0)
                return;

            var targets = await LoadTargets(toResolve.Select(fv => fv.ReferencedEntryId!));
            var displayFields = await LoadDisplayFields(referenceFields.Values);

            foreach (var fv in toResolve)
                ApplyLabel(fv, referenceFields[fv.FieldId], targets, displayFields);

            await db.SaveChangesAsync();
        }

        public async Task RefreshReferencesToEntry(string changedEntryId)
        {
            // AsTracking: the context runs no-tracking by default, so without it the label writes
            // below would be silently dropped on SaveChanges.
            var referencing = await db.FieldValues
                .AsTracking()
                .Include(fv => fv.Field)
                .Where(fv => fv.ReferencedEntryId == changedEntryId)
                .ToListAsync();
            if (referencing.Count == 0)
                return;

            var targets = await LoadTargets([changedEntryId]);
            var displayFields = await LoadDisplayFields(referencing.Select(fv => fv.Field));

            foreach (var fv in referencing)
                ApplyLabel(fv, fv.Field, targets, displayFields);

            await db.SaveChangesAsync();
        }

        public async Task RefreshFieldReferences(string fieldId)
        {
            var field = await db.Fields.FirstOrDefaultAsync(f => f.Id == fieldId);
            if (field == null || field.Type != DataTypes.Reference)
                return;

            var values = await db.FieldValues
                .AsTracking()
                .Where(fv => fv.FieldId == fieldId && fv.ReferencedEntryId != null)
                .ToListAsync();
            if (values.Count == 0)
                return;

            var targets = await LoadTargets(values.Select(fv => fv.ReferencedEntryId!));
            var displayFields = await LoadDisplayFields([field]);

            foreach (var fv in values)
                ApplyLabel(fv, field, targets, displayFields);

            await db.SaveChangesAsync();
        }

        private async Task<Dictionary<string, Entry>> LoadTargets(IEnumerable<string> entryIds)
        {
            var ids = entryIds.Distinct().ToList();
            var entries = await db.Entries
                .AsNoTracking()
                .Include(e => e.FieldValues)
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();
            return entries.ToDictionary(e => e.Id);
        }

        private async Task<Dictionary<string, Field>> LoadDisplayFields(IEnumerable<Field> referenceFields)
        {
            var ids = referenceFields
                .Select(f => f.ReferencedDisplayFieldId)
                .Where(id => id != null)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                return [];

            var fields = await db.Fields
                .AsNoTracking()
                .Where(f => ids.Contains(f.Id))
                .ToListAsync();
            return fields.ToDictionary(f => f.Id);
        }

        private static void ApplyLabel(
            FieldValue fv,
            Field referenceField,
            Dictionary<string, Entry> targets,
            Dictionary<string, Field> displayFields)
        {
            if (fv.ReferencedEntryId == null
                || !targets.TryGetValue(fv.ReferencedEntryId, out var target)
                || target.TrackerId != referenceField.ReferencedTrackerId)
            {
                fv.ReferencedEntryId = null;
                fv.StringValue = null;
                return;
            }

            Field? displayField = referenceField.ReferencedDisplayFieldId != null
                && displayFields.TryGetValue(referenceField.ReferencedDisplayFieldId, out var df)
                    ? df
                    : null;

            fv.StringValue = DisplayLabel(target, displayField);
        }

        private static string DisplayLabel(Entry target, Field? displayField)
        {
            if (displayField != null)
            {
                var fv = target.FieldValues.FirstOrDefault(v => v.FieldId == displayField.Id);
                if (fv != null)
                {
                    // target is loaded AsNoTracking, so seeding the nav is harmless.
                    fv.Field = displayField;
                    var label = fv.GetValueAsString();
                    if (!string.IsNullOrWhiteSpace(label))
                        return label;
                }
            }

            return target.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
