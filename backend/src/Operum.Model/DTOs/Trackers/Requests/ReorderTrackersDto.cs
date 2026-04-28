namespace Operum.Model.DTOs.Trackers.Requests
{
    public class ReorderTrackersDto
    {
        public required List<string> TrackerIds { get; set; } = [];
        public required string Filter { get; set; }
    }
}
