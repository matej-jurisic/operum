namespace Operum.Model.Constants
{
    public static class DashboardWidgetTypes
    {
        public const string Analytic = "analytic";

        public const string QuickAdd = "quickAdd";

        // Filtered only by whichever filter widgets it's linked to.
        public const string Entries = "entries";

        // Two facets: typed filter clauses (Slots/ValueBySlot/Links) and DashboardView
        // presets (PresetIds/SelectedPresetId/PresetLinks) that apply a view's whole clause
        // set to followers.
        public const string Filter = "filter";

        public const string Header = "header";

        public const string Divider = "divider";

        // Shares its Config shape (TextWidgetConfigDto) with Header.
        public const string Note = "note";

        // Holds a sub-grid of widgets; children reference it via DashboardItem.ParentItemId.
        // Cannot be nested inside another Container.
        public const string Container = "container";

        // Container with named tabs; children also set DashboardItem.ParentTabId, and only
        // the active tab's children render.
        public const string TabsContainer = "tabsContainer";

        public static readonly HashSet<string> All =
            [Analytic, QuickAdd, Entries, Filter, Header, Divider, Note, Container, TabsContainer];

        public static bool IsValid(string type) => All.Contains(type);

        public static bool IsContainer(string type) => type is Container or TabsContainer;
    }
}
