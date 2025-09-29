namespace Operum.Model.DTOs.Trackers.Requests
{
    public class AddTrackerAnalyticFieldDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string AnalyticRequiredDataTypeId { get; set; } = string.Empty;
    }

    public class AddTrackerAnalyticDto
    {
        public string AnalyticId { get; set; } = string.Empty;
        public List<AddTrackerAnalyticFieldDto> TrackerAnalyticFieldList { get; set; } = [];
    }
}
