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
        public const int MinWidth = 1;
        public const int MinHeight = 1;
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

        // A quick-add button is a single row of chrome, not a chart — it only ever needs
        // room for an icon, a name and a button.
        public static readonly (int Width, int Height) QuickAddSize = (3, 3);

        // A view selector is just a dropdown under a label — the same footprint as a
        // quick-add button.
        public static readonly (int Width, int Height) ViewSize = (3, 3);

        // An entries table wants the same room a chart does: enough width for a few
        // columns and enough height to show more than a couple of rows.
        public static readonly (int Width, int Height) EntriesSize = (6, 6);

        // A header is one short line of text, but it reads as dividing the board into
        // sections only if it spans the row it sits in rather than sitting in a column of
        // its own.
        public static readonly (int Width, int Height) HeaderSize = (Columns, 2);

        // A divider needs even less height than a header — just enough for a bare line to
        // read as a deliberate gap rather than unfinished layout — but the same full width.
        public static readonly (int Width, int Height) DividerSize = (Columns, 1);

        // A note is read a paragraph at a time, so it gets a card-sized footprint rather
        // than a full row.
        public static readonly (int Width, int Height) NoteSize = (4, 4);
    }
}
