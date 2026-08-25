namespace Operum.Model.Constants
{
    // A Query is a single clause: either one filter or one sort. Views combine several of
    // them, which is why a query never carries a list of either.
    public static class QueryKinds
    {
        public const string Filter = "filter";
        public const string Sort = "sort";

        public static readonly HashSet<string> All = [Filter, Sort];

        public static bool IsValid(string kind) => All.Contains(kind);
    }
}
