namespace Operum.Model.DTOs.Analytics
{
    public class AnalyticDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int AnalyticTypeId { get; set; }
        public string ResultType { get; set; } = string.Empty;
        public string AnalyticTypeName { get; set; } = string.Empty;
        public List<AnalyticRequiredDataTypeDto> AnalyticRequiredDataTypes { get; set; } = [];
    }
}
