using Operum.Model.Enums;

namespace Operum.Model.Constants
{
    // DashboardItemDisplayMode spelled out, for the hand-edited board document only. Every
    // other payload carries the enum's number.
    public static class DashboardDocumentDisplayModes
    {
        public const string Full = "full";
        public const string Expandable = "expandable";
        public const string Hidden = "hidden";

        public static readonly IReadOnlyList<string> All = [Full, Expandable, Hidden];

        public static string From(DashboardItemDisplayMode mode) => mode switch
        {
            DashboardItemDisplayMode.Expandable => Expandable,
            DashboardItemDisplayMode.Hidden => Hidden,
            _ => Full
        };

        public static bool TryParse(string? name, out DashboardItemDisplayMode mode)
        {
            switch (name?.Trim().ToLowerInvariant())
            {
                case Full:
                    mode = DashboardItemDisplayMode.Full;
                    return true;
                case Expandable:
                    mode = DashboardItemDisplayMode.Expandable;
                    return true;
                case Hidden:
                    mode = DashboardItemDisplayMode.Hidden;
                    return true;
                default:
                    mode = DashboardItemDisplayMode.Full;
                    return false;
            }
        }
    }
}
