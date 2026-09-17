namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Payload must name exactly the dashboard's own DashboardViews; Order is reassigned by position.
    public class ReorderDashboardViewsDto
    {
        public List<string> DashboardViewIds { get; set; } = [];
    }
}
