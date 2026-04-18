namespace Operum.Model.DTOs.TrackerConstants.Requests
{
    public class CreateTrackerConstantValueFilterDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class CreateTrackerConstantValueDto
    {
        public int Priority { get; set; }
        public string Value { get; set; } = string.Empty;
        public List<CreateTrackerConstantValueFilterDto> Filters { get; set; } = [];
    }
}
