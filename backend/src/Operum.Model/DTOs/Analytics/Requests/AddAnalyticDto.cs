namespace Operum.Model.DTOs.Analytics.Requests
{
    public class AddAnalyticFieldDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class AddAnalyticDto
    {
        public string Code { get; set; } = string.Empty;
        public string ResultType { get; set; } = string.Empty;
        public List<AddAnalyticFieldDto> AnalyticFields { get; set; } = [];
    }
}
