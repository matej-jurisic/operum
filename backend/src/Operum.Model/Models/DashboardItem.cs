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

        public string DashboardId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardId))]
        public virtual Dashboard Dashboard { get; set; } = null!;

        public virtual List<DashboardItemSource> Sources { get; set; } = [];
    }
}
