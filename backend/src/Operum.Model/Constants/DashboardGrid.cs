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
        // The wide grid is 24 columns and its row is 2px on the client. A row is dwarfed by
        // the 16px margin baked into every widget's height, so the vertical step a drag or
        // resize snaps to is really row + margin: 18px now, down from 36px. It was 12
        // columns / 40px rows before IncreaseDashboardGridResolution and 24 / 20px before
        // HalveDashboardGridRowHeight; each migration doubled every stored H/Y (and
        // MobileH/MobileY) so no board moved when the step halved.
        public const int Columns = 24;
        public const int MobileColumns = 4;
        public const int MinWidth = 2;
        // Two rows -- a 20px sliver, the same floor it was at one 20px row. A divider or
        // header is a layout accent rather than content, so the grid lets a widget be
        // squeezed down that far; the client caps the arrange-mode controls that would
        // otherwise overflow a cell that short.
        public const int MinHeight = 2;
        public const int MaxHeight = 160;

        // How wide the grid is for the arrangement being saved. Unknown variants fall back
        // to the wide grid, which is the one every board is arranged on first.
        public static int ColumnsFor(string variant) =>
            variant == DashboardLayoutVariants.Mobile ? MobileColumns : Columns;

        // The narrow grid is only four columns wide, so the minimum a widget can be squeezed
        // to there is half the screen rather than a sixth of it.
        public static int MinWidthFor(string variant) =>
            Math.Min(MinWidth, ColumnsFor(variant));

        // What a widget is worth on the grid before the user has resized it. The user
        // moves it from here, so these aim for the smallest footprint each kind still
        // reads well at rather than a generous one.
        //
        // Heights are in the 2px-row grid, where the vertical step a drag snaps to is
        // row + margin == 18px: a cell of height h is 18h - 16 pixels tall on the client.
        // The client's card thresholds (see cardSizing.ts) are the reference points --
        // a chart wants ~300px of plot under its ~48px header, and drops its axes below
        // 300px wide / 200px tall.
        public static (int Width, int Height) DefaultSizeFor(string resultType) => resultType switch
        {
            // A single number reads as well small as large; header + a line of value.
            AnalyticTypes.SingleValue => (6, 8),
            // A donut reads best near-square -- ~450px wide, ~290px of it plot + legend.
            AnalyticTypes.Donut => (8, 18),
            // A month grid can't reflow: it's ~280x250px whatever cell it sits in, so a
            // wider or taller default just frames it in empty space.
            AnalyticTypes.Calendar => (8, 18),
            // Line/Bar/Scatter/Composed: half the wide grid, ~290px of plot under the header.
            _ => (12, 20)
        };

        // A quick-add button is a single row of chrome, not a chart -- an icon, a name and
        // a button is all it ever draws.
        public static readonly (int Width, int Height) QuickAddSize = (6, 7);

        // A filter widget shows as a compact chip: a header and a one-line summary of the
        // values currently set, opening a modal to edit them (presets included). It needs
        // no more room than a quick-add button.
        public static readonly (int Width, int Height) FilterSize = (6, 7);

        // An entries table wants the same room a chart does: enough width for a few
        // columns and enough height to show a dozen or so rows.
        public static readonly (int Width, int Height) EntriesSize = (12, 24);

        // A header is one short line of text spanning its row -- that span is what makes it
        // read as a section break rather than a stray label, so it takes the full width but
        // only enough height for the line and the arrange-mode icons.
        public static readonly (int Width, int Height) HeaderSize = (Columns, 5);

        // A divider is a bare rule: full width, and as short as it can be while still
        // reading as a deliberate gap rather than unfinished layout.
        public static readonly (int Width, int Height) DividerSize = (Columns, 3);

        // A note is read a paragraph at a time, so it gets a card-sized footprint rather
        // than a full row -- room for roughly eight lines before it scrolls.
        public static readonly (int Width, int Height) NoteSize = (8, 12);
    }
}
