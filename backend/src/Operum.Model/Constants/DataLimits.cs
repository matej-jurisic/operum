namespace Operum.Model.Constants
{
    public static class DataLimits
    {
        public const int MaxFieldCount = 25;
        public const int MaxEntryCount = 1000;
        public const int MaxTrackerCount = 20;
        public const int MaxAnalyticCount = 10;
        public const int MaxConstantCount = 25;
        public const int MaxConstantValueCount = 6;
        public const int MaxViewCount = 25;
        public const int MaxQueryCount = 50;
        public const int MaxSorts = 3;
        public const int MaxFilters = 6;
        // A query is a single clause, so a view can hold as many of them as it can hold
        // filters and sorts put together.
        public const int MaxQueriesPerView = MaxFilters + MaxSorts;
        public const int MaxDashboardCount = 20;
        public const int MaxDashboardItemCount = 10;
        public const int MaxDashboardItemSourceCount = 5;
    }
}
