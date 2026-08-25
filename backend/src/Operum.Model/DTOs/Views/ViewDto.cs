using Operum.Model.DTOs.Queries;

namespace Operum.Model.DTOs.Views
{
    public class ViewDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Ordered: precedence for sort-merge (first-field-wins) and display order.
        public List<QueryDto> Queries { get; set; } = [];
    }
}
