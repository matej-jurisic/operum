namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class ReorderDashboardsDto
    {
        public required List<string> DashboardIds { get; set; } = [];
    }
}
