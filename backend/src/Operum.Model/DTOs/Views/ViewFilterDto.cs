using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Views
{
    public class ViewFilterDto
    {
        public string Id { get; set; } = string.Empty;
        public FieldDto Field { get; set; } = null!;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
