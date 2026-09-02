using Operum.Model.Constants.Fields;
using Operum.Model.Integrations;
using Operum.Model.Models;

namespace Operum.Service.Domain.Integrations
{
    /// <summary>
    /// Checks a target's mappings before they are saved, so a bad one is refused at the point
    /// the user made it rather than surfacing much later as a sync error with no obvious cause.
    /// Returns null when everything is valid, otherwise the first problem found.
    /// </summary>
    public static class MappingValidator
    {
        /// <summary>
        /// What a tracker field of each type may be fed from. Mostly an exact match; the two
        /// exceptions are date/datetime, which are one storage column and already
        /// interchangeable everywhere else, and timespan into number, which lets a duration be
        /// tracked as raw seconds by anyone who would rather have a plain number to do
        /// arithmetic on.
        /// </summary>
        private static readonly Dictionary<string, string[]> AcceptedFieldTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [DataTypes.Number] = [DataTypes.Number],
            [DataTypes.String] = [DataTypes.String],
            [DataTypes.Bool] = [DataTypes.Bool],
            [DataTypes.TimeSpan] = [DataTypes.TimeSpan, DataTypes.Number],
            [DataTypes.Date] = [DataTypes.Date, DataTypes.DateTime],
            [DataTypes.DateTime] = [DataTypes.Date, DataTypes.DateTime],
        };

        public static string? Validate(
            IReadOnlyList<FieldMapping> mappings,
            IReadOnlyList<SourceField> catalog,
            IReadOnlyList<Field> trackerFields)
        {
            if (mappings.Count == 0)
                return "A target needs at least one field mapping.";

            var catalogByKey = catalog.ToDictionary(f => f.Key, f => f, StringComparer.OrdinalIgnoreCase);
            var fieldsById = trackerFields.ToDictionary(f => f.Id, f => f);
            var seenFieldIds = new HashSet<string>();

            foreach (var mapping in mappings)
            {
                if (!catalogByKey.TryGetValue(mapping.SourceKey, out var sourceField))
                    return $"'{mapping.SourceKey}' is not a value this integration provides.";

                if (!fieldsById.TryGetValue(mapping.FieldId, out var field))
                    return "A mapped field does not belong to this tracker.";

                // A calculated field is derived from a formula; writing to it would be
                // overwritten on the next evaluation anyway.
                if (field.IsCalculated)
                    return $"'{field.Name}' is a calculated field and cannot be filled by an integration.";

                // The stored index enforces this too, but the message here can name the field.
                if (!seenFieldIds.Add(mapping.FieldId))
                    return $"'{field.Name}' is mapped more than once; a field can only have one source.";

                if (!AcceptedFieldTypes.TryGetValue(sourceField.Type, out var accepted))
                    return $"'{sourceField.Label}' has an unrecognised type '{sourceField.Type}'.";

                if (!accepted.Contains(field.Type, StringComparer.OrdinalIgnoreCase))
                    return $"'{sourceField.Label}' is a {sourceField.Type} and cannot fill '{field.Name}', which is a {field.Type}.";

                // With SkipWhenNull the mapper omits the key whenever the provider reports no
                // value, and the writer refuses to create an entry missing a required field --
                // so every record without this metric would be dropped, silently and forever.
                // Clearing instead is the only coherent pairing.
                if (field.Required && mapping.SkipWhenNull)
                    return $"'{field.Name}' is required, so its mapping cannot skip empty values -- a record without '{sourceField.Label}' could never be imported.";
            }

            return null;
        }
    }
}
