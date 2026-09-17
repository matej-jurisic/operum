using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // Tracker and field mapping live on the shared WidgetSource; this only holds the
    // board-specific filter/label.
    public class DashboardItemSource
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }
        public string? Label { get; set; }

        // A filter widget can layer further clauses on top; that link lives on its own Config.
        public string? ViewId { get; set; }

        public string DashboardItemId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardItemId))]
        public virtual DashboardItem DashboardItem { get; set; } = null!;

        public string WidgetSourceId { get; set; } = string.Empty;
        [ForeignKey(nameof(WidgetSourceId))]
        public virtual WidgetSource WidgetSource { get; set; } = null!;
    }
}
