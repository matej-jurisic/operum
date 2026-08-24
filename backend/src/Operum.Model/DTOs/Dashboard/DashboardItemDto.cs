namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int Order { get; set; }
        // The single analytic definition every source below is calculated with.
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public List<DashboardItemSourceDto> Sources { get; set; } = [];
    }
}
