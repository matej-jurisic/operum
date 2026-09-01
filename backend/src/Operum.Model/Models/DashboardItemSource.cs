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

        // The fixed tracker view this source reads through, if any. A view selector widget
        // on the board can layer further clauses on top of it (see DashboardService), but
        // that link lives on the selector's Config, not here.
        public string? ViewId { get; set; }

        public string DashboardItemId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardItemId))]
        public virtual DashboardItem DashboardItem { get; set; } = null!;

        // The shared widget source this placement's field mapping and tracker come from.
        public string WidgetSourceId { get; set; } = string.Empty;
        [ForeignKey(nameof(WidgetSourceId))]
        public virtual WidgetSource WidgetSource { get; set; } = null!;
    }
}
