namespace Operum.Model.DTOs.Queries
{
    public class QueryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public List<QuerySortDto> Sorts { get; set; } = [];
        public List<QueryFilterDto> Filters { get; set; } = [];
    }
}
