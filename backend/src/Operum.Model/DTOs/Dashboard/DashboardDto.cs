namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public List<DashboardItemDto> Items { get; set; } = [];
    }
}
