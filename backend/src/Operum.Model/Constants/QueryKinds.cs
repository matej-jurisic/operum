namespace Operum.Model.Constants
{
    // A Query is a single filter or sort clause. Columns are deliberately not a query kind;
    // views own their columns directly (see ViewColumn).
    public static class QueryKinds
    {
        public const string Filter = "filter";
        public const string Sort = "sort";

        public static readonly HashSet<string> All = [Filter, Sort];

        public static bool IsValid(string kind) => All.Contains(kind);
    }
}
