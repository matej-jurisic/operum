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

        // Line/Bar only: how the axis field is bucketed before Code aggregates each bucket
        // (see AnalyticGroupings). Null for every other result type, whose calculation is
        // the Code alone.
        public string? Grouping { get; set; }

        // Combined charts only: restricts the chart to the x-axis values every source has
        // a point for. Ignored by a single-source widget.
        public bool MatchedValuesOnly { get; set; }

        // Goal widgets only (ResultType == "Goal"): the target the calculated value is shown
        // as progress toward, stored as a string in the value field's own format (an
        // invariant number, or hh:mm:ss for a duration). Null for every other result type.
        public string? GoalTarget { get; set; }

        // Goal widgets only: a Constants.Analytics.GoalDirections value. Null behaves as
        // HigherIsBetter (the original behavior, before a goal could be a cap/budget).
        public string? GoalDirection { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        [ForeignKey(nameof(OwnerId))]
        public virtual User Owner { get; set; } = null!;

        public virtual List<WidgetSource> Sources { get; set; } = [];
    }
}
