namespace Operum.Model.DTOs.Analytics
{
    public class CalendarPointDto
    {
        public string? EntryId { get; set; }
        public DateTime? Date { get; set; }
        public string? Name { get; set; }

        // Null on a single-tracker calendar.
        public string? TrackerName { get; set; }
        public string? Color { get; set; }
    }
}
