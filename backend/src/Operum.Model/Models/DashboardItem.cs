using Operum.Model.Constants;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class DashboardItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Kept as the reading order of the board (top-left to bottom-right), derived from
        // the grid placement below whenever the layout is saved.
        public int Order { get; set; }

        // What this item renders. Only DashboardWidgetTypes.Analytic exists today, so the
        // analytic definition below is always filled in; a widget type that isn't a chart
        // would leave it empty and configure itself through Config instead.
        public string Type { get; set; } = DashboardWidgetTypes.Analytic;

        // Widget settings as JSON, for widget types whose configuration isn't an analytic
        // definition. Null for analytic widgets.
        public string? Config { get; set; }

        // Where the item sits on the dashboard grid, in DashboardGrid.Columns columns.
        // A zero width means the item predates layouts and the client places it itself.
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        // The analytic definition every source of this item is calculated with. It lives
        // on the item rather than on each source so a multi-tracker chart can only ever
        // combine series that were produced the same way.
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        // Combined charts only: restricts the chart to the x-axis values every source has a
        // point for, so the series are compared over the same range instead of each source
        // trailing off wherever its own data stops. Ignored by a single-source item.
        public bool MatchedValuesOnly { get; set; }

        public string DashboardId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardId))]
        public virtual Dashboard Dashboard { get; set; } = null!;

        public virtual List<DashboardItemSource> Sources { get; set; } = [];
    }
}
