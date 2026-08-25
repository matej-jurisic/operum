using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Queries
{
    // One clause: a filter (Field/Operator/Value) or a sort (Field/Descending), told apart
    // by Kind. The unused half is left at its default rather than being sent as null noise.
    public class QueryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public FieldDto Field { get; set; } = null!;

        public string? Operator { get; set; }
        public string? Value { get; set; }

        public bool Descending { get; set; }
    }
}
