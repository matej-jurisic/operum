using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Queries
{
    public class QuerySortDto
    {
        public string Id { get; set; } = string.Empty;
        public FieldDto Field { get; set; } = null!;
        public int Order { get; set; }
        public bool Descending { get; set; } = false;
    }
}
