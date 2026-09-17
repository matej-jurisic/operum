namespace Operum.Model.Constants
{
    // A board is arranged separately for desktop and mobile; each placement is only
    // meaningful with the column count (DashboardGrid) it was made in.
    public static class DashboardLayoutVariants
    {
        public const string Desktop = "desktop";
        public const string Mobile = "mobile";

        public static readonly HashSet<string> All = [Desktop, Mobile];

        public static bool IsValid(string variant) => All.Contains(variant);
    }
}
