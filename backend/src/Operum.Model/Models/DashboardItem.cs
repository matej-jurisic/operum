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

        public string DashboardId { get; set; } = string.Empty;
        [ForeignKey(nameof(DashboardId))]
        public virtual Dashboard Dashboard { get; set; } = null!;

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
