using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Notifications
{
    /// <summary>
    /// Renders the {fieldValueList} push-body token for an Entry-mode notification: one line per
    /// entry, each field the user picked as "Display" rendered "Name: value", entries capped so a
    /// large batch of newly-matching entries can't blow up the push body.
    /// </summary>
    public static class NotificationFieldValueListBuilder
    {
        private const int MaxEntries = 5;

        public static string Build(IReadOnlyList<Entry> orderedEntries, IReadOnlyList<string> displayFieldIds)
        {
            if (displayFieldIds.Count == 0 || orderedEntries.Count == 0)
                return string.Empty;

            var lines = orderedEntries
                .Take(MaxEntries)
                .Select(e => BuildEntryLine(e, displayFieldIds))
                .Where(line => line.Length > 0)
                .ToList();

            var remaining = orderedEntries.Count - MaxEntries;
            if (remaining > 0)
                lines.Add(remaining == 1 ? "and 1 more" : $"and {remaining} more");

            return string.Join("\n", lines);
        }

        private static string BuildEntryLine(Entry entry, IReadOnlyList<string> displayFieldIds)
        {
            var parts = displayFieldIds
                .Select(fieldId => entry.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId))
                .Where(fv => fv != null)
                .Select(fv => $"{fv!.Field.Name}: {fv.GetValueAsString() ?? "-"}");

            return string.Join(", ", parts);
        }
    }
}
