using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Entries;
using Operum.Model.DTOs.Fields;
using Operum.Model.Enums;

namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardWidgetLayoutDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        // Analytic/Entries widgets only: how this grid draws the widget — inline, as a
        // button that opens the real thing in a modal, or not at all.
        public DashboardItemDisplayMode DisplayMode { get; set; }
    }

    // The tracker summary a QuickAdd widget's button needs — resolved server-side from
    // Config's trackerId so the client can render the button immediately instead of
    // fetching the tracker itself once the card mounts.
    public class QuickAddTrackerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
    }

    // One clause of a Filter widget's own typed clause set, resolved for the card to
    // render an input for: what it filters (data type + operator, shown as a label) and the
    // value currently typed on the board (null when unset -- the clause is then not applied).
    // QueryId is the pooled query id, the key SetFilterValues writes back under.
    public class FilterClauseDto
    {
        public string QueryId { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Operator { get; set; }
        public string? Value { get; set; }
    }

    // One preset a Filter widget offers -- a DashboardView on the same board whose clause
    // shape matches the widget's, resolved to its id, name and the value per clause in the
    // widget's own clause order, so picking it on the board just fills those value inputs.
    public class FilterPresetOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string?> Values { get; set; } = [];
    }

    // What a DashboardWidgetTypes.Filter widget's card needs -- resolved server-side from
    // Config the same way QuickAddTrackerDto is: the widget's own filter clauses with their
    // current values, and the matching-shape DashboardViews it offers as presets.
    public class FilterWidgetDto
    {
        public List<FilterClauseDto> Clauses { get; set; } = [];
        public List<FilterPresetOptionDto> Presets { get; set; } = [];
    }

    // What a DashboardWidgetTypes.Entries widget's table needs — resolved server-side the
    // same way QuickAddTrackerDto is: the tracker it reads from, the columns to show in
    // order (Config's ColumnFieldIds, or every field when it names none), and the rows
    // themselves, already filtered/sorted by whatever view selectors this placement follows
    // and capped to the most recent handful. Unlike the tracker page, the card does not
    // fetch its own rows -- the board hands them over.
    public class EntriesWidgetDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public List<FieldDto> Columns { get; set; } = [];
        public List<EntryDto> Entries { get; set; } = [];
    }

    // One item of a dashboard as the client renders it: where it sits on each of the two
    // grids, what kind of widget it is, and the payload that kind needs. An analytic widget
    // carries the chart calculated for it; a QuickAdd widget carries the tracker its button
    // opens instead; a Filter widget carries its typed clauses and presets; an Entries
    // widget carries its table's tracker, columns and rows.
    public class DashboardWidgetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        // The Container widget this one sits inside on the wide grid, or null when it sits
        // on the board itself. Layout below is then relative to that container's sub-grid.
        // Always null on the narrow grid, where containers are flattened away.
        public string? ParentItemId { get; set; }
        // Placement on the wide grid, in DashboardGrid.Columns columns.
        public DashboardWidgetLayoutDto Layout { get; set; } = new();
        // Placement on the narrow grid, in DashboardGrid.MobileColumns columns. The client
        // picks between the two by its own width, and only writes back the one it rendered.
        public DashboardWidgetLayoutDto MobileLayout { get; set; } = new();
        public string? Config { get; set; }
        public AnalyticDto? Analytic { get; set; }
        public QuickAddTrackerDto? QuickAddTracker { get; set; }
        public FilterWidgetDto? Filter { get; set; }
        public EntriesWidgetDto? EntriesWidget { get; set; }
        // The color of the single tracker every source of this widget reads from. Null when
        // the widget has no single owning tracker — a combined chart spanning more than one —
        // so the client falls back to the dashboard's own color instead.
        public string? TrackerColor { get; set; }
    }
}
