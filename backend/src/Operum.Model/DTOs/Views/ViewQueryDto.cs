using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Views
{
    // Filter vs sort is told apart by Kind; the unused half is left at its default.
    public class ViewQueryDto
    {
        public string Kind { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public FieldDto Field { get; set; } = null!;

        public string? Operator { get; set; }
        public string? Value { get; set; }

        public bool Descending { get; set; }
    }
}
