namespace Operum.Model.Enums
{
    // How an Analytic/Entries widget is drawn on one of the board's two grids. Set
    // independently for the wide grid and the narrow one a phone renders, so a chart can be
    // shown in full on desktop and collapsed (or dropped) on mobile.
    public enum DashboardItemDisplayMode
    {
        // Drawn inline at the size the widget was given on the grid.
        Full = 0,

        // Drawn as a small button that opens the widget at full size in a modal.
        Expandable = 1,

        // Not drawn on this grid at all. The widget keeps its placement for when it is
        // shown again, and is reachable from the board's hidden-widgets list.
        Hidden = 2,
    }
}
