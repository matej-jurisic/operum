namespace Operum.Model.DTOs.Analytics.Requests
{
    public class CreateAnalyticDto
    {
        public string Code { get; set; } = string.Empty;
        public string ResultType { get; set; } = string.Empty;
        public List<CreateAnalyticFieldDto> AnalyticFields { get; set; } = [];
    }
}
