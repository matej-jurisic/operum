using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // A named, dashboard-scoped collection of field-agnostic clauses -- "Current Month",
    // "This Quarter", "All Time". A view selector widget offers a set of these as its
    // dropdown; picking one re-filters every widget wired to that selector, resolving each
    // clause against the field the selector maps it to on that widget's tracker.
    public class DashboardView
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }

        public string DashboardId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardId))]
        public virtual Dashboard Dashboard { get; set; } = null!;

        public virtual List<DashboardViewQuery> DashboardViewQueries { get; set; } = [];
    }
}
