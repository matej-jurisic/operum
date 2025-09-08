namespace Operum.Model.DTOs.Views
{
    public class ViewDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public List<ViewSortDto> Sorts { get; set; } = [];
    }
}
