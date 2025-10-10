using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics
{
    public class SingleValueAnalyticDto : AnalyticDto
    {
        public string Value { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? EntryId { get; set; }

        public SingleValueAnalyticDto()
        {
            ResultType = AnalyticTypes.SingleValue;
        }
    }
}
