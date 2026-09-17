using Operum.Model.Constants.Analytics;

namespace Operum.Model.Constants
{
    // Two grids: the wide one a desktop renders, the narrow one a phone renders (see
    // DashboardLayoutVariants). Client-sent placements are clamped to these bounds before save.
    public static class DashboardGrid
    {
        // Row is 2px on the client; drag/resize snaps to row + 16px margin = 18px per unit.
        public const int Columns = 24;
        public const int MobileColumns = 4;
        public const int MinWidth = 2;
        public const int MinHeight = 2;
        public const int MaxHeight = 160;

        public static int ColumnsFor(string variant) =>
            variant == DashboardLayoutVariants.Mobile ? MobileColumns : Columns;

        public static int MinWidthFor(string variant) =>
            Math.Min(MinWidth, ColumnsFor(variant));

        // Cell height h renders as 18h - 16 pixels on the client (see cardSizing.ts).
        public static (int Width, int Height) DefaultSizeFor(string resultType) => resultType switch
        {
            AnalyticTypes.SingleValue => (6, 8),
            AnalyticTypes.Goal => (7, 11),
            AnalyticTypes.Donut => (8, 18),
            AnalyticTypes.Calendar => (8, 18),
            _ => (12, 20)
        };

        public static readonly (int Width, int Height) QuickAddSize = (6, 7);

        public static readonly (int Width, int Height) FilterSize = (6, 7);

        public static readonly (int Width, int Height) EntriesSize = (12, 24);

        public static readonly (int Width, int Height) HeaderSize = (Columns, 5);

        public static readonly (int Width, int Height) DividerSize = (Columns, 3);

        public static readonly (int Width, int Height) NoteSize = (8, 12);

        // A nested widget's placement uses the same Columns as a top-level one.
        public static readonly (int Width, int Height) ContainerSize = (Columns, 30);

        public static readonly (int Width, int Height) TabsContainerSize = (Columns, 34);
    }
}
