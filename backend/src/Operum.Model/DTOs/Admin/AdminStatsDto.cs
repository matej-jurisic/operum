namespace Operum.Model.DTOs.Admin
{
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalTrackers { get; set; }
        public int TotalEntries { get; set; }
        public int EntriesLast30Days { get; set; }
    }
}
