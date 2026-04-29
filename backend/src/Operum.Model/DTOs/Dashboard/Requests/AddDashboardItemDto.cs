using System.ComponentModel.DataAnnotations;

namespace Operum.Model.DTOs.Dashboard.Requests
{
    public class AddDashboardItemDto
    {
        [Required]
        public string AnalyticId { get; set; } = string.Empty;
        [Required]
        public string TrackerId { get; set; } = string.Empty;
        public List<string> ViewIds { get; set; } = [];
    }
}
