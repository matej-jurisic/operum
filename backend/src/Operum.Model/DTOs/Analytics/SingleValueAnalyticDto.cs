using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class SingleValueAnalyticDto : AnalyticDto
    {
        public string Value { get; set; } = string.Empty;
        public string? EntryId { get; set; }
        public FieldDto ValueField { get; set; } = null!;

        public SingleValueAnalyticDto()
        {
            ResultType = AnalyticTypes.SingleValue;
        }
    }
}
