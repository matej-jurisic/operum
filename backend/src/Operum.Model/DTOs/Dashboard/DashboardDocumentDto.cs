using Operum.Model.Common;
using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Dashboard
{
    // The board as an editable document: everything a placement decides for itself, with the
    // wiring (sources, filter links, goal targets) echoed read-only for context. A save must
    // name every item already on the board exactly once; adding and removing widgets stays in
    // the UI, where the cascades live.
    public class DashboardDocumentDto
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public DashboardDocumentBoardDto Board { get; set; } = new();
        public List<DashboardDocumentItemDto> Items { get; set; } = [];
    }

    public class DashboardDocumentBoardDto
    {
        // Read-only: rejected on save if changed.
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Color { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Icon { get; set; }
    }

    // DisplayMode is a DashboardDocumentDisplayModes value rather than the stored enum's
    // number, since this document is read and written by hand.
    public class DashboardDocumentLayoutDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public string DisplayMode { get; set; } = string.Empty;
    }

    public class DashboardDocumentTabDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // Every source of a widget, named for reading rather than editing: what a placement can
    // change about its sources is edited through the widget's own dialog.
    public class DashboardDocumentSourceDto
    {
        public string Id { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? ViewId { get; set; }
        public List<string> Fields { get; set; } = [];
    }

    // Read-only context. A save that changed anything here is rejected rather than ignored,
    // so an edit meant to rewire the board never silently disappears.
    public class DashboardDocumentWiringDto
    {
        public List<DashboardDocumentSourceDto>? Sources { get; set; }
        public FilterWidgetConfigDto? Filter { get; set; }
        public List<GoalConditionalTargetDto>? GoalConditionalTargets { get; set; }
    }

    // A field left out is left alone; a field set to null is cleared. Text, Tabs and
    // ColumnFieldIds have no null state, so null is refused there rather than silently
    // meaning nothing. Order is derived from the desktop placement on every save and is
    // deliberately absent.
    public class DashboardDocumentItemDto
    {
        public string Id { get; set; } = string.Empty;

        // Read-only: rejected on save if changed.
        public string? Type { get; set; }
        public string? Name { get; set; }

        // Null puts the widget back on the board itself.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> ParentItemId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> ParentTabId { get; set; }
        public DashboardDocumentLayoutDto? Layout { get; set; }
        public DashboardDocumentLayoutDto? MobileLayout { get; set; }
        // Null is "Auto": the tracker's color, or the board's.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Color { get; set; }
        public bool? ShowTrend { get; set; }
        public bool? YAxisFromZero { get; set; }

        // Header/Note/Container text; a TabsContainer's title. Empty clears it.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Text { get; set; }

        // TabsContainer only. Tabs can be renamed and reordered, not added or removed.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<List<DashboardDocumentTabDto>> Tabs { get; set; }

        // Entries widgets only. Empty shows every field.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<List<string>> ColumnFieldIds { get; set; }

        // Read-only: rejected on save if changed.
        public DashboardDocumentWiringDto? Wiring { get; set; }
    }
}
