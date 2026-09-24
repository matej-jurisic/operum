namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Type { get; set; } = string.Empty;
        // The Container item this sits inside on the wide grid, or null on the board itself.
        public string? ParentItemId { get; set; }
        // Set when the parent is a TabsContainer; null otherwise.
        public string? ParentTabId { get; set; }
        public DashboardWidgetLayoutDto Layout { get; set; } = new();
        public DashboardWidgetLayoutDto MobileLayout { get; set; } = new();
        public string? Config { get; set; }
        // Empty for kinds with no name (View/QuickAdd/Header/Divider/Note). Lets a form label
        // this item without a second fetch; see ViewWidgetForm.
        public string Name { get; set; } = string.Empty;
        // The Widget/EntriesWidget's own name, before the auto-generated fallback above is
        // applied. Null when never set, so an edit form can leave its field blank rather than
        // filling in the computed fallback.
        public string? RawName { get; set; }
        // Lets a caller find which widgets a View selector for a given tracker could link.
        public List<string> TrackerIds { get; set; } = [];
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        // Line/Bar only.
        public string? Grouping { get; set; }
        // Combined charts only.
        public bool MatchedValuesOnly { get; set; }
        // Lets an edit form preload the current choice without a second fetch; see EditWidgetModal.
        public bool YAxisFromZero { get; set; } = true;
        // Goal widgets only, in order. Lets the edit form preload them without a second fetch.
        public List<GoalConditionalTargetDto> GoalConditionalTargets { get; set; } = [];
        // Null for "Auto" (the tracker/dashboard default).
        public string? Color { get; set; }
        public bool ShowTrend { get; set; } = true;
        // Calendar widgets only. Null for automatic.
        public string? CalendarStartMonth { get; set; }
        // Goal widgets only. Lets the edit form preload it without a second fetch.
        public string? GoalDirection { get; set; }
        // Goal widgets only: the target the value is shown as progress toward.
        public string? GoalTarget { get; set; }
        public List<DashboardItemSourceDto> Sources { get; set; } = [];
    }
}
