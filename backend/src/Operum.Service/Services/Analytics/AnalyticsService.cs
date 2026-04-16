using Operum.Model.Common;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.DTOs.Analytics;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Analytics
{
    public class AnalyticsService : IAnalyticsService
    {
        public Result<AnalyticConfigDto> GetAnalyticConfig()
        {
            var config = new AnalyticConfigDto
            {
                ResultTypes = [.. AnalyticDefinitionList.ByResultType.Select(rt => new AnalyticConfigType
                {
                    Name = rt.Key,
                    Codes = [.. rt.Value.Codes.Select(code => new AnalyticConfigCode
                    {
                        Code = code.Key,
                        Name = string.IsNullOrEmpty(code.Value.Label) ? code.Key : code.Value.Label,
                        Purposes = [.. code.Value.AllowedDataTypes
                            .Select(p => new AnalyticConfigPurpose
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
