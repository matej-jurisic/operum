using Operum.Model.Constants.Analytics;

namespace Operum.Model.Constants
{
    // The grids a dashboard's widgets are placed on. The client lays out the same number of
    // columns, so a stored X/W means the same thing on both sides; anything the client
    // sends is clamped to these bounds before it is saved.
    //
    // There are two: the wide grid a desktop renders, and the narrow one a phone renders.
    // Each item carries a placement in both (see DashboardLayoutVariants).
    public static class DashboardGrid
    {
        public const int Columns = 12;
        public const int MobileColumns = 4;
        public const int MinWidth = 2;
        public const int MinHeight = 2;
        public const int MaxHeight = 40;

        // How wide the grid is for the arrangement being saved. Unknown variants fall back
        // to the wide grid, which is the one every board is arranged on first.
        public static int ColumnsFor(string variant) =>
            variant == DashboardLayoutVariants.Mobile ? MobileColumns : Columns;

        // The narrow grid is only four columns wide, so the minimum a widget can be squeezed
        // to there is half the screen rather than a sixth of it.
        public static int MinWidthFor(string variant) =>
            Math.Min(MinWidth, ColumnsFor(variant));

        // What a widget is worth on the grid before the user has resized it. Charts need
        // room for their axes, a single value only needs a line of text.
        public static (int Width, int Height) DefaultSizeFor(string resultType) => resultType switch
        {
            AnalyticTypes.SingleValue => (3, 2),
            AnalyticTypes.Donut => (4, 6),
            AnalyticTypes.Calendar => (6, 8),
            _ => (6, 6)
        };
    }
}
