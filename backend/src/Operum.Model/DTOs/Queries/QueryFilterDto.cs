using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Queries
{
    public class QueryFilterDto
    {
        public string Id { get; set; } = string.Empty;
        public FieldDto Field { get; set; } = null!;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
