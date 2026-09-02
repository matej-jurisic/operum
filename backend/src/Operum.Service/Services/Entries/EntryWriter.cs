using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Entries
{
    public class EntryWriter(
        OperumContext db,
        IFormulaEvaluationService formulaEvaluationService,
        ILogger<EntryWriter> logger) : IEntryWriter
    {
        // One bad mapping would otherwise produce a message per record in the batch.
        private const int MaxReportedErrors = 20;

        public async Task<EntryWriteResult> ApplyAsync(
            string trackerId,
            string source,
            IReadOnlyList<EntryWriteRecord> records,
            List<Field> fields,
            TimeZoneInfo timeZone,
            CancellationToken ct = default)
        {
            if (records.Count == 0)
                return EntryWriteResult.Empty;

            // A batch carrying the same external id twice would break the unique index the
            // moment both were inserted. Last one wins: providers send revisions in order.
            var deduped = records
                .GroupBy(r => r.ExternalId)
                .Select(g => g.Last())
                .ToList();

            var externalIds = deduped.Select(r => r.ExternalId).ToList();

            // AsTracking because the context runs no-tracking by default, which also skips
            // identity resolution -- the same FieldValue row could otherwise materialise
            // twice and conflict on save. The same reason NotificationEvaluatorService does it.
            var existingEntries = await db.Entries
                .AsTracking()
                .Include(e => e.FieldValues)
                .Where(e => e.TrackerId == trackerId
                    && e.Source == source
                    && e.ExternalId != null
                    && externalIds.Contains(e.ExternalId))
                .ToListAsync(ct);

            var existingByExternalId = existingEntries
                .ToDictionary(e => e.ExternalId!, e => e);

            var writableFields = fields.Where(f => !f.IsCalculated).ToDictionary(f => f.Id, f => f);
            var requiredFieldIds = writableFields.Values
                .Where(f => f.Required)
                .Select(f => f.Id)
                .ToList();

            var entryCount = await db.Entries.CountAsync(e => e.TrackerId == trackerId, ct);

            var errors = new List<string>();
            var errorCount = 0;
            int created = 0, updated = 0, deleted = 0, skipped = 0;

            void Report(string message)
            {
                errorCount++;
                if (errors.Count < MaxReportedErrors)
                    errors.Add(message);
            }

            // Entries whose calculated fields need recomputing once everything is saved,
            // each with the full set of values a formula may resolve against.
            var touched = new List<(Entry Entry, List<FieldValue> FieldValues)>();

            foreach (var record in deduped)
            {
                existingByExternalId.TryGetValue(record.ExternalId, out var entry);

                if (record.Operation == EntryWriteOperation.Delete)
                {
                    // A delete for something never imported is not a failure -- the provider
                    // is allowed to tell us about records we never took.
                    if (entry != null)
                    {
                        db.Entries.Remove(entry);
                        deleted++;
                    }
                    continue;
                }

                var isNew = entry == null;

                if (isNew)
                {
                    if (entryCount >= DataLimits.MaxEntryCount)
                    {
                        skipped++;
                        Report(Messages.MaxNumberReached("entries", DataLimits.MaxEntryCount));
                        continue;
                    }

                    // Only a create can leave a required field unset; an update inherits
                    // whatever the entry already holds.
                    var missing = requiredFieldIds
                        .Where(id => !record.ValuesByFieldId.TryGetValue(id, out var v) || string.IsNullOrWhiteSpace(v))
                        .ToList();

                    if (missing.Count > 0)
                    {
                        skipped++;
                        Report($"{record.ExternalId}: {Messages.Required(string.Join(", ", missing.Select(id => writableFields[id].Name)))}");
                        continue;
                    }

                    entry = new Entry
                    {
                        TrackerId = trackerId,
                        CreatedAt = DateTime.UtcNow,
                        Source = source,
                        ExternalId = record.ExternalId,
                        ExternalGroupId = record.GroupId,
                    };

                    await db.Entries.AddAsync(entry, ct);
                    entryCount++;
                }

                // This dictionary, not entry.FieldValues, is the entry's full set from here on.
                // A new value is added to the DbSet and to this, never to the navigation
                // collection: the entry is tracked, so EF fixup would put it there itself and
                // the collection would hold the same row twice -- which
                // EvaluateAndPersistCalculatedFields turns into a throw when it keys its
                // values by field id. The FK is carried by EntryId, which Entry assigns at
                // construction, so nothing needs the navigation to be right.
                var valuesByFieldId = entry!.FieldValues.ToDictionary(fv => fv.FieldId, fv => fv);

                foreach (var (fieldId, value) in record.ValuesByFieldId)
                {
                    if (!writableFields.TryGetValue(fieldId, out var field))
                    {
                        // The mapping outlived its field, or points at a calculated one.
                        // Cascades should prevent the first; refuse the second either way.
                        Report($"{record.ExternalId}: no writable field {fieldId} on this tracker");
                        continue;
                    }

                    if (!valuesByFieldId.TryGetValue(fieldId, out var fieldValue))
                    {
                        fieldValue = new FieldValue { EntryId = entry.Id, FieldId = fieldId };
                        await db.FieldValues.AddAsync(fieldValue, ct);
                        valuesByFieldId[fieldId] = fieldValue;
                    }

                    // The one coercion path in the codebase: every DataTypes branch, false on
                    // anything it cannot parse. A null clears the column.
                    if (!fieldValue.SetFieldValue(field, value))
                        Report($"{record.ExternalId}: could not read '{value}' as {field.Type} for {field.Name}");
                }

                // A record can move between groups upstream; keep the stored parent current so
                // reconciliation below looks at the right set.
                if (!isNew && record.GroupId != null)
                    entry.ExternalGroupId = record.GroupId;

                if (isNew) created++; else updated++;

                touched.Add((entry, [.. valuesByFieldId.Values]));
            }

            deleted += await ReconcileGroups(trackerId, source, deduped, ct);

            await db.SaveChangesAsync(ct);

            foreach (var (entry, fieldValues) in touched)
            {
                try
                {
                    await formulaEvaluationService.EvaluateAndPersistCalculatedFields(
                        trackerId, entry.Id, fieldValues, fields, timeZone);
                }
                catch (Exception ex)
                {
                    // A formula that throws must not cost us the entries already written.
                    logger.LogError(ex, "Calculated fields failed for entry {EntryId} on tracker {TrackerId}", entry.Id, trackerId);
                    Report($"{entry.ExternalId}: calculated fields failed");
                }
            }

            return new EntryWriteResult(created, updated, deleted, skipped, errorCount, errors);
        }

        /// <summary>
        /// Removes children a parent no longer has. A record carrying a GroupId promises the
        /// batch holds every current child of that parent, so anything stored under it that is
        /// not in the batch was deleted upstream -- a split dropped from a transaction, say.
        /// Without this the entry, and its money, would linger forever.
        /// </summary>
        private async Task<int> ReconcileGroups(
            string trackerId, string source, List<EntryWriteRecord> records, CancellationToken ct)
        {
            var idsByGroup = records
                .Where(r => r.GroupId != null && r.Operation == EntryWriteOperation.Upsert)
                .GroupBy(r => r.GroupId!)
                .ToDictionary(g => g.Key, g => g.Select(r => r.ExternalId).ToHashSet());

            if (idsByGroup.Count == 0)
                return 0;

            var groupIds = idsByGroup.Keys.ToList();

            var stored = await db.Entries
                .AsTracking()
                .Where(e => e.TrackerId == trackerId
                    && e.Source == source
                    && e.ExternalGroupId != null
                    && groupIds.Contains(e.ExternalGroupId))
                .ToListAsync(ct);

            var removed = 0;
            foreach (var entry in stored)
            {
                if (idsByGroup[entry.ExternalGroupId!].Contains(entry.ExternalId!))
                    continue;

                db.Entries.Remove(entry);
                removed++;
            }

            return removed;
        }
    }
}
