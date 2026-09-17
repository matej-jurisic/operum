using Operum.Model.Constants.Fields;
using Operum.Model.Integrations;
using Operum.Model.Models;

namespace Operum.Service.Domain.Integrations
{
    // Returns null when every mapping is valid, otherwise the first problem found.
    public static class MappingValidator
    {
        // Mostly an exact type match; date/datetime are interchangeable, and timespan may feed a number field.
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

                if (field.IsCalculated)
                    return $"'{field.Name}' is a calculated field and cannot be filled by an integration.";

                // The stored index enforces this too, but the message here can name the field.
                if (!seenFieldIds.Add(mapping.FieldId))
                    return $"'{field.Name}' is mapped more than once; a field can only have one source.";

                if (!AcceptedFieldTypes.TryGetValue(sourceField.Type, out var accepted))
                    return $"'{sourceField.Label}' has an unrecognised type '{sourceField.Type}'.";

                if (!accepted.Contains(field.Type, StringComparer.OrdinalIgnoreCase))
                    return $"'{sourceField.Label}' is a {sourceField.Type} and cannot fill '{field.Name}', which is a {field.Type}.";

                // SkipWhenNull on a required field would silently drop every record missing this metric.
                if (field.Required && mapping.SkipWhenNull)
                    return $"'{field.Name}' is required, so its mapping cannot skip empty values -- a record without '{sourceField.Label}' could never be imported.";
            }

            return null;
        }
    }
}
