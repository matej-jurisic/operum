using Operum.Model.Constants.Analytics;

namespace Operum.Model.Constants
{
    // The grid a dashboard's widgets are placed on. The client lays out the same number of
    // columns, so a stored X/W means the same thing on both sides; anything the client
    // sends is clamped to these bounds before it is saved.
    public static class DashboardGrid
    {
        public const int Columns = 12;
        public const int MinWidth = 2;
        public const int MinHeight = 2;
        public const int MaxHeight = 40;

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
