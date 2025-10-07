using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Services.Analytics
{
    public class AnalyticsService : IAnalyticsService
    {
        public Result<AnalyticConfigDto> GetAnalyticConfig()
        {
            var config = new AnalyticConfigDto
            {
                ResultTypes = [.. AnalyticDefinitions.ByResultType.Select(rt => new ResultTypeDto
                {
                    Name = rt.Key,
                    Codes = [.. rt.Value.Codes.Select(code => new CodeDto
                    {
                        Name = code.Key,
                        Purposes = [.. code.Value.AllowedDataTypes
                            .Select(p => new PurposeDto
                            {
                                Name = p.Key,
                                AllowedDataTypes = [.. p.Value]
                            })]
                    })]
                })]
            };

            return Result.Success(config);
        }
    }
}
