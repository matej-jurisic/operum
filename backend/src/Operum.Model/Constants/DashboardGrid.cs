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
            AnalyticTypes.Goal => (6, 7),
            AnalyticTypes.Donut => (5, 20),
            AnalyticTypes.Calendar => (5, 20),
            _ => (8, 16)
        };

        public static readonly (int Width, int Height) QuickAddSize = (3, 6);

        public static readonly (int Width, int Height) FilterSize = (7, 3);

        public static readonly (int Width, int Height) EntriesSize = (12, 24);

        public static readonly (int Width, int Height) HeaderSize = (Columns, 3);

        public static readonly (int Width, int Height) DividerSize = (Columns, 3);

        public static readonly (int Width, int Height) NoteSize = (8, 12);

        public static readonly (int Width, int Height) ContainerSize = (10, 24);

        public static readonly (int Width, int Height) TabsContainerSize = (10, 24);
    }
}
