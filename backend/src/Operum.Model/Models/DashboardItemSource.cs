using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // One tracker's contribution to a dashboard item: which tracker to read entries from,
    // which of its fields fill the purposes required by the item's ResultType/Code, and
    // which view (if any) narrows the entries first. The definition only exists here, so
    // it never shows up among the tracker's own analytics and disappears with the item.
    public class DashboardItemSource
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }
        public string? Label { get; set; }
        public string? ViewId { get; set; }

        public string DashboardItemId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardItemId))]
        public virtual DashboardItem DashboardItem { get; set; } = null!;

        public virtual List<DashboardItemSourceField> Fields { get; set; } = [];

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;
    }
}
