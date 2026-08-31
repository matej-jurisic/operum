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
        // The wide grid is 24 columns and its row is 20px on the client, so a widget can be
        // placed and sized to half the step it used to snap to on either axis. It was 12
        // columns / 40px rows before the IncreaseDashboardGridResolution migration, which
        // doubled every stored X/W/H (and MobileH) so no board moved when the step halved.
        public const int Columns = 24;
        public const int MobileColumns = 4;
        public const int MinWidth = 2;
        // One row. A divider or header is a layout accent rather than content, so the grid
        // lets a widget be squeezed down to a 20px sliver; the client caps the arrange-mode
        // controls that would otherwise overflow a cell that short.
        public const int MinHeight = 1;
        public const int MaxHeight = 80;

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
        //
        // Sizes below are in the 24-column / 20px-row grid: every one is twice what it was
        // on the old 12-column / 40px-row grid, so a widget added now lands at the same
        // footprint it always did.
        public static (int Width, int Height) DefaultSizeFor(string resultType) => resultType switch
        {
            AnalyticTypes.SingleValue => (6, 4),
            AnalyticTypes.Donut => (8, 12),
            AnalyticTypes.Calendar => (12, 16),
            _ => (12, 12)
        };

        // A quick-add button is a single row of chrome, not a chart — it only ever needs
        // room for an icon, a name and a button.
        public static readonly (int Width, int Height) QuickAddSize = (6, 6);

        // A view selector is just a dropdown under a label — the same footprint as a
        // quick-add button.
        public static readonly (int Width, int Height) ViewSize = (6, 6);

        // An entries table wants the same room a chart does: enough width for a few
        // columns and enough height to show more than a couple of rows.
        public static readonly (int Width, int Height) EntriesSize = (12, 12);

        // A header is one short line of text, but it reads as dividing the board into
        // sections only if it spans the row it sits in rather than sitting in a column of
        // its own.
        public static readonly (int Width, int Height) HeaderSize = (Columns, 4);

        // A divider needs even less height than a header — just enough for a bare line to
        // read as a deliberate gap rather than unfinished layout — but the same full width.
        public static readonly (int Width, int Height) DividerSize = (Columns, 2);

        // A note is read a paragraph at a time, so it gets a card-sized footprint rather
        // than a full row.
        public static readonly (int Width, int Height) NoteSize = (8, 8);
    }
}
