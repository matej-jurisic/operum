using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class DashboardItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }

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
