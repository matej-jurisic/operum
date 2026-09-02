namespace Operum.Model.Constants
{
    public static class DataLimits
    {
        // Raised from 25 for integrations: a provider's source catalog is wider than a
        // hand-built tracker ever was (intervals.icu wellness alone is ~45 keys), so a
        // user mapping a useful subset of one needs room the old cap didn't leave.
        public const int MaxFieldCount = 60;
        // Raised from 1000 for integrations. A tracker fed by a sync loop is a data sink,
        // not a hand-kept log: daily wellness saturated the old cap in under three years
        // and a transaction feed did it inside one.
        //
        // The cost of this is paid in FieldValues, which is EAV -- a tracker at this
        // ceiling with a full field set is ~1.5M rows, and every view filter compiles to a
        // correlated EXISTS over it (see ViewQueryBuilder). The composite indexes in
        // OperumContext.OnModelCreating are what keep that affordable; raising this
        // further without measuring those queries first is not safe.
        public const int MaxEntryCount = 25000;
        public const int MaxTrackerCount = 20;
        // Connections a user may hold at once, and trackers one connection may feed. Both are
        // generous for the real use -- a person has one intervals.icu account and points it at
        // a tracker or two -- and exist so a scripted client cannot open them without bound.
        public const int MaxIntegrationCount = 10;
        public const int MaxIntegrationTargetCount = 10;
        // Per-owner cap across the Widget Library (charts + Entries widgets combined). A
        // widget can now serve many dashboards at once, unlike the old per-tracker
        // MaxAnalyticCount this replaces -- see Widgets/WidgetsService.
        public const int MaxWidgetCount = 30;
        public const int MaxConstantCount = 25;
        public const int MaxConstantValueCount = 6;
        public const int MaxViewCount = 25;
        // Per-user cap on the field-agnostic clause pool (see QueryPool). Clauses are
        // value-deduplicated, so this counts distinct clauses across every view and
        // dashboard view the user has authored.
        public const int MaxQueryCount = 200;
        // Named clause sets a single dashboard can offer through its view selectors.
        public const int MaxDashboardViewCount = 15;
        public const int MaxSorts = 3;
        public const int MaxFilters = 6;
        // A column names one field, so a view showing every field shows this many.
        public const int MaxColumns = MaxFieldCount;
        // A query is a single clause, so a view can hold as many of them as it can hold
        // filters and sorts put together. Columns are not queries and are counted apart.
        public const int MaxQueriesPerView = MaxFilters + MaxSorts;
        public const int MaxDashboardCount = 20;
        public const int MaxDashboardItemCount = 30;
        public const int MaxDashboardItemSourceCount = 5;
        // A header reads as a section title, so it stays short — the same length a
        // tracker or field name is allowed.
        public const int MaxHeaderTextLength = 100;
        // A note is read as a paragraph, so it's given the same room a description gets.
        public const int MaxNoteTextLength = 500;
    }
}
