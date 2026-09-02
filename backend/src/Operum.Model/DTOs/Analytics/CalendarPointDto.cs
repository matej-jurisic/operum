namespace Operum.Model.DTOs.Analytics
{
    public class CalendarPointDto
    {
        public string? EntryId { get; set; }
        public DateTime? Date { get; set; }
        public string? Name { get; set; }

        // Set only when the calendar merges more than one tracker, so the card can colour
        // each event by its source. Null on a single-tracker calendar.
        public string? TrackerName { get; set; }
        public string? Color { get; set; }
    }
}
