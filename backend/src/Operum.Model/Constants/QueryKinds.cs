namespace Operum.Model.Constants
{
    // A Query is a single clause: one filter or one sort. Views combine several of them,
    // which is why a query never carries a list of either.
    //
    // Which columns a view shows is deliberately NOT a query: a column names a field and
    // nothing else, so there would be no clause to author and nothing to reuse. Views own
    // their columns directly (see ViewColumn).
    public static class QueryKinds
    {
        public const string Filter = "filter";
        public const string Sort = "sort";

        public static readonly HashSet<string> All = [Filter, Sort];

        public static bool IsValid(string kind) => All.Contains(kind);
    }
}
