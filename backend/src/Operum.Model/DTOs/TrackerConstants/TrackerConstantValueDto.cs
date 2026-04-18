namespace Operum.Model.DTOs.TrackerConstants
{
    public class TrackerConstantValueFilterDto
    {
        public string Id { get; set; } = string.Empty;
        public string FieldId { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class TrackerConstantValueDto
    {
        public string Id { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Value { get; set; } = string.Empty;
        public List<TrackerConstantValueFilterDto> Filters { get; set; } = [];
    }
}
