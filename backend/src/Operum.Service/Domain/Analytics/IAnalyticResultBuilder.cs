using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics
{
    public interface IAnalyticResultBuilder
    {
        string SupportedType { get; }
        Result<AnalyticDto> Build(AnalyticResultBuilderRequest request);
    }
}
