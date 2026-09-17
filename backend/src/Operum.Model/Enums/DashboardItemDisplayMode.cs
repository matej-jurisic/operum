namespace Operum.Model.Enums
{
    // Set independently for the wide and narrow grid.
    public enum DashboardItemDisplayMode
    {
        Full = 0,

        // A small button that opens the widget at full size in a modal.
        Expandable = 1,

        // Not drawn; reachable from the board's hidden-widgets list.
        Hidden = 2,
    }
}
