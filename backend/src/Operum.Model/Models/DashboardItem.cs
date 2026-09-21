using Operum.Model.Constants;
using Operum.Model.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class DashboardItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // What the board document calls this item: readable, unique on its board, given the
        // first time the board is exported and kept from then on. Null until then.
        public string? Key { get; set; }

        // Reading order of the board (top-left to bottom-right), derived from the grid
        // placement whenever the layout is saved.
        public int Order { get; set; }

        public string Type { get; set; } = DashboardWidgetTypes.Analytic;

        // Null for Analytic; for Entries, just the column list (EntriesWidgetConfigDto).
        public string? Config { get; set; }

        // In DashboardGrid.Columns columns. A zero width means the item predates layouts and
        // the client places it itself.
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        // In DashboardGrid.MobileColumns columns; kept apart from the desktop placement so
        // arranging on one doesn't overwrite the other.
        public int MobileX { get; set; }
        public int MobileY { get; set; }
        public int MobileW { get; set; }
        public int MobileH { get; set; }

        // Analytic/Entries widgets only.
        public DashboardItemDisplayMode DisplayMode { get; set; }
        public DashboardItemDisplayMode MobileDisplayMode { get; set; }

        // Line chart widgets only; ignored by every other widget type.
        public bool YAxisFromZero { get; set; } = true;

        // Goal widgets only: ordered JSON list of GoalConditionalTargetDto. Board-scoped
        // (names this board's pooled query ids). Null or "[]" means the default always applies.
        public string? GoalConditionalTargets { get; set; }

        // Mantine color name; null means no override. Placement-scoped like YAxisFromZero.
        // Ignored for a combined (multi-source) widget.
        public string? Color { get; set; }

        // Single-source SingleValue/Goal placements only.
        public bool ShowTrend { get; set; } = true;

        public string DashboardId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardId))]
        public virtual Dashboard Dashboard { get; set; } = null!;

        // Only one level deep. On container delete, children are reparented to the board
        // (ParentItemId nulled) rather than deleted -- see DashboardService.RemoveDashboardItem
        // and the SetNull behavior in OperumContext.
        public string? ParentItemId { get; set; }
        [ForeignKey(nameof(ParentItemId))]
        public virtual DashboardItem? ParentItem { get; set; }
        public virtual List<DashboardItem> Children { get; set; } = [];

        // Names a tab id in the parent's Config (TabsContainerConfigDto.Tabs), not a row, so
        // there is no FK. Deleting a tab repoints its children to the first surviving tab.
        public string? ParentTabId { get; set; }

        // Deleting the Widget takes every placement of it with it (see OperumContext).
        public string? WidgetId { get; set; }
        [ForeignKey(nameof(WidgetId))]
        public virtual Widget? Widget { get; set; }

        public string? EntriesWidgetId { get; set; }
        [ForeignKey(nameof(EntriesWidgetId))]
        public virtual EntriesWidget? EntriesWidget { get; set; }

        public virtual List<DashboardItemSource> Sources { get; set; } = [];
    }
}
