namespace Operum.Model.DTOs.Dashboard
{
    // SlotId (not QueryId) is what a value and a follower's field mapping key off, since two
    // slots may resolve to the same pooled QueryId. ValueBySlot changes re-filter every
    // follower (see SetFilterValues); an unset/empty slot is left unapplied. PresetIds names
    // DashboardViews whose clause shape matches exactly; picking one just writes ValueBySlot.
    //
    // Pre-slot configs stored ValueByQuery/FieldByQuery keyed by pooled query id;
    // DashboardService.TryParseFilterConfig folds those into slots on read.
    public class FilterWidgetConfigDto
    {
        public List<FilterClauseSlotDto> Slots { get; set; } = [];
        public Dictionary<string, string?> ValueBySlot { get; set; } = [];
        public List<WidgetLinkDto> Links { get; set; } = [];

        public List<string> PresetIds { get; set; } = [];
    }

    public class FilterClauseSlotDto
    {
        public string SlotId { get; set; } = string.Empty;
        public string QueryId { get; set; } = string.Empty;
    }
}
