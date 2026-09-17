namespace Operum.Model.Constants
{
    public static class DataLimits
    {
        public const int MaxFieldCount = 60;
        // FieldValues is EAV; a tracker at this ceiling with a full field set is ~1.5M rows.
        // Raising this without checking the composite indexes in OperumContext.OnModelCreating
        // (view filters compile to a correlated EXISTS, see ViewQueryBuilder) is not safe.
        public const int MaxEntryCount = 25000;
        public const int MaxTrackerCount = 30;
        public const int MaxIntegrationCount = 10;
        public const int MaxIntegrationTargetCount = 10;
        // Shared across the Widget Library (charts + Entries widgets); replaces the old
        // per-tracker MaxAnalyticCount now that a widget can serve many dashboards.
        public const int MaxWidgetCount = 100;
        public const int MaxConstantCount = 25;
        public const int MaxConstantValueCount = 6;
        public const int MaxViewCount = 25;
        // Counts distinct clauses across every view and dashboard view (see QueryPool);
        // clauses are value-deduplicated.
        public const int MaxQueryCount = 200;
        public const int MaxDashboardViewCount = 15;
        public const int MaxSorts = 3;
        public const int MaxFilters = 6;
        public const int MaxColumns = MaxFieldCount;
        public const int MaxQueriesPerView = MaxFilters + MaxSorts;
        public const int MaxDashboardCount = 20;
        public const int MaxDashboardItemCount = 30;
        public const int MaxDashboardItemSourceCount = 5;
        public const int MaxHeaderTextLength = 100;
        public const int MaxNoteTextLength = 500;
        public const int MaxDashboardTabCount = 8;
        public const int MaxTabNameLength = 40;
    }
}
