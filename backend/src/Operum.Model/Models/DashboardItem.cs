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

        // What this item renders: Analytic/Entries place a shared Widget/EntriesWidget (see
        // below); every other type configures itself entirely through Config instead.
        public string Type { get; set; } = DashboardWidgetTypes.Analytic;

        // Widget settings as JSON, for widget types whose configuration isn't a placed
        // definition. Null for Analytic widgets; for Entries, just the placement-only
        // column list (see EntriesWidgetConfigDto) since the tracker lives on EntriesWidget.
        public string? Config { get; set; }

        // Where the item sits on the wide grid, in DashboardGrid.Columns columns.
        // A zero width means the item predates layouts and the client places it itself.
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        // The same item's placement on the narrow grid a phone renders, in
        // DashboardGrid.MobileColumns columns. Kept apart from the wide placement above so
        // arranging the board on a phone cannot overwrite the desktop arrangement, and vice
        // versa. Filled in whenever an item is created, so a zero width here means the same
        // thing it does above.
        public int MobileX { get; set; }
        public int MobileY { get; set; }
        public int MobileW { get; set; }
        public int MobileH { get; set; }

        // Whether this item renders as a small button on the wide grid that opens the real
        // widget in a modal instead of drawing it inline. Analytic/Entries widgets only —
        // kept apart from the narrow grid's copy below the same way every other layout
        // property is, so a chart can be collapsed on the phone but drawn in full on desktop.
        public bool Expandable { get; set; }
        public bool MobileExpandable { get; set; }

        // Line chart widgets only: whether the y-axis is anchored at zero (the default) or
        // fitted to the data's own range. Fitting is what makes a series that only ever
        // moves between, say, 1000 and 1100 readable instead of a flat line pinned to the
        // top of a 0-based axis. Ignored by every other widget type.
        public bool YAxisFromZero { get; set; } = true;

        public string DashboardId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardId))]
        public virtual Dashboard Dashboard { get; set; } = null!;

        // The Container item this one sits inside, or null when it sits on the board
        // itself. Only ever set on non-Container items, and only one level deep. When a
        // container is deleted its children are reparented to the board (ParentItemId
        // nulled) rather than deleted -- see DashboardService.RemoveDashboardItem and the
        // SetNull delete behavior in OperumContext.
        public string? ParentItemId { get; set; }
        [ForeignKey(nameof(ParentItemId))]
        public virtual DashboardItem? ParentItem { get; set; }
        public virtual List<DashboardItem> Children { get; set; } = [];

        // The shared chart definition this Analytic-type item places on the board. Deleting
        // the Widget takes every placement of it with it (see OperumContext) -- a placement
        // can't render without a definition.
        public string? WidgetId { get; set; }
        [ForeignKey(nameof(WidgetId))]
        public virtual Widget? Widget { get; set; }

        // The shared Entries definition this Entries-type item places -- the Entries
        // equivalent of WidgetId above.
        public string? EntriesWidgetId { get; set; }
        [ForeignKey(nameof(EntriesWidgetId))]
        public virtual EntriesWidget? EntriesWidget { get; set; }

        public virtual List<DashboardItemSource> Sources { get; set; } = [];
    }
}
