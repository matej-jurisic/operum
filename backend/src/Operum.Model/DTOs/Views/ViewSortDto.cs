using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Views
{
    public class ViewSortDto
    {
        public string Id { get; set; } = string.Empty;
        public FieldDto Field { get; set; } = null!;
        public bool Descending { get; set; } = false;
    }
}
