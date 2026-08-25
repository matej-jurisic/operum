namespace Operum.Model.Constants
{
    // Which of a board's two grids an arrangement belongs to. A placement is meaningless
    // without the column count it was made in, and a phone renders far fewer columns than a
    // desktop, so a board is arranged twice and each arrangement is stored separately.
    // Without that split, dragging a widget on a phone would write a folded-down placement
    // over the desktop board.
    public static class DashboardLayoutVariants
    {
        public const string Desktop = "desktop";
        public const string Mobile = "mobile";

        public static readonly HashSet<string> All = [Desktop, Mobile];

        public static bool IsValid(string variant) => All.Contains(variant);
    }
}
