using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // A reusable chart definition: the calculation (ResultType/Code) plus the tracker
    // source(s) that feed it. Not owned by any one dashboard or tracker -- it can be
    // placed on any number of dashboards via DashboardItem.WidgetId, and editing it here
    // is what every one of those placements shows.
    public class Widget
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        // Combined charts only: restricts the chart to the x-axis values every source has
        // a point for. Ignored by a single-source widget.
        public bool MatchedValuesOnly { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        [ForeignKey(nameof(OwnerId))]
        public virtual User Owner { get; set; } = null!;

        public virtual List<WidgetSource> Sources { get; set; } = [];
    }
}
