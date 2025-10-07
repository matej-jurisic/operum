using Operum.Model.Models;

namespace Operum.Model.Extensions
{
    public static class AnalyticFieldExtensions
    {
        public static Dictionary<string, Field> MapByPurpose(
            this IEnumerable<AnalyticField> fields)
            => fields.ToDictionary(f => f.Purpose, f => f.Field);
    }
}
