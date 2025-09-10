namespace Operum.Model.DTOs.Views
{
    public class ViewDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public List<ViewSortDto> Sorts { get; set; } = [];
        public List<ViewFilterDto> Filters { get; set; } = [];
    }
}
