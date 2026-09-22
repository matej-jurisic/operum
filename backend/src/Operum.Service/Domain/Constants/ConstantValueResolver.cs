using Operum.Model.Models;

namespace Operum.Service.Domain.Constants
{
    public static class ConstantValueResolver
    {
        /// <summary>Returns the raw string of whichever conditional value's filters match first (by priority), or the base value if none match.</summary>
        public static string ResolveRawValue(
            TrackerConstant constant,
            Dictionary<string, FieldValue> fieldValuesByFieldId,
            Dictionary<string, Field> fieldsById,
            TimeZoneInfo tz)
        {
            if (constant.Values.Count == 0)
                return constant.Value;

            var match = constant.Values
                .OrderBy(v => v.Priority)
                .FirstOrDefault(v => EntryFilterMatcher.Matches(v.Filters, fieldValuesByFieldId, fieldsById, tz));

            return match?.Value ?? constant.Value;
        }
    }
}
