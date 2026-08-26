using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // The shared definition of an Entries (read-only table) widget: just which tracker it
    // reads from. Not owned by any one dashboard -- placed the same way a chart Widget is,
    // via DashboardItem.EntriesWidgetId.
    public class EntriesWidget
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public string OwnerId { get; set; } = string.Empty;
        [ForeignKey(nameof(OwnerId))]
        public virtual User Owner { get; set; } = null!;
    }
}
