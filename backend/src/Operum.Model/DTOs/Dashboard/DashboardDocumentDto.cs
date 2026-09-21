using Operum.Model.Common;
using Operum.Model.Constants;
using System.Text.Json.Serialization;

namespace Operum.Model.DTOs.Dashboard
{
    // The board as one hand-editable document, complete enough to build a board from nothing,
    // and free of ids: an item is named by its key, everything else by its name. A key that
    // matches an item on the board updates it, any other key is a new item, and an item on the
    // board that the document leaves out is deleted.
    public class DashboardDocumentDto
    {
        public const int CurrentSchemaVersion = 3;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public DashboardDocumentBoardDto Board { get; set; } = new();
        public List<DashboardDocumentItemDto> Items { get; set; } = [];
    }

    public class DashboardDocumentBoardDto
    {
        public string Name { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Color { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Icon { get; set; }

        // Named clause sets a filter widget can offer, told apart by name. Left out, the board's
        // presets stay as they are; present, the list is the whole set and any preset it omits
        // is deleted.
        public List<DashboardDocumentPresetDto>? Presets { get; set; }
    }

    public class DashboardDocumentPresetDto
    {
        public string Name { get; set; } = string.Empty;
        public List<DashboardDocumentClauseDto> Clauses { get; set; } = [];
    }

    // One clause of a preset or of a filter widget. Key names a filter widget's clause so a
    // link's fields and a goal's conditions can refer to it; a preset has no use for one.
    public class DashboardDocumentClauseDto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Key { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Kind { get; set; }
        public string DataType { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Operator { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Descending { get; set; }
    }

    // DisplayMode is a DashboardDocumentDisplayModes value rather than the stored enum's
    // number, since this document is read and written by hand.
    public class DashboardDocumentLayoutDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public string DisplayMode { get; set; } = DashboardDocumentDisplayModes.Full;
    }

    // One source of an analytic widget. The tracker and fields are the Library widget's and
    // fixed once it exists; the label and the fixed view belong to this placement. Fields are
    // "Purpose: Field name".
    public class DashboardDocumentSourceDto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TrackerName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Label { get; set; }
        // A view of the tracker, by name. Null clears it.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> View { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Fields { get; set; }
    }

    // The Widget Library definition behind an analytic placement. Fixed once it exists;
    // authored only on a new item, which creates it in the Library.
    public class DashboardDocumentWidgetDto
    {
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Grouping { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool MatchedValuesOnly { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? GoalTarget { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? GoalDirection { get; set; }
    }

    // A filter widget's clauses, the followers it filters and the presets it offers.
    public class DashboardDocumentFilterDto
    {
        public List<DashboardDocumentClauseDto> Clauses { get; set; } = [];
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Presets { get; set; }
        public List<DashboardDocumentFilterLinkDto> Links { get; set; } = [];
    }

    // Makes one widget follow the filter: Item is its key, Fields maps a clause key to the
    // field of the tracker that clause filters. The tracker is only named for a widget that
    // reads more than one.
    public class DashboardDocumentFilterLinkDto
    {
        public string Item { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TrackerName { get; set; }
        public Dictionary<string, string> Fields { get; set; } = [];
    }

    // What an item is wired to. Only the parts that fit the item's type apply. A part left out
    // is left alone on an existing item; the Library definition (widget, sources' trackers and
    // fields, quickAdd/entries tracker) can only be read there, and a save that changed it is
    // rejected rather than ignored.
    public class DashboardDocumentWiringDto
    {
        // Analytic or entries, on a new item: the Widget Library widget to place, by name.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Library { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DashboardDocumentWidgetDto? Widget { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DashboardDocumentSourceDto>? Sources { get; set; }
        // Conditions are keyed by a filter clause's key.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GoalConditionalTargetDto>? GoalConditionalTargets { get; set; }

        // QuickAdd and entries.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TrackerName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DashboardDocumentFilterDto? Filter { get; set; }
    }

    // A field left out is left alone; a field set to null is cleared. Text, Tabs and Columns
    // have no null state, so null is refused there rather than silently meaning nothing. Order
    // is derived from the desktop placement on every save and is deliberately absent. Layout,
    // MobileLayout, and DisplayMode may be left out of a new item, which is then placed below
    // what is already on the board.
    public class DashboardDocumentItemDto
    {
        // Letters, digits, dot, dash and underscore. Given to an item the first time its board
        // is exported, and the item's name in the document from then on.
        public string Key { get; set; } = string.Empty;

        // Required on a new item; read-only on an existing one.
        public string? Type { get; set; }
        // On a new analytic or entries item this names the Library widget; read-only otherwise.
        public string? Name { get; set; }

        // The key of the container this sits in. Null puts the widget back on the board itself.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Parent { get; set; }
        // The name of the tab, inside a tabs container.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string> Tab { get; set; }
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

        // TabsContainer only: the tab names, in order. The list is the whole set.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<List<string>> Tabs { get; set; }

        // Entries widgets only: field names. Empty shows every field.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<List<string>> Columns { get; set; }

        public DashboardDocumentWiringDto? Wiring { get; set; }
    }
}
