namespace Operum.Model.DTOs.Analytics
{
    public class AnalyticDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<AnalyticRequiredDataTypeDto> AnalyticRequiredDataTypes { get; set; } = [];
    }
}
