using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // One WidgetSource's placement on a board: how this board filters and labels it. Which
    // tracker it reads from and which fields fill its purposes are the shared definition and
    // live on the WidgetSource itself instead -- see WidgetSourceId below.
    public class DashboardItemSource
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }
        public string? Label { get; set; }

        // A source is filtered at most one way: either a fixed ViewId (unaffected by
        // anything else on the board), or LinkedViewWidgetId pointing at a
        // DashboardWidgetTypes.View item whose own selection decides the filter instead —
        // and can be changed live from the board. PlaceWidget rejects both being set.
        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }
        [ForeignKey(nameof(LinkedViewWidgetId))]
        public virtual DashboardItem? LinkedViewWidget { get; set; }

        public string DashboardItemId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardItemId))]
        public virtual DashboardItem DashboardItem { get; set; } = null!;

        // The shared widget source this placement's field mapping and tracker come from.
        public string WidgetSourceId { get; set; } = string.Empty;
        [ForeignKey(nameof(WidgetSourceId))]
        public virtual WidgetSource WidgetSource { get; set; } = null!;
    }
}
