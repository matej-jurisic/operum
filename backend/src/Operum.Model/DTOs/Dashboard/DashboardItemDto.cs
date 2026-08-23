namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<DashboardItemSourceDto> Sources { get; set; } = [];
    }
}
