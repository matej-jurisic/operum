namespace Operum.Model.DTOs.Dashboard.Requests
{
    // All-or-nothing reorder: the payload must name exactly the dashboard's own
    // DashboardViews, and Order is reassigned from position in the list.
    public class ReorderDashboardViewsDto
    {
        public List<string> DashboardViewIds { get; set; } = [];
    }
}
